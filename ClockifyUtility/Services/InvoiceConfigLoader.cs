using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ClockifyUtility.Models;

namespace ClockifyUtility.Services
{
    public class InvoiceConfigLoader
    {
        public class LoadedConfigResult
        {
            public required string FilePath { get; set; }
            public InvoiceConfig? Config { get; set; }
            public required List<string> Errors { get; set; }
        }

        public static List<LoadedConfigResult> LoadAllConfigs(string directory)
        {
            var results = new List<LoadedConfigResult>();
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Invoice config directory not found: {directory}");

            var files = Directory.GetFiles(directory, "*.json");
            foreach (var file in files)
            {
                LoadedConfigResult result = new LoadedConfigResult { FilePath = file, Errors = new List<string>() };
                try
                {
                    var json = File.ReadAllText(file);
                    var config = JsonConvert.DeserializeObject<InvoiceConfig>(json);
                    var errors = InvoiceConfigValidator.Validate(config!);
                    result.Config = errors.Count == 0 ? config : null;
                    result.Errors = errors;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Exception: {ex.Message}");
                }
                results.Add(result);
            }
            return results;
        }
    }
}
