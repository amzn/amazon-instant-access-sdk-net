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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using Amazon.DTASDK.V2;
using Amazon.DTASDK.V2.Signature;
using Newtonsoft.Json.Linq;

namespace DTASDKWeb.Controllers
{
    /// <summary>
    /// Base class for Instant Access Controller. Verfies the signature is correct in the headers,
    /// fowards the operation to the implementing controllers, and serializes the response.
    /// </summary>
    public abstract class InstantAccessController : ApiController
    {

        private readonly Signer _signer = new Signer();

        protected abstract CredentialStore CredentialStore { get; }

        protected Serializer Serializer { get; } = new Serializer();


        [HttpPost]
        public async Task<IHttpActionResult> Post()
        {
            try
            {
                var content = await Request.Content.ReadAsStringAsync().ConfigureAwait(false);
                var request = CreateIntantAccessRequest(content);

                if (!_signer.Verify(request, CredentialStore))
                {
                    return new StatusCodeResult(HttpStatusCode.Forbidden, Request);
                }

                var operation = GetInstantAccessOperation(content);
                var result = await ProcessOperation(operation, content).ConfigureAwait(false);
                return Json(result, Serializer.SerializerSettings);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        protected abstract Task<object> ProcessOperation(InstantAccessOperation operation, string content);

        private Request CreateIntantAccessRequest(string content)
        {
            var request = new Request(Request.RequestUri.OriginalString, Amazon.DTASDK.V2.Request.Method.Post,
                Request.Content.Headers.ContentType.MediaType);
            foreach (var httpRequestHeader in Request.Headers)
            {
                request.SetHeader(httpRequestHeader.Key, httpRequestHeader.Value.FirstOrDefault());
            }
            request.Body = content;
            return request;
        }

        private static InstantAccessOperation GetInstantAccessOperation(string content)
        {
            var jObject = JObject.Parse(content);

            return (InstantAccessOperation)
                Enum.Parse(typeof(InstantAccessOperation), jObject["operation"].ToString(), true);
        }
    }
}