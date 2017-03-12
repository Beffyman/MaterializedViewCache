using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ViewMaterializerCache
{
    public class GetMethodDelegate
    {
		public object MethodCaller { get; set; }
		public MethodInfo Method { get; set; }

    }
}
