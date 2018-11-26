using CommandLine;
using ConsoleTableExt;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SolutionManagement
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineOption>(args)
                   .WithParsed<CommandLineOption>(opts =>
                   {
                       try
                       {
                           var solutionDirectory = GetSolutionsDirectory(opts);
                           var solutions = FindSolutions(solutionDirectory);

                           if (solutions.Count == 0)
                           {
                               Console.WriteLine($"No csproj found in {solutionDirectory}");

                               return;
                           }

                           if (opts.Verbose)
                           {
                               Console.WriteLine($"{solutions.Count} csproj founds");
                           }

                           AnalyseNetFwkVersionFor(solutions);
                           AnalyseNugetUsagesFor(solutions);

                           if (opts.AnalyseNetVersion)
                           {
                               PrintFwkVersion(solutions, opts);
                           }

                           if (opts.AnalyseNugetVersion)
                           {
                               PrintNugetUsageFor(solutions, opts);
                           }

                           if (opts.ConsolidateDependencies)
                           {

                               PrintNugetConsolidationInformations(solutions, opts);
                           }
                       }
                       catch (Exception e)
                       {
                           return;
                       }

                   });



        }

        private static void AnalyseNugetUsagesFor(List<Solution> solutions)
        {
            solutions.ForEach(solution =>
            {
                if (!File.Exists(solution.PackageConfigPath))
                {
                    return;
                }
                var p = XDocument.Load(solution.PackageConfigPath);
                p.Descendants().Where(d => d.Name.LocalName == "package").ToList().ForEach(node =>
                {
                    solution.Nugets.Add(new Nuget
                    {
                        Name = node.Attribute("id").Value,
                        TargetFramework = node.Attribute("targetFramework").Value,
                        Version = node.Attribute("version").Value
                    });
                });
            });
        }


        private static void PrintNugetUsageFor(List<Solution> solutions, CommandLineOption opts)
        {
            DataTable table = new DataTable();

            table.Columns.Add("Solution", typeof(string));
            table.Columns.Add("Nuget Name", typeof(string));
            table.Columns.Add("Version", typeof(string));
            table.Columns.Add("Target Framework", typeof(string));

            if (!string.IsNullOrEmpty(opts.NugetOutputFile))
            {
                File.Delete(opts.NugetOutputFile);
            }
            solutions.ForEach(solution =>
            {
                solution.Nugets.ForEach(nuget =>
                {
                    if (!string.IsNullOrEmpty(opts.NugetOutputFile))
                    {
                        File.AppendAllText(opts.NugetOutputFile, $"{solution.Name};{nuget.Name};{nuget.Version};{nuget.TargetFramework};{Environment.NewLine}");
                    }
                    table.Rows.Add(solution.Name, nuget.Name, nuget.Version, nuget.TargetFramework);
                });
            });

            if (opts.Verbose)
                ConsoleTableBuilder.From(table).ExportAndWriteLine();
        }

        private static void PrintNugetConsolidationInformations(List<Solution> solutions, CommandLineOption opts)
        {
            IEnumerable<Nuget> nugets = solutions.SelectMany(s => s.Nugets);
            if (!string.IsNullOrEmpty(opts.FilterDependency))
            {
                nugets = nugets.Where(n => n.Name.Equals(opts.FilterDependency));
            }

            nugets.Select(n => n.Name).Distinct().ToList().ForEach(n =>
            {
                DataTable table = new DataTable();

                table.Columns.Add("Nuget Name", typeof(string));
                table.Columns.Add("Target Framework", typeof(string));
                table.Columns.Add("Used versions", typeof(string));

                nugets.Select(_n => _n.TargetFramework).Distinct().ToList().ForEach(tv =>
                {
                    var usedNugetVersions = nugets.Where(_n => _n.Name == n && _n.TargetFramework == tv).Select(_n => _n.Version).Distinct().ToList();
                    if (usedNugetVersions.Any())
                    {
                        table.Rows.Add(n, tv, string.Join(", ", usedNugetVersions));
                    }
                });

                if (opts.Verbose)
                {
                    ConsoleTableBuilder.From(table).ExportAndWriteLine();
                    Console.WriteLine(Environment.NewLine);
                }
            });
        }


        private static void PrintFwkVersion(List<Solution> solutions, CommandLineOption opts)
        {
            List<IGrouping<string, Solution>> a = solutions.GroupBy(s => s.TargetFramework).ToList();
            DataTable table = new DataTable();

            table.Columns.Add("Framework", typeof(string));
            table.Columns.Add("Projects", typeof(string));

            if (!string.IsNullOrEmpty(opts.NetFwkOutputFile))
            {
                File.Delete(opts.NetFwkOutputFile);
            }

            a.ForEach((grouping) =>
            {
                grouping.ToList().ForEach(solution =>
                {
                    if (!string.IsNullOrEmpty(opts.NetFwkOutputFile))
                    {
                        File.AppendAllText(opts.NetFwkOutputFile, $"{solution.Name};{solution.TargetFramework};{Environment.NewLine}");
                    }
                    table.Rows.Add(solution.TargetFramework, solution.Name);
                });
            });

            if (opts.Verbose)
            {
                ConsoleTableBuilder.From(table).ExportAndWriteLine();
            }
        }

        private static void AnalyseNetFwkVersionFor(List<Solution> solutions)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();


            solutions.ForEach(solution =>
            {
                XDocument csprojXml = XDocument.Load(solution.CsProjPath);
                var targetVersion = "Unknown";
                try
                {
                    targetVersion = csprojXml.Descendants().Single(d => d.Name.LocalName == "TargetFrameworkVersion").Value;
                }
                catch (System.Exception e)
                {
                    targetVersion = csprojXml.Descendants().Single(d => d.Name.LocalName == "TargetFramework").Value;
                }
                solution.TargetFramework = targetVersion;
            });
        }

        private static List<Solution> FindSolutions(string solutionDirectory)
        {
            return Directory.EnumerateFiles(solutionDirectory, "*.csproj", SearchOption.AllDirectories).ToList().Select(csproj => new Solution
            {
                CsProjPath = csproj,
                Name = Path.GetFileName(csproj).Replace(".csproj", string.Empty),
                PackageConfigPath = Path.GetDirectoryName(csproj) + "/packages.config"
            }).ToList();
        }

        private static string GetSolutionsDirectory(CommandLineOption opts)
        {
            if (!string.IsNullOrEmpty(opts.ProjectDirectory))
            {
                if (!Directory.Exists(opts.ProjectDirectory))
                {
                    if (opts.Verbose)
                    {
                        Console.WriteLine($"Directory {opts.ProjectDirectory} does not exists");
                    }
                    throw new Exception();
                }

                return opts.ProjectDirectory;
            }

            var dir = string.Empty;
            while (string.IsNullOrEmpty(dir))
            {
                Console.WriteLine("Please type the app root directory");
                dir = Console.ReadLine();

                if (!Directory.Exists(dir))
                {
                    Console.WriteLine($"{dir} does not exists");
                    dir = string.Empty;
                }
            }

            return dir;
        }
    }
}
