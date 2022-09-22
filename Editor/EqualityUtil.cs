using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor
{
    public static class EqualityUtil
    {
        public static bool EqualsTypeSigArray(List<TypeSig> a, List<TypeSig> b)
        {
            if (a == b)
            {
                return true;
            }
            if (a != null && b != null)
            {
                if (a.Count != b.Count)
                {
                    return false;
                }
                for (int i = 0; i < a.Count; i++)
                {
                    if (!TypeEqualityComparer.Instance.Equals(a[i], b[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
