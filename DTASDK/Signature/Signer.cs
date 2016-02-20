/*
 * Copyright 2010-2015 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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

namespace Amazon.DTASDK.V2.Signature
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Security.Cryptography;

    /// <summary>
    /// The class which verifies and signs authentication requests.
    /// </summary>
    public class Signer
    {
        private const string AlgorithmHeader = "DTA1-HMAC-SHA256";
        private const string XAmzDateHeader = "x-amz-date";
        private const string AuthorizationHeader = "Authorization";
        // Specified by the AWSAuthv4 to be a +/- 15 minute window, expressed in milliseconds, that we allow. We *can* tighten this up.
        private const int TimeToleranceInMilliseconds = 15 * 1000 * 60;
        private const string HmacAlgorithm = "HmacSHA256";
        /** The default encoding to use when URL encoding */
        private const string DateTimeFormat = "yyyyMMdd'T'HHmmss'Z'";
        private const string DateStampFormat = "yyyyMMdd";
        private const string EmptyScope = "";
        private const string SignedHeadersString = "SignedHeaders=";
        private const string CredentialsString = "Credential=";
        private const string SignatureString = "Signature=";

        private AuthenticationHeaderParser AuthenticationHeaderParser { get; }
        private Clock Clock { get; }

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
            AuthenticationHeaderParser = authenticationHeaderParser;
            Clock = clock;
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
        public bool Verify(Request request, Credential credential)
        {
            CredentialStore store = new CredentialStore();
            store.Add(credential);
            return Verify(request, store);
        }

        /// <summary>
        /// Verifies the request against a credential store.
        /// </summary>
        /// <param name="request">
        /// the request to verify.
        /// </param>
        /// <param name="credentialStore">
        /// the credential store used to verify the request.
        /// </param>
        /// <returns>
        /// true if the request validates. false for any other reason except a SigningException.
        /// </returns>
        public bool Verify(Request request, CredentialStore credentialStore)
        {
            DateTime now = Clock.Now();
            string strDate = now.ToString(DateStampFormat);
            string dateTime = request.GetHeader(XAmzDateHeader);

            if (dateTime == null)
            {
                return false;
            }

            // Fail if the Authentication header is not found
            string actualAuthorization = request.GetHeader(AuthorizationHeader);
            if (string.IsNullOrEmpty(actualAuthorization))
            {
                return false;
            }

            request.RemoveHeader(AuthorizationHeader);

            // Clear any header that isn't in the list of signed signedHeaders
            AuthenticationHeader authenticationHeader = AuthenticationHeaderParser.Parse(actualAuthorization);

            if (authenticationHeader == null)
            {
                return false;
            }

            string[] signedHeaders = authenticationHeader.SignedHeaders.Split(';');

            RemoveUnsignedHeaders(request, signedHeaders);

            DateTime dateOfRequest;
            if (!DateTime.TryParseExact(dateTime, DateTimeFormat, null, DateTimeStyles.None, out dateOfRequest))
            {
                return false;
            }

            TimeSpan timeSpan = now - dateOfRequest;
            if (Math.Abs(timeSpan.TotalMilliseconds) > TimeToleranceInMilliseconds)
            {
                return false;
            }

            // The credential info should follow this pattern: KEYID/DATE
            string[] credentialInfo = authenticationHeader.Credential.Split('/');

            if (credentialInfo.GetLength(0) < 2)
            {
                return false;
            }

            Credential credential;
            try
            {
                credential = credentialStore.Get(credentialInfo[0]);
            }
            catch (CredentialNotFoundException)
            {
                return false;
            }

            byte[] timedKey = Sign(strDate, credential.SecretKey);
            string canonicalRequest = GetCanonicalRequest(request);
            string stringToSign = GetStringToSign(AlgorithmHeader, dateTime, EmptyScope, canonicalRequest);
            string signature = BitConverter.ToString(Sign(stringToSign, timedKey)).Replace("-", "").ToLower();

            string computedAuthorization = GetAuthorizatioinHeader(request, credential, strDate, signature);
            return computedAuthorization.Equals(actualAuthorization);
        }

        /// <summary>
        /// Trims out any headers that were not signed from the request to allow easier calculation of the signature for
        /// verification.
        /// </summary>
        /// <param name="request">
        /// the request to remove headers from.
        /// </param>
        /// <param name="signedHeaders">
        /// the headers that should remain.
        /// </param>
        private static void RemoveUnsignedHeaders(Request request, string[] signedHeaders)
        {
            var unsignedHeaders = request.GetHeaderNames()
                .Where(header => !signedHeaders.Contains(header, StringComparer.OrdinalIgnoreCase)).ToList();
            foreach (string header in unsignedHeaders)
            {
                request.RemoveHeader(header);
            }
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
        public void Sign(Request request, Credential credential)
        {
            DateTime now = Clock.Now();
            string strDate = now.ToString(DateStampFormat);
            string dateTime = now.ToString(DateTimeFormat);

            request.SetHeader(XAmzDateHeader, dateTime);

            byte[] timedKey = Sign(strDate, credential.SecretKey);

            // Remove the Authorization header from the request, since it could have been set if sign() was previously
            // called on this request.
            request.RemoveHeader(AuthorizationHeader);

            string canonicalRequest = GetCanonicalRequest(request);
            string stringToSign = GetStringToSign(AlgorithmHeader, dateTime, EmptyScope, canonicalRequest);
            string signature = BitConverter.ToString(Sign(stringToSign, timedKey)).Replace("-", "").ToLower();
            request.SetHeader("Authorization", GetAuthorizatioinHeader(request, credential, strDate, signature));
        }

        private static string GetAuthorizatioinHeader(Request request, Credential credential, string strDate, string signature) =>
            $"{AlgorithmHeader} {SignedHeadersString}{GetSignedHeadersString(request)}, " +
            $"{CredentialsString}{credential.PublicKey}/{strDate}, {SignatureString}{signature}";

        /// <summary>
        /// Calculates and returns the content hash of the request. Per AwsAuthV4, we don't sign the body, we sign the hash
        /// of the body. This allows validation to be offloaded to other servers more easily.
        /// </summary>
        /// <param name="request">
        /// the request to get the content from
        /// </param>
        /// <returns>
        /// a String representing the hexidecimal hash of the body
        /// </returns>
        private static string GetContentHash(Request request) =>
            BitConverter.ToString(Hash(request.Body ?? "")).Replace("-", "").ToLower();

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
            try
            {
                SHA256 sha = SHA256.Create();
                return sha.ComputeHash(Encoding.Default.GetBytes(text));
            }
            catch (ArgumentNullException e)
            {
                throw new SigningException(e.Message, e);
            }
            catch (ObjectDisposedException e)
            {
                throw new SigningException(e.Message, e);
            }
        }

        /// <summary>
        /// Returns a canonical string representation of the request headers
        /// </summary>
        /// <param name="request">
        /// to get the headers from.
        /// </param>
        /// <returns>
        /// a string of the form header_name1:header_value1 \n header_name2:header_value2
        /// </returns>
        private static string GetCanonicalizedHeaderString(Request request)
        {
            var headers = request.Headers.Select(header => $"{header.Key.Trim().ToLowerInvariant()}:{header.Value.Trim()}");
            return string.Join("\n", headers) + "\n";
        }

        /// <summary>
        /// Returns a string with all the signed headers names found in the request separated by ';'.
        /// </summary>
        /// <param name="request">
        /// to get the headers from.
        /// </param>
        /// <returns>
        /// a string of the form HEADER_NAME1;HEADER_NAME2;HEADER_NAME3 to sign
        /// </returns>
        private static string GetSignedHeadersString(Request request) =>
            string.Join(";", request.Headers.Keys.Select(s => s.ToLower()));

        /// <summary>
        /// Returns a canonical string representation of the resourcePath.
        /// </summary>
        /// <param name="resourcePath">
        /// The path from which to create a canonical encoding
        /// </param>
        /// <returns>
        /// The uri-converted path preceded by a '/', or a '/' alone if the path is empty.
        /// </returns>
        private static string GetCanonicalizedResourcePath(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return "/";
            }

            string value = Uri.EscapeUriString(resourcePath);

            if (value.StartsWith("/"))
            {
                return value;
            }

            return "/" + value;
        }

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
        /// <param name="request">
        /// the request to canonicalize.
        /// </param>
        /// <returns>
        /// the canonical request.
        /// </returns>
        private static string GetCanonicalRequest(Request request)
        {
            /* This would url-encode the resource path for the first time */
            Uri url;
            try
            {
                url = new Uri(request.Url);
            }
            catch (UriFormatException murle)
            {
                throw new SigningException(murle.Message, murle);
            }
            string canonicalRequest =
                $"{request.RequestMethod.ToString().ToUpperInvariant()}\n" +
                $"{GetCanonicalizedResourcePath(url.AbsolutePath)}\n\n" +
                $"{GetCanonicalizedHeaderString(request)}\n{GetSignedHeadersString(request)}\n{GetContentHash(request)}";
            return canonicalRequest;
        }

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
        /// the scope of the request.
        /// </param>
        /// <param name="canonicalRequest">
        /// the canonical representation of the request.
        /// </param>
        /// <returns>
        /// The string generated from all inputs.
        /// </returns>
        private static string GetStringToSign(string algorithm, string dateTime, string scope, string canonicalRequest) =>
            $"{algorithm}\n{dateTime}\n{scope}\n{BitConverter.ToString(Hash(canonicalRequest)).Replace("-", string.Empty).ToLower()}";

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
            Sign(Encoding.Default.GetBytes(stringToSign), key);

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
            Sign(Encoding.Default.GetBytes(data), Encoding.Default.GetBytes(key));

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
            try
            {
                HMAC mac = HMAC.Create(HmacAlgorithm);
                mac.Key = keyBytes;
                return mac.ComputeHash(dataBytes);
            }
            catch (CryptographicException ike)
            {
                throw new SigningException(ike.Message, ike);
            }
        }
    }
}
