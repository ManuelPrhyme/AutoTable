$exe = 'C:\Users\manue\Desktop\Desktop_Apps\AutoTable\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\AutoTable.exe'
$db = Join-Path $env:TEMP 'autotable_test.db'
$err = Join-Path $env:TEMP 'AutoTable_startup_error.txt'

Remove-Item $db -ErrorAction SilentlyContinue
Remove-Item $err -ErrorAction SilentlyContinue

$p = Start-Process -FilePath $exe -PassThru
Start-Sleep -Seconds 8
if (-not $p.HasExited) {
    Stop-Process -Id $p.Id -Force
    Write-Output 'App process survived startup (DB init OK).'
} else {
    Write-Output 'App exited before timeout.'
}

if (Test-Path $db) {
    Write-Output ('DB file present: ' + $db)
    $len = (Get-Item $db).Length
    Write-Output ('DB size: ' + $len + ' bytes')
    $tables = sqlite3 $db '.tables' 2>$null
    if ($null -ne $tables) { Write-Output ('Tables: ' + $tables) }
} else {
    Write-Output 'DB file MISSING.'
}

if (Test-Path $err) {
    Write-Output 'STARTUP ERROR DETECTED:'
    Get-Content $err
} else {
    Write-Output 'No startup error file.'
}