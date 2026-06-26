using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FeatureTool.Services
{
    public sealed class FeatureNameProvider
    {
        private Dictionary<string, FeatureEntry>? _map;

        public bool IsLoaded => _map != null;

        private static readonly JsonSerializerOptions s_jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public void Load(string culture = "en-US")
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", $"map.{culture}.json");
            if (!File.Exists(path))
            {
                _map = new Dictionary<string, FeatureEntry>();
                return;
            }

            var json = File.ReadAllText(path);
            _map = JsonSerializer.Deserialize<Dictionary<string, FeatureEntry>>(json, s_jsonOpts)
                   ?? new Dictionary<string, FeatureEntry>();
        }

        public string GetName(uint featureId)
        {
            var key = featureId.ToString();
            return _map != null && _map.TryGetValue(key, out var e) && !string.IsNullOrWhiteSpace(e.Name)
                ? e.Name
                : featureId.ToString();
        }

        private sealed class FeatureEntry
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}
