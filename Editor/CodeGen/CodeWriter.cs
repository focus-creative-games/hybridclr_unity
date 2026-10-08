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

using System;
using System.Collections.Generic;
using System.Text;

namespace HybridCLR.CppCodeGen
{
    public class CodeWriter
    {
        private readonly List<string> _lines = new List<string>();

        private readonly int _indentSize;

        private readonly char _indentChar;

        private int _indentLevel = 0;

        private string _indent = string.Empty;

        public CodeWriter(int indentSize = 4, char indentChar = ' ')
        {
            if (indentSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(indentSize));
            }
            _indentSize = indentSize;
            _indentChar = indentChar;
        }

        public int IndentLevel => _indentLevel;

        public void IncreaseIndent()
        {
            _indentLevel++;
            _indent = new string(_indentChar, _indentLevel * _indentSize);
        }

        public void DecreaseIndent()
        {
            if (_indentLevel <= 0)
            {
                throw new InvalidOperationException("Indent level cannot be negative");
            }
            _indentLevel--;
            _indent = _indentLevel == 0 ? string.Empty : new string(_indentChar, _indentLevel * _indentSize);
        }

        public void ResetIndent()
        {
            _indentLevel = 0;
            _indent = string.Empty;
        }

        public void WriteLine(string line)
        {
            _lines.Add($"{_indent}{line}");
        }

        public void WriteLine()
        {
            _lines.Add(string.Empty);
        }

        public string GetContent(string newLine = "\n")
        {
            return string.Join(newLine, _lines);
        }

        public void Save(string filePath)
        {
            System.IO.File.WriteAllText(filePath, GetContent() + "\n", new UTF8Encoding(false));
            UnityEngine.Debug.Log($"write file to {filePath}");
        }
    }
}
