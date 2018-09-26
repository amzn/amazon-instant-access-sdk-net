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
using System.Globalization;
using Amazon.DTASDK.V2.Signature;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Amazon.DTASDK.V2.Tests
{

    [TestClass]
    public class SignerTest
    {
        public static readonly string AUTHORIZATION_HEADER = "Authorization";
        public static readonly string X_AMZ_DATE_HEADER = "x-amz-date";

        // By design several of these first tests only verify that signatures calculate to an externally verified signature.
        // The more interesting and robust tests are the round trip ones.

        [TestMethod]
        public void validSignature()
        {
            Mock<Clock> clock = new Mock<Clock>();
            DateTime mockDate = getDate(2011, 9, 9, 23, 36, 0);
            clock.Setup(Clock => Clock.Now()).Returns(mockDate);

            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            Mock<IDTARequest> mockDTARequest = createRequestMock("http://amazon.com", "GET", "application/json", "body");
            IDTARequest dtaRequestMock = mockDTARequest.Object;

            signer.Sign(dtaRequestMock, credential);

            Assert.AreEqual("20110909T233600Z", dtaRequestMock.Headers["x-amz-date"].ToString());
            Assert.AreEqual("DTA1-HMAC-SHA256 SignedHeaders=content-type;x-amz-date, Credential=KEYID/20110909, " +
                "Signature=4d2f81ea2cf8d6963f8176a22eec4c65ae95c63502326a7c148686da7d50f47e",
                dtaRequestMock.Headers["Authorization"].ToString());
        }

        [TestMethod]
        public void validSignatureNullBody()
        {
            Mock<Clock> clock = new Mock<Clock>();
            DateTime mockDate = getDate(2011, 9, 9, 23, 36, 0);
            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            var mockDTARequest = createRequestMock("http://amazon.com", "GET", "application/json");
            IDTARequest request = mockDTARequest.Object;

            signer.Sign(request, credential);

            Assert.AreEqual("20110909T233600Z", request.Headers["x-amz-date"].ToString());
            Assert.AreEqual("DTA1-HMAC-SHA256 SignedHeaders=content-type;x-amz-date, Credential=KEYID/20110909, " +
                "Signature=d3042ffc41e6456535558faa130655a1c957263467e78d4485e70884b49ea52b",
                request.Headers["Authorization"].ToString());
        }

        [TestMethod]
        public void additionalHeadersAreSigned()
        {
            Mock<Clock> clock = new Mock<Clock>();
            DateTime mockDate = getDate(2011, 9, 9, 23, 36, 0);
            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            var mockDTARequest = createRequestMock("http://amazon.com", "GET", "application/json", "body");
            IDTARequest request = mockDTARequest.Object;
            request.Headers["aaa"] = "aaa";
            request.Headers["zzz"] = "zzz";

            signer.Sign(request, credential);

            Assert.AreEqual("20110909T233600Z", request.Headers["x-amz-date"].ToString());
            Assert.AreEqual("DTA1-HMAC-SHA256 SignedHeaders=aaa;content-type;x-amz-date;zzz, Credential=KEYID/20110909, " +
                "Signature=16ec5ffa0e33e8ec8f87f14bb5fd8a03545dbffe99eb3a89f5de450e791ef262",
                request.Headers["Authorization"].ToString());
        }

        [TestMethod]
        public void verifyNoAuthorization()
        {
            Credential credential = new Credential("SECRETKEY", "KEYID");
            Signer signer = new Signer();

            var mockDTARequest = createRequestMock("http://amazon.com", "GET", "application/json");
            IDTARequest request = mockDTARequest.Object;
            request.Headers[X_AMZ_DATE_HEADER] = "20110909T233600Z";

            Assert.IsFalse(signer.Verify(request, credential));
        }

        [TestMethod]
        public void verifyBadAuthorization()
        {
            Credential credential = new Credential("SECRETKEY", "KEYID");
            Signer signer = new Signer();

            var mockDTARequest = createRequestMock("http://amazon.com", "GET", "application/json");
            IDTARequest request = mockDTARequest.Object;
            request.Headers[AUTHORIZATION_HEADER] = "THIS IS AN INVALID AUTHORIZATION HEADER";
            request.Headers[X_AMZ_DATE_HEADER] = "20110909T233600Z";

            Assert.IsFalse(signer.Verify(request, credential));
        }

        [TestMethod]
        public void verifyNoDate()
        {
            Credential credential = new Credential("SECRETKEY", "KEYID");
            Signer signer = new Signer();

            var mockDTARequest = createRequestMock("http://amazon.com", "GET", "application/json");
            IDTARequest request = mockDTARequest.Object;
            request.Headers[AUTHORIZATION_HEADER] = string.Empty;

            Assert.IsFalse(signer.Verify(request, credential));
        }

        [TestMethod]
        public void roundTrip()
        {
            Mock<Clock> clock = new Mock<Clock>();
            DateTime mockDate = getDate(2011, 9, 9, 23, 36, 0);
            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            var mockDTARequest = createRequestMock("http://amazon.com", "GET", "application/json", "body");
            var signRequest = mockDTARequest.Object;
            signRequest.Headers["aaa"] = "aaa";
            signRequest.Headers["zzz"] = "zzz";

            signer.Sign(signRequest, credential);

            string authorization = signRequest.Headers[AUTHORIZATION_HEADER];

            mockDTARequest = createRequestMock("http://amazon.com", "GET", "application/json", "body");
            var verificationRequest = mockDTARequest.Object;
            verificationRequest.Headers["aaa"] = "aaa";
            verificationRequest.Headers["zzz"] = "zzz";
            verificationRequest.Headers[X_AMZ_DATE_HEADER] = "20110909T233600Z";
            verificationRequest.Headers[AUTHORIZATION_HEADER] = authorization;

            Assert.IsTrue(signer.Verify(verificationRequest, credential));
        }

        [TestMethod]
        public void roundTripWithCredentialStore()
        {
            Mock<Clock> clock = new Mock<Clock>();
            DateTime mockDate = getDate(2011, 9, 9, 23, 36, 0);
            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            var mockDTARequest = createRequestMock("http://amazon.com", "GET", "application/json", "body");
            var signRequest = mockDTARequest.Object;
            signRequest.Headers["aaa"] = "aaa";
            signRequest.Headers["zzz"] = "zzz";

            signer.Sign(signRequest, credential);

            string authorization = signRequest.Headers[AUTHORIZATION_HEADER];

            mockDTARequest = createRequestMock("http://amazon.com", "GET", "application/json", "body");
            var verificationRequest = mockDTARequest.Object;
            verificationRequest.Headers["aaa"] = "aaa";
            verificationRequest.Headers["zzz"] = "zzz";
            verificationRequest.Headers[X_AMZ_DATE_HEADER] = "20110909T233600Z";
            verificationRequest.Headers[AUTHORIZATION_HEADER] = authorization;

            CredentialStore store = new CredentialStore();
            store.LoadFromContents("AUXKEY AUXKEYID\nSECRETKEY KEYID\n");

            Assert.IsTrue(signer.Verify(verificationRequest, store));
        }

        [TestMethod]
        public void fiveMinutesAfterValidates()
        {
            Mock<Clock> clock = new Mock<Clock>();
            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            DateTime mockDate = getDate(2011, 9, 9, 23, 36 + 5, 0); // Go 5 minutes after

            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            var mockDTARequest = createRequestMock("http://amazon.com", "GET", "application/json", "body");
            IDTARequest request = mockDTARequest.Object;
            request.Headers["aaa"] = "aaa";
            request.Headers["zzz"] = "zzz";
            request.Headers[X_AMZ_DATE_HEADER] = "20110909T233600Z";
            request.Headers[AUTHORIZATION_HEADER] =
                            "DTA1-HMAC-SHA256 SignedHeaders=aaa;content-type;x-amz-date;zzz, Credential=KEYID/20110909, Signature=16ec5ffa0e33e8ec8f87f14bb5fd8a03545dbffe99eb3a89f5de450e791ef262";
            Assert.IsTrue(signer.Verify(request, credential));
        }

        [TestMethod]
        public void fiveMinutesBeforeValidates()
        {
            Mock<Clock> clock = new Mock<Clock>();
            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            DateTime mockDate = getDate(2011, 9, 9, 23, 36 - 5, 0); // Go 5 minutes before

            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            var mockDTARequest = createRequestMock("http://amazon.com", "GET", "application/json", "body");
            IDTARequest request = mockDTARequest.Object;
            request.Headers["aaa"] = "aaa";
            request.Headers["zzz"] = "zzz";
            request.Headers[X_AMZ_DATE_HEADER] = "20110909T233600Z";
            request.Headers[AUTHORIZATION_HEADER] =
                            "DTA1-HMAC-SHA256 SignedHeaders=aaa;content-type;x-amz-date;zzz, Credential=KEYID/20110909, Signature=16ec5ffa0e33e8ec8f87f14bb5fd8a03545dbffe99eb3a89f5de450e791ef262";
            Assert.IsTrue(signer.Verify(request, credential));
        }

        [TestMethod]
        public void after15MinuteWindowFailsValidation()
        {
            Mock<Clock> clock = new Mock<Clock>();
            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            DateTime mockDate = getDate(2011, 9, 9, 23, 36 + 15, 1); // Go 15 Minutes after

            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            var mockDTARequest = createRequestMock("http://amazon.com", "GET", "application/json", "body");
            IDTARequest request = mockDTARequest.Object;
            request.Headers["aaa"] = "aaa";
            request.Headers["zzz"] = "zzz";
            request.Headers[X_AMZ_DATE_HEADER] = "20110909T233600Z";
            request.Headers[AUTHORIZATION_HEADER] =
                            "DTA1-HMAC-SHA256 SignedHeaders=aaa;content-type;x-amz-date;zzz, Credential=KEYID/20110909, Signature=16ec5ffa0e33e8ec8f87f14bb5fd8a03545dbffe99eb3a89f5de450e791ef262";
            Assert.IsFalse(signer.Verify(request, credential));
        }

        [TestMethod]
        public void before15MinuteWindowFailsValidation()
        {
            Mock<Clock> clock = new Mock<Clock>();
            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            DateTime mockDate = getDate(2011, 9, 9, 23, 36 - 16, 59); // Go 15 Minutes Before

            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Credential credential = new Credential("SECRETKEY", "KEYID");
            var mockDTARequest = createRequestMock("http://amazon.com", "GET", "application/json", "body");
            IDTARequest request = mockDTARequest.Object;
            request.Headers["aaa"] = "aaa";
            request.Headers["zzz"] = "zzz";
            request.Headers[X_AMZ_DATE_HEADER] = "20110909T233600Z";
            request.Headers[AUTHORIZATION_HEADER] =
                            "DTA1-HMAC-SHA256 SignedHeaders=aaa;content-type;x-amz-date;zzz, Credential=KEYID/20110909, Signature=16ec5ffa0e33e8ec8f87f14bb5fd8a03545dbffe99eb3a89f5de450e791ef262";
            Assert.IsFalse(signer.Verify(request, credential));
        }

        private DateTime getDate(int year, int month, int day, int hour, int minute, int second)
        {
            return new DateTime(year, month, day, hour, minute, second, 0, new GregorianCalendar(), DateTimeKind.Utc);
        }

        private static Mock<IDTARequest> createRequestMock(string path, string method, string contentType, string body = null)
        {
            var mockDTARequest = new Mock<IDTARequest>();
            var headers = new Dictionary<string, StringValues>(StringComparer.InvariantCultureIgnoreCase);
            headers["Content-type"] = contentType;
            mockDTARequest.SetupGet(r => r.HttpMethod).Returns(method);
            mockDTARequest.SetupGet(r => r.Uri).Returns(new Uri(path));
            mockDTARequest.SetupGet(r => r.Body).Returns(body);
            mockDTARequest.SetupGet(r => r.Headers).Returns(headers);
            return mockDTARequest;
        }

    }
}
