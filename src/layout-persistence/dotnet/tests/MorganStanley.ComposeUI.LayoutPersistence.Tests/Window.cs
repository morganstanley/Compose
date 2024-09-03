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

namespace MorganStanley.ComposeUI.LayoutPersistence.Tests;

public class Window
{
    public string Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is Window other)
        {
            return Id == other.Id &&
                   X == other.X &&
                   Y == other.Y &&
                   Width == other.Width &&
                   Height == other.Height;
        }
        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = 17;
            hashCode = hashCode * 31 + (Id?.GetHashCode() ?? 0);
            hashCode = hashCode * 31 + X;
            hashCode = hashCode * 31 + Y;
            hashCode = hashCode * 31 + Width;
            hashCode = hashCode * 31 + Height;
            return hashCode;
        }
    }
}
