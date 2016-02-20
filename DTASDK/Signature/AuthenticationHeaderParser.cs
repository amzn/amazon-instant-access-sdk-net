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

using System.Text.RegularExpressions;

namespace Amazon.DTASDK.V2.Signature
{
    /// <summary>
    /// An object which parses out strings representing Headers and creates an AuthenticationHeader
    /// </summary>
    public class AuthenticationHeaderParser
    {
        private const string AuthenticationHeaderRegex = @"(\S+) SignedHeaders=(\S+), Credential=(\S+), Signature=([^(\s|,)]+)";

        /// <summary>
        /// Parses out the Header of a request, and creates a new AuthenticaitonHeader populated with the data from this header.
        /// </summary>
        /// <param name="headerString">
        /// String representing the Header of an authentication request
        /// </param>
        /// <returns>
        /// An AuthenticationHeader object populated with data from headerString
        /// </returns>
        public AuthenticationHeader Parse(string headerString)
        {
            var match = Regex.Match(headerString, AuthenticationHeaderRegex);
            if (!match.Success) return null;
            return new AuthenticationHeader
            {
                Algorithm = match.Groups[1].Value,
                SignedHeaders = match.Groups[2].Value,
                Credential = match.Groups[3].Value,
                Signature = match.Groups[4].Value
            };
        }
    }
}
