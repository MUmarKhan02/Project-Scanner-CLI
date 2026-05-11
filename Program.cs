using System;
using System.IO;

namespace ProjectScanner
{
    class Program
    {
        static void Main(string[] args)
        {
            // Step 1: Get the target directory from args, or default to current directory
            string targetPath = args.Length > 0 ? string.Join(" ", args) : Directory.GetCurrentDirectory();

            if (!Directory.Exists(targetPath))
            {
                Console.WriteLine($"Error: Directory not found -> {targetPath}");
                return;
            }

            Console.WriteLine($"Scanning project at: {targetPath}");
            Console.WriteLine(new string('-', 50));

            // Step 2: Collect all files in the directory (recursive)
            string[] allFiles = Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories);

            allFiles = Array.FindAll(allFiles, f => Path.GetFileName(f) != "HOW_TO_RUN.txt");
            // Step 3: Pass the files to your scanner (you'll build this out)
            var report = ProjectAnalyzer.Analyze(targetPath, allFiles);

            // Step 4: Print the report
            ReportPrinter.Print(report);
            ReportPrinter.SaveToFile(report);
        }
    }
}
