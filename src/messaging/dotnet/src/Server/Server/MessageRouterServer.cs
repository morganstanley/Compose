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
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Messaging.Exceptions;
using MorganStanley.ComposeUI.Messaging.Instrumentation;
using MorganStanley.ComposeUI.Messaging.Protocol;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;
using MorganStanley.ComposeUI.Messaging.Server.Abstractions;

namespace MorganStanley.ComposeUI.Messaging.Server;

internal class MessageRouterServer : IMessageRouterServer
{
    public MessageRouterServer(
        MessageRouterServerDependencies dependencies,
        ILogger<MessageRouterServer>? logger = null)
    {
        _accessTokenValidator = dependencies.AccessTokenValidator;
        _logger = logger ?? NullLogger<MessageRouterServer>.Instance;
    }

    public async ValueTask DisposeAsync()
    {
        _stopTokenSource.Cancel();

        await Task.WhenAll(_clients.Values.Select(client => client.Connection.CloseAsync().AsTask()));
        await Task.WhenAll(_tasks);
    }

    public ValueTask ClientConnected(IClientConnection connection)
    {
        var client = new Client(connection);
        _connectionToClient[connection] = client;

        _logger.LogInformation("Client '{ClientId}' connected", client.ClientId);

        _tasks.Add(
            Task.Factory.StartNew(
                () => ProcessMessages(client, _stopTokenSource.Token),
                TaskCreationOptions.RunContinuationsAsynchronously));

        return default;
    }

    public ValueTask ClientDisconnected(IClientConnection connection)
    {
        if (!_connectionToClient.TryRemove(connection, out var client)
            || !_clients.TryRemove(client.ClientId, out _))
        {
            return default;
        }

        _logger.LogInformation("Client '{ClientId}' disconnected", client.ClientId);

        client.StopTokenSource.Cancel();

        // TODO: Clean up leftover junk like topic subscriptions
        return new ValueTask(client.StopTaskSource.Task);
    }

    private readonly ConcurrentDictionary<string, Client> _clients = new();
    private readonly ILogger<MessageRouterServer> _logger;
    private readonly ConcurrentDictionary<string, ServiceInvocation> _serviceInvocations = new();
    private readonly ConcurrentDictionary<string, string> _serviceRegistrations = new();
    private readonly CancellationTokenSource _stopTokenSource = new();
    private readonly HashSet<Task> _tasks = new(); // Tasks that must be awaited on DisposeAsync
    private readonly ConcurrentDictionary<string, Topic> _topics = new();
    private readonly ConcurrentDictionary<IClientConnection, Client> _connectionToClient = new();
    private readonly IAccessTokenValidator? _accessTokenValidator;

    private async Task HandleConnectRequest(
        Client client,
        ConnectRequest message,
        CancellationToken cancellationToken)
    {
        OnRequestStart(message);

        try
        {
            if (_accessTokenValidator != null)
            {
                await _accessTokenValidator.Validate(client.ClientId, message.AccessToken);
            }

            await client.Connection.SendAsync(
                new ConnectResponse
                {
                    ClientId = client.ClientId
                },
                CancellationToken.None);

            _clients.TryAdd(client.ClientId, client);
            OnRequestStop(message);
        }
        catch (Exception e)
        {
            try
            {
                await client.Connection.SendAsync(
                    new ConnectResponse
                    {
                        Error = new Error(e),
                    },
                    CancellationToken.None);
            }
            finally
            {
                OnRequestStop(message, e);
            }
        }
    }

