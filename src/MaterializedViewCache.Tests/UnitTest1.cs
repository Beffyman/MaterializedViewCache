using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MaterializedViewCache.Attributes;

namespace MaterializedViewCache.Tests
{






	[TestClass]
	public class UnitTest1
	{
		public class TestVm
		{
			[PropertyLookupDto(typeof(SourceDto), nameof(SourceDto.Property1))]
			public int vmProp1 { get; set; }

			[PropertyLookupDto(typeof(SourceDto), nameof(SourceDto.Property2))]
			public string vmProp2 { get; set; }

			[PropertyLookupDto(typeof(SourceDto), nameof(SourceDto.Property3))]
			public bool vmProp3 { get; set; }


		}


		public class SourceDto
		{
			public int Property1 { get; set; }
			public string Property2 { get; set; }
			public bool Property3 { get; set; }
		}

		public SourceDto Get(int param1, string param2, bool param3)
		{
			return new SourceDto
			{
				Property1 = param1,
				Property2 = param2,
				Property3 = param3
			};
		}


		[TestMethod]
		public void TestMethod1()
		{
			//Use dependency injection if possible
			MaterializedViewCache.ViewCacheService service = new MaterializedViewCache.ViewCacheService();



			service.Register(typeof(UnitTest1).GetMethod(nameof(UnitTest1.Get)),this);

			var vm = service.Get<TestVm>(new System.Collections.Generic.Dictionary<string, object>
			{
				{ "param1",3 },
				{ "param2","testing" },
				{ "param3",false },

			});

			Assert.IsNotNull(vm);



		}
	}
}
