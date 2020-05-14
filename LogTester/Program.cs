using AzureBlobLogger;
using System;

namespace LogTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var constr = "<your blob connection string here>";
            var container = "logs";
            var blobName = "test.log";

            var logger = new BlobLogger(constr, container, blobName);
            logger.Append("Test log -> TEXT.", LogLevel.Text);
            logger.Append("Test log -> DEBUG.", LogLevel.Debug);
            logger.Append("Test log -> INFO.", LogLevel.Information);
            logger.Append("Test log -> WARN.", LogLevel.Warning);
            logger.Append("Test log -> ERROR.", LogLevel.Error);
            logger.Append("Test log -> FATAL.", LogLevel.Fatal);
            logger.Append("Test log -> Exception", LogLevel.Error);
            logger.Append(new Exception("This is an exception"), LogLevel.Error);
            logger.Flush().Wait();
        }
    }
}
