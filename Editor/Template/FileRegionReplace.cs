using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.Template
{
    public class FileRegionReplace
    {
        private readonly string _tplCode;

        private readonly Dictionary<string, string> _regionReplaceContents = new Dictionary<string, string>();

        public FileRegionReplace(string tplCode)
        {
            _tplCode = tplCode;
        }

        public void Replace(string regionName, string regionContent)
        {
            _regionReplaceContents.Add(regionName, regionContent);
        }

        public string GenFinalString()
        {
            string originContent = _tplCode;

            string resultContent = originContent;

            foreach (var c in _regionReplaceContents)
            {
                resultContent = TemplateUtil.ReplaceRegion(resultContent, c.Key, c.Value);
            }
            return resultContent;
        }

        public void Commit(string outputFile)
        {
            string resultContent = GenFinalString();
            var utf8WithoutBOM = new System.Text.UTF8Encoding(false);
            File.WriteAllText(outputFile, resultContent, utf8WithoutBOM);
        }
    }
}
