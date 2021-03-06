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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Amazon.DTASDK.V2
{
    /// <summary>
    /// Serializes and deserializes json AIA requests and responses.
    /// </summary>
    public class Serializer
    {
        public static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = { new StringEnumConverter() }
        };

        private JsonSerializerSettings serializerSettings = DefaultSerializerSettings;
        private JsonSerializer serializer = JsonSerializer.Create(DefaultSerializerSettings);
        public JsonSerializerSettings SerializerSettings
        {
            get
            {
                return serializerSettings;
            }
            set
            {
                serializerSettings = value;
                serializer = JsonSerializer.Create(serializerSettings);
            }
        }

        public T Deserialize<T>(JObject json) =>
            json.ToObject<T>(serializer);

    }
}