using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MaterializedViewCache
{
    internal class GetMethodDelegate
    {
		public Func<object> MethodCallerGetter { get; set; }
		public object MethodCaller { get; set; }
		public MethodInfo Method { get; set; }

    }
}
