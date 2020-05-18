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

        /// <summary>
        /// Get or set the auto flush time span. For current version,
        /// auto flush functions have been abandoned. As it seems not
        /// so stable.
        /// </summary>
        [Obsolete]
        public TimeSpan AutoFlushTimeSpan { get; set; }

        /// <summary>
        /// Get or set the log count to trigger auto flush. For current
        /// version, auto flush functions have been abandoned. As it
        /// seems not so stable.
        /// </summary>
        [Obsolete]
        public int AutoFlushCount { get; set; }

        /// <summary>
        /// Initialize a new BlobLogger instance.
        /// </summary>
        /// <param name="constr">the connection string to the Azure storage.</param>
        /// <param name="container">the container name.</param>
        /// <param name="blobName">the blob name.</param>
        public BlobLogger(string constr, string container, string blobName)
        {
            this.constr = constr;
            this.container = container;
            this.blobName = blobName;

            this.logItems = new SortedList<DateTime, AzureBlobLogItem>();
            this.logItemsLocker = new object();
            this.AutoFlushCount = 30;
            this.AutoFlushTimeSpan = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Append a text to the log cache.
        /// </summary>
        /// <param name="text">the text contents.</param>
        /// <param name="level">the log level.</param>
        public void Append(string text, LogLevel level = LogLevel.Debug)
        {
            var item = new AzureBlobLogItem(level, text.Split('\r', '\n'));
            this.logItems.Add(item.Time, item);
        }

        /// <summary>
        /// Append an object to the log cache. (use obj.ToString())
        /// </summary>
        /// <param name="obj">the object.</param>
        /// <param name="level">the level.</param>
        public void Append(object obj, LogLevel level = LogLevel.Debug)
        {
            var item = new AzureBlobLogItem(level, obj);
            this.logItems.Add(item.Time, item);
        }

        /// <summary>
        /// Set the a new blob name for future flush actions.
        /// </summary>
        /// <param name="newBlobName">new blob name.</param>
        public void SetLocation(string newBlobName)
        {
            this.blobName = newBlobName;
        }

        private void Evaluate()
        {
            if(this.logItems.Count >AutoFlushCount || DateTime.Now - lastFlushTime > AutoFlushTimeSpan)
            {
                this.BlobUploadTask = UploadToAzure();
            }
        }

        /// <summary>
        /// Write the current cache to remote server.
        /// </summary>
        public async Task Flush()
        {
            // Wait until the previous task is finished.
            if (BlobUploadTask != null && !BlobUploadTask.IsCompleted)
            {
                await BlobUploadTask;
            }
            this.BlobUploadTask = UploadToAzure();
            await this.BlobUploadTask;
        }

        private async Task UploadToAzure()
        {

            var logs = new StringBuilder();
            var bakup = new List<AzureBlobLogItem>();

            lastFlushTime = DateTime.Now;

            lock (logItemsLocker)
            {
                if (logItems.Count == 0)
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
            catch(Exception ex)
            {
                // Revert if failed.
                lock (logItemsLocker)
                {
                    foreach (var item in bakup)
                    {
                        logItems.Add(item.Time, item);
                    }
                }

                throw new Exception("Cannot write to remote server.", ex);
            }
        }

        public void Dispose()
        {
            BlobUploadTask?.Wait();
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
