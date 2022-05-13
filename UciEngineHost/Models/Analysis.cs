using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UciEngineHost.Models {
    internal class Analysis {

        public AnalysisRequest? Request { get; set; }

        public string? RequestedEngine { get; set; }

        public Dictionary<string, string>? Parameters { get; set; }

        public Func<EngineDefinition, EngineEval, Task>? OnDataReceived { get; set; }

        public Func<EngineDefinition, EngineEval, Task>? OnStop { get; set; }

    }
}
