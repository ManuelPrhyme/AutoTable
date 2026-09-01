param(
    [switch]$NoClean
)
Set-Location $PSScriptRoot

# ---------------------------------------------------------------
# Synchronous build runner for the AutoTable WinUI project.
# Handles this environment's flaky XAML-compiler staging:
#   - kills lingering MSBuild/XamlCompiler processes that lock obj\*
#   - cleans obj/bin (unless -NoClean)
#   - retries once, self-staging App.xaml into the win-x64
#     intermediate folder if the compiler failed to emit it (WMC0601)
# ---------------------------------------------------------------

$pidFile = Join-Path $PSScriptRoot 'build_pid.txt'
$doneFile = Join-Path $PSScriptRoot 'build_done.txt'
$out = Join-Path $PSScriptRoot 'build_full.txt'
$err = Join-Path $PSScriptRoot 'build_err.txt'
Remove-Item $doneFile -Force -ErrorAction SilentlyContinue
# This process (the runner) is what the poller watches.
Set-Content $pidFile $PID

$env:MSBUILDDISABLENODEREUSE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

function Stop-BuildProcesses {
    Get-Process -Name MSBuild,dotnet,VBCSCompiler,XamlCompiler -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    & taskkill /F /IM MSBuild.exe /T 2>$null
    & taskkill /F /IM dotnet.exe /T 2>$null
    & taskkill /F /IM XamlCompiler.exe /T 2>$null
    & taskkill /F /IM VBCSCompiler.exe /T 2>$null
    Start-Sleep -Seconds 2
}

function Invoke-DotnetBuild {
    Remove-Item $out,$err -Force -ErrorAction SilentlyContinue
    $p = Start-Process -FilePath 'dotnet' `
        -ArgumentList 'build','AutoTable.csproj','-p:Platform=x64' `
        -WorkingDirectory $PSScriptRoot `
        -RedirectStandardOutput $out `
        -RedirectStandardError $err `
        -WindowStyle Hidden -PassThru
    $null = $p.WaitForExit()
    $code = 0
    try { $code = [int]$p.ExitCode } catch { $code = 1 }
    return $code
}

function Test-StagedAppXaml {
    $staged = Get-ChildItem obj -Recurse -Filter 'MainWindow.xaml' -File -ErrorAction SilentlyContinue |
        Where-Object { $_.DirectoryName -match '\\win-x64$' } | Select-Object -First 1
    return ($null -ne $staged) -and (Test-Path (Join-Path $staged.DirectoryName 'App.xaml'))
}

function Stage-AppXaml {
    $staged = Get-ChildItem obj -Recurse -Filter 'MainWindow.xaml' -File -ErrorAction SilentlyContinue |
        Where-Object { $_.DirectoryName -match '\\win-x64$' } | Select-Object -First 1
    if ($null -ne $staged -and (Test-Path (Join-Path $PSScriptRoot 'App.xaml'))) {
        Copy-Item (Join-Path $PSScriptRoot 'App.xaml') (Join-Path $staged.DirectoryName 'App.xaml') -Force
        Write-Output "STAGED App.xaml into $($staged.DirectoryName)"
        return $true
    }
    return $false
}

Stop-BuildProcesses

if (-not $NoClean) {
    Remove-Item 'obj' -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item 'bin' -Recurse -Force -ErrorAction SilentlyContinue
    Write-Output 'cleaned obj/bin'
}

Write-Output 'BUILD ATTEMPT 1/2'
$code = Invoke-DotnetBuild

if ($code -ne 0 -and -not (Test-StagedAppXaml)) {
    Write-Output 'BUILD ATTEMPT 2/2 (after staging App.xaml)'
    $null = Stage-AppXaml
    Stop-BuildProcesses
    $code = Invoke-DotnetBuild
}

Set-Content $doneFile ([string][int]$code)
Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
Write-Output ("BUILD EXIT CODE: " + $code)