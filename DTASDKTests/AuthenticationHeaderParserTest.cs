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

using Amazon.DTASDK.V2.Signature;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.DTASDK.V2.Tests
{
    [TestClass]
    public class AuthenticationHeaderParserTest
    {
        static readonly string SIGNED_HEADERS_STRING = "SignedHeaders=";
        static readonly string CREDENTIALS_STRING = "Credential=";
        static readonly string SIGNATURE_STRING = "Signature=";
        static readonly string ALGORITHM_HEADER = "DTA1-HMAC-SHA256";

        [TestMethod]
        public void testSingleHeaderAuthentication()
        {
            string value = ALGORITHM_HEADER + " " + SIGNED_HEADERS_STRING + "aaa;content-type;x-amz-date;zzz, "
                + CREDENTIALS_STRING + "KEYID/20110909, "
                + SIGNATURE_STRING + "87729cb3475859a18b5d9cead0bba82f0f56a85c2a13bed3bc229c6c35e06628";

            AuthenticationHeader result = new AuthenticationHeaderParser().Parse(value);

            Assert.IsNotNull(result);
            Assert.AreEqual(ALGORITHM_HEADER, result.Algorithm);
            Assert.AreEqual("KEYID/20110909", result.Credential);
            Assert.AreEqual("aaa;content-type;x-amz-date;zzz", result.SignedHeaders);
            Assert.AreEqual("87729cb3475859a18b5d9cead0bba82f0f56a85c2a13bed3bc229c6c35e06628", result.Signature);
        }

        [TestMethod]
        public void testSingleHeaderAuthentication2()
        {
            string value = ALGORITHM_HEADER + " "
                + SIGNED_HEADERS_STRING + "content-type;x-amz-date;x-amz-dta-version;x-amz-request-id, "
                + CREDENTIALS_STRING + "367caa91-cde5-48f2-91fe-bb95f546e9f0/20131207, "
                + SIGNATURE_STRING + "6fe5d5bbf4acda9b0f47f66db3ad8f23a33117ee52b45ae69983bec0b50550fe";

            AuthenticationHeader result = new AuthenticationHeaderParser().Parse(value);

            Assert.IsNotNull(result);
            Assert.AreEqual(ALGORITHM_HEADER, result.Algorithm);
            Assert.AreEqual("367caa91-cde5-48f2-91fe-bb95f546e9f0/20131207", result.Credential);
            Assert.AreEqual("content-type;x-amz-date;x-amz-dta-version;x-amz-request-id", result.SignedHeaders);
            Assert.AreEqual("6fe5d5bbf4acda9b0f47f66db3ad8f23a33117ee52b45ae69983bec0b50550fe", result.Signature);
        }

        [TestMethod]
        public void testInvalidHeader()
        {
            Assert.IsNull(new AuthenticationHeaderParser().Parse("DAT"));
        }

        [TestMethod]
        public void testFutureCompatibilityForMultipleHeaders()
        {
            string value = ALGORITHM_HEADER + " " + SIGNED_HEADERS_STRING + "aaa;content-type;x-amz-date;zzz, "
                    + CREDENTIALS_STRING + "KEYID/20110909, "
                    + SIGNATURE_STRING + "87729cb3475859a18b5d9cead0bba82f0f56a85c2a13bed3bc229c6c35e06628, "
                    + CREDENTIALS_STRING + "KEYID2/20110909, "
                    + SIGNATURE_STRING + "OTHERSIGNATUREalskdjfasldkjf234lkj234lkjalkj234lkj324lkj2345lkj2";

            AuthenticationHeader result = new AuthenticationHeaderParser().Parse(value);

            Assert.IsNotNull(result);
            Assert.AreEqual(ALGORITHM_HEADER, result.Algorithm);
            Assert.AreEqual("KEYID/20110909", result.Credential);
            Assert.AreEqual("aaa;content-type;x-amz-date;zzz", result.SignedHeaders);
            Assert.AreEqual("87729cb3475859a18b5d9cead0bba82f0f56a85c2a13bed3bc229c6c35e06628", result.Signature);
        }
    }
}
