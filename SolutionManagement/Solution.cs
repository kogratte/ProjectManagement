using System.Collections.Generic;

namespace SolutionManagement
{
    public class Solution
    {
        public string Name { get; set; }
        public string TargetFramework { get; set; }
        public string CsProjPath { get; set; }
        public string PackageConfigPath { get; set; }
        public List<Nuget> Nugets { get; set; }

        public Solution()
        {
            this.Nugets = new List<Nuget>();
        }
    }
}
