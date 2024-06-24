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

namespace MorganStanley.ComposeUI.Messaging.Abstractions
{
    public interface IMessageBuffer
    {
        /// <summary>
        ///     Gets the bytes of the underlying buffer as a <see cref="ReadOnlySpan{T}" />
        /// </summary>
        /// <returns></returns>
        ReadOnlySpan<byte> GetSpan();

        /// <summary>
        ///     Gets the string value of the buffer.
        /// </summary>
        /// <returns></returns>
        string GetString();

        /// <summary>
        /// Deserializes the JSON content of the buffer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        T? ReadJson<T>(JsonSerializerOptions? options = null);
    }
}
