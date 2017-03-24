# MaterializedViewCache
[![MaterializedViewCache](https://img.shields.io/nuget/v/MaterializedViewCache.svg?maxAge=2592000)](https://www.nuget.org/packages/MaterializedViewCache/)

A service that can build and cache view models based on what parameters they were provided to be built with.

## Example

```csharp

public class TestVm
{
	[MemberLookupDto(typeof(SourceDto), nameof(SourceDto.Property1))]
	public int vmProp1 { get; set; }

	[MemberLookupDto(typeof(SourceDto), nameof(SourceDto.Property2))]
	public string vmProp2 { get; set; }

	[MemberLookupDto(typeof(SourceDto), nameof(SourceDto.Property3))]
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


public void MemoryCacheTest()
{
	Configuration.Setup(new MemoryCacheSettings
	{
		JsonSettings = new JsonSerializerSettings
		{
			Formatting = Formatting.Indented
		},
		ParallelGet = false
	},true);
	Configuration.Container.Register(typeof(UnitTest1).GetMethod(nameof(UnitTest1.Get)), this);

	//Use dependency injection if possible
	MemoryCacheService service = new MemoryCacheService();

	int param1 = 3;
	string param2 = "testing";
	bool param3 = false;


	var vm = service.Get<TestVm>(new System.Collections.Generic.Dictionary<string, object>
	{
		{ nameof(param1), param1 },
		{ nameof(param2), param2 },
		{ nameof(param3), param3 },
	});

	service.Clean();

	Assert.NotNull(vm);
}

public void RavenDbCacheTest()
{
	Configuration.Setup(new RavenDbCacheSettings
	{
		JsonSettings = new JsonSerializerSettings
		{
			Formatting = Formatting.Indented
		},
		ParallelGet = false,
		CacheDatabaseName = "ViewTestingDatabase",
		ServerUrl = new Uri("http://localhost:8080")
	},true);
	Configuration.Container.Register(typeof(UnitTest1).GetMethod(nameof(UnitTest1.Get)), this);

	//Use dependency injection if possible
	RavenDbCacheService service = new RavenDbCacheService();

	int param1 = 3;
	string param2 = "testing";
	bool param3 = false;


	var vm = service.Get<TestVm>(new System.Collections.Generic.Dictionary<string, object>
	{
		{ nameof(param1), param1 },
		{ nameof(param2), param2 },
		{ nameof(param3), param3 },
	});

	service.Clean();

	Assert.NotNull(vm);
}
}

```