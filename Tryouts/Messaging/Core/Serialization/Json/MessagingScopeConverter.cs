﻿// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MorganStanley.ComposeUI.Messaging.Core.Serialization.Json;

internal class MessagingScopeConverter : JsonConverter<MessagingScope>
{
    public override bool HandleNull => true;

    public override MessagingScope Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return MessagingScope.Parse(reader.GetString()!);
            case JsonTokenType.Null:
                return MessagingScope.Default;
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, MessagingScope value, JsonSerializerOptions options)
    {
        if (value == default)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.AsString());
        }
    }
}
