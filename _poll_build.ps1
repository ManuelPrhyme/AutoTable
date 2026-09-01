Set-Location $PSScriptRoot

$pidFile = Join-Path $PSScriptRoot 'build_pid.txt'
$doneFile = Join-Path $PSScriptRoot 'build_done.txt'

if (-not (Test-Path $pidFile)) {
    if (-not (Test-Path $doneFile)) {
        Write-Output 'no build in progress (no build_pid.txt)'
        exit
    }
} else {
    $bp = (Get-Content $pidFile).Trim()
    $proc = Get-Process -Id $bp -ErrorAction SilentlyContinue
    if ($proc) {
        Write-Output ('BUILD RUNNING runner-pid=' + $bp + ' elapsed=' + [int]((Get-Date) - $proc.StartTime).TotalSeconds + 's')
        exit
    }
}

Write-Output 'BUILD FINISHED'
if (Test-Path $doneFile) {
    $code = (Get-Content $doneFile -Raw -ErrorAction SilentlyContinue)
    Write-Output ('dotnet exit code: ' + $code.Trim())
}
Write-Output '=== relevant build output ==='
if (Test-Path (Join-Path $PSScriptRoot 'build_full.txt')) {
    Get-Content (Join-Path $PSScriptRoot 'build_full.txt') |
        Select-String -Pattern 'error |Build succeeded|Build FAILED|WMC9999|WMC0001|CS1061|output.json was not created|STAGED App.xaml' |
        Select-Object -Last 40 | ForEach-Object { $_.Line }
}