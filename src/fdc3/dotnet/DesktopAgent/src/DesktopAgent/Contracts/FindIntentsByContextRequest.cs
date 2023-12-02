﻿/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

using System.Text.Json.Serialization;
using MorganStanley.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

/// <summary>
/// Request by calling the fdc3.findIntentsByContext.
/// </summary>
internal sealed class FindIntentsByContextRequest
{
    [JsonConstructor]
    public FindIntentsByContextRequest(
        string fdc3InstanceId,
        Context context,
        string? resultType = null)
    {
        Fdc3InstanceId = fdc3InstanceId;
        Context = context;
        ResultType = resultType;
    }

    /// <summary>
    /// Unique identifier from the application which sent the FindIntentsByContextRequest type of message.
    /// Probably the instanceId of the application which can be queried from the window.object, etc.
    /// </summary>
    public string Fdc3InstanceId { get; }

    /// <summary>
    /// <see cref="MorganStanley.Fdc3.Context.Context"/>
    /// </summary>
    public Context Context { get; }

    /// <summary>
    /// ResultType, indicating what resultType the requesting app is expecting.
    /// </summary>
    public string? ResultType { get; set; }
}