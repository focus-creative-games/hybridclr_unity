using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.AOT
{
    public class ConstraintContext
    {
        public GenericClass ApplyConstraints(GenericClass gc)
        {
            return gc;
        }

        public GenericMethod ApplyConstraints(GenericMethod gm)
        {
            return gm;
        }
    }
}
