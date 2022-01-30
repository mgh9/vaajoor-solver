using System;
using System.Diagnostics;
using System.Reflection;

namespace VaajoorSolver
{
    partial class Program
    {
        static void Main(string[] args)
        {
            showHelp();

            string persianWordsFileName;
            int wordCharsCount;

            try
            {
                // a text file (full path) containing each words in a new line
                persianWordsFileName = args[0];         
                
                // length of the word (e.g : 5)
                wordCharsCount = int.Parse(args[1]);    
            }
            catch (Exception ex)
            {
                LogProvider.Info($"Invalid arguments! {ex.Message}");
                return;
            }

            try
            {
                var theSolver = new Solver(persianWordsFileName, wordCharsCount);
                if(theSolver.Solve(out string answer))
                {
                    // and we have a winner...
                }
            }
            catch (Exception)
            {
                // ...
                throw;
            }
        }

        private static void showHelp()
        {
            var theAss = Assembly.GetExecutingAssembly();
            var theVersionInfo = FileVersionInfo.GetVersionInfo(theAss.Location);
            var productName = theVersionInfo.ProductName;
            var productVersion = theVersionInfo.ProductVersion;
            var companyName = theVersionInfo.CompanyName;

            Console.WriteLine("*****************************");
            Console.WriteLine($"{productName} v{productVersion} by {companyName}");
            Console.WriteLine($"Usage : VaajoorSolver.exe \"Persian Words LineByLine\" WordCharsCount");
            Console.WriteLine($"Example : VaajoorSolver.exe \"c:\\PersianWordsLineByLine.txt\" 5");
            Console.WriteLine("*****************************");
        }
    }
}
