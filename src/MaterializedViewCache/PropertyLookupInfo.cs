using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MaterializedViewCache.Attributes;

namespace MaterializedViewCache
{
	internal class MemberLookupInfo
	{
		public MemberInfo memberInfo { get; set; }
		public MemberLookupDtoAttribute attribute { get; set; }

	}
}
