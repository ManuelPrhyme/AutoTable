    param(
    [string]$Project = "AutoTable.csproj"
)
Set-Location $PSScriptRoot
dotnet build $Project -p:Platform=x64 --no-restore 1>build_full.txt 2>&1
$code = $LASTEXITCODE
Set-Content build_exitcode.txt $code
Write-Host "BUILD EXIT CODE: $code"