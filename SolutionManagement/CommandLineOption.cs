using CommandLine;

namespace SolutionManagement
{
    public class CommandLineOption
    {
        [Option('f', "netFwkVersion", Required = false, HelpText = "Analyse the .Net framework usage")]
        public bool AnalyseNetVersion { get; set; }

        [Option('d', "dependencies", Required = false, HelpText = "Analyse the project dependencies")]
        public bool AnalyseNugetVersion { get; set; }

        [Option("netOutputFile", Required = false, HelpText = ".Net analysis output file (csv file)")]
        public string NetFwkOutputFile { get; set; }

        [Option("nugetOutputFile", Required = false, HelpText = "Project dependencies output file (csv file)")]
        public string NugetOutputFile { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Print to console")]
        public bool Verbose { get; set; }

        [Option('p', "projectDirectory", Required = false, HelpText = "The project directory")]
        public string ProjectDirectory { get; set; }
    }
}
