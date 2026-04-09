using System;
using System.IO;
using DofusRetroManager.Models;
using Newtonsoft.Json;

namespace DofusRetroManager.Services
{
    public class ConfigService
    {
        private readonly string _configPath;

        public ConfigService()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            _configPath = Path.Combine(appDir, "config.json");
        }

        public string ConfigPath => _configPath;

        public AppConfig Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigService] Load error: {ex.Message}");
            }
            return new AppConfig();
        }

        public void Save(AppConfig config)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(_configPath, json);
        }
    }
}
