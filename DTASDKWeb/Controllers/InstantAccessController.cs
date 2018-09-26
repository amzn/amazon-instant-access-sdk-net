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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using Amazon.DTASDK.V2;
using Amazon.DTASDK.V2.Signature;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http.Features;

namespace DTASDKWeb.Controllers
{
    /// <summary>
    /// Base class for Instant Access Controller. Verifies the signature is correct in the headers,
    /// forwards the operation to the implementing controllers, and serializes the response.
    /// </summary>
    [ApiController]
    public abstract class InstantAccessController : Controller
    {

        private readonly Signer signer = new Signer();

        protected CredentialStore CredentialStore { get; set; }

        protected Serializer Serializer { get; } = new Serializer();

        [HttpPost]
        public async Task<ActionResult> Post()
        {
            try
            {
                string content;
                using(var sr = new StreamReader(Request.Body))
                {
                    content = await sr.ReadToEndAsync();
                }

                if (!signer.Verify(new DTARequest(Request, content), CredentialStore))
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }

                var jObject = JObject.Parse(content);
                InstantAccessOperation operation = GetInstantAccessOperation(jObject["operation"].ToString());
                var result = await ProcessOperation(operation, jObject).ConfigureAwait(false);
                return new JsonResult(result, Serializer.SerializerSettings);
            }

            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private static InstantAccessOperation GetInstantAccessOperation(string operation) =>
             (InstantAccessOperation)Enum.Parse(typeof(InstantAccessOperation), operation, true);

        protected abstract Task<object> ProcessOperation(InstantAccessOperation operation, JObject content);

        /// <summary>
        /// An implentation of IDTARequest wrapping HttpRequest
        /// </summary>
        private class DTARequest : IDTARequest
        {

            private readonly HttpRequest httpRequest;
            private readonly string content;

            public DTARequest(HttpRequest httpRequest, string content)
            {
                this.httpRequest = httpRequest;
                this.content = content;
            }

            public string HttpMethod => httpRequest.Method;

            public Uri Uri
            {
                get =>
                    new Uri(httpRequest.GetDisplayUrl());
            }

            public IDictionary<string, StringValues> Headers => httpRequest.Headers;

            public void SetHeader(string key, string value) => httpRequest.Headers[key] = value;

            public string Body => content;
        }
    }
}