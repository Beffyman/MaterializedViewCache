using System;
using System.Collections.Generic;
using System.Text;

namespace MaterializedViewCache.Services.RavenDb
{
    internal class ViewJson
    {

		public ulong Id { get; set; }
		public int TypeHash { get; set; }
		public string Json { get; set; }
    }
}
