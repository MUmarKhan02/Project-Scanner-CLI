using System;
using System.Collections.Generic;
using System.IO;
namespace ProjectScanner
{
    // Holds everything discovered about the project
    class ProjectReport
    {
        public string ProjectPath { get; set; }
        public List<string> DetectedStacks { get; set; } = new List<string>();      // e.g. "React + Node", "ASP.NET Core"
        public List<string> RunSteps { get; set; } = new List<string>();
        public List<string> Terminals { get; set; } = new List<string>();  // commands per terminal

        public List<string> Ports { get; set; } = new List<string>();
    }

    // Responsible for looking at files and figuring out the project type
    static class ProjectAnalyzer
    {

        private static List<string> ReadNpmScripts(string packageJsonPath)
        {
            var scripts = new List<string>();

            if (packageJsonPath == null) return scripts;

            string content = File.ReadAllText(packageJsonPath);

            int scriptsIndex = content.IndexOf("\"scripts\"");
            if (scriptsIndex == -1) return scripts;

            int openBrace = content.IndexOf('{', scriptsIndex);
            int closeBrace = content.IndexOf('}', openBrace);

            if (openBrace == -1 || closeBrace == -1) return scripts;

            string scriptsBlock = content.Substring(openBrace + 1, closeBrace - openBrace - 1);

            string[] lines = scriptsBlock.Split('\n');

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("\"") && trimmed.Contains(":"))
                {
                    int firstQuote = trimmed.IndexOf('"');
                    int secondQuote = trimmed.IndexOf('"', firstQuote + 1);
                    if (secondQuote > firstQuote + 1)
                    {
                        string scriptName = trimmed.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                        scripts.Add(scriptName);
                    }
                }
            }

