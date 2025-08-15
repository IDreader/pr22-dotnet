@echo off
if "%1"=="" (
	call :generate %*
) else (
	call :%*
)
goto :EOF

rem --- Generate ---------------------------------------------------------------
:generate

echo Generating project files for Visual Studio 2010 .NET C# ...

mkdir vcs100

set PROJECTS=

for %%i in ( *.cs ) do (
	call :add %%~ni
	mkdir vcs100\%%~ni
	mkdir vcs100\%%~ni\Properties
	call :AssemblyInfo %%~ni > vcs100\%%~ni\Properties\AssemblyInfo.cs
	call :csproj %%~ni > vcs100\%%~ni\%%~ni.csproj
)

call :sln > vcs100.sln

goto :EOF

rem --- Projects list ----------------------------------------------------------
:add

set PROJECTS=%PROJECTS%^ %*

goto :EOF

rem --- Project ----------------------------------------------------------------

:csproj

echo ﻿^<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003"^>
echo   ^<PropertyGroup^>
echo     ^<Configuration Condition=" '$(Configuration)' == '' "^>Debug^</Configuration^>
echo     ^<Platform Condition=" '$(Platform)' == '' "^>AnyCPU^</Platform^>
echo     ^<ProductVersion^>8.0.50727^</ProductVersion^>
echo     ^<SchemaVersion^>2.0^</SchemaVersion^>
echo     ^<ProjectGuid^>{{4BFC3160-44D9-48e4-831D-41A475382212}}^</ProjectGuid^>
echo     ^<OutputType^>Exe^</OutputType^>
echo     ^<AppDesignerFolder^>Properties^</AppDesignerFolder^>
echo     ^<RootNamespace^>%1^</RootNamespace^>
echo     ^<AssemblyName^>%1^</AssemblyName^>
echo     ^<TargetFrameworkVersion^>v4.0^</TargetFrameworkVersion^>
echo     ^<TargetFrameworkProfile^>Client^</TargetFrameworkProfile^>
echo   ^</PropertyGroup^>
echo   ^<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "^>
echo     ^<DebugSymbols^>true^</DebugSymbols^>
echo     ^<OutputPath^>bin\AnyCPU\Debug\^</OutputPath^>
echo     ^<DefineConstants^>DEBUG;TRACE^</DefineConstants^>
echo     ^<DebugType^>full^</DebugType^>
echo     ^<Optimize^>false^</Optimize^>
echo     ^<PlatformTarget^>AnyCPU^</PlatformTarget^>
echo     ^<ErrorReport^>prompt^</ErrorReport^>
echo     ^<WarningLevel^>4^</WarningLevel^>
echo   ^</PropertyGroup^>
echo   ^<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "^>
echo     ^<OutputPath^>bin\AnyCPU\Release\^</OutputPath^>
echo     ^<DefineConstants^>TRACE^</DefineConstants^>
echo     ^<Optimize^>true^</Optimize^>
echo     ^<DebugType^>pdbonly^</DebugType^>
echo     ^<PlatformTarget^>AnyCPU^</PlatformTarget^>
echo     ^<ErrorReport^>prompt^</ErrorReport^>
echo     ^<WarningLevel^>4^</WarningLevel^>
echo   ^</PropertyGroup^>
echo   ^<ItemGroup^>
echo     ^<Reference Include="Pr22"^>
echo      ^<HintPath^>..\..\..\Framework4.0\Pr22.dll^</HintPath^>
echo     ^</Reference^>
echo     ^<Reference Include="System" /^>
echo     ^<Reference Include="System.Data" /^>
echo     ^<Reference Include="System.Drawing" /^>
echo     ^<Reference Include="System.Xml" /^>
echo   ^</ItemGroup^>
echo   ^<ItemGroup^>
echo   ^<Compile Include="..\..\%1.cs"^>
echo     ^<Link^>%1.cs^</Link^>
echo   ^</Compile^>
setlocal
set inclist=.
for /f "tokens=*" %%f in ('findstr /l "Extension." %1.cs') do call :include "%%f"
endlocal
echo   ^<Compile Include="Properties\AssemblyInfo.cs" /^>
echo   ^</ItemGroup^>
echo   ^<Import Project="$(MSBuildBinPath)\Microsoft.CSharp.targets" /^>
echo   ^<!-- To modify your build process, add your task inside one of the targets below and uncomment it.
echo        Other similar extension points exist, see Microsoft.Common.targets.
echo   ^<Target Name="BeforeBuild"^>
echo   ^</Target^>
echo   ^<Target Name="AfterBuild"^>
echo   ^</Target^>
echo   --^>
echo ^</Project^>

goto :EOF

rem --- include ----------------------------------------------------------------

:include

set  incfile=%~1
set  incfile=%incfile:*Extension.=%
set  incfile=%incfile:.= %
set  incfile=%incfile:(= %
set  incfile=%incfile:)= %
call :remend incfile %incfile%

