/*
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

using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;

public sealed class Fdc3DesktopAgentOptions : IOptions<Fdc3DesktopAgentOptions>
{
    /// <summary>
    /// When set to any value, the Desktop Agent will create the specified user channel on startup and will join to it.
    /// </summary>
    public string? ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the source URL of a static JSON file, indicating what User Channel set is being supported by the DesktopAgent.
    /// </summary>
    public Uri? UserChannelConfigFile { get; set; }

    /// <summary>
    /// Sets the UserChannel set.
    /// </summary>
    public ChannelItem[]? UserChannelConfig { get; set; }

    /// <summary>
    /// Indicates a timeout value for getting the IntentResult from the backend in milliseconds.
    /// When set to any value, it sets the timeout for the getResult() client calls, which should wait either for this timeout or the task which gets the appropriate resolved IntentResolution.
    /// Timeout by default is 5 seconds. 
    /// </summary>
    public TimeSpan IntentResultTimeout { get; set; } = TimeSpan.FromSeconds(65);

    /// <summary>
    /// Indicates timeout value for registering the listeners when a new instance of an FDC3 app is launched.
    /// </summary>
    public TimeSpan ListenerRegistrationTimeout { get; set; } = TimeSpan.FromSeconds(5);

    Fdc3DesktopAgentOptions IOptions<Fdc3DesktopAgentOptions>.Value => this;
}
