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

            using (var logger = new BlobLogger(constr, container, blobName))
            {
                logger.Append("TEXT log test.", LogLevel.Text);
                logger.Append("DEBUG log test.", LogLevel.Debug);
                logger.Append("INFO log test.", LogLevel.Information);
                logger.Append("WARN log test.", LogLevel.Warning);
                logger.Append("ERROR log test.", LogLevel.Error);
                logger.Append("FATAL log test.", LogLevel.Fatal);
                logger.Flush().Wait();
                try
                {
                    var b = 0;
                    var a = 1 / b;
                }
                catch (Exception ex)
                {
                    logger.Append(ex, LogLevel.Error);
                }
                try
                {
                    var c = new[] { 1, 2, 3 };
                    var d = c[3];
                }catch(Exception ex)
                {
                    logger.Append(ex, LogLevel.Error);
                }
                logger.Append(new Exception("System Exception Test"), LogLevel.Error);
                Parallel.Invoke(
                    logger.Flush().Wait,
                    logger.Flush().Wait
                    ); ;
            }
        }
    }
}
