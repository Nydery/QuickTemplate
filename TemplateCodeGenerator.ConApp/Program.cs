﻿using System.Diagnostics;
using TemplateCodeGenerator.ConApp.Generator;

namespace TemplateCodeGenerator.ConApp
{
    internal partial class Program
    {
        static Program()
        {
            ClassConstructing();
            HomePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
                        Environment.OSVersion.Platform == PlatformID.MacOSX)
                       ? Environment.GetEnvironmentVariable("HOME")
                       : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            UserPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            SourcePath = GetCurrentSolutionPath();
            TargetPaths = Array.Empty<string>();
            ClassConstructed();
        }
        static partial void ClassConstructing();
        static partial void ClassConstructed();

        #region Properties
        private static string? HomePath { get; set; }
        private static string UserPath { get; set; }
        private static string SourcePath { get; set; }
        private static string[] TargetPaths { get; set; }
        private static string[] SearchPatterns => StaticLiterals.SourceFileExtensions.Split('|');
        #endregion Properties

        static void Main(/*string[] args*/)
        {
            RunApp();
        }

        #region Console methods
        private static readonly bool canBusyPrint = true;
        private static bool runBusyProgress = false;
        private static void RunApp()
        {
            var input = string.Empty;

            while (input.Equals("x") == false)
            {
                var menuIndex = 0;
                var maxWaiting = 10 * 60 * 1000;    // 10 minutes
                var sourceSolutionName = GetSolutionNameByPath(SourcePath);

                Console.Clear();
                Console.WriteLine("Template Code Generator");
                Console.WriteLine("=======================");
                Console.WriteLine();
                Console.WriteLine($"Code generation for '{sourceSolutionName}' from: {SourcePath}");
                Console.WriteLine();
                Console.WriteLine($"[{++menuIndex}] Change source path");

                Console.WriteLine($"[{++menuIndex}] Compile solution...");
                Console.WriteLine($"[{++menuIndex}] Start code generation...");
                Console.WriteLine("[x|X] Exit");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Choose: ");
                input = Console.ReadLine()?.ToLower() ?? String.Empty;

                if (Int32.TryParse(input, out var select))
                {
                    if (select == 1)
                    {
                        var solutionPath = GetCurrentSolutionPath();
                        var qtProjects = GetQuickTemplateProjects(solutionPath).Union(new[] { solutionPath }).ToArray();

                        for (int i = 0; i < qtProjects.Length; i++)
                        {
                            if (i == 0)
                                Console.WriteLine();

                            Console.WriteLine($"Change path to: [{i + 1}] {qtProjects[i]}");
                        }
                        Console.WriteLine();
                        Console.Write("Select or enter source path: ");
                        var selectOrPath = Console.ReadLine();

                        if (Int32.TryParse(selectOrPath, out int number))
                        {
                            if ((number - 1) >= 0 && (number - 1) < qtProjects.Length)
                            {
                                SourcePath = qtProjects[number - 1];
                            }
                        }
                        else if (string.IsNullOrEmpty(selectOrPath) == false)
                        {
                            SourcePath = selectOrPath;
                        }
                    }
                    else if (select == 2)
                    {
                        var solutionProperties = SolutionProperties.Create(SourcePath);
                        var entityProject = EntityProject.Create(solutionProperties);
                        var arguments = $"build \"{solutionProperties.SolutionFilePath}\" -c Release";
                        Console.WriteLine(arguments);
                        Debug.WriteLine($"dotnet.exe {arguments}");
                        var csprojStartInfo = new ProcessStartInfo("dotnet.exe")
                        {
                            Arguments = arguments,
                            //WorkingDirectory = projectPath,
                            UseShellExecute = false
                        };
                        Process.Start(csprojStartInfo)?.WaitForExit(maxWaiting);
                        Console.Write("Press any key ");
                        Console.ReadKey();
                    }
                    else if (select == 3)
                    {
                        var solutionProperties = SolutionProperties.Create(SourcePath);
                        var entityProject = EntityProject.Create(solutionProperties);

                        foreach (var item in entityProject.EntityTypes)
                        {
                            Console.WriteLine(item.Name);
                        }
                        Console.Write("Press any key ");
                        Console.ReadKey();
                    }
                }
                Console.ResetColor();
            }
        }
        private static void PrintBusyProgress()
        {
            Console.WriteLine();
            runBusyProgress = true;
            Task.Factory.StartNew(async () =>
            {
                while (runBusyProgress)
                {
                    if (canBusyPrint)
                    {
                        Console.Write(".");
                    }
                    await Task.Delay(250).ConfigureAwait(false);
                }
            });
        }
        private static void PrintHeader(string sourcePath, string[] targetPaths)
        {
            var index = 0;
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Template Code Generator");
            Console.WriteLine("=======================");
            Console.WriteLine();
            Console.WriteLine($"Source: {sourcePath}");
            Console.WriteLine();
            foreach (var target in targetPaths)
            {
                Console.WriteLine($"   Generation for: [{++index,2}] {target}");
            }
            Console.WriteLine("   Generation for: [ a] ALL");
            Console.WriteLine();

            if (Directory.Exists(sourcePath) == false)
            {
                Console.WriteLine($"Source-Path '{sourcePath}' not exists");
            }
            foreach (var item in targetPaths)
            {
                if (Directory.Exists(item) == false)
                {
                    Console.WriteLine($"   Target-Path '{item}' not exists");
                }
            }
            Console.WriteLine();
        }
        #endregion Console methods

        #region Helpers
        private static string GetCurrentSolutionPath()
        {
            int endPos = AppContext.BaseDirectory
                                   .IndexOf($"{nameof(TemplateCodeGenerator)}", StringComparison.CurrentCultureIgnoreCase);

            return AppContext.BaseDirectory[..endPos];
        }
        private static string GetSolutionNameByPath(string solutionPath)
        {
            return solutionPath.Split(new char[] { '\\', '/' })
                               .Where(e => string.IsNullOrEmpty(e) == false)
                               .Last();
        }
        private static string[] GetQuickTemplateProjects(string sourcePath)
        {
            var directoryInfo = new DirectoryInfo(sourcePath);
            var parentDirectory = directoryInfo.Parent != null ? directoryInfo.Parent.FullName : SourcePath;
            var qtDirectories = Directory.GetDirectories(parentDirectory, "QT*", SearchOption.AllDirectories)
                                         .Where(d => d.Replace(UserPath, String.Empty).Contains('.') == false)
                                         .ToList();
            return qtDirectories.ToArray();
        }
        #endregion Helpers

        #region Partial methods
        static partial void BeforeGetTargetPaths(string sourcePath, List<string> targetPaths, ref bool handled);
        static partial void AfterGetTargetPaths(string sourcePath, List<string> targetPaths);
        #endregion Partial methods
    }
}