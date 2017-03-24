$gitVer = git describe --tags

nuget pack MaterializedViewCache.nuspec -Version $gitVer