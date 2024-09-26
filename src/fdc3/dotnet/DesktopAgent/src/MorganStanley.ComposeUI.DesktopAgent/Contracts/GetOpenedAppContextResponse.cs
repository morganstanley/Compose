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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

internal sealed class GetOpenedAppContextResponse
{
    /// <summary>
    /// Indicates if error happened during the execution.
    /// </summary>
    public string? Error { get; set; }

    //TODO: proper deserialization.
    /// <summary>
    /// Context that should be sent directly to the opened app via the fdc3.open().
    /// </summary>
    public string? Context { get; set; }

    public static GetOpenedAppContextResponse Failure(string error) => new() {Error = error};
    public static GetOpenedAppContextResponse Success(string context) => new() {Context = context};
}