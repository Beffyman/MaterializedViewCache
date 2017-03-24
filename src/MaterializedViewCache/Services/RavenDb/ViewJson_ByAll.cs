using Raven.Client.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaterializedViewCache.Services.RavenDb
{
	internal class ViewJson_ByAll : AbstractIndexCreationTask<ViewJson>
	{
		public ViewJson_ByAll()
		{
			Map = view => from v in view
						  select new
						  {
							  Id = v.Id,
							  TypeHash = v.TypeHash,
							  Json = v.Json
						  };
		}
	}
}
