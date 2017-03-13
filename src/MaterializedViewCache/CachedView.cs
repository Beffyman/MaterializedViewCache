using System;
using System.Collections.Generic;
using System.Text;

namespace MaterializedViewCache
{
    internal class CachedView
    {
		public Type CachedType { get; set; }

		public Dictionary<string, object> Parameters { get; set; }

		public object CachedVM { get; set; }


		public override bool Equals(object obj)
		{
			if(obj is CachedView v)
			{
				return v.CachedType == CachedType && Parameters.DictEqual(v.Parameters);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			int hash = 13;

			hash = (hash * 7) + CachedType.GetHashCode();
			hash = (hash * 7) + Parameters.GetHashCode();

			return hash;
		}
	}
}
