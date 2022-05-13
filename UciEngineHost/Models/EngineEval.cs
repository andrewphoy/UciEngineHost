using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UciEngineHost.Models {
    public class EngineEval {

        public EngineEval() {
            Variations = new Dictionary<int, EngineInfo>();
        }

        public bool WhiteToMove { get; set; }

        /// <summary>
        /// The best move from the engine, UCI format (ex. e2e4)
        /// </summary>
        public string? BestMove { get; set; }

        /// <summary>
        /// A human readable evaluation string
        /// </summary>
        public string EvalString {
            get {
                if (Variations.ContainsKey(1)) {
                    return Variations[1].EvalString;
                } else {
                    return "";
                }
            }
        }

        public long Counter { get; set; }

        public int? Cp => Variations.ContainsKey(1) ? Variations[1].ScoreCp : null;
        public int? Mate => Variations.ContainsKey(1) ? Variations[1].ScoreMate : null;

        public Dictionary<int, EngineInfo> Variations { get; set; }
    }
}
