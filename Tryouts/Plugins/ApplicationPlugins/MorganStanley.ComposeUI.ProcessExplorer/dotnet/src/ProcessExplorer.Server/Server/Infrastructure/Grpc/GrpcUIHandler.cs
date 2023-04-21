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

using System.Collections.Concurrent;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessExplorer.Abstractions.Entities.Connections;
using ProcessExplorer.Abstractions.Entities.Modules;
using ProcessExplorer.Abstractions.Entities.Registrations;
using ProcessExplorer.Abstractions.Infrastructure;
using ProcessExplorer.Abstractions.Infrastructure.Protos;
using ProcessExplorer.Abstractions.Logging;
using ProcessExplorer.Server.Logging;
using ProcessExplorer.Abstractions.Processes;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Server.Server.Helper;

namespace ProcessExplorer.Server.Server.Infrastructure.Grpc;

internal class GrpcUiHandler : IUiHandler
{
    private readonly object _uiHandlersLock = new();
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<Guid, IClientConnection<Message>> _uiHandlers;

    public GrpcUiHandler(
        ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _uiHandlers = new ();
    }
    
    public Task AddConnections(string assemblyId, IEnumerable<ConnectionInfo> connections)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any() || connections == null || !connections.Any()) return Task.CompletedTask;
        }

        try
        {
            var message = new Message()
            {
                Action = ActionType.AddConnectionListAction,
                AssemblyId = assemblyId,
                Connections = { connections.Select(conn => conn.DeriveProtoConnectionType()) }
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.AddConnectionsError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task AddProcess(ProcessInfoData process)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any() || process == null) return Task.CompletedTask;
        }

        try
        {
            var list = new List<Process>
            {
                process.DeriveProtoProcessType()
            };

            var message = new Message()
            {
                Action = ActionType.AddProcessAction,
                Processes = { list }
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.AddProcessError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public void AddClientConnection<T>(Guid id, IClientConnection<T> connection)
    {
        if (typeof(T) == typeof(Message))
        {
            lock (_uiHandlersLock)
            {
                if (!_uiHandlers.TryAdd(id, connection as IClientConnection<Message>)) _logger.UIHandlerConnectionAddingError(id.ToString());
            }
        }
    }

    public void RemoveClientConnection(Guid id)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.TryRemove(id, out _)) _logger.UIHandlerConnectionRemoveError(id.ToString());
        }
    }

    public Task AddProcesses(IEnumerable<ProcessInfoData> processes)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any() || processes == null || !processes.Any()) return Task.CompletedTask;
        }

        try
        {
            var message = new Message()
            {
                Action = ActionType.AddProcessListAction,
                Processes = { processes.Select(proc => proc.DeriveProtoProcessType()) }
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.AddProcessesError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task AddRuntimeInfo(string assemblyId, ProcessExplorer.Abstractions.Entities.ProcessInfoCollectorData dataObject)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any() || dataObject == null) return Task.CompletedTask;
        }

        try
        {
            var list = new List<ProcessInfoCollectorData>()
            {
                dataObject.DeriveProtoRuntimeInfoType()
            };

            var message = new Message()
            {
                Action = ActionType.AddRuntimeInfoAction,
                AssemblyId = assemblyId,
                RuntimeInfo = { list }
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.AddRuntimeError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task AddRuntimeInfo(IEnumerable<KeyValuePair<string, ProcessExplorer.Abstractions.Entities.ProcessInfoCollectorData>> runtimeInfo)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any() || !runtimeInfo.Any()) return Task.CompletedTask;
        }

        try
        {
            var message = new Message()
            {
                Action = ActionType.AddMultipleRuntimeInfoAction,
                MultipleRuntimeInfo = { runtimeInfo.DeriveProtoDictionaryType(ProtoConvertHelper.DeriveProtoRuntimeInfoType) }
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.AddRuntimesError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task TerminateProcess(int pid)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any()) return Task.CompletedTask;
        }

        try
        {
            var message = new Message()
            {
                Action = ActionType.RemoveProcessByIdAction,
                ProcessId = pid
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.RemoveProcessByIdError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task UpdateConnection(string assemblyId, ConnectionInfo connection)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any()) return Task.CompletedTask;
        }

        try
        {
            var list = new List<Connection>()
            {
                connection.DeriveProtoConnectionType()
            };

            var message = new Message()
            {
                Action = ActionType.UpdateConnectionAction,
                Connections = { list }
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.UpdateConnectionError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task UpdateEnvironmentVariables(string assemblyId, IEnumerable<KeyValuePair<string, string>> environmentVariables)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any() || !environmentVariables.Any()) return Task.CompletedTask;
        }

        try
        {
            var message = new Message()
            {
                Action = ActionType.UpdateEnvironmentVariablesAction,
                AssemblyId = assemblyId,
                EnvironmentVariables = { environmentVariables.DeriveProtoDictionaryType() }
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.UpdateEnvironmentVariablesError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task UpdateModules(string assemblyId, IEnumerable<ModuleInfo> modules)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any() || !modules.Any()) return Task.CompletedTask;
        }

        try
        {
            var message = new Message()
            {
                Action = ActionType.UpdateModulesAction,
                AssemblyId = assemblyId,
                Modules = { modules.Select(module => module.DeriveProtoModuleType()) }
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.UpdateModulesError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task UpdateProcess(ProcessInfoData process)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any()) return Task.CompletedTask;
        }

        try
        {
            var list = new List<Process>()
            {
                process.DeriveProtoProcessType()
            };

            var message = new Message()
            {
                Action = ActionType.UpdateProcessAction,
                Processes = { list }
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.UpdateProcessError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task UpdateRegistrations(string assemblyId, IEnumerable<RegistrationInfo> registrations)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any() || !registrations.Any()) return Task.CompletedTask;
        }

        try
        {
            var message = new Message()
            {
                Action = ActionType.UpdateRegistrationsAction,
                AssemblyId = assemblyId,
                Registrations = { registrations.Select(registration => registration.DeriveProtoRegistrationType()) }
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.UpdateRegistrationsError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task AddSubsystem(Guid subsystemId, SubsystemInfo subsystem)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any()) return Task.CompletedTask;
        }

        try
        {
            var map = new MapField<string, Subsystem>
            {
                { subsystemId.ToString(), subsystem.DeriveProtoSubsystemType() }
            };

            var message = new Message()
            {
                Action = ActionType.AddSubsystemAction,
                Subsystems = { map }
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.AddSubsystemsError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task AddSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any() || !subsystems.Any()) return Task.CompletedTask;
        }

        try
        {
            var message = new Message()
            {
                Action = ActionType.AddSubsystemsAction,
                Subsystems = { subsystems.DeriveProtoDictionaryType(ProtoConvertHelper.DeriveProtoSubsystemType) }
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.AddSubsystemsError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task UpdateSubsystemInfo(Guid subsystemId, SubsystemInfo subsystem)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any()) return Task.CompletedTask;
        }

        try
        {
            var map = new MapField<string, Subsystem>()
            {
                { subsystemId.ToString(), subsystem.DeriveProtoSubsystemType() }
            };

            var message = new Message()
            {
                Action = ActionType.UpdateSubsystemAction,
                Subsystems = { map }
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.UpdateSubsystemError(exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task UpdateProcessStatus(KeyValuePair<int, Status> process)
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any()) return Task.CompletedTask;
        }

        try
        {
            var map = new MapField<int, string>
            {
                { process.Key, process.Value.ToString() }
            };

            var message = new Message()
            {
                Action = ActionType.UpdateProcessStatusAction,
                ProcessStatusChanges = { map }
            };

            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch (Exception exception)
        {
            _logger.UpdateProcessStatusError(process.Key, exception, exception);
        }

        return Task.CompletedTask;
    }

    public Task SubscriptionIsAliveUpdate()
    {
        lock (_uiHandlersLock)
        {
            if (!_uiHandlers.Any()) return Task.CompletedTask;
        }

        var message = new Message()
        {
            Action = ActionType.SubscriptionAliveAction
        };

        try
        {
            return UpdateInfoOnUI(handler => handler.SendMessage(message));
        }
        catch(Exception exception)
        {
            _logger.SubscriptionError(exception, exception);
        }

        return Task.CompletedTask;
    }

    private IEnumerable<IClientConnection<Message>> CreateCopyOfClients()
    {
        lock (_uiHandlersLock)
        {
            return _uiHandlers.Select(kvp => kvp.Value);
        }
    }

    private Task UpdateInfoOnUI(Func<IClientConnection<Message>, Task> handlerAction)
    {
        try
        {
            return Task.WhenAll(CreateCopyOfClients().Select(handlerAction));
        }
        catch (Exception exception)
        {
            _logger.UiInformationCannotBeUpdatedError(exception);
            return Task.CompletedTask;
        }
    }
}
