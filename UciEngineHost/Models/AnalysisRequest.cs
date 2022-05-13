using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UciEngineHost.Models {
    public class AnalysisRequest {

        public string? Session { get; set; }
        public int? Counter { get; set; }
        public bool? UciNewGame { get; set; }
        public bool VerboseEvaluation { get; set; }

        public string? Variant { get; set; }
        public int? Threads { get; set; }
        public int? HashSize { get; set; }
        public bool? StopRequested { get; set; }

        public string? Path { get; set; }
        public int? MaxDepth { get; set; }
        public int? MultiPv { get; set; }
        public int? Ply { get; set; }
        public bool? ThreadMode { get; set; }
        public string? InitialFen { get; set; }
        public string? CurrentFen { get; set; }
        public List<string>? Moves { get; set; }

    }
}
