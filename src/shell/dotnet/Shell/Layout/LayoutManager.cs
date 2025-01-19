// Morgan Stanley makes this available to you under the Apache License,
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

using Infragistics.Windows.DockManager;
using MorganStanley.ComposeUI.LayoutPersistence.Abstractions;
using MorganStanley.ComposeUI.ModuleLoader;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;
using System.Linq;

namespace MorganStanley.ComposeUI.Shell.Layout
{
    internal class LayoutManager
    {

        private readonly XamDockManager _xamDockManager;
        private readonly IModuleLoader _moduleLoader;
        private readonly ILayoutPersistence<string> _layoutPersistence;
        private readonly Dictionary<string, TaskCompletionSource<bool>> _moduleReadyTasks;

        private const string DockmanagerLayout = "layout";
        private const string MainWindowLayout = "mainWindow";
        private const string Modules = "modules";
        private const string SerializationId = "serializationId";

        public readonly Dictionary<string, WebContentPane> WebContentPanes;
        public bool IsLayoutLoading { get; private set; } = false;

        public LayoutManager(XamDockManager dockManager, IModuleLoader moduleLoader, ILayoutPersistence<string> layoutPersistence)
        {
            _xamDockManager = dockManager ?? throw new ArgumentNullException(nameof(dockManager));
            _moduleLoader = moduleLoader ?? throw new ArgumentNullException(nameof(moduleLoader));
            _layoutPersistence = layoutPersistence ?? throw new ArgumentNullException(nameof(layoutPersistence));
            _moduleReadyTasks = [];
            WebContentPanes = [];
        }

        public async Task LoadLayoutAsync()
        {
            var result = MessageBox.Show("Do you want to load the saved layout?",
                              "Load Layout",
                              MessageBoxButton.YesNo,
                              MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SetApplicationEnabledState(false);
                });

                IsLayoutLoading = true;

                try
                {
                    foreach (var pane in _xamDockManager.GetPanes(PaneNavigationOrder.ActivationOrder))
                    {
                        pane.ExecuteCommand(ContentPaneCommands.Close);
                    }

                    await LoadMainWindowLayout();
                    await LoadModulesAsync();
                    await LoadDockManagerLayout();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading layout: {ex.Message}");
                }
                finally
                {
                    IsLayoutLoading = false;

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SetApplicationEnabledState(true);
                    });
                }
            }
        }

        public async Task SaveLayoutAsync()
        {
            var result = MessageBox.Show("Are you sure you want to save the current layout?",
                              "Save Layout",
                              MessageBoxButton.YesNo,
                              MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SetApplicationEnabledState(false);
                });

                try
                {
                    await SaveMainWindowLayoutAsync();
                    await SaveModulesAsync();
                    await SaveDockManagerLayoutAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving layout: {ex.Message}");
                }
                finally
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SetApplicationEnabledState(true);
                    });
                }
            }
        }

        public void AddAndSetModuleContentState(WebContent webContent)
        {
            if (webContent == null) throw new ArgumentNullException(nameof(webContent));

            var serializationId = (webContent.ModuleInstance?.StartRequest.Parameters
                .FirstOrDefault(p => p.Key == SerializationId).Value) ?? throw new InvalidOperationException("SerializationId cannot be null");

            WebContentPanes[serializationId] = new WebContentPane(webContent, _moduleLoader);

            if (_moduleReadyTasks.ContainsKey(serializationId))
            {
                _moduleReadyTasks[serializationId].SetResult(true);
            }
        }

        private async Task SaveMainWindowLayoutAsync()
        {
            var mainWindow = new MainWindowParameters
            {
                WindowState = Application.Current.MainWindow.WindowState,
                Width = Application.Current.MainWindow.Width,
                Height = Application.Current.MainWindow.Height,
                Top = Application.Current.MainWindow.Top,
                Left = Application.Current.MainWindow.Left
            };

            await _layoutPersistence.SaveLayoutAsync(MainWindowLayout, JsonSerializer.Serialize(mainWindow));
        }

        private async Task LoadMainWindowLayout()
        {
            var mainWindow = await _layoutPersistence.LoadLayoutAsync(MainWindowLayout);

            if (!string.IsNullOrWhiteSpace(mainWindow))
            {
                var mainWindowParameters = JsonSerializer.Deserialize<MainWindowParameters>(mainWindow);

                if (mainWindowParameters != null)
                {
                    Application.Current.MainWindow.WindowState = mainWindowParameters.WindowState;
                    Application.Current.MainWindow.Width = mainWindowParameters.Width;
                    Application.Current.MainWindow.Height = mainWindowParameters.Height;
                    Application.Current.MainWindow.Top = mainWindowParameters.Top;
                    Application.Current.MainWindow.Left = mainWindowParameters.Left;
                }
            }
        }

        private async Task SaveModulesAsync()
        {
            var moduleIds = new Dictionary<string, string>();

            foreach (var pane in _xamDockManager.GetPanes(PaneNavigationOrder.VisibleOrder))
            {
                if (pane is WebContentPane webContentPane)
                {
                    var moduleId = webContentPane.WebContent?.ModuleInstance?.Manifest.Id;
                    if (!string.IsNullOrWhiteSpace(moduleId))
                    {
                        moduleIds.Add(pane.SerializationId, moduleId);
                    }
                }
            }

            await _layoutPersistence.SaveLayoutAsync(Modules, JsonSerializer.Serialize(moduleIds));         
        }

        private async Task LoadModulesAsync()
        {
            var modulesLayout = await _layoutPersistence.LoadLayoutAsync(Modules);

            if (!string.IsNullOrWhiteSpace(modulesLayout))
            {
                var moduleStates = JsonSerializer.Deserialize<Dictionary<string, string>>(modulesLayout);

                if (moduleStates != null)
                {
                    var moduleTasks = new List<Task>();

                    foreach (var moduleId in moduleStates.Keys)
                    {
                        var tcs = new TaskCompletionSource<bool>();
                        _moduleReadyTasks[moduleId] = tcs;

                        await _moduleLoader.StartModule(new StartRequest(moduleStates[moduleId], new Dictionary<string, string>
                    {
                        { SerializationId, moduleId }
                    }));

                        moduleTasks.Add(tcs.Task);
                    }

                    await Task.WhenAll(moduleTasks);
                }
            }
        }

        private async Task SaveDockManagerLayoutAsync()
        {
            var layout = _xamDockManager.SaveLayout();
            await _layoutPersistence.SaveLayoutAsync(DockmanagerLayout, layout);
        }

        private async Task LoadDockManagerLayout()
        {
            var layout = await _layoutPersistence.LoadLayoutAsync(DockmanagerLayout);

            if (!string.IsNullOrWhiteSpace(layout))
            {
                _xamDockManager.LoadLayout(layout);
            }
        }

        public void SetApplicationEnabledState(bool isEnabled)
        {
            Application.Current.MainWindow.IsEnabled = isEnabled;

            foreach (var pane in _xamDockManager.GetPanes(PaneNavigationOrder.VisibleOrder))
            {
                pane.IsEnabled = isEnabled;
            }
        }
    }
}
