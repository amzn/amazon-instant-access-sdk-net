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
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;

namespace Amazon.DTASDK.V2.Signature
{

    static class SignatureUtils
    {
        public static string HexEncode(this byte[] bytes)
        {
            var sb = new InplaceStringBuilder(bytes.Length * 2);
            foreach (var _byte in bytes)
            {
                sb.Append($"{_byte:x2}");
            }
            return sb.ToString();
        }
    }
}