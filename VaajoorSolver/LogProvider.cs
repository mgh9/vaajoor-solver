using System;
using System.IO;

namespace VaajoorSolver
{
    internal static class LogProvider
    {
        internal static string LogFilePath { get; private set; }

        static  LogProvider()
        {
            LogFilePath = @$"{Environment.CurrentDirectory}\log.txt";
        }

        internal static void Info(string message)
        {
            message = $"{DateTime.Now} : {message}{Environment.NewLine}";
            Console.WriteLine(message);
            File.AppendAllText(LogFilePath, message);
        }
    }
}
