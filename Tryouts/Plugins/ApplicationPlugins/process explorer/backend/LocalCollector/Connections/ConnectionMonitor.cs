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

namespace LocalCollector.Connections;

public class ConnectionMonitor : IConnectionMonitor
{
    private ConnectionMonitorInfo Data { get; } = new();
    ConnectionMonitorInfo IConnectionMonitor.Data
    {
        get => Data;
    }

    private readonly object _locker = new();

    public event EventHandler<ConnectionInfo>? _connectionStatusChanged;

    public ConnectionMonitor(SynchronizedCollection<ConnectionInfo> connections)
    {
        Data.Connections = connections;
    }

    public void AddConnection(ConnectionInfo connectionInfo)
    {
        lock (_locker)
        {
            Data.Connections.Add(connectionInfo);
        }
    }

    public void RemoveConnection(ConnectionInfo connectionInfo)
    {
        lock (_locker)
        {
            var element = Data.Connections
                .FirstOrDefault(x => x.Id == connectionInfo.Id);

            if (element == null)
            {
                return;
            }

            var index = Data.Connections.IndexOf(element);
            Data.Connections.RemoveAt(index);
        }
    }

    public void AddConnections(SynchronizedCollection<ConnectionInfo> connections)
    {
        lock (_locker)
        {
            foreach (var conn in connections)
            {
                var element = Data.Connections
                    .FirstOrDefault(item => item.Id == conn.Id);

                if (element == null)
                {
                    continue;
                }

                var index = Data.Connections.IndexOf(element);
                if (index != -1)
                {
                    Data.Connections[index] = conn;
                }
                else
                {
                    Data.Connections.Add(conn);
                }
            }
        }
    }

    public void UpdateConnection(Guid connId, ConnectionStatus status)
    {
        if (Data.Connections.Count <= 0)
        {
            return;
        }

        lock (_locker)
        {
            var conn = Data.Connections
                .FirstOrDefault(c => c.Id == connId);

            if (conn == null || conn.Status == status.ToStringCached())
            {
                return;
            }

            conn.Status = status.ToStringCached();
            _connectionStatusChanged?.Invoke(this, conn);
        }
    }
}
