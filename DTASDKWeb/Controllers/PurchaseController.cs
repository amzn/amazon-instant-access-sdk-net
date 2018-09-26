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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTASDKWeb.Controllers
{
    /// <summary>
    /// AIA Purchasing controller.
    /// </summary>
    /// <remarks>
    /// Implement a concrete version of this class with your purchasing logic.
    /// </remarks>
    public abstract class PurchaseController : InstantAccessController
    {
        protected override async Task<object> ProcessOperation(InstantAccessOperation operation, JObject content)
        {
            switch (operation)
            {
                case InstantAccessOperation.Purchase:
                    return await
                        ProcessFulfillPurchase(
                            Serializer.Deserialize<PurchaseRequest>(content))
                            .ConfigureAwait(false);
                case InstantAccessOperation.Revoke:
                    return await
                        ProcessRevokePurchase(
                            Serializer.Deserialize<PurchaseRequest>(content))
                            .ConfigureAwait(false);
                case InstantAccessOperation.SubscriptionActivate:
                    return await
                        ProcessSubscriptionActivate(
                            Serializer.Deserialize<SubscriptionActivateRequest>(content))
                            .ConfigureAwait(false);
                case InstantAccessOperation.SubscriptionDeactivate:
                    return await
                        ProcessSubscriptionRevoke(
                            Serializer.Deserialize<SubscriptionDeactivateRequest>(content))
                            .ConfigureAwait(false);
                default:
                    throw new ArgumentException(
                        $"Operation[{operation}] is not supported by {nameof(PurchaseController)}");
            }
        }

        /// <summary>
        /// Proccesses Subscription Activation requests.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> of <see cref="SubscriptionResponse"/> or an exception if subscriptions are not supported.</returns>
        protected abstract Task<SubscriptionResponse> ProcessSubscriptionActivate(SubscriptionActivateRequest request);

        /// <summary>
        /// Processes Subscription Revoke requests.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> of <see cref="SubscriptionResponse"/> or an exception if subscriptions are not supported.</returns>
        protected abstract Task<SubscriptionResponse> ProcessSubscriptionRevoke(SubscriptionDeactivateRequest request);


        /// <summary>
        /// Processes Fulfill Purchase requests.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> of <see cref="FulfillPurchaseResponse"/> or an exception if bill once purchases are not supported.</returns>
        protected abstract Task<FulfillPurchaseResposne> ProcessFulfillPurchase(PurchaseRequest purchaseRequest);

        /// <summary>
        /// Processes Revoke Purchases requests.
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> of <see cref="RevokePurchaseResponse"/> or an exception if bill once purchases are not supported.</returns>
        protected abstract Task<RevokePurchaseResponse> ProcessRevokePurchase(PurchaseRequest purchaseRequest);

    }
}