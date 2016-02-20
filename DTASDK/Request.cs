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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Amazon.DTASDK.V2
{
    /// <summary>
    /// Represents an HTTP request. This is used to for both signing and verifying a request to be signed.
    /// </summary>
    public class Request
    {
        public const string ContentTypeHeader = "Content-Type";
        public const string DefaultUserAgent = "Mozilla/5.0 (compatible; Amazon Instant Access/1.0";
        public string Url { get; set; }
        public Method RequestMethod { get; set; }
        public string Body { get; set; }
        public SortedDictionary<string, string> Headers { get; }
        public string UserAgent { get; set; }

        /// <summary>
        /// Creates a Request from an HttpWebRequest. Useful for verifying the signature of a request.
        ///
        /// NOTE: This consumes the body of the request which can cause issues when you try and read it again.
        /// </summary>
        /// <param name="httpRequest">
        /// the HttpWebRequest to copy
        /// </param>
        public Request(HttpWebRequest httpRequest)
        {
            Url = httpRequest.RequestUri.OriginalString;
            RequestMethod = (Method)Enum.Parse(typeof(Method), httpRequest.Method, true);

            WebHeaderCollection headerCollection = httpRequest.Headers;
            var headers = headerCollection.AllKeys.ToDictionary(header => header, header => headerCollection[header]);
            Headers = new SortedDictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);

            StreamReader bodyReader = new StreamReader(httpRequest.GetRequestStream());
            Body = bodyReader.ReadToEnd();
            bodyReader.Close();
        }

        /// <summary>
        /// Creates a Request built from manually-entered url, method, and contentType.
        /// </summary>
        /// <param name="url">
        /// The url from which the request originiates
        /// </param>
        /// <param name="method">
        /// Whether the request is a GET or POST
        /// </param>
        /// <param name="contentType">
        /// The type of content in the body of the request
        /// </param>
        public Request(string url, Method method, string contentType)
        {
            Headers = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                throw new ArgumentException(nameof(url) + " is incorrectly formatted");
            }

            Url = url;
            RequestMethod = method;
            Headers.Add(ContentTypeHeader, contentType);
            UserAgent = DefaultUserAgent;
        }

        /// <summary>
        /// An Enum representing the type of request.
        /// </summary>
        public enum Method
        {
            Post,
            Get
        }

        /// <summary>
        /// Gets the Header associated with the given headerName
        /// </summary>
        /// <param name="headerName">
        /// The name of the Header to retrieve
        /// </param>
        /// <returns>
        /// The Header associated with the given headerName, or null if the header does not exist.
        /// </returns>
        public string GetHeader(string headerName)
        {
            string result;
            return Headers.TryGetValue(headerName, out result) ? result : null;
        }

        /// <summary>
        /// Adds the given Header to the dictionary of Headers, and associates it with the given name. If a Header is already
        /// associated with the given name, the existing Header is replaced with the new one.
        /// </summary>
        /// <param name="name">
        /// The name with which to associate the given Header
        /// </param>
        /// <param name="value">
        /// The value of the new Header to create.
        /// </param>
        public void SetHeader(string name, string value) => Headers[name] = value;

        /// <summary>
        /// Removes the given Header from the dictionary.
        /// </summary>
        /// <param name="headerName"></param>
        public void RemoveHeader(string headerName) => Headers.Remove(headerName);

        /// <summary>
        /// Returns a list of strings representing all of the Headers in the dictionary.
        /// </summary>
        /// <returns>
        /// List of strings representing all of the Headers in the dictionary
        /// </returns>
        public ICollection<string> GetHeaderNames() => Headers.Keys;
    }
}
