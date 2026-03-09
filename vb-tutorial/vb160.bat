@echo off
if "%1"=="" (
	call :generate %*
) else (
	call :%*
)
goto :EOF

rem --- Generate ---------------------------------------------------------------
:generate

echo Generating project files for Visual Studio 2019 .NET VB ...

mkdir vb160

set PROJECTS=
for /f "tokens=1,2 delims=." %%i in ('dotnet --version') do @set dver=net%%i.%%j

for %%i in ( *.vb ) do (
	call :add %%~ni
	mkdir vb160\%%~ni
	call :vbproj %%~ni > vb160\%%~ni\%%~ni.vbproj
)

call :sln > vb160.sln

goto :EOF

rem --- Projects list ----------------------------------------------------------
:add

set PROJECTS=%PROJECTS%^ %*

goto :EOF

rem --- Project ----------------------------------------------------------------

:vbproj

echo ﻿^<Project Sdk="Microsoft.NET.Sdk"^>
echo.
echo   ^<PropertyGroup^>
echo     ^<OutputType^>Exe^</OutputType^>
echo     ^<TargetFramework^>%dver%^</TargetFramework^>
echo   ^</PropertyGroup^>
echo.
echo   ^<ItemGroup^>
echo     ^<Compile Include="..\..\%1.vb" Link="%1.vb" /^>
setlocal
set inclist=.
for /f "tokens=*" %%f in ('findstr /l "Extension." %1.vb') do call :include "%%f"
endlocal
echo   ^</ItemGroup^>
echo.
echo   ^<ItemGroup^>
echo     ^<Reference Include="Pr22"^>
echo       ^<HintPath^>..\..\..\Pr22.dll^</HintPath^>
echo     ^</Reference^>
echo   ^</ItemGroup^>
echo.
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
if   "%repl%"=="%inclist%" if exist "Pr22.Extension\%incfile%.vb" (
set  inclist=%inclist% %incfile%
echo     ^<Compile Include="..\..\Pr22.Extension\%incfile%.vb" Link="%incfile%.vb" /^>
)

goto :EOF

:remend
set %1=%2
goto :EOF

rem --- Workspace --------------------------------------------------------------
:sln

setlocal enabledelayedexpansion

echo ﻿
echo Microsoft Visual Studio Solution File, Format Version 12.00
echo # Visual Studio Version 16
echo VisualStudioVersion = 16.0.29519.181
echo MinimumVisualStudioVersion = 10.0.40219.1

set "Counter=0"
set "list="
for %%i in ( %PROJECTS% ) do (
	set /A "Counter+=1"
	call :GUID guid !Counter!
	if not "!list!"=="" set "list=!list!;"
	set "list=!list!!guid!"
	echo Project^("{54DB8EDF-3F83-4BEB-BE6B-559620A583C0}"^) = "%%i", "vb160\%%i\%%i.vbproj", "{!guid!}"
	echo EndProject
)

echo Global
echo 	GlobalSection(SolutionConfigurationPlatforms) = preSolution
echo 		Debug^|Any CPU = Debug^|Any CPU
echo 		Release^|Any CPU = Release^|Any CPU
echo 	EndGlobalSection
echo GlobalSection(ProjectConfigurationPlatforms) = postSolution

for %%g in (%list%) do (
	echo		{%%g}.Debug^|Any CPU.ActiveCfg = Debug^|Any CPU
	echo		{%%g}.Debug^|Any CPU.Build.0 = Debug^|Any CPU
	echo		{%%g}.Release^|Any CPU.ActiveCfg = Release^|Any CPU
	echo		{%%g}.Release^|Any CPU.Build.0 = Release^|Any CPU
)

echo 	EndGlobalSection
echo 	GlobalSection(SolutionProperties) = preSolution
echo 		HideSolutionNode = FALSE
echo 	EndGlobalSection
echo    GlobalSection(ExtensibilityGlobals) = postSolution
echo       SolutionGuid = {9CB1AC86-52A7-42DA-B50C-AAEF9D355CDC}
echo    EndGlobalSection
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
