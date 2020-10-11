using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries
{
    public static class Logging
    {
        public static string lineSeparator = new string('-', 120) + "\n";

        public delegate void LogDelegate(object output);
        public static event LogDelegate LogOutput;

        public static void Log(object output)
        {
            LogOutput?.Invoke(output);
        }
        public static void Log(Exception e, string path)
        {
            string message = $"{lineSeparator}[{Utilities.GetCurrentTime()}] Exception thrown: \n-> {e.Source} | {e.TargetSite}:\n-> \"{e.Message}\"\n-> StackTrace:\n{e.StackTrace}\n{lineSeparator}";
            WriteText(path: path, content: message);
        }

        public static void Log(object exceptionObject, string path)
        {
            string message = $"{lineSeparator}[{Utilities.GetCurrentTime()}] Unknown exception thrown: \n-> {exceptionObject}\n{lineSeparator}";
            WriteText(path: path, content: message);
        }

        public static void Log(string s, string path)
        {
            WriteText(path: path, content: $"{lineSeparator}[{Utilities.GetCurrentTime()}]\n{s}\n{lineSeparator}");
        }

        public static void WriteText(string path = "", string content = "", bool createNewFile = false)
        {
            // Safely writes string content to a file, and ensures that the stream is closed afterwards
            if (createNewFile) File.WriteAllText(path, "");
            try
            {
                var writer = File.AppendText(path);
                writer.WriteLine(content);
                writer.Close();
            }
            catch (Exception e) // File is already open
            {
                Debug.WriteLine(e.Message);
            }
        }
    }
}
