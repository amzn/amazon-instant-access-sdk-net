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
    [Serializable]
    public sealed class SigningException : Exception
    {
        public SigningException() { }

        public SigningException(string message) : base(message) { }

        public SigningException(string message, Exception inner) : base(message, inner) { }

        private SigningException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
