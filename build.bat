@echo Off
set config=%1
if "%config%" == "" (
   set config=Release
)

set version=
if not "%PackageVersion%" == "" (
   set version=-Version %PackageVersion%
)

%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild "src\Extension\NuGet.PackageSourceDiscovery.sln" /p:Configuration="%config%" /m /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false

mkdir Build
mkdir Build\Extension
bin\nuget.exe pack "src\Extension\NuGet.PackageSourceDiscovery.Extension\NuGet.PackageSourceDiscovery.Extension.csproj" -symbols -o Build\Extension -p Configuration="%config%" %version%
copy src\Extension\NuGet.PackageSourceDiscovery.Extension\bin\%config%\*.dll Build\Extension
copy src\Extension\NuGet.PackageSourceDiscovery.Extension\bin\%config%\*.pdb Build\Extension

mkdir Build\CmdLet
bin\nuget.exe pack "src\CmdLet\DiscoverPackageSources.nuspec" -o Build\CmdLet -p SolutionDir=%cd% %version%