call set repl=%%inclist:%incfile%^=%%
if   "%repl%"=="%inclist%" if exist "Pr22.Extension\%incfile%.cs" (
set  inclist=%inclist% %incfile%
echo   ^<Compile Include="..\..\Pr22.Extension\%incfile%.cs"^>
echo     ^<Link^>%incfile%.cs^</Link^>
echo   ^</Compile^>
)

goto :EOF

:remend
set %1=%2
goto :EOF

rem --- AssemblyInfo -----------------------------------------------------------

:AssemblyInfo

echo using System.Reflection;
echo using System.Runtime.CompilerServices;
echo using System.Runtime.InteropServices;
echo // General Information about an assembly is controlled through the following
echo // set of attributes. Change these attribute values to modify the information
echo // associated with an assembly.
echo [assembly: AssemblyTitle("%1")]
echo [assembly: AssemblyDescription("")]
echo [assembly: AssemblyConfiguration("")]
echo [assembly: AssemblyCompany("Adaptive Recognition")]
echo [assembly: AssemblyProduct("%1")]
echo [assembly: AssemblyCopyright("Copyright � 2021, Adaptive Recognition")]
echo [assembly: AssemblyTrademark("")]
echo [assembly: AssemblyCulture("")]
echo // Setting ComVisible to false makes the types in this assembly not visible
echo // to COM components.  If you need to access a type in this assembly from
echo // COM, set the ComVisible attribute to true on that type.
echo [assembly: ComVisible(false)]
echo // The following GUID is for the ID of the typelib if this project is exposed to COM
echo [assembly: Guid("83aaf6fe-42c6-48f6-a296-126b3a94a4aa")]
echo // Version information for an assembly consists of the following four values:
echo //
echo //      Major Version
echo //      Minor Version
echo //      Build Number
echo //      Revision
echo //
echo [assembly: AssemblyVersion("2.2.0.0")]
echo [assembly: AssemblyFileVersion("2.2.0.0")]

goto :EOF

rem --- Workspace --------------------------------------------------------------
:sln

setlocal enabledelayedexpansion

echo ﻿
echo Microsoft Visual Studio Solution File, Format Version 11.00
echo # Visual Studio 2010

set "Counter=0"
set "list="
for %%i in ( %PROJECTS% ) do (
	set /A "Counter+=1"
	call :GUID guid !Counter!
	if not "!list!"=="" set "list=!list!;"
	set "list=!list!!guid!"
	echo Project^("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"^) = "%%i", "vcs100\%%i\%%i.csproj", "{!guid!}"
	echo EndProject
)

echo Global
echo 	GlobalSection(SolutionConfigurationPlatforms) = preSolution
echo 		Debug^|Any CPU = Debug^|Any CPU
echo 		Release^|Any CPU = Release^|Any CPU
echo 	EndGlobalSection
echo GlobalSection(ProjectConfigurationPlatforms) = postSolution

for %%g in (%list%) do (
	echo		{%%g}.Debug^|Win32.ActiveCfg = Debug^|Win32
	echo		{%%g}.Debug^|Win32.Build.0 = Debug^|Win32
	echo		{%%g}.Debug^|x64.ActiveCfg = Debug^|x64
	echo		{%%g}.Debug^|x64.Build.0 = Debug^|x64
	echo		{%%g}.Release^|Win32.ActiveCfg = Release^|Win32
	echo		{%%g}.Release^|Win32.Build.0 = Release^|Win32
	echo		{%%g}.Release^|x64.ActiveCfg = Release^|x64
	echo		{%%g}.Release^|x64.Build.0 = Release^|x64
)

echo 	EndGlobalSection
echo 	GlobalSection(SolutionProperties) = preSolution
echo 		HideSolutionNode = FALSE
echo 	EndGlobalSection
echo EndGlobal

endlocal

goto :EOF

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:GUID <xReturn> <xStep>
:: Generate a GUID without delayed expansion.
setlocal
set "xStep=%~2"
set "xGUID="
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
set "xGUID=%xGUID%-"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
set "xGUID=%xGUID%-"
set "xGUID=%xGUID%4"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
set "xGUID=%xGUID%-"
set "xGUID=%xGUID%A"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
set "xGUID=%xGUID%-"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
call :AppendHex xGUID "%xGUID%" "%xStep%"
endlocal & if not "%~1"=="" set "%~1=%xGUID%"
goto :eof
:: by David Ruhmann

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:AppendHex <xReturn> <xInput> <xStep>
:: Append a hexidecimal number to the end of the input.
:: 1. Generate Random Number = 0-15
:: 2. Convert Number to Hexidecimal
:: 3. Append to Input
setlocal
set "xStep=%~3"
set /a xValue=%random%+%xStep%
set /a "xValue=%xValue% %% 16"
if "%xValue%"=="10" set "xValue=A"
if "%xValue%"=="11" set "xValue=B"
if "%xValue%"=="12" set "xValue=C"
if "%xValue%"=="13" set "xValue=D"
if "%xValue%"=="14" set "xValue=E"
if "%xValue%"=="15" set "xValue=F"
endlocal & if not "%~1"=="" set "%~1=%~2%xValue%"
goto :eof
:: by David Ruhmann


pause
