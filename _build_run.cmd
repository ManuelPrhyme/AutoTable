@echo off
cd /d "c:\Users\manue\Desktop\Desktop_Apps\AutoTable"
echo Starting restore...
dotnet restore --nologo
if errorlevel 1 echo ___RESTORE_FAILED___ & exit /b 1
echo Starting build...
dotnet build -v:m --nologo > build_out.log 2>&1
if errorlevel 1 echo ___BUILD_FAILED___ >> build_out.log & exit /b 1
echo ___BUILD_DONE___ >> build_out.log
