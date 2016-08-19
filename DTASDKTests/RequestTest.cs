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

namespace Amazon.DTASDK.V2.Tests
{
    [TestClass]
    public class RequestTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "inputUrl is not a valid Url")]
        public void ctor_BlankUrl()
        {
            new Request("", Request.Method.Get, "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "inputUrl is not a valid Url")]
        public void ctor_MalformedURL()
        {
            new Request("blork", Request.Method.Get, "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "inputUrl is not a valid Url")]
        public void ctor_MalformedURI()
        {
            new Request("http://amazon.com/?s=^IXIC", Request.Method.Get, "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "inputUrl cannot be null.")]
        public void ctor_NullUrl()
        {
            new Request(null, Request.Method.Get, "");
        }

        [TestMethod]
        public void defaultValues()
        {
            Request request = new Request("http://amazon.com", Request.Method.Get, "content/type");
            Assert.AreEqual("content/type", request.GetHeader(Request.ContentTypeHeader));
            Assert.IsTrue(request.GetHeaderNames().Contains(Request.ContentTypeHeader));
            Assert.AreEqual(Request.DefaultUserAgent, request.UserAgent);
        }

        [TestMethod]
        public void getSetHeader()
        {
            Request request = new Request("http://amazon.com", Request.Method.Get, "content/type");
            request.SetHeader("key", "value");
            Assert.AreEqual("value", request.GetHeader("key"));
        }

        [TestMethod]
        public void getHeaderNames()
        {
            Request request = new Request("http://amazon.com", Request.Method.Get, "content/type");
            request.SetHeader("key", "value");
            request.SetHeader("key2", "value");
            Assert.IsTrue(request.GetHeaderNames().Contains("key"));
            Assert.IsTrue(request.GetHeaderNames().Contains("key2"));
            Assert.IsTrue(request.GetHeaderNames().Contains(Request.ContentTypeHeader));
        }

        [TestMethod]
        public void headersInsenstive()
        {
            Request request = new Request("http://amazon.com", Request.Method.Get, "content/type");
            request.SetHeader("KeyName", "values");

            Assert.AreEqual("values", request.GetHeader("Keyname"));
            Assert.AreEqual("values", request.GetHeader("keyName"));
            Assert.AreEqual("values", request.GetHeader("keyname"));
        }

        [TestMethod]
        public void fromHttpWebRequest()
        {
            Mock<HttpWebRequest> hsr = new Mock<HttpWebRequest>();
            Uri mockUri = new Uri("http://amazon.com:80/servlet/path");

            WebHeaderCollection headers = new WebHeaderCollection
            {
                {"header1", "headerValue1"},
                {"header2", "headerValue2"},
                {Request.ContentTypeHeader, "content-type"}
            };

            MemoryStream mockStream = new MemoryStream();
            StreamWriter bodyWriter = new StreamWriter(mockStream);
            bodyWriter.Write("body");
            bodyWriter.Flush();
            mockStream.Seek(0, SeekOrigin.Begin);

            hsr.Setup(HttpWebRequest => HttpWebRequest.GetRequestStream()).Returns(() => mockStream);
            hsr.Setup(HttpWebRequest => HttpWebRequest.Headers).Returns(() => headers);
            hsr.Setup(HttpWebRequest => HttpWebRequest.ContentType).Returns(() => "content-type");
            hsr.Setup(HttpWebRequest => HttpWebRequest.Method).Returns(() => "GET");
            hsr.Setup(HttpWebRequest => HttpWebRequest.RequestUri).Returns(() => mockUri);

            Request request = new Request(hsr.Object);

            Assert.AreEqual("headerValue1", request.GetHeader("header1"));
            Assert.AreEqual("headerValue2", request.GetHeader("header2"));
            Assert.AreEqual("content-type", request.GetHeader(Request.ContentTypeHeader));
            Assert.AreEqual(Request.Method.Get, request.RequestMethod);
            Assert.AreEqual("http://amazon.com:80/servlet/path", request.Url);
            Assert.AreEqual("body", request.Body);
        }

        [TestMethod]
        public void fromHttpServletRequest_withQueryParams()
        {
            Mock<HttpWebRequest> hsr = new Mock<HttpWebRequest>();
            Uri mockUri = new Uri("http://amazon.com:8888/servlet?param=value&param2=value2");

            WebHeaderCollection headers = new WebHeaderCollection
            {
                {"header1", "headerValue1"},
                {"header2", "headerValue2"},
                {Request.ContentTypeHeader, "content-type"}
            };

            MemoryStream mockStream = new MemoryStream();
            StreamWriter bodyWriter = new StreamWriter(mockStream);
            bodyWriter.Write("body");
            bodyWriter.Flush();
            mockStream.Seek(0, SeekOrigin.Begin);

            hsr.Setup(HttpWebRequest => HttpWebRequest.GetRequestStream()).Returns(() => mockStream);
            hsr.Setup(HttpWebRequest => HttpWebRequest.Headers).Returns(() => headers);
            hsr.Setup(HttpWebRequest => HttpWebRequest.ContentType).Returns(() => "content-type");
            hsr.Setup(HttpWebRequest => HttpWebRequest.Method).Returns(() => "GET");
            hsr.Setup(HttpWebRequest => HttpWebRequest.RequestUri).Returns(() => mockUri);

            Request request = new Request(hsr.Object);

            Assert.AreEqual("headerValue1", request.GetHeader("header1"));
            Assert.AreEqual("headerValue2", request.GetHeader("header2"));
            Assert.AreEqual("content-type", request.GetHeader(Request.ContentTypeHeader));
            Assert.AreEqual(Request.Method.Get, request.RequestMethod);
            Assert.AreEqual("http://amazon.com:8888/servlet?param=value&param2=value2", request.Url);
            Assert.AreEqual("body", request.Body);
        }

        [TestMethod]
        public void testSerializeSBS2Request()
        {
            string SBS2JsonRequestBody = @"{""operation""SubscriptionActivate"",
                                            ""subscriptionId"":""6f3092e5-0326-42b7-a107-416234d548d8"",
                                            ""productId"": ""subscriptionA"",
                                            ""userId"": ""12345""}";

            Serializer serializer = new Serializer();

            SubscriptionActivateRequest request = serializer.Deserialize<SubscriptionActivateRequest>(SBS2JsonRequestBody);
            Assert.AreEqual("SubscriptionActivate", request.Operation);
            Assert.AreEqual("6f3092e5-0326-42b7-a107-416234d548d8", request.SubscriptionId);
            Assert.AreEqual("subscriptionA", request.ProductId);
            Assert.AreEqual("12345", request.UserId);
            Assert.AreEqual(0, request.NumberOfSubscriptionsInGroup);
            Assert.AreEqual(null, request.SubscriptionGroupId);

        }

        [TestMethod]
        public void testSerializeTeamSubsRequest()
        {
            string SBS2JsonRequestBody = @"{""operation""SubscriptionActivate"",
                                            ""subscriptionId"":""6f3092e5-0326-42b7-a107-416234d548d8"",
                                            ""productId"": ""subscriptionA"",
                                            ""userId"": ""12345"",
                                            ""numberOfSubscriptionsInGroup"": 3,
                                            ""subscriptionGroupId"": ""868a2dd8-64ce-11e6-874a-5065f33e6360""
                                            }";

            Serializer serializer = new Serializer();

            SubscriptionActivateRequest request = serializer.Deserialize<SubscriptionActivateRequest>(SBS2JsonRequestBody);
            Assert.AreEqual("SubscriptionActivate", request.Operation);
            Assert.AreEqual("6f3092e5-0326-42b7-a107-416234d548d8", request.SubscriptionId);
            Assert.AreEqual("subscriptionA", request.ProductId);
            Assert.AreEqual("12345", request.UserId);
            Assert.AreEqual(3, request.NumberOfSubscriptionsInGroup);
            Assert.AreEqual("868a2dd8-64ce-11e6-874a-5065f33e6360", request.SubscriptionGroupId);

        }
    }
}
