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
using System.Net;
using System.IO;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Amazon.DTASDK.V2;
using Newtonsoft.Json.Linq;

namespace Amazon.DTASDK.V2.Tests
{
    [TestClass]
    public class SerializerTest
    {

        [TestMethod]
        public void testSerializeSBS2Request()
        {
            string SBS2JsonRequestBody = @"{""operation"":""SubscriptionActivate"",
                                            ""subscriptionId"":""6f3092e5-0326-42b7-a107-416234d548d8"",
                                            ""productId"": ""subscriptionA"",
                                            ""userId"": ""12345""}";

            Serializer serializer = new Serializer();
            SubscriptionActivateRequest request = serializer.Deserialize<SubscriptionActivateRequest>(JObject.Parse(SBS2JsonRequestBody));
            Assert.AreEqual(InstantAccessOperation.SubscriptionActivate, request.Operation);
            Assert.AreEqual("6f3092e5-0326-42b7-a107-416234d548d8", request.SubscriptionId);
            Assert.AreEqual("subscriptionA", request.ProductId);
            Assert.AreEqual("12345", request.UserId);
            Assert.AreEqual(0, request.NumberOfSubscriptionsInGroup);
            Assert.AreEqual(null, request.SubscriptionGroupId);

        }

        [TestMethod]
        public void testSerializeTeamSubsRequest()
        {
            string SBS2JsonRequestBody = @"{""operation"":""SubscriptionActivate"",
                                            ""subscriptionId"":""6f3092e5-0326-42b7-a107-416234d548d8"",
                                            ""productId"": ""subscriptionA"",
                                            ""userId"": ""12345"",
                                            ""numberOfSubscriptionsInGroup"": 3,
                                            ""subscriptionGroupId"": ""868a2dd8-64ce-11e6-874a-5065f33e6360""
                                            }";

            Serializer serializer = new Serializer();

            SubscriptionActivateRequest request = serializer.Deserialize<SubscriptionActivateRequest>(JObject.Parse(SBS2JsonRequestBody));
            Assert.AreEqual(InstantAccessOperation.SubscriptionActivate, request.Operation);
            Assert.AreEqual("6f3092e5-0326-42b7-a107-416234d548d8", request.SubscriptionId);
            Assert.AreEqual("subscriptionA", request.ProductId);
            Assert.AreEqual("12345", request.UserId);
            Assert.AreEqual(3, request.NumberOfSubscriptionsInGroup);
            Assert.AreEqual("868a2dd8-64ce-11e6-874a-5065f33e6360", request.SubscriptionGroupId);

        }
    }
}
