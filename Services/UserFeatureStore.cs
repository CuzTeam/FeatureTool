using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FeatureTool.Services
{
    public sealed class UserFeatureStore
    {
        private static readonly string s_path = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            ".feature-ids.json");

        private static readonly JsonSerializerOptions s_jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        private Dictionary<string, UserFeature> _entries = new();

        public void Load()
        {
            if (!File.Exists(s_path))
            {
                _entries = new Dictionary<string, UserFeature>();
                return;
            }

            try
            {
                var json = File.ReadAllText(s_path);
                _entries = JsonSerializer.Deserialize<Dictionary<string, UserFeature>>(json, s_jsonOpts)
                           ?? new Dictionary<string, UserFeature>();
            }
            catch
            {
                _entries = new Dictionary<string, UserFeature>();
            }
        }

        public bool TryGetNote(uint featureId, out string note)
        {
            note = string.Empty;
            return _entries.TryGetValue(featureId.ToString(), out var e) && !string.IsNullOrWhiteSpace(e.Note)
                && (note = e.Note!) == note;
        }

        public void Set(uint featureId, string? note)
        {
            var key = featureId.ToString();
            if (string.IsNullOrWhiteSpace(note))
            {
                _entries.Remove(key);
            }
            else
            {
                _entries[key] = new UserFeature { Id = featureId, Note = note };
            }
            Save();
        }

        public bool Contains(uint featureId) => _entries.ContainsKey(featureId.ToString());

        private void Save()
        {
            var dir = Path.GetDirectoryName(s_path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var json = JsonSerializer.Serialize(_entries, s_jsonOpts);
            File.WriteAllText(s_path, json);
        }

        private sealed class UserFeature
        {
            public uint Id { get; set; }
            public string? Note { get; set; }
        }
    }
}
