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

using MorganStanley.ComposeUI.LayoutPersistence.Abstractions;

namespace MorganStanley.ComposeUI.LayoutPersistence;

public class FileLayoutPersistence : ILayoutPersistence<string>
{
    private readonly string _basePath;
    private readonly SemaphoreSlim _semaphore = new(1,1);

    public FileLayoutPersistence(string basePath)
    {
        _basePath = NormalizeFilePath(basePath);

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task SaveLayoutAsync(string layoutName, string layoutData, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(layoutName);

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            await File.WriteAllTextAsync(filePath, layoutData, cancellationToken);
        }
        finally 
        {
            _semaphore.Release();
        }
    }

    public async Task<string?> LoadLayoutAsync(string layoutName, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(layoutName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Layout file not found: {filePath}");
        }

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }
        finally
        { 
            _semaphore.Release(); 
        }
    }

    private string GetFilePath(string layoutName)
    {
        var combinedPath = Path.Combine(_basePath, $"{layoutName}.layout");
        var fullPath = Path.GetFullPath(combinedPath);

        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid layoutName argument. File cannot be saved outside of the base directory.", layoutName);
        }

        return fullPath;
    }

    private static string NormalizeFilePath(string path)
    {
        if (path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedPath = Uri.UnescapeDataString(path[7..]);
            return Path.GetFullPath(normalizedPath);
        }

        return Path.GetFullPath(path);
    }
}
