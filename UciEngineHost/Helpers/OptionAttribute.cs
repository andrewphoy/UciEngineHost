using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace UciEngineHost.Helpers {
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal sealed class OptionAttribute : Attribute {

        public string Arg { get; }
        public object DefaultValue { get; }

        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool Required { get; set; }

        public OptionAttribute(string arg, object defaultValue) {
            this.Arg = arg; 
            this.DefaultValue = defaultValue;
            this.Required = false;
        }

        public override string ToString() {
            return this.Name ?? this.Arg ?? base.ToString() ?? "Option";
        }

        public static T Parse<T>(string[] args) {
            int i = 0;
            int cnt = args.Length;
            var data = new Dictionary<string, string>();
            string lastEntry = null;

            while (i < cnt) {
                if (args[i].StartsWith("-")) {
                    // it is the name of an arg
                    if ((i + 1) < cnt && !args[i + 1].StartsWith("-")) {
                        lastEntry = args[i].TrimStart(new char[] { '-' });
                        data[lastEntry] = args[i + 1];
                        i += 2;
                    } else {
                        lastEntry = args[i].TrimStart(new char[] { '-' });
                        data[lastEntry] = "true";
                        i += 1;
                    }
                } else {
                    // append to the previous arg
                    if (lastEntry != null) {
                        data[lastEntry] = data[lastEntry] + " " + args[i];
                    }
                    i += 1;
                }
            }

            T ret = Activator.CreateInstance<T>();

            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                var attr = property.GetCustomAttribute<OptionAttribute>();
                if (attr == null) {
                    continue;
                }

                string[] names = attr.Arg.Split(new char[] { '|' });
                bool handled = false;
                foreach (string name in names) {
                    if (data.TryGetValue(name, out string? overrideData)) {
                        if (property.PropertyType == typeof(bool)) {
                            handled = bool.TryParse(overrideData, out bool parsed);
                            property.SetValue(ret, parsed, null);
                        } else if (property.PropertyType == typeof(int)) {
                            handled = int.TryParse(overrideData, out int parsed);
                            property.SetValue(ret, parsed, null);
                        } else if (property.PropertyType == typeof(string)) {
                            handled = true;
                            property.SetValue(ret, overrideData, null);
                        } else if (property.PropertyType.IsEnum) {
                            handled = true;
                            overrideData = overrideData.ToLower();
                            property.SetValue(ret, Enum.Parse(property.PropertyType, overrideData), null);
                        }
                    }
                }

                if (!handled && attr.Required) {
                    throw new ArgumentNullException(attr.ToString());
                }

                if (!handled) {
                    // try to use the default value
                    if (attr.DefaultValue != null) {
                        property.SetValue(ret, attr.DefaultValue, null);
                    }
                }
            }

            return ret;
        }

    }
}
