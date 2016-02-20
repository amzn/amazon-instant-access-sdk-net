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
using System.Runtime.Serialization;

namespace Amazon.DTASDK.V2.Signature
{
    /// <summary>
    /// Thrown when a credential is not stored within the CredentialStore, or when an error occurs when retrieving the Credential.
    /// </summary>
    [Serializable]
    public sealed class CredentialNotFoundException : Exception
    {
        public CredentialNotFoundException() { }

        public CredentialNotFoundException(string message) : base(message) { }

        public CredentialNotFoundException(string message, Exception inner)
            : base(message, inner) { }

        private CredentialNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
