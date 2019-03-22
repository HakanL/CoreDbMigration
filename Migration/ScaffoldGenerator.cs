using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Haukcode.Migration
{
    public static class ScaffoldGenerator
    {
        public static void ExecuteScaffold(
            string baseDirectory,
            string connectionString,
            string destinationDatabase,
            string modelsDirectory,
            string contextName)
        {
            // Delete all files from the models folder
            Directory.CreateDirectory(Path.Combine(baseDirectory, modelsDirectory));
            DeleteAllFiles(Path.Combine(baseDirectory, modelsDirectory));

            var processStartInfo = new ProcessStartInfo
            {
                WorkingDirectory = baseDirectory,
                FileName = "dotnet",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            processStartInfo.ArgumentList.Add("ef");
            processStartInfo.ArgumentList.Add("dbcontext");
            processStartInfo.ArgumentList.Add("scaffold");
            processStartInfo.ArgumentList.Add($"{connectionString}; Database={destinationDatabase}");
            processStartInfo.ArgumentList.Add("Microsoft.EntityFrameworkCore.SqlServer");
            processStartInfo.ArgumentList.Add("--output-dir");
            processStartInfo.ArgumentList.Add(modelsDirectory);
            processStartInfo.ArgumentList.Add("--context");
            processStartInfo.ArgumentList.Add(contextName);
            processStartInfo.ArgumentList.Add("--force");

            var scaffoldProcess = new Process
            {
                StartInfo = processStartInfo
            };
            scaffoldProcess.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine(e.Data);
            };
            scaffoldProcess.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine(e.Data);
            };
            scaffoldProcess.Start();
            scaffoldProcess.BeginOutputReadLine();
            scaffoldProcess.BeginErrorReadLine();
            scaffoldProcess.WaitForExit();
            if (scaffoldProcess.ExitCode != 0)
                throw new Exception("Error while scaffolding");
        }

        public static void DeleteAllFiles(string folder)
        {
            var di = new DirectoryInfo(folder);
            foreach (FileInfo file in di.EnumerateFiles())
                file.Delete();
        }
    }
}
