rem msbuild Aurora.csproj /property:Configuration=Debug

msbuild Aurora.csproj /property:Configuration=Release

copy bin\Release\Aurora.dll lib\20
rmdir /Q /S bin
rmdir /Q /S obj
nuget pack Aurora.nuspec
