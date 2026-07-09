using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace SIMS_Assignment.Storage
{
    public class JsonStorage<T>
    {
        private readonly string _filePath;
        private readonly object _lock = new();

        public JsonStorage(IWebHostEnvironment env, string fileName)
        {
            var dataDir = Path.Combine(env.ContentRootPath, "DataStorage");
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);
            _filePath = Path.Combine(dataDir, fileName);
        }

        public List<T> Load()
        {
            lock (_lock)
            {
                if (!File.Exists(_filePath))
                    return new List<T>();
                try
                {
                    var json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading from {_filePath}: {ex.Message}");
                    return new List<T>();
                }
            }
        }

        public void Save(List<T> data)
        {
            lock (_lock)
            {
                try
                {
                    var directory = Path.GetDirectoryName(_filePath);
                    if (directory != null && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var json = JsonSerializer.Serialize(data, options);
                    File.WriteAllText(_filePath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving to {_filePath}: {ex.Message}");
                }
            }
        }
    }
}
