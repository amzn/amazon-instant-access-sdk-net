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
using System.Threading.Tasks;
using Amazon.DTASDK.V2;
using Newtonsoft.Json.Linq;

namespace DTASDKWeb.Controllers
{
    /// <summary>
    /// AIA Acount Linking Controller.
    /// </summary>
    /// <remarks>
    /// Implement a concrete version of this class with your account linking logic.
    /// </remarks>
    public abstract class AccountLinkingController : InstantAccessController
    {
        protected override async Task<object> ProcessOperation(InstantAccessOperation operation, JObject content)
        {
            if (operation != InstantAccessOperation.GetUserId)
                throw new ArgumentException(
                    $"Operation[{operation}] is not supported by {nameof(AccountLinkingController)}");

            return await GetUserId(Serializer.Deserialize<GetUserIdRequest>(content)).ConfigureAwait(false);
        }

        protected abstract Task<GetUserIdResponse> GetUserId(GetUserIdRequest request);
    }
}