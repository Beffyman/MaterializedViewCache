using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ViewMaterializerCache.Attributes;

namespace ViewMaterializerCache
{
	internal class PropertyLookupInfo
	{
		public PropertyInfo propertyInfo { get; set; }
		public PropertyLookupDtoAttribute attribute { get; set; }

	}
}
