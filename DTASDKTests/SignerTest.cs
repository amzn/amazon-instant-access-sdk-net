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
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Amazon.DTASDK.V2.Signature;

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

            Request request = new Request("http://amazon.com", Request.Method.Get, "application/json") {Body = "body"};

            signer.Sign(request, credential);

            Assert.AreEqual("20110909T233600Z", request.GetHeader("x-amz-date"));
            Assert.AreEqual("DTA1-HMAC-SHA256 " + "SignedHeaders=content-type;x-amz-date, " + "Credential=KEYID/20110909, "
                    + "Signature=4d2f81ea2cf8d6963f8176a22eec4c65ae95c63502326a7c148686da7d50f47e",
                    request.GetHeader("Authorization"));
        }

        [TestMethod]
        public void validSignatureAfterSigningTwice()
        {
            Mock<Clock> mockClock = new Mock<Clock>();
            DateTime date1 = getDate(2011, 9, 9, 01, 01, 0);
            DateTime date2 = getDate(2011, 9, 9, 23, 36, 0);

            mockClock.SetupSequence(clock => clock.Now()).Returns(date1).Returns(date2);

            Signer signer = new Signer(new AuthenticationHeaderParser(), mockClock.Object);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            Request request = new Request("http://amazon.com", Request.Method.Get, "application/json") {Body = "body"};

            signer.Sign(request, credential);
            signer.Sign(request, credential);

            Assert.AreEqual("20110909T233600Z", request.GetHeader("x-amz-date"));
            Assert.AreEqual("DTA1-HMAC-SHA256 " + "SignedHeaders=content-type;x-amz-date, " + "Credential=KEYID/20110909, "
                    + "Signature=4d2f81ea2cf8d6963f8176a22eec4c65ae95c63502326a7c148686da7d50f47e",
                    request.GetHeader("Authorization"));
        }

        [TestMethod]
        public void validSignatureNullBody()
        {
            Mock<Clock> clock = new Mock<Clock>();
            DateTime mockDate = getDate(2011, 9, 9, 23, 36, 0);
            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            Request request = new Request("http://amazon.com", Request.Method.Get, "application/json");
            request.Body = null;

            signer.Sign(request, credential);

            Assert.AreEqual("20110909T233600Z", request.GetHeader("x-amz-date"));
            Assert.AreEqual("DTA1-HMAC-SHA256 " + "SignedHeaders=content-type;x-amz-date, " + "Credential=KEYID/20110909, "
                    + "Signature=d3042ffc41e6456535558faa130655a1c957263467e78d4485e70884b49ea52b",
                    request.GetHeader("Authorization"));
        }

        [TestMethod]
        public void additonalHeadersAreSigned()
        {
            Mock<Clock> clock = new Mock<Clock>();
            DateTime mockDate = getDate(2011, 9, 9, 23, 36, 0);
            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            Request request = new Request("http://amazon.com", Request.Method.Get, "application/json") {Body = "body"};
            request.SetHeader("aaa", "aaa");
            request.SetHeader("zzz", "zzz");

            signer.Sign(request, credential);

            Assert.AreEqual("20110909T233600Z", request.GetHeader("x-amz-date"));
            Assert.AreEqual("DTA1-HMAC-SHA256 " + "SignedHeaders=aaa;content-type;x-amz-date;zzz, "
                    + "Credential=KEYID/20110909, "
                    + "Signature=16ec5ffa0e33e8ec8f87f14bb5fd8a03545dbffe99eb3a89f5de450e791ef262",
                    request.GetHeader("Authorization"));
        }

        [TestMethod]
        public void verifyNoAuthorization()
        {
            Credential credential = new Credential("SECRETKEY", "KEYID");
            Signer signer = new Signer();

            Request verificationRequest = new Request("http://amazon.com", Request.Method.Get, "application/json");
            verificationRequest.SetHeader(X_AMZ_DATE_HEADER, "20110909T233600Z");

            Assert.IsFalse(signer.Verify(verificationRequest, credential));
        }

        [TestMethod]
        public void verifyBadAuthrorization()
        {
            Credential credential = new Credential("SECRETKEY", "KEYID");
            Signer signer = new Signer();

            Request verificationRequest = new Request("http://amazon.com", Request.Method.Get, "application/json");
            verificationRequest.SetHeader(AUTHORIZATION_HEADER, "THIS IS AN INVALID AUTHORZIATION HEADER");
            verificationRequest.SetHeader(X_AMZ_DATE_HEADER, "20110909T233600Z");

            Assert.IsFalse(signer.Verify(verificationRequest, credential));
        }

        [TestMethod]
        public void verifyNoDate()
        {
            Credential credential = new Credential("SECRETKEY", "KEYID");
            Signer signer = new Signer();

            Request verificationRequest = new Request("http://amazon.com", Request.Method.Get, "application/json");
            verificationRequest.SetHeader(AUTHORIZATION_HEADER, "");

            Assert.IsFalse(signer.Verify(verificationRequest, credential));
        }

        [TestMethod]
        public void roundTrip()
        {
            Mock<Clock> clock = new Mock<Clock>();
            DateTime mockDate = getDate(2011, 9, 9, 23, 36, 0);
            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            Request request = new Request("http://amazon.com", Request.Method.Get, "application/json");
            request.Body = "body";
            request.SetHeader("aaa", "aaa");
            request.SetHeader("zzz", "zzz");

            signer.Sign(request, credential);

            string authorization = request.GetHeader(AUTHORIZATION_HEADER);

            // NOTE: HttpServletRequest always seems to put a trailing slash on bare urls so the test mimics this
            Request verificationRequest = new Request("http://amazon.com/", Request.Method.Get, "application/json");
            verificationRequest.Body = "body";
            verificationRequest.SetHeader("aaa", "aaa");
            verificationRequest.SetHeader("zzz", "zzz");
            verificationRequest.SetHeader(X_AMZ_DATE_HEADER, "20110909T233600Z");
            verificationRequest.SetHeader(AUTHORIZATION_HEADER, authorization);

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

            Request request = new Request("http://amazon.com", Request.Method.Get, "application/json");
            request.Body = "body";
            request.SetHeader("aaa", "aaa");
            request.SetHeader("zzz", "zzz");

            signer.Sign(request, credential);

            string authorization = request.GetHeader(AUTHORIZATION_HEADER);

            // NOTE: HttpServletRequest always seems to put a trailing slash on bare urls so the test mimics this
            Request verificationRequest = new Request("http://amazon.com/", Request.Method.Get, "application/json");
            verificationRequest.Body = "body";
            verificationRequest.SetHeader("aaa", "aaa");
            verificationRequest.SetHeader("zzz", "zzz");
            verificationRequest.SetHeader(X_AMZ_DATE_HEADER, "20110909T233600Z");
            verificationRequest.SetHeader(AUTHORIZATION_HEADER, authorization);

            CredentialStore store = new CredentialStore();
            store.LoadFromContents("AUXKEY AUXKEYID\nSECRETKEY KEYID\n");

            Assert.IsTrue(signer.Verify(verificationRequest, store));
        }

        [TestMethod]
        public void ignoresExtraHeaders()
        {
            Mock<Clock> clock = new Mock<Clock>();
            DateTime mockDate = getDate(2011, 9, 9, 23, 36, 0);
            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            Request request = new Request("http://amazon.com", Request.Method.Get, "application/json");
            request.Body = "body";

            signer.Sign(request, credential);

            string authorization = request.GetHeader(AUTHORIZATION_HEADER);

            // NOTE: HttpServletRequest always seems to put a trailing slash on bare urls so the test mimics this
            Request verificationRequest = new Request("http://amazon.com/", Request.Method.Get, "application/json");
            verificationRequest.Body = "body";
            verificationRequest.SetHeader("aaa", "aaa");
            verificationRequest.SetHeader("zzz", "zzz");
            verificationRequest.SetHeader(X_AMZ_DATE_HEADER, "20110909T233600Z");
            verificationRequest.SetHeader(AUTHORIZATION_HEADER, authorization);
        }

        [TestMethod]
        public void fiveMinutesAfterValidates()
        {
            Mock<Clock> clock = new Mock<Clock>();
            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            DateTime mockDate = getDate(2011, 9, 9, 23, 36 + 5, 0); // Go 5 minutes after

            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            Request verificationRequest = new Request("http://amazon.com", Request.Method.Get, "application/json");
            verificationRequest.Body = "body";
            verificationRequest.SetHeader("aaa", "aaa");
            verificationRequest.SetHeader("zzz", "zzz");
            verificationRequest.SetHeader(X_AMZ_DATE_HEADER, "20110909T233600Z");
            verificationRequest
                    .SetHeader(
                            AUTHORIZATION_HEADER,
                            "DTA1-HMAC-SHA256 SignedHeaders=aaa;content-type;x-amz-date;zzz, Credential=KEYID/20110909, Signature=16ec5ffa0e33e8ec8f87f14bb5fd8a03545dbffe99eb3a89f5de450e791ef262");

            Assert.IsTrue(signer.Verify(verificationRequest, credential));
        }

        [TestMethod]
        public void fiveMinutesBeforeValidates()
        {
            Mock<Clock> clock = new Mock<Clock>();
            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            DateTime mockDate = getDate(2011, 9, 9, 23, 36 - 5, 0); // Go 5 minutes before

            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            Request verificationRequest = new Request("http://amazon.com", Request.Method.Get, "application/json");
            verificationRequest.Body = "body";
            verificationRequest.SetHeader("aaa", "aaa");
            verificationRequest.SetHeader("zzz", "zzz");
            verificationRequest.SetHeader(X_AMZ_DATE_HEADER, "20110909T233600Z");
            verificationRequest
                    .SetHeader(
                            AUTHORIZATION_HEADER,
                            "DTA1-HMAC-SHA256 SignedHeaders=aaa;content-type;x-amz-date;zzz, Credential=KEYID/20110909, Signature=16ec5ffa0e33e8ec8f87f14bb5fd8a03545dbffe99eb3a89f5de450e791ef262");

            Assert.IsTrue(signer.Verify(verificationRequest, credential));
        }

        [TestMethod]
        public void after15MinuteWindowFailsValidation()
        {
            Mock<Clock> clock = new Mock<Clock>();
            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            DateTime mockDate = getDate(2011, 9, 9, 23, 36 + 15, 1); // Go 15 Minutes after

            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            Request verificationRequest = new Request("http://amazon.com", Request.Method.Get, "application/json");
            verificationRequest.Body = "body";
            verificationRequest.SetHeader("aaa", "aaa");
            verificationRequest.SetHeader("zzz", "zzz");
            verificationRequest.SetHeader(X_AMZ_DATE_HEADER, "20110909T233600Z");
            verificationRequest
                    .SetHeader(
                            AUTHORIZATION_HEADER,
                            "DTA1-HMAC-SHA256 SignedHeaders=aaa;content-type;x-amz-date;zzz, Credential=KEYID/20110909, Signature=87729cb3475859a18b5d9cead0bba82f0f56a85c2a13bed3bc229c6c35e06628");

            Assert.IsFalse(signer.Verify(verificationRequest, credential));
        }

        [TestMethod]
        public void before15MinuteWindowFailsValidation()
        {
            Mock<Clock> clock = new Mock<Clock>();
            Signer signer = new Signer(new AuthenticationHeaderParser(), clock.Object);

            DateTime mockDate = getDate(2011, 9, 9, 23, 36 - 16, 59); // Go 15 Minutes Before

            clock.Setup(Clock => Clock.Now()).Returns(() => mockDate);

            Credential credential = new Credential("SECRETKEY", "KEYID");

            Request verificationRequest = new Request("http://amazon.com", Request.Method.Get, "application/json");
            verificationRequest.Body = "body";
            verificationRequest.SetHeader("aaa", "aaa");
            verificationRequest.SetHeader("zzz", "zzz");
            verificationRequest.SetHeader(X_AMZ_DATE_HEADER, "20110909T233600Z");
            verificationRequest
                    .SetHeader(
                            AUTHORIZATION_HEADER,
                            "DTA1-HMAC-SHA256 SignedHeaders=aaa;content-type;x-amz-date;zzz, Credential=KEYID/20110909, Signature=87729cb3475859a18b5d9cead0bba82f0f56a85c2a13bed3bc229c6c35e06628");

            Assert.IsFalse(signer.Verify(verificationRequest, credential));
        }

        private DateTime getDate(int year, int month, int day, int hour, int minute, int second)
        {
            return new DateTime(year, month, day, hour, minute, second, 0, new GregorianCalendar(), DateTimeKind.Utc);
        }

    }
}
