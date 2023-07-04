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

using Shell.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ShellTests
{
    public class ReadResourceTests
    {
        [Fact]
        public void ResourceNotAvailable()
        {
            var resource = ResourceReader.ReadResource("NotAvailableResource");

            Assert.Null(resource);
        }

        [Fact]
        public void ResourceCanBeRead()
        {
            var resource = ResourceReader.ReadResource(@"Shell.fdc3-iife-bundle.js");

            Assert.NotNull(resource);
        }
    }
}
