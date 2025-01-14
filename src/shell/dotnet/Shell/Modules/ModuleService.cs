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

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Web.WebView2.Wpf;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Modules;

internal sealed class ModuleService : IHostedService
{
    private readonly IModuleLoader _moduleLoader;
    private ConcurrentBag<IAsyncDisposable> _disposables = new();
    private readonly ILogger<ModuleService> _logger;

    public ModuleService(
        IModuleLoader moduleLoader, 
        ILogger<ModuleService>? logger = null)
    {
        _moduleLoader = moduleLoader;
        _logger = logger ?? NullLogger<ModuleService>.Instance;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var observable = _moduleLoader.LifetimeEvents.ToAsyncObservable();
        var subscription = await observable.SubscribeAsync(async lifetimeEvent =>
        {
            if (lifetimeEvent.EventType == LifetimeEventType.Started
                && lifetimeEvent.Instance.Manifest.ModuleType == ModuleType.Web)
            {
                await OnWebModuleStarted(lifetimeEvent);
            }
        });

        _disposables.Add(subscription);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var disposable in _disposables)
        {
            await disposable!.DisposeAsync();
        }
    }

    private Task OnWebModuleStarted(LifetimeEvent e)
    {
        var properties = e.Instance.GetProperties().OfType<WebStartupProperties>().FirstOrDefault();
        if (properties == null)
        {
            return Task.CompletedTask;
        }

        var webWindowOptions = e.Instance.GetProperties().OfType<WebWindowOptions>().FirstOrDefault();

        try
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                App.Current.CreateWebContent(
                e.Instance,
                    webWindowOptions ?? new WebWindowOptions
                    {
                        Url = properties.Url.ToString(),
                        IconUrl = properties.IconUrl?.ToString()
                    });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown when trying to create a web window: {ExceptionType}: {ExceptionMessage}", ex.GetType().FullName, ex.Message);
        }

        return Task.CompletedTask;
    }
}