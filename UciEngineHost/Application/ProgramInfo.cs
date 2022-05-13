using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UciEngineHost {
    internal static class ProgramInfo {

        internal static Version? AssemblyVersion {
            get {
                return Assembly.GetEntryAssembly()?.GetName().Version;
            }
        }

        internal static string AssemblyGuid {
            get {
                object[]? attributes = Assembly.GetEntryAssembly()?.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
                if (attributes == null || attributes.Length == 0) {
                    return string.Empty;
                }
                return ((System.Runtime.InteropServices.GuidAttribute)attributes[0]).Value;
            }
        }

        internal static string AssemblyTitle {
            get {
                var assy = Assembly.GetEntryAssembly();
                if (assy == null) {
                    return "UciEngineHost";
                }
                object[] attributes = assy.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0) {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "") {
                        return titleAttribute.Title;
                    }
                }
                return Path.GetFileNameWithoutExtension(assy.Location);
            }
        }
    }
}
