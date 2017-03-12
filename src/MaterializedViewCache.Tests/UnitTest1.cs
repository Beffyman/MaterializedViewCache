using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ViewMaterializerCache.Attributes;

namespace MaterializedViewCache.Tests
{
	public class TestVm
	{
		[PropertyLookupDto(typeof(SourceDto),nameof(SourceDto.Property1))]
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





	[TestClass]
	public class UnitTest1
	{
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
			ViewMaterializerCache.ViewCacheService service = new ViewMaterializerCache.ViewCacheService();



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