    private async Task HandleInvokeRequest(
        Client client,
        InvokeRequest message,
        CancellationToken cancellationToken)
    {
        OnRequestStart(message);

        try
        {
            Client? serviceClient = null;

            if (!_serviceRegistrations.TryGetValue(message.Endpoint, out var serviceClientId)
                || !_clients.TryGetValue(serviceClientId, out serviceClient))
            {
                throw ThrowHelper.UnknownEndpoint(message.Endpoint);
            }

            var invocation = new ServiceInvocation(
                message,
                Guid.NewGuid().ToString(),
                client.ClientId,
                serviceClient.ClientId);

            if (!_serviceInvocations.TryAdd(invocation.ServiceRequestId, invocation))
                throw ThrowHelper.DuplicateRequestId();

            await serviceClient.Connection.SendAsync(
                new InvokeRequest
                {
                    RequestId = invocation.ServiceRequestId,
                    Endpoint = message.Endpoint,
                    Payload = message.Payload,
                    CorrelationId = message.CorrelationId,
                },
                cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "An exception of type {ExceptionClassName} was thrown while processing an invocation for service '{Endpoint}'.",
                e.GetType().FullName,
                message.Endpoint);

            try
            {
                await client.Connection.SendAsync(
                    new InvokeResponse
                    {
                        RequestId = message.RequestId,
                        Error = new Error(e),
                    },
                    CancellationToken.None);
            }
            finally
            {
                OnRequestStop(message, e);
            }
        }
    }

    private async Task HandleInvokeResponse(
        Client client,
        InvokeResponse message,
        CancellationToken cancellationToken)
    {
        if (!_serviceInvocations.TryRemove(message.RequestId, out var invocation))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("ServiceInvocation could not be retrieved. RequestId: {0}", message.RequestId);
            }
            
