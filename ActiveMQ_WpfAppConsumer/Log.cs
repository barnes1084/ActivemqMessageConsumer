using System;
using System.IO;
using System.Linq;

namespace Schedule
{
    public class Log
    {
        // Used to send logs to a text file, identified by date
        public static void ToFile(string message, string filepath = "default")
        {
            try
            {
                filepath = SetFilePath(filepath);
                CleanUpLogs(filepath);

                using (StreamWriter writer = File.AppendText(filepath + $@"\{DateTime.Now.ToString("yyMMdd")}.txt"))
                {
                    writer.WriteLine($"{DateTime.Now} -  {message}");
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message} - {ex.StackTrace}");
            }
        }


        private static string SetFilePath(string filepath)
        {
            try
            {
                if (filepath.Equals("default"))
                {
                    Directory.CreateDirectory($@"{Directory.GetCurrentDirectory()}\Logs");
                    filepath = $@"{Directory.GetCurrentDirectory()}\Logs";
                }
                else
                {
                    Directory.CreateDirectory($@"{filepath}\Logs");
                    filepath = $@"{filepath}\Logs";
                }

                return filepath;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message} - {ex.StackTrace}");
            }
        }


        private static void CleanUpLogs(string filepath)
        {
            try
            {

                foreach (var fi in new DirectoryInfo(filepath)
                    .GetFiles()
                    .OrderByDescending(x => x.LastWriteTime)
                    .Skip(5))
                    fi.Delete();

            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message} - {ex.StackTrace}");
            }
        }
    }
}
