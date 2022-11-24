using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Runtime
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Struct)]
    public class UnchangedAttribute : Attribute
    {
        public bool Unchanged { get; }

        public UnchangedAttribute(bool unchanged = true)
        {
            Unchanged = unchanged;
        }
    }
}