            return scripts;
        }


        public static ProjectReport Analyze(string path, string[] files)
        {
            var report = new ProjectReport { ProjectPath = path };

            bool hasPackageJson = HasFile(files, "package.json");
            bool hasCsproj = HasFileWithExtension(files, ".csproj");
            bool hasRequirementsTxt = HasFile(files, "requirements.txt");
            bool hasDockerCom = HasFile(files, "docker-compose.yml");
            bool hasManagePy = HasFile(files, "manage.py");
            bool hasGemfile = HasFile(files, "Gemfile");
            bool hasPomXml = HasFile(files, "pom.xml");
            bool hasBuildGradle = HasFile(files, "build.gradle");
            bool hasCMakeLists = HasFile(files, "CMakeLists.txt");
            bool hasGoMod = HasFile(files, "go.mod");
            bool hasCargoToml = HasFile(files, "Cargo.toml");
            bool hasComposerJson = HasFile(files, "composer.json");

            bool hasSlnFile = HasFileWithExtension(files, ".sln");
            bool hasDotEnv = HasFile(files, ".env");
            bool hasDotEnvExample = HasFile(files, ".env.example");
            bool hasLaunchSettings = HasFile(files, "launchSettings.json");
            bool hasViteConfig = HasFile(files, "vite.config.js") || HasFile(files, "vite.config.ts");

            // --- Always add this first ---
            report.RunSteps.Add("Open a terminal in the project folder");

            // --- Node / JS ---
            if (hasPackageJson)
            {
                report.DetectedStacks.Add("Node.js / JavaScript");
                 
                report.RunSteps.Add("Run: npm install");

                string packageJsonPath = FindFile(files, "package.json");
                List<string> scripts = ReadNpmScripts(packageJsonPath);

                if (scripts.Count > 0)
                {
                    foreach (string script in scripts)
                    {
                        report.RunSteps.Add($"Run: npm run {script}");

                        if (IsLongRunningScript(script))
                            report.Terminals.Add($"npm run {script}");
                    }
                }
                else
                {
                    report.RunSteps.Add("Run: npm run dev   (or npm start)");
                    report.Terminals.Add("npm run dev");
                }

                if (hasViteConfig)
                {
                    string viteConfigPath = FindFile(files, "vite.config.js") ?? FindFile(files, "vite.config.ts");
                    string port = ReadPortFromViteConfig(viteConfigPath);
                    if (port != null)
                        report.Ports.Add($"http://localhost:{port}  (from vite.config)");
                    else
                        report.Ports.Add("http://localhost:5173  (Vite default)");
                }
            }

            // --- .NET ---
            if (hasCsproj)
            {
                report.DetectedStacks.Add("ASP.NET Core / .NET");
                 
                report.RunSteps.Add("Run: dotnet restore");
                report.RunSteps.Add("Run: dotnet run");
                report.Terminals.Add("dotnet run");

                if (hasLaunchSettings)
                {
                    string launchPath = FindFile(files, "launchSettings.json");
                    string port = ReadPortFromLaunchSettings(launchPath);
                    if (port != null)
                        report.Ports.Add($"http://localhost:{port}  (from launchSettings.json)");
                }
            }

            // --- Visual Studio Solution ---
            if (hasSlnFile)
            {
                report.RunSteps.Add("Visual Studio Solution detected");
                report.RunSteps.Add("Open the .sln file in Visual Studio");
                report.RunSteps.Add("Press F5 to run, or use: dotnet run from the project subfolder");
            }

            // --- Python / Django ---
            if (hasRequirementsTxt && hasManagePy)
            {
                report.DetectedStacks.Add("Python / Django");
                 
                report.RunSteps.Add("Run: pip install -r requirements.txt");
                report.RunSteps.Add("Run: python manage.py migrate");
                report.RunSteps.Add("Run: python manage.py runserver");
                report.Terminals.Add("python manage.py runserver");
            }
            else if (hasRequirementsTxt)
            {
                report.DetectedStacks.Add("Python");
                 
                report.RunSteps.Add("Run: pip install -r requirements.txt");
                report.RunSteps.Add("Run: python main.py");
                report.Terminals.Add("python main.py");
            }

            // --- Ruby ---
            if (hasGemfile)
            {
                report.DetectedStacks.Add("Ruby");
                 
                report.RunSteps.Add("Run: bundle install");
                report.RunSteps.Add("Run: ruby main.rb   (or rails server if Rails project)");
                report.Terminals.Add("ruby main.rb");
            }

            // --- Java (Maven) ---
            if (hasPomXml)
            {
                report.DetectedStacks.Add("Java / Maven");
                 
                report.RunSteps.Add("Run: mvn install");
                report.RunSteps.Add("Run: mvn spring-boot:run   (or mvn exec:java if not Spring)");
                report.Terminals.Add("mvn spring-boot:run");
            }

            // --- Java (Gradle) ---
            if (hasBuildGradle)
            {
                report.DetectedStacks.Add("Java / Gradle");
                 
                report.RunSteps.Add("Run: gradle build");
                report.RunSteps.Add("Run: gradle run");
                report.Terminals.Add("gradle run");
            }

            // --- C++ (CMake) ---
            if (hasCMakeLists)
            {
                report.DetectedStacks.Add("C++ / CMake");
                 
                report.RunSteps.Add("Run: mkdir build && cd build");
                report.RunSteps.Add("Run: cmake ..");
                report.RunSteps.Add("Run: make");
                report.RunSteps.Add("Run: ./your_output_binary");
                report.Terminals.Add("make && ./your_output_binary");
            }

            // --- Go ---
            if (hasGoMod)
            {
                report.DetectedStacks.Add("Go");
                 
                report.RunSteps.Add("Run: go mod tidy");
                report.RunSteps.Add("Run: go run .");
                report.Terminals.Add("go run .");
            }

            // --- Rust ---
            if (hasCargoToml)
            {
                report.DetectedStacks.Add("Rust / Cargo");
                 
                report.RunSteps.Add("Run: cargo build");
                report.RunSteps.Add("Run: cargo run");
                report.Terminals.Add("cargo run");
            }

            // --- PHP ---
            if (hasComposerJson)
            {
                report.DetectedStacks.Add("PHP / Composer");
                 
                report.RunSteps.Add("Run: composer install");
                report.RunSteps.Add("Run: php artisan serve   (or php -S localhost:8000 if not Laravel)");
                report.Terminals.Add("php artisan serve");
            }
            // --- .env handling ---
            if (hasDotEnvExample && !hasDotEnv)
            {
                report.RunSteps.Add("WARNING: No .env file found — copy .env.example to .env and fill in your values");
                report.RunSteps.Add("Run: cp .env.example .env   (or copy manually on Windows)");
            }
            else if (hasDotEnv)
            {
                string dotEnvPath = FindFile(files, ".env");
                string port = ReadPortFromEnv(dotEnvPath);
                if (port != null)
                    report.Ports.Add($"http://localhost:{port}  (from .env)");
            }

            // --- Docker (can be alongside any stack) ---
            if (hasDockerCom)
            {
                report.RunSteps.Add("Docker detected — alternatively run: docker-compose up");
                report.Terminals.Add("docker-compose up");
            }
            // --- Fallback if nothing was detected ---
            if (report.DetectedStacks.Count == 0)
            {
                report.DetectedStacks.Add("Unknown — could not detect stack");
                report.RunSteps.Add("No known project files detected. Check the folder manually.");
            }

            return report;
        }

        private static bool HasFile(string[] files, string fileName)
        {
            foreach (string file in files)
            { if (Path.GetFileName(file) == fileName) return true; }
            return false;
        }
        private static string FindFile(string[] files, string fileName)
        {
            foreach (string file in files)
            { if (Path.GetFileName(file) == fileName) return file; }
            return null;
        }
        private static bool HasFileWithExtension(string[] files, string extension)
        {
            foreach (string file in files)
            { if (Path.GetExtension(file) == extension) return true; }
            return false;
        }
        private static string ReadPortFromEnv(string envPath)
        {
            if (envPath == null) return null;

            string[] lines = File.ReadAllLines(envPath);

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("PORT="))
                {
                    return trimmed.Substring("PORT=".Length).Trim();
                }
            }
            return null;
        }
        private static string ReadPortFromLaunchSettings(string launchPath)
        {
            if (launchPath == null) return null;

            string content = File.ReadAllText(launchPath);

            int httpsIndex = content.IndexOf("\"applicationUrl\"");
            if (httpsIndex == -1) return null;

            int colonIndex = content.IndexOf("localhost:", httpsIndex);
            if (colonIndex == -1) return null;

            int portStart = colonIndex + "localhost:".Length;
            int portEnd = portStart;

            while (portEnd < content.Length && char.IsDigit(content[portEnd]))
                portEnd++;

            if (portEnd == portStart) return null;

            return content.Substring(portStart, portEnd - portStart);
        }
        private static string ReadPortFromViteConfig(string viteConfigPath)
        {
            if (viteConfigPath == null) return null;

            string content = File.ReadAllText(viteConfigPath);

            int portIndex = content.IndexOf("port:");
            if (portIndex == -1) return null;

            int portStart = portIndex + "port:".Length;

            while (portStart < content.Length && (content[portStart] == ' ' || content[portStart] == '\t'))
                portStart++;

            int portEnd = portStart;

            while (portEnd < content.Length && char.IsDigit(content[portEnd]))
                portEnd++;

            if (portEnd == portStart) return null;

            return content.Substring(portStart, portEnd - portStart);
        }
        private static bool IsLongRunningScript(string scriptName)
        {
            string[] persistentScripts = { "dev", "start", "serve", "watch" };

            foreach (string persistent in persistentScripts)
            {
                if (scriptName.ToLower() == persistent)
                    return true;
            }
            return false;
        }
    }

    // Responsible for displaying the report to the console
    static class ReportPrinter
    {
        public static void Print(ProjectReport report)
        {
            Console.WriteLine($"Stack Detected : {string.Join(" + ", report.DetectedStacks)}");
            Console.WriteLine();
            Console.WriteLine("How to run this project:");
            Console.WriteLine(new string('=', 50));

            for (int i = 0; i < report.RunSteps.Count; i++)
                Console.WriteLine($"  Step {i + 1}: {report.RunSteps[i]}");

            if (report.Terminals.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"Requires {report.Terminals.Count} terminal(s):");
                for (int i = 0; i < report.Terminals.Count; i++)
                    Console.WriteLine($"  Terminal {i + 1}: {report.Terminals[i]}");
            }
            if (report.Ports.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Ports:");
                for (int i = 0; i < report.Ports.Count; i++)
                    Console.WriteLine($"  {report.Ports[i]}");
            }


        }
        public static void SaveToFile(ProjectReport report)
        {
            string outputPath = Path.Combine(report.ProjectPath, "HOW_TO_RUN.txt");

            var lines = new List<string>();

            lines.Add($"Project Scanner Report");
            lines.Add($"Generated: {DateTime.Now}");
            lines.Add($"Project Path: {report.ProjectPath}");
            lines.Add(new string('=', 50));
            lines.Add("");
            lines.Add($"Stack Detected: {string.Join(" + ", report.DetectedStacks)}");
            lines.Add("");
            lines.Add("How to run this project:");
            lines.Add(new string('=', 50));

            for (int i = 0; i < report.RunSteps.Count; i++)
                lines.Add($"  Step {i + 1}: {report.RunSteps[i]}");

            if (report.Terminals.Count > 0)
            {
                lines.Add("");
                lines.Add($"Requires {report.Terminals.Count} terminal(s):");
                for (int i = 0; i < report.Terminals.Count; i++)
                    lines.Add($"  Terminal {i + 1}: {report.Terminals[i]}");
            }

            if (report.Ports.Count > 0)
            {
                lines.Add("");
                lines.Add("Ports:");
                for (int i = 0; i < report.Ports.Count; i++)
                    lines.Add($"  {report.Ports[i]}");
            }

            File.WriteAllLines(outputPath, lines);
            Console.WriteLine("");
            Console.WriteLine($"Report saved to: {outputPath}");
        }
    }
}
