using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UciEngineHost.Models;

namespace UciEngineHost {
    public class Configuration {

        public Configuration() {
            this.Engines = new List<EngineDefinition>();
        }

        public List<EngineDefinition> Engines { get; private set; }

        public void Load() {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (File.Exists(path)) {
                var data = JsonSerializer.Deserialize<ConfigJsonFile>(File.ReadAllText(path));
                if (data != null) {
                    if (data.Engines != null) {
                        var engines = new List<EngineDefinition>();
                        bool hasSelected = false;
                        foreach (var engine in data.Engines) {
                            if (!string.IsNullOrEmpty(engine.Name) && !string.IsNullOrEmpty(engine.Path)) {
                                engine.Selected = !hasSelected && engine.Default.HasValue && engine.Default.Value;
                                if (engine.Selected) {
                                    hasSelected = true;
                                }
                                engines.Add(engine);
                            }
                        }
                        if (!hasSelected && engines.Count > 0) {
                            engines[0].Selected = true;
                        }
                        this.Engines = engines;
                    }
                }
            }
        }
    }
}
