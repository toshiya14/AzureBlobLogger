using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureBlobLogger
{
    public class BlobLogger : IDisposable
    {
        private string constr;
        private string container;
        private string blobName;

        private SortedList<DateTime, AzureBlobLogItem> logItems;
        private object logItemsLocker;
        private DateTime lastFlushTime;

        private Task BlobUploadTask;

        public BlobLogger(string constr, string container, string blobName)
        {
            this.constr = constr;
            this.container = container;
            this.blobName = blobName;

            this.logItems = new SortedList<DateTime, AzureBlobLogItem>();
            this.logItemsLocker = new object();
        }

        public void Append(string text, LogLevel level = LogLevel.Debug)
        {
            var item = new AzureBlobLogItem(level, text.Split('\r', '\n'));
            this.logItems.Add(item.Time, item);
            Evaluate();
        }

        public void Append(object obj, LogLevel level = LogLevel.Debug)
        {
            var item = new AzureBlobLogItem(level, obj);
            this.logItems.Add(item.Time, item);
            Evaluate();
        }

        public void SetLocation(string newBlobName)
        {
            this.blobName = newBlobName;
        }

        private void Evaluate()
        {
            if(this.logItems.Count > 30 || DateTime.Now - lastFlushTime > TimeSpan.FromMinutes(5))
            {
                this.BlobUploadTask = Flush();
            }
        }

        public async Task Flush()
        {
            var logs = new StringBuilder();
            var bakup = new List<AzureBlobLogItem>();

            lastFlushTime = DateTime.Now;

            lock (logItemsLocker)
            {
                if(logItems.Count == 0)
                {
                    return;
                }

                foreach (var item in logItems)
                {

                    foreach (var line in item.Value.Lines)
                    {
                        logs.AppendLine($"[{item.Value.Time:yyyy/MM/dd HH:mm:ss.fff}][{item.Value.Level}] {line}");
                    }
                }
                bakup.AddRange(logItems.Select(x => x.Value));
                logItems.Clear();
            }
            try
            {
                var blob = new AzureBlobOperator(this.constr);
                var bytes = Encoding.UTF8.GetBytes(logs.ToString());
                await blob.Append(this.container, this.blobName, bytes);
            }
            catch
            {
                // Revert if failed.
                lock (logItemsLocker)
                {
                    foreach (var item in bakup)
                    {
                        logItems.Add(item.Time, item);
                    }
                }
            }
        }

        public void Dispose()
        {
            BlobUploadTask.Wait();
            Flush().Wait();
        }
    }

    public enum LogLevel
    {
        Text = 0x0,
        Debug = 0x1,
        Information = 0x10,
        Warning = 0x100,
        Error = 0x1000,
        Fatal = 0x10000
    }
}
