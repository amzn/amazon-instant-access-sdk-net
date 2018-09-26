/*
 * Copyright 2010-2018 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;

using static System.Text.Encoding;

namespace Amazon.DTASDK.V2.Signature
{

    /// <summary>
    /// The class which verifies and signs authentication requests.
    /// </summary>
    public class Signer
    {
        private const string AlgorithmHeader = "DTA1-HMAC-SHA256";
        private const string XAmzDateHeader = "x-amz-date";
        private const string AuthorizationHeader = "Authorization";
        // Specified by the AWSAuthv4 to be a +/- 15 minute window, expressed in milliseconds,
        // that we allow. We *can* tighten this up.
        private const int TimeToleranceInMilliseconds = 15 * 1000 * 60;
        private const string HmacAlgorithm = "HmacSHA256";
        private const string ISO8601Date = "yyyyMMdd";
        private const string ISO8601DateTime = "yyyyMMddTHHmmssZ";
        private const string SignedHeadersString = "SignedHeaders=";
        private const string CredentialsString = "Credential=";
        private const string SignatureString = "Signature=";

        private readonly AuthenticationHeaderParser authenticationHeaderParser;
        private readonly Clock clock;

        /// <summary>
        /// Creates a complete Signer ready to be used.
        /// </summary>
        public Signer() : this(new AuthenticationHeaderParser(), new Clock()) { }

        /// <summary>
        /// Creates a Signer with a clock and a parser. This is used for testing purposes.
        /// </summary>
        /// <param name="authenticationHeaderParser">
        /// the header parser
        /// </param>
        /// <param name="clock">
        /// the Clock from which to retrieve the current time
        /// </param>
        public Signer(AuthenticationHeaderParser authenticationHeaderParser, Clock clock)
        {
            this.authenticationHeaderParser = authenticationHeaderParser;
            this.clock = clock;
        }

        /// <summary>
        /// Verifies the request against a credential.
        /// </summary>
        /// <param name="request">
        /// the request to verify.
        /// </param>
        /// <param name="credential">
        /// the credential used to verify the request.
        /// </param>
        /// <returns>
        /// true if the request validates. false for any other reason except a SigningException.
        /// </returns>
        public bool Verify(IDTARequest request, Credential credential)
        {
            CredentialStore store = new CredentialStore();
            store.Add(credential);
            return Verify(request, store);
        }

        /// <summary>
        /// Verifies the request against a credential store.
        /// </summary>
        /// <param name="request">
        /// The request to verify.
        /// </param>
        /// <param name="credentialStore">
        /// The credential store used to verify the request.
        /// </param>
        /// <returns>
        /// true if the request validates. false for any other reason except a SigningException.
        /// </returns>
        public bool Verify(IDTARequest request, CredentialStore credentialStore)
        {
            if (!TryGetRequestDate(request.Headers,
                    out DateTime amznAuthDateTime))
            {
                return false;
            }

            if (!request.Headers.TryGetValue(AuthorizationHeader,
                    out var requestAuthorization))
            {
                return false;
            }

            if (!authenticationHeaderParser.TryParse(requestAuthorization,
                    out var authenticationHeader))
            {
                return false;
            }

            // The credential info should follow this pattern: KEYID/DATE
            string[] credentialInfo = authenticationHeader.Credential.Split('/');
            if (credentialInfo.Length < 2)
            {
                return false;
            }

            if (!credentialStore.TryGetValue(credentialInfo[0], out var credential))
            {
                return false;
            }

            string signedHeaders = authenticationHeader.SignedHeaders;
            string[] signedHeadersList = signedHeaders.Split(';');

            if (!TryGetHeadersToSign(signedHeadersList, request.Headers, out var headersToSign))
            {
                return false;
            }

            string date = amznAuthDateTime.ToString(ISO8601Date);
            string dateTime = amznAuthDateTime.ToString(ISO8601DateTime);

            string canonicalHeaders = GetCanonicalHeaders(headersToSign);
            string canonicalRequest = GetCanonicalRequest(
                request.HttpMethod.ToUpperInvariant(),
                request.Uri.AbsolutePath,
                string.Empty,
                canonicalHeaders,
                signedHeaders,
                HexEncodedHash(request.Body ?? ""));

            string stringToSign = GetStringToSign(
                AlgorithmHeader,
                dateTime,
                string.Empty,
                HexEncodedHash(canonicalRequest));

            byte[] timedKey = Sign(date, credential.SecretKey);
            string signature = Sign(stringToSign, timedKey).HexEncode();

            string computedAuthorization = GetAuthorizationHeader(signedHeaders, credential, date, signature);
            return computedAuthorization.Equals(requestAuthorization);
        }

        private bool TryGetRequestDate(IDictionary<String, StringValues> headers, out DateTime dateOfRequest)
        {
            DateTime now = clock.Now();
            string strDate = now.ToString(ISO8601Date);

            if (!headers.TryGetValue(XAmzDateHeader, out StringValues dateTimeStr) ||
                string.IsNullOrEmpty(dateTimeStr))
            {
                dateOfRequest = default;
                return false;
            }

            if (!DateTime.TryParseExact(
                    dateTimeStr.ToString(),
                    ISO8601DateTime,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal,
                    out dateOfRequest))
            {
                return false;
            }

            TimeSpan timeSpan = now - dateOfRequest;
            return Math.Abs(timeSpan.TotalMilliseconds) <= TimeToleranceInMilliseconds;
        }

        private bool TryGetHeadersToSign(
            string[] signedHeaders,
            IDictionary<string, StringValues> headers,
            out ICollection<KeyValuePair<string, StringValues>> headersToSign)
        {
            var result = new Dictionary<string, StringValues>(
                signedHeaders.Length,
                StringComparer.InvariantCultureIgnoreCase);

            foreach (var header in signedHeaders)
            {
                if (!headers.TryGetValue(header, out var value))
                {
                    headersToSign = default;
                    return false;
                }
                result.Add(header, value);
            }
            headersToSign = result;
            return true;
        }

        /// <summary>
        /// Signs the request and adds the authentication headers (Authentication & x-amz-date).
        /// </summary>
        /// <param name="request">
        /// the request to sign.
        /// </param>
        /// <param name="credential">
        /// the credential to use when signing.
        /// </param>
        public void Sign(IDTARequest request, Credential credential)
        {
            DateTime now = clock.Now();
            string date = now.ToString(ISO8601Date);
            string dateTime = now.ToString(ISO8601DateTime);

            request.Headers[XAmzDateHeader] = dateTime;

            var headersToSign = request.Headers.OrderBy(kv => kv.Key).ToArray();

            string signedHeaders = GetSignedHeaders(headersToSign);
            string canonicalHeaders = GetCanonicalHeaders(headersToSign);

            string canonicalRequest = GetCanonicalRequest(
                request.HttpMethod.ToUpperInvariant(),
                request.Uri.AbsolutePath,
                string.Empty,
                canonicalHeaders,
                signedHeaders,
                HexEncodedHash(request.Body ?? ""));

            string authorizationString = GetStringToSign(
                AlgorithmHeader,
                dateTime,
                string.Empty,
                HexEncodedHash(canonicalRequest));

            byte[] timedKey = Sign(date, credential.SecretKey);
            string signature = Sign(authorizationString, timedKey).HexEncode();

            request.Headers[AuthorizationHeader] = GetAuthorizationHeader(signedHeaders, credential, date, signature);
        }

        private static string GetAuthorizationHeader(
            string signedHeaders,
            Credential credential,
            string date,
            string signature) =>
            $"{AlgorithmHeader} " +
            $"{SignedHeadersString}{signedHeaders}, " +
            $"{CredentialsString}{credential.PublicKey}/{date}, " +
            $"{SignatureString}{signature}";

        /// <summary>
        /// Calculates the hash and returns a hex encoded string.
        /// </summary>
        /// <param name="str">
        /// the request to get the content from
        /// </param>
        /// <returns>
        /// a String representing the hexadecimal hash of the body
        /// </returns>
        private static string HexEncodedHash(string str) => Hash(str).HexEncode();

        /// <summary>
        /// Creates a SHA256 hash of the given string
        /// </summary>
        /// <param name="text">
        /// The string to be hashed
        /// </param>
        /// <returns>
        /// The SHA256 hash of the given string
        /// </returns>
        private static byte[] Hash(string text)
        {
            using (SHA256 sha = SHA256.Create())
                return sha.ComputeHash(UTF8.GetBytes(text));
        }

        /// <summary>
        /// Returns a canonical string representation of the request headers
        /// </summary>
        /// <param name="headers">
        /// The headers to construct the canonical string from
        /// </param>
        /// <returns>
        /// a string of the form header_name1:header_value1 \n header_name2:header_value2
        /// </returns>
        private static string GetCanonicalHeaders(IEnumerable<KeyValuePair<string, StringValues>> headers) =>
            string.Concat(headers.Select(header =>
            $"{NormalizeWhiteSpace(header.Key).ToLowerInvariant()}:" +
            $"{NormalizeWhiteSpace(header.Value)}\n"));

        private static string NormalizeWhiteSpace(string str) =>
            Regex.Replace(str, @"\s+", " ");

        /// <summary>
        /// Returns a string with all the signed headers names found in the request separated by ';'.
        /// </summary>
        /// <param name="headers">
        /// The headers to be signed.
        /// </param>
        /// <returns>
        /// a string of the form HEADER_NAME1;HEADER_NAME2;HEADER_NAME3 to sign
        /// </returns>
        private static string GetSignedHeaders(IEnumerable<KeyValuePair<string, StringValues>> headers) =>
            String.Join(';', headers.Select(kv => kv.Key.ToLowerInvariant()));

        /// <summary>
        /// Returns the canonical representation of the request. The canonical request is of the form:
        ///
        /// <pre>
        /// METHOD
        /// CANONICAL_PATH
        /// CANONICAL_QUERY_STRING
        /// CANONICAL_HEADER_STRING
        /// SIGNED_HEADERS
        /// CONTENT_HASH
        /// </pre>
        ///
        /// Which for a get request to http://amazon.com/path would be:
        ///
        /// <pre>
        /// GET
        /// /path
        ///
        /// x-amz-date
        /// 20110909T112349Z
        /// CONTENT_HASH
        /// </pre>
        /// </summary>
        /// <returns>
        /// the canonical request.
        /// </returns>
        private static string GetCanonicalRequest(
            string httpRequestMethod,
            string canonicalURI,
            string canonicalQueryString,
            string canonicalHeaders,
            string signedHeaders,
            string hashEndcodedRequestPayload) =>
            $"{httpRequestMethod}\n" +
            $"{canonicalURI}\n" +
            $"{canonicalQueryString}\n" +
            $"{canonicalHeaders}\n" +
            $"{signedHeaders}\n" +
            $"{hashEndcodedRequestPayload}";

        /// <summary>
        /// Takes data parsed from the header of an authentication request and converts them into a string with which
        /// to generate a signature.
        /// </summary>
        /// <param name="algorithm">
        /// The hashing algorithm to use.
        /// </param>
        /// <param name="dateTime">
        /// The time at which the request was placed.
        /// </param>
        /// <param name="scope">
        /// The scope of the request.
        /// </param>
        /// <param name="canonicalRequestHash">
        /// The hash of canonical representation of the request.
        /// </param>
        /// <returns>
        /// The string generated from all inputs.
        /// </returns>
        private static string GetStringToSign(
            string algorithm,
            string dateTime,
            string scope,
            string canonicalRequestHash) =>
            $"{algorithm}\n" +
            $"{dateTime}\n" +
            $"{scope}\n" +
            $"{canonicalRequestHash}";

        /// <summary>
        /// Creates a hashed signature from the string and private key provided.
        /// </summary>
        /// <param name="stringToSign">
        /// The string for which to create a signature
        /// </param>
        /// <param name="key">
        /// The private key to use in hashing
        /// </param>
        /// <returns>
        /// the hashed signature
        /// </returns>
        private static byte[] Sign(string stringToSign, byte[] key) =>
            Sign(UTF8.GetBytes(stringToSign), key);

        /// <summary>
        /// Creates a hashed signature from the string and private key provided.
        /// </summary>
        /// <param name="data">
        /// The string for which to create a signature
        /// </param>
        /// <param name="key">
        /// a string representing private key to use in hashing
        /// </param>
        /// <returns>
        /// the hashed signature
        /// </returns>
        private static byte[] Sign(string data, string key) =>
            Sign(UTF8.GetBytes(data), UTF8.GetBytes(key));

        /// <summary>
        /// Creates a hashed signature from the string and private key provided.
        /// </summary>
        /// <param name="dataBytes">
        /// A byte array generated from the string to be signed.
        /// </param>
        /// <param name="keyBytes">
        /// The private key to use in hashing
        /// </param>
        /// <returns>
        /// the hashed signature
        /// </returns>
        private static byte[] Sign(byte[] dataBytes, byte[] keyBytes)
        {
            using (HMAC mac = HMAC.Create(HmacAlgorithm))
            {
                mac.Key = keyBytes;
                return mac.ComputeHash(dataBytes);
            }
        }
    }
}