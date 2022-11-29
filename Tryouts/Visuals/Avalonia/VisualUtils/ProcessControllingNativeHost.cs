﻿/// ********************************************************************************************************
///
/// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License").
/// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
/// See the NOTICE file distributed with this work for additional information regarding copyright ownership.
/// Unless required by applicable law or agreed to in writing, software distributed under the License
/// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
/// 
/// ********************************************************************************************************

using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MorganStanley.ComposeUI.Tryouts.Visuals.Avalonia.VisualUtils
{
    using static Win32Exports;

    internal class ProcessControllingNativeHost : NativeHostBase
    {
        private Process _process;

        internal override IntPtr WindowHandle => _process.MainWindowHandle;

        public ProcessControllingNativeHost(Process process)
        {
            _process = process;
            _process.EnableRaisingEvents = true;

            _process.Exited += OnProcessExited;
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            SetParent(WindowHandle, IntPtr.Zero);

        }

        protected override void OnRootWindowClosed()
        {
            this.DestroyProcess();
        }


        public void DestroyProcess()
        {
            _process?.Kill(true);

            _process?.WaitForExit();

            _process?.Dispose();

            _process = null;
        }
    }
}
