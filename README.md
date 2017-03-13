# MaterializedViewCache
[![MaterializedViewCache](https://img.shields.io/nuget/v/MaterializedViewCache.svg?maxAge=2592000)](https://www.nuget.org/packages/MaterializedViewCache/)

A service that can build and cache view models based on what parameters they were provided to be built with.

## Example

```csharp

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


public void UsageMethod()
{
	//Use dependency injection if possible
	MaterializedViewCache.ViewCacheService service = new MaterializedViewCache.ViewCacheService();

	service.Register(typeof(UnitTest1).GetMethod(nameof(UnitTest1.Get)),this);

	int param1 = 3;
	string param2 = "testing";
	bool param3 = false;


	service.Register(typeof(UnitTest1).GetMethod(nameof(UnitTest1.Get)),this);

	var vm = service.Get<TestVm>(new System.Collections.Generic.Dictionary<string, object>
	{
		{nameof(param1),param1},
		{ nameof(param2),param2 },
		{nameof(param3),param3},

	});

}

```