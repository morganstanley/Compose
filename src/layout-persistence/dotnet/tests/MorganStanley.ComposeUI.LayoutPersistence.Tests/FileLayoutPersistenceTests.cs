/*
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

using System.Text.Json;
using FluentAssertions;

namespace MorganStanley.ComposeUI.LayoutPersistence.Tests;

public class FileLayoutPersistenceTests : IDisposable
{
    private readonly string _testDirectory = "test_layouts";
    private readonly FileLayoutPersistence _persistence;

    public FileLayoutPersistenceTests()
    {
        _persistence = new FileLayoutPersistence($"file://{_testDirectory}");
    }

    [Fact]
    public async Task SaveLayout_ShouldCreateFile()
    {
        var layoutData = new LayoutData
        {
            Windows = new List<Window>
            {
                new() { Id = "1", X = 10, Y = 20, Width = 300, Height = 200 }
            }   
        };

        var serializedLayout = JsonSerializer.Serialize(layoutData);
        var layoutName = "TestLayout";

        await _persistence.SaveLayoutAsync(layoutName, serializedLayout);
        var filePath = Path.Combine(_testDirectory, "TestLayout.layout");

        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task LoadLayout_ShouldReturnCorrectContent()
    {
        var layoutData = new LayoutData
        {
            Windows = new List<Window>
            {
                new() { Id = "1", X = 10, Y = 20, Width = 300, Height = 200 }
            }
        };

        var serializedLayout = JsonSerializer.Serialize(layoutData);
        var layoutName = "TestLayout";

        await _persistence.SaveLayoutAsync(layoutName, serializedLayout);
        var loadedData = await _persistence.LoadLayoutAsync(layoutName);

        loadedData.Should().Be(serializedLayout);
    }

    [Fact]
    public async Task SaveLayout_WithInvalidLayoutName_ShouldThrowArgumentException()
    {
        var layoutData = new LayoutData
        {
            Windows = new List<Window>
            {
                new() { Id = "1", X = 10, Y = 20, Width = 300, Height = 200 }
            }
        };

        var serializedLayout = JsonSerializer.Serialize(layoutData);
        var layoutName = "../TestLayout";

        Func<Task> act = async () => await _persistence.SaveLayoutAsync(layoutName, serializedLayout);
        await act.Should().ThrowAsync<ArgumentException>()
           .WithMessage("Invalid layoutName argument. File cannot be saved outside of the base directory. *");
    }

    [Fact]
    public async Task LoadLayout_NonExistentFile_ShouldThrowFileNotFoundException()
    {
        Func<Task> act = async () => await _persistence.LoadLayoutAsync("NonExistentLayout");

        await act.Should().ThrowAsync<FileNotFoundException>()
           .WithMessage("Layout file not found: *");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
