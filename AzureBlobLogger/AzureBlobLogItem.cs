using System;
using System.Collections.Generic;
using System.Text;

namespace AzureBlobLogger
{
    internal struct AzureBlobLogItem
    {

        public LogLevel Level { get; private set; }

        public string[] Lines { get; private set; }

        public DateTime Time { get; private set; }

        public AzureBlobLogItem(LogLevel level, string[] lines)
        {
            this.Level = level;
            this.Lines = lines ?? Array.Empty<string>();
            this.Time = DateTime.UtcNow.AddHours(AzureBlobLogConfig.TimeZoneOffset);
        }

        public AzureBlobLogItem(LogLevel level, object obj)
        {
            if (obj is Exception || obj.GetType().IsSubclassOf(typeof(Exception)))
            {
                var list = new List<string>();
                var ex = obj as Exception;
                if (ex != null) {
                    list.Add($"========== [object {obj.GetType().Name}] ==========");
                    list.Add($"  Message: {ex.Message}");
                    if (ex.StackTrace != null)
                    {
                        list.Add($"  StackTrace:");
                        foreach (var line in ex.StackTrace.Split('\r', '\n'))
                        {
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                continue;
                            }
                            else
                            {
                                list.Add("            " + line);
                            }
                        }
                    }
                    list.Add($"============================================");
                }
                this.Lines = list.ToArray();
            }
            else
            {
                this.Lines = new string[1] { $"[object {obj.GetType().Name}] {obj.ToString().Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t")}" };
            }

            this.Level = level;
            this.Time = DateTime.UtcNow.AddHours(AzureBlobLogConfig.TimeZoneOffset);
        }
    }
}
