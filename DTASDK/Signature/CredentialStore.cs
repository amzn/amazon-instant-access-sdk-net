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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Amazon.DTASDK.V2.Signature
{

    /// <summary>
    /// Class that is used to manage multiple credentials.
    ///
    /// The load methods can be called to load keys from a file path string, a FileStream or a
    /// string representing the file's contents.
    ///
    /// Each line of the file/stream/string must contain a secret key and a public key separated by an empty space, for
    /// example:
    ///
    /// 69b2048d-8bf8-4c1c-b49d-e6114897a9a5 dce53190-1f70-4206-ad28-0e1ab3683161
    ///
    /// Credentials, then, can be accessed by the public key using CredentialStore.Get(String)
    /// </summary>
    public class CredentialStore
    {

        private Dictionary<string, Credential> store = new Dictionary<string, Credential>();

        public Credential this[string key]
        {
            get => Get(key);
        }

        /// <summary>
        /// Gets the credential for a given public key.
        /// </summary>
        /// <param name="publicKey">
        /// the public key
        /// </param>
        /// <returns>
        /// the credential
        /// </returns>
        public Credential Get(string publicKey)
        {
            if (TryGetValue(publicKey, out Credential credential)) return credential;
            string message = "Credential not found for public key: " + publicKey;
            throw new CredentialNotFoundException(message);
        }

        public bool TryGetValue(string publicKey, out Credential credential) =>
            store.TryGetValue(publicKey, out credential);


        /// <summary>
        /// Adds the new credential to the store. If the store already contains the public key the credential is replaced.
        /// </summary>
        /// <param name="credential">
        /// the credential object to be added
        /// </param>
        public void Add(Credential credential)
        {
            store.Add(credential.PublicKey, credential);
        }

        /// <summary>
        /// Removes the credential from the store.
        /// </summary>
        /// <param name="publicKey">
        /// the public key of the credential to be removed
        /// </param>
        public void Remove(string publicKey)
        {
            store.Remove(publicKey);
        }

        /// <summary>
        /// Loads keys from a file and populates the store.
        ///
        /// Each line of the file must contain a secret key and a public key separated by an empty space.
        /// </summary>
        /// <param name="filePath">
        /// the file object that contains the keys
        /// </param>
        public void LoadFromFilePath(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException(nameof(filePath) + " must be path to exisiting file");
            }

            LoadFromStream(new FileStream(filePath, FileMode.Open));
        }

        /// <summary>
        /// Loads keys from a input stream and populates the store.
        ///
        /// Each line of the file must contain a secret key and a public key separated by an empty space.
        /// </summary>
        /// <param name="stream">
        /// the stream object that contains the keys
        /// </param>
        public void LoadFromStream(Stream stream)
        {
            if (stream == null)
            {
                string message = "Invalid keys";
                throw new ArgumentException(message);
            }
            StreamReader reader = new StreamReader(stream);
            string contents = reader.ReadToEnd();
            reader.Close();

            LoadFromContents(contents);
        }

        /// <summary>
        /// Loads keys from a string and populates the store.
        /// Each line of the file must contain a secret key and a public key separated by an empty space.
        /// </summary>
        /// <param name="contents">
        /// the string object that contains the keys
        /// </param>
        public void LoadFromContents(string contents)
        {
            if (string.IsNullOrEmpty(contents))
            {
                string message = "Invalid keys";
                throw new ArgumentException(message);
            }

            string[] lines = contents.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // credentials should be separate by an empty space
                string[] keys = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

                // Invalid format
                if (keys.Length < 2)
                {
                    throw new ArgumentException("Invalid credentials format found");
                }

                string secretKey = keys[0];
                string publicKey = keys[1];

                store.Add(publicKey, new Credential(secretKey, publicKey));
            }
        }
    }
}