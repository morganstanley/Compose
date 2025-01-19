// /*
//  * Morgan Stanley makes this available to you under the Apache License,
//  * Version 2.0 (the "License"). You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0.
//  *
//  * See the NOTICE file distributed with this work for additional information
//  * regarding copyright ownership. Unless required by applicable law or agreed
//  * to in writing, software distributed under the License is distributed on an
//  * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//  * or implied. See the License for the specific language governing permissions
//  * and limitations under the License.
//  */

using System;
using System.Threading.Tasks;
using Infragistics.Windows.DockManager;
using Infragistics.Windows.DockManager.Events;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell;

internal class WebContentPane : ContentPane
{
    public WebContentPane(WebContent webContent, IModuleLoader moduleLoader)
    {
        WebContent = webContent;
        _moduleLoader = moduleLoader;

        Header = webContent.ModuleInstance?.Manifest.Name ?? WebContent.Title ?? "New tab";
        Content = webContent.Content;
        Image = WebContent.Icon;
        Name = $"Pane_{DateTime.Now.Ticks}";
        SerializationId = Name;

        CloseAction = PaneCloseAction.RemovePane;
        Closing += Pane_Closing;
        Closed += Pane_Closed;
        WebContent.CloseRequested += WebContent_CloseRequested;
    }

    private void WebContent_CloseRequested(object? sender, System.EventArgs e)
    {
        ExecuteCommand(ContentPaneCommands.Close);
    }

    private void Pane_Closed(object? sender, PaneClosedEventArgs e)
    {
        WebContent.Dispose();
    }

    private void Pane_Closing(object? sender, PaneClosingEventArgs e)
    {
        if (WebContent.ModuleInstance == null)
            return;

        switch (WebContent.LifetimeEvent)
        {
            case LifetimeEventType.Stopped:
                return;

            case LifetimeEventType.Stopping:
                e.Cancel = true;
                Visibility = System.Windows.Visibility.Hidden;
                return;

            default:
                Visibility = System.Windows.Visibility.Hidden;
                Task.Run(() => _moduleLoader.StopModule(new StopRequest(WebContent.ModuleInstance.InstanceId)));
                return;
        }
    }

    public WebContent WebContent { get; }

    private readonly IModuleLoader _moduleLoader;
}
