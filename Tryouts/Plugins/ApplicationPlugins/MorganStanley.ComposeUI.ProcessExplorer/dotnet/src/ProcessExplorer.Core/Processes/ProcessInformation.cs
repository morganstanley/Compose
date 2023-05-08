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

using System.ComponentModel;
using System.Diagnostics;
using ProcessExplorer.Abstractions.Extensions;
using ProcessExplorer.Abstractions.Processes;

namespace ProcessExplorer.Core.Processes;

[Serializable]
public class ProcessInformation
{
    private ProcessInfoData _processInfo;
    public ProcessInfoData ProcessInfo
    {
        get
        {
            return _processInfo;
        }
        private set
        {
            _processInfo = value;
        }
    }

    public ProcessInformation(Process process)
    {
        _processInfo = new()
        {
            ProcessId = process.Id,
            ProcessName = process.ProcessName,
        };
    }

    internal static ProcessInformation GetProcessInfoWithCalculatedData(Process process, IProcessInfoMonitor processMonitor)
    {
        var processInformation = new ProcessInformation(process);
        SetProcessInfoData(processInformation, processMonitor);
        return processInformation;
    }

    internal static void SetProcessInfoData(ProcessInformation processInfo, IProcessInfoMonitor manager)
    {
        try
        {
            var process = Process.GetProcessById(processInfo.ProcessInfo.ProcessId);
            process.Refresh();

            processInfo._processInfo.PriorityLevel = process.BasePriority;
            processInfo._processInfo.PrivateMemoryUsage = process.PrivateMemorySize64;
            processInfo._processInfo.ParentId = manager.GetParentId(process.Id, process.ProcessName);
            processInfo._processInfo.MemoryUsage = manager.GetMemoryUsage(process.Id, process.ProcessName);
            processInfo._processInfo.ProcessorUsage = manager.GetCpuUsage(process.Id, process.ProcessName);
            processInfo._processInfo.StartTime = process.StartTime.ToString("yyyy.MM.dd. hh:mm:s");
            processInfo._processInfo.ProcessorUsageTime = process.TotalProcessorTime;
            processInfo._processInfo.PhysicalMemoryUsageBit = process.WorkingSet64;
            processInfo._processInfo.ProcessPriorityClass = process.PriorityClass.ToStringCached();
            processInfo._processInfo.VirtualMemorySize = process.VirtualMemorySize64;

            var array = new ProcessThread[process.Threads.Count];
            process.Threads.CopyTo(array, 0);

            processInfo._processInfo.Threads = array;

            processInfo._processInfo.ProcessStatus =
                process.HasExited == false ?
                    ProcessStatus.Running.ToStringCached()
                    : ProcessStatus.Stopped.ToStringCached();
        }
        catch (Exception exception)
        {
            if (exception is Win32Exception || exception is NotSupportedException) return;
            processInfo._processInfo.ProcessStatus = ProcessStatus.Terminated.ToStringCached();
        }
    }
}
