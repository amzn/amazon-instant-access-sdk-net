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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Amazon.DTASDK.V2.Signature;
using System.IO;

namespace Amazon.DTASDK.V2.Tests
{
    [TestClass]
    public class CredentialStoreTest
    {
        private const string VALID_FILE = "validFile.txt";
        private const string INVALID_FILE = "invalidFile.txt";
        private const string INVALID_KEY = "871dbe31-3b46-4ca5-b9a2-8ad78eac4a4f";

        private static readonly string[] KEYS = { "69b2048d-8bf8-4c1c-b49d-e6114897a9a5",
                "dce53190-1f70-4206-ad28-0e1ab3683161", "f0a2586d-24ea-432f-a833-2da18f15ebd4",
                "eb3ce251-ef76-48ee-abb0-5886b1a3dfa0", "7568ccc2-9881-4468-ad73-025d16f0662e",
                "5de206ab-3a06-4354-a9a4-bfd6efee8027" };


        [TestInitialize]
        public void setUp()
        {
            StreamWriter writer = new StreamWriter(File.Create(VALID_FILE));

            writer.Write("{0} {1}\n", KEYS[0], KEYS[1]);
            // Intentionally check if blank lines are supported
            writer.Write("{0} {1}\n\n", KEYS[2], KEYS[3]);
            writer.Write("{0} {1}\n", KEYS[4], KEYS[5]);
            writer.Write("\n");
            writer.Flush();
            writer.Close();


            writer = new StreamWriter(File.Create(INVALID_FILE));
            writer.Write("{0}{1}\n", KEYS[0], KEYS[1]);
            writer.Write("{0} {1}\n", KEYS[2], KEYS[3]);
            writer.Write("{0} {1}\n", KEYS[4], KEYS[5]);
            writer.Write("\n");
            writer.Flush();
            writer.Close();
        }

        [TestMethod]
        public void testLoadFromFilePath()
        {
            CredentialStore store = new CredentialStore();
            store.LoadFromFilePath(VALID_FILE);

            assertCorrectCredentials(store);
        }

        [TestMethod]
        public void testLoadFromInputStream()
        {
            CredentialStore store = new CredentialStore();
            FileStream testStream = new FileStream(VALID_FILE, FileMode.Open);
            store.LoadFromStream(testStream);

            assertCorrectCredentials(store);
        }

        [TestMethod]
        public void testLoadFromString()
        {
            CredentialStore store = new CredentialStore();
            FileStream stream = new FileStream(VALID_FILE, FileMode.Open);
            StreamReader reader = new StreamReader(stream);

            string contents = reader.ReadToEnd();
            reader.Close();

            store.LoadFromContents(contents);

            assertCorrectCredentials(store);
        }

        private void assertCorrectCredentials(CredentialStore store)
        {
            Assert.AreEqual(KEYS[0], store.Get(KEYS[1]).SecretKey);
            Assert.AreEqual(KEYS[1], store.Get(KEYS[1]).PublicKey);

            Assert.AreEqual(KEYS[2], store.Get(KEYS[3]).SecretKey);
            Assert.AreEqual(KEYS[3], store.Get(KEYS[3]).PublicKey);

            Assert.AreEqual(KEYS[4], store.Get(KEYS[5]).SecretKey);
            Assert.AreEqual(KEYS[5], store.Get(KEYS[5]).PublicKey);
        }

        [TestMethod]
        [ExpectedException(typeof(CredentialNotFoundException), "Invalid keys")]
        public void testGetInvalidCredential()
        {
            CredentialStore store = new CredentialStore();
            store.LoadFromStream(new FileStream(VALID_FILE, FileMode.Open));

            store.Get(INVALID_KEY);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Invalid credentials format found on line 1")]
        public void testInvalidFile()
        {
            CredentialStore store = new CredentialStore();
            store.LoadFromFilePath(INVALID_FILE);
        }
    }
}
