@echo off
echo %1
:: this script needs https://www.nuget.org/packages/ilmerge

:: set your target executable name (typically [projectname].exe)
SET APP_NAME=FileVerifierConsole.exe
set APP_NAME2=fvconsole.exe

:: set your NuGet ILMerge Version, this is the number from the package manager install, for example:
:: PM> Install-Package ilmerge -Version 3.0.29
:: to confirm it is installed for a given project, see the packages.config file
SET ILMERGE_VERSION=3.0.29

:: the full ILMerge should be found here:
SET ILMERGE_PATH=%USERPROFILE%\.nuget\packages\ilmerge\%ILMERGE_VERSION%\tools\net452
:: dir "%ILMERGE_PATH%"\ILMerge.exe

echo Merging %APP_NAME% ...

:: add project DLL's starting with replacing the FirstLib with this project's DLL
:: Note: When used as a post-build event, this will be called from directory containing
:: the built .exes and .dlls. 
"%ILMERGE_PATH%"\ILMerge.exe %APP_NAME%  ^
  /lib:%1 ^
  /out:%APP_NAME2% ^
  fileverifier.dll ^
  commandline.dll ^
  newtonsoft.json.dll ^ 


:Done
dir %APP_NAME%