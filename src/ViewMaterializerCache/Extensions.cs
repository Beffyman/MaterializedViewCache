using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ViewMaterializerCache
{
    internal static class Extensions
    {
		public static bool DictEqual<T,K>(this Dictionary<T,K> dict1, Dictionary<T, K> dict2)
		{
			return dict1.Count == dict2.Count && !dict1.Except(dict2).Any();
		}


	}
}
