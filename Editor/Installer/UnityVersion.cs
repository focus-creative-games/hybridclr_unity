// Copyright 2026 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#if UNITY_6000_3_OR_NEWER && UNITY_EDITOR_OSX
#define NEW_IL2CPP_PATH
#endif
using System.Text.RegularExpressions;

namespace HybridCLR.Installer
{
    public class UnityVersion
    {
        public int major;
        public int minor1;
        public int minor2;
        public bool isTuanjieEngine;

        public override string ToString()
        {
            return $"{major}.{minor1}.{minor2}";
        }


        private static readonly Regex s_unityVersionPat = new Regex(@"(\d+)\.(\d+)\.(\d+)");

        public static UnityVersion ParseUnityVersion(string versionStr)
        {
            var matches = s_unityVersionPat.Matches(versionStr);
            if (matches.Count == 0)
            {
                return null;
            }
            Match match = matches[matches.Count - 1];
            int major = int.Parse(match.Groups[1].Value);
            int minor1 = int.Parse(match.Groups[2].Value);
            int minor2 = int.Parse(match.Groups[3].Value);
            bool isTuanjieEngine = versionStr.Contains("t");
            return new UnityVersion { major = major, minor1 = minor1, minor2 = minor2, isTuanjieEngine = isTuanjieEngine };
        }
    }
}
