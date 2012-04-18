msbuild Aurora.csproj /property:Configuration=Debug
msbuild Aurora.csproj /property:Configuration=Release

rem copy bin\Release\Aurora.dll lib\20
rem rmdir /Q /S bin
rem rmdir /Q /S obj
rem nuget pack Aurora.nuspec
