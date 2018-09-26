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

namespace Amazon.DTASDK.V2
{
    public enum InstantAccessOperation
    {
        GetUserId,
        Purchase,
        Revoke,
        SubscriptionActivate,
        SubscriptionDeactivate,
    }

    public abstract class InstantAccessRequest
    {
        public InstantAccessOperation Operation { get; set; }
    }

    public sealed class GetUserIdRequest : InstantAccessRequest
    {
        public string InfoField1 { get; set; }
        public string InfoField2 { get; set; }
        public string InfoField3 { get; set; }
    }

    public sealed class PurchaseRequest : InstantAccessRequest
    {
        public string PurchaseToken { get; set; }
        public string UserId { get; set; }
        public string ProductId { get; set; }
        public string Reason { get; set; }
    }

    public class SubscriptionRequest : InstantAccessRequest
    {
        public string SubscriptionId { get; set; }
    }

    public sealed class SubscriptionActivateRequest : SubscriptionRequest
    {
        public string ProductId { get; set; }

        public string UserId { get; set; }

        public int NumberOfSubscriptionsInGroup { get; set; }

        public string SubscriptionGroupId { get; set; }
    }

    public enum SubscriptionReason
    {
        NOT_RENEWED,
        USER_REQUEST,
        CUSTOMER_SERVICE_REQUEST,
        PAYMENT_PROBLEM,
        TESTING,
        UNABLE_TO_FULFILL
    }

    public enum SubscriptionPeriod
    {
        FREE_TRIAL,
        GRACE_PERIOD,
        NOT_STARTED,
        REGULAR
    }

    public sealed class SubscriptionDeactivateRequest : SubscriptionRequest
    {
        public SubscriptionReason Reason { get; set; }
        public SubscriptionPeriod Period { get; set; }
    }

    public abstract class InstantAccessResponse<T>
    {
        public T Response { get; set; }
    }

    public enum GetUserIdResult
    {
        OK,
        FAILED_ACCOUNT_INVALID
    }

    public sealed class GetUserIdResponse : InstantAccessResponse<GetUserIdResult>
    {
        public string UserId { get; set; }
    }

    public enum FulfillPurchaseResult
    {
        OK,
        FAIL_USER_NOT_ELIGIBLE,
        FAIL_USER_INVALID,
        FAIL_OTHER
    }

    public sealed class FulfillPurchaseResposne
        : InstantAccessResponse<FulfillPurchaseResult>
    { }

    public enum RevokePurchaseResult
    {
        OK,
        FAILED_USER_INVALID,
        FAILED_INVALID_PURCHASETOKEN,
        FAIL_OTHER
    }

    public sealed class RevokePurchaseResponse
        : InstantAccessResponse<RevokePurchaseResult>
    { }

    public enum SubscriptionResult
    {
        OK,
        FAIL_USER_NOT_ELIGIBLE,
        FAIL_USER_INVALID,
        FAIL_INVALID_SUBSCRIPTION,
        FAIL_OTHER
    }

    public sealed class SubscriptionResponse
        : InstantAccessResponse<SubscriptionResult>
    { }

}