using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UciEngineHost.Models {
    public class UciOption {

        public string Name { get; set; }
        public string Type { get; set; }
        public string Default { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }

        public List<string>? Options { get; set; }
    }
}
