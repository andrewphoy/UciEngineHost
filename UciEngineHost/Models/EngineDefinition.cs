using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UciEngineHost.Models {
    public class EngineDefinition {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool? Default { get; set; }

        [JsonIgnore]
        public bool Selected { get; internal set; }

        [JsonIgnore]
        public bool IsReady { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, UciOption> Options { get; internal set; }

        [JsonIgnore]
        public string? ResolvedPath { get; internal set; }

        [JsonIgnore]
        public string? UciName { get; internal set; }

        [JsonIgnore]
        public string? UciAuthor { get; internal set; }
        
    }
}
