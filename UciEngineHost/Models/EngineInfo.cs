using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UciEngineHost.Models {
    public class EngineInfo {
        public int MultiPv { get; set; }
        public int Depth { get; set; }
        public int Seldepth { get; set; }
        public bool WhiteToMove { get; set; }
        public string EvalString {
            get {
                int normalizingFactor = WhiteToMove ? 1 : -1;
                if (ScoreMate.HasValue) {
                    int distToMate = ScoreMate.Value * normalizingFactor;
                    return "#" + distToMate.ToString();

                } else if (ScoreCp.HasValue) {
                    decimal score = ((decimal)ScoreCp.Value * normalizingFactor) / 100;
                    return score.ToString("0.00");

                } else {
                    return "";
                }
            }
        }
        public int? ScoreCp { get; set; }
        public int? ScoreMate { get; set; }
        public long Nodes { get; set; }
        public long NodesPerSecond { get; set; }
        public int TableBaseHits { get; set; }
        public int HashFull { get; set; }
        public long ElapsedMs { get; set; }
        public string Variation { get; set; }

        public int WdlWin { get; set; }
        public int WdlDraw { get; set; }
        public int WdlLoss { get; set; }
    }
}
