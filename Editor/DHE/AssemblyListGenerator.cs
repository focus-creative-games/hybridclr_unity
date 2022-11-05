using HybridCLR.Editor.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Editor.DHE
{
    public class AssemblyListGenerator
    {
        public class Options
        {
            public string OutputFile { get; set; }

            public List<string> DifferentialHybridAssembyList { get; set; }
        }

        private readonly Options _options;
        public AssemblyListGenerator(Options options)
        {
            _options = options;
        }

        public void Generate()
        {
            var frr = new FileRegionReplace(File.ReadAllText(_options.OutputFile));

            List<string> lines = new List<string>();
            foreach (var ass in _options.DifferentialHybridAssembyList)
            {
                lines.Add($"\t\t\"{ass}\",");
            }

            frr.Replace("DHE", string.Join("\n", lines));

            frr.Commit(_options.OutputFile);
            Debug.Log($"output:{_options.OutputFile}");
        }
    }
}