            return;
        }

        try
        {
            if (!_clients.TryGetValue(invocation.CallerClientId, out var caller))
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("Client: {0}, could nt be retrieved when handling invoke response. Invocation: {1}, RequestId: {2}", invocation.CallerClientId, invocation, message.RequestId);
                }
                
                return;
            }

            var response = new InvokeResponse
            {
                RequestId = invocation.Request.RequestId,
                Payload = message.Payload,
                Error = message.Error,
            };

            await caller.Connection.SendAsync(response, CancellationToken.None);
            OnRequestStop(invocation.Request);
        }
        catch (Exception e)
        {
            OnRequestStop(invocation.Request, e);

            throw;
        }
    }

    private async Task HandlePublishMessage(
        Client client,
        PublishMessage message,
        CancellationToken cancellationToken)
    {
        OnRequestStart(message);

        try
        {
            if (string.IsNullOrWhiteSpace(message.Topic))
                return;

            var topic = _topics.GetOrAdd(message.Topic, topicName => new Topic(topicName, ImmutableHashSet<string>.Empty));

            var outgoingMessage = new Protocol.Messages.TopicMessage
            {
                Topic = message.Topic,
                Payload = message.Payload,
                SourceId = client.ClientId,
                CorrelationId = message.CorrelationId,
            };

            await Task.WhenAll(
                topic.Subscribers.Select(
                    async subscriberId =>
                    {
                        if (_clients.TryGetValue(subscriberId, out var subscriber))
                            await subscriber.Connection.SendAsync(outgoingMessage, cancellationToken);
                    }));

            await client.Connection.SendAsync(
                new PublishResponse
                {
                    RequestId = message.RequestId
                },
                CancellationToken.None);

            OnRequestStop(message);
        }
        catch (Exception exception)
        {
            try
            {
                await client.Connection.SendAsync(
                    new PublishResponse
                    {
                        RequestId = message.RequestId,
                        Error = new Error(exception)
                    },
                    CancellationToken.None);
            }
            finally
            {
                OnRequestStop(message, exception);
            }
            
            throw;
        }
    }

    private async Task HandleRegisterServiceRequest(
        Client client,
        RegisterServiceRequest message,
        CancellationToken cancellationToken)
    {
        OnRequestStart(message);

        try
        {
            Endpoint.Validate(message.Endpoint);

            if (!_serviceRegistrations.TryAdd(message.Endpoint, client.ClientId))
                throw ThrowHelper.DuplicateEndpoint(message.Endpoint);

            await client.Connection.SendAsync(
                new RegisterServiceResponse
                {
                    RequestId = message.RequestId,
                },
                CancellationToken.None);

            OnRequestStop(message);
        }
        catch (Exception e)
        {
            try
            {
                await client.Connection.SendAsync(
                    new RegisterServiceResponse
                    {
                        RequestId = message.RequestId,
                        Error = new Error(e),
                    },
                    CancellationToken.None);
            }
            finally
            {
                OnRequestStop(message, e);
            }
        }
    }

    private async Task HandleSubscribeMessage(
        Client client,
        SubscribeMessage message,
        CancellationToken cancellationToken)
    {
        OnRequestStart(message);

        try
        {
            if (!Protocol.Topic.IsValidTopicName(message.Topic))
                return;

            var topic = _topics.AddOrUpdate(
                message.Topic,
                // ReSharper disable once VariableHidesOuterVariable
                static (topicName, client) => new Topic(topicName, ImmutableHashSet<string>.Empty.Add(client.ClientId)),
                // ReSharper disable once VariableHidesOuterVariable
                static (topicName, topic, client) =>
                {
                    topic.Subscribers = topic.Subscribers.Add(client.ClientId);

                    return topic;
                },
                client);

            await client.Connection.SendAsync(
                new SubscribeResponse
                {
                    RequestId = message.RequestId
                },
                CancellationToken.None);

            OnRequestStop(message);
        }
        catch (Exception exception)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Exception thrown while handling subscription message: {0}.", exception);
            }
            
            try
            {
                await client.Connection.SendAsync(
                    new SubscribeResponse
                    {
                        RequestId = message.RequestId,
                        Error = new Error(exception)
                    },
                    CancellationToken.None);
            }
            finally
            {
                OnRequestStop(message);
            }
        }
    }

    private async Task HandleUnregisterServiceRequest(
        Client client,
        UnregisterServiceRequest request,
        CancellationToken cancellationToken)
    {
        OnRequestStart(request);
        
        _serviceRegistrations.TryRemove(new KeyValuePair<string, string>(request.Endpoint, client.ClientId));

        try
        {
            await client.Connection.SendAsync(
                new UnregisterServiceResponse
                {
                    RequestId = request.RequestId,
                },
                CancellationToken.None);
        }
        finally
        {
            OnRequestStop(request);
        }
    }

    private async Task HandleUnsubscribeMessage(
        Client client,
        UnsubscribeMessage message,
        CancellationToken cancellationToken)
    {
        OnRequestStart(message);

        try
        {
            if (string.IsNullOrWhiteSpace(message.Topic))
                return;

            var topic = _topics.AddOrUpdate(
                message.Topic,
                // ReSharper disable once VariableHidesOuterVariable
                static (topicName, client) => new Topic(topicName, ImmutableHashSet<string>.Empty),
                // ReSharper disable once VariableHidesOuterVariable
                static (topicName, topic, client) =>
                {
                    topic.Subscribers = topic.Subscribers.Remove(client.ClientId);

                    return topic;
                },
                client);
            
            await client.Connection.SendAsync(
                new UnsubscribeResponse
                {
                    RequestId = message.RequestId
                }, 
                CancellationToken.None);
            
            OnRequestStop(message);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Exception thrown while handling unsubscribe message...");

            try
            {
                await client.Connection.SendAsync(
                    new UnsubscribeResponse
                    {
                        RequestId= message.RequestId,
                        Error = new Error(exception)
                    }, CancellationToken.None);
            }
            finally
            {
                OnRequestStop(message);
            }
        }
    }

    private async Task ProcessMessages(Client client, CancellationToken cancellationToken)
    {
        try
        {
            while (!client.StopTokenSource.IsCancellationRequested)
            {
                var message = await client.Connection.ReceiveAsync(cancellationToken);
                OnMessageReceived(message);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        "Message '{MessageType}' received from client '{ClientId}'",
                        message.Type,
                        client.ClientId);
                }

                try
                {
                    switch (message.Type)
                    {
                        case MessageType.Connect:
                            await HandleConnectRequest(client, (ConnectRequest) message, cancellationToken);

                            break;

                        case MessageType.Subscribe:
                            await HandleSubscribeMessage(client, (SubscribeMessage) message, cancellationToken);

                            break;

                        case MessageType.Unsubscribe:
                            await HandleUnsubscribeMessage(client, (UnsubscribeMessage) message, cancellationToken);

                            break;

                        case MessageType.Publish:
                            await HandlePublishMessage(client, (PublishMessage) message, cancellationToken);

                            break;

                        case MessageType.Invoke:
                            await HandleInvokeRequest(client, (InvokeRequest) message, cancellationToken);

                            break;

                        case MessageType.InvokeResponse:
                            await HandleInvokeResponse(client, (InvokeResponse) message, cancellationToken);

                            break;

                        case MessageType.RegisterService:
                            await HandleRegisterServiceRequest(
                                client,
                                (RegisterServiceRequest) message,
                                cancellationToken);

                            break;

                        case MessageType.UnregisterService:
                            await HandleUnregisterServiceRequest(
                                client,
                                (UnregisterServiceRequest) message,
                                cancellationToken);

                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        e,
                        "Exception thrown while processing a message from client '{ClientId}': {ExceptionMessage}",
                        client.ClientId,
                        e.Message);
                }
            }
        }
        catch (MessageRouterException e) when (e.Name is MessageRouterErrors.ConnectionClosed
                                                   or MessageRouterErrors.ConnectionAborted)
        {
            client.StopTokenSource.Cancel();
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Unhandled exception while processing messages from client '{ClientId}': {ExceptionMessage}",
                client.ClientId,
                e.Message);

            client.StopTokenSource.Cancel();
        }

        client.StopTaskSource.TrySetResult();
    }

    private void OnMessageReceived(Message message)
    {
        if (MessageRouterDiagnosticSource.Log.IsEnabled(MessageRouterEventTypes.MessageReceived))
        {
            MessageRouterDiagnosticSource.Log.Write(
                MessageRouterEventTypes.MessageReceived,
                new MessageRouterEvent(this, MessageRouterEventTypes.MessageReceived, message));
        }
    }

    private void OnMessageSent(Message message)
    {
        if (MessageRouterDiagnosticSource.Log.IsEnabled(MessageRouterEventTypes.MessageSent))
        {
            MessageRouterDiagnosticSource.Log.Write(
                MessageRouterEventTypes.MessageSent,
                new MessageRouterEvent(this, MessageRouterEventTypes.MessageSent, message));
        }
    }

    private void OnRequestStart(Message message)
    {
        if (MessageRouterDiagnosticSource.Log.IsEnabled(MessageRouterEventTypes.RequestStart))
        {
            MessageRouterDiagnosticSource.Log.Write(
                MessageRouterEventTypes.RequestStart,
                new MessageRouterEvent(this, MessageRouterEventTypes.RequestStart, message));
        }
    }

    private void OnRequestStop(Message message, Exception? exception = null)
    {
        if (MessageRouterDiagnosticSource.Log.IsEnabled(MessageRouterEventTypes.RequestStop))
        {
            MessageRouterDiagnosticSource.Log.Write(
                MessageRouterEventTypes.RequestStop,
                new MessageRouterEvent(this, MessageRouterEventTypes.RequestStop, message));
        }
    }

    private class Client
    {
        public Client(IClientConnection connection)
        {
            Connection = connection;
        }

        public IClientConnection Connection { get; }
        public CancellationTokenSource StopTokenSource { get; } = new();
        public TaskCompletionSource StopTaskSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string ClientId { get; } = Guid.NewGuid().ToString("N");
    }

    private class Topic
    {
        public Topic(string name, ImmutableHashSet<string> subscribers)
        {
            Name = name;
            Subscribers = subscribers;
        }

        public string Name { get; }

        public ImmutableHashSet<string> Subscribers { get; set; }
    }

    private class ServiceInvocation
    {
        public ServiceInvocation(
            InvokeRequest request,
            string serviceRequestId,
            string callerClientId,
            string serviceClientId)
        {
            Request = request;
            CallerClientId = callerClientId;
            ServiceClientId = serviceClientId;
            ServiceRequestId = serviceRequestId;
        }

        public InvokeRequest Request { get; }
        public string ServiceRequestId { get; }
        public string CallerClientId { get; }
        public string ServiceClientId { get; }
    }
}