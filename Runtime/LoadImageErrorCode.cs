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

﻿
namespace HybridCLR
{
    public enum LoadImageErrorCode
    {
        OK = 0,
        BAD_IMAGE, // invalid dll file
        NOT_IMPLEMENT, // not implement feature
        AOT_ASSEMBLY_NOT_FIND, // AOT assembly not found
        HOMOLOGOUS_ONLY_SUPPORT_AOT_ASSEMBLY, // can not load supplementary metadata assembly for non-AOT assembly
        HOMOLOGOUS_ASSEMBLY_HAS_LOADED, // can not load supplementary metadata assembly for the same assembly
        INVALID_HOMOLOGOUS_MODE, // invalid homologous image mode
        PDB_BAD_FILE, // invalid pdb file
        UNKNOWN_IMAGE_FORMAT,
        UNSUPPORT_FORMAT_VERSION,
        UNMATCH_FORMAT_VARIANT,
    };
}

