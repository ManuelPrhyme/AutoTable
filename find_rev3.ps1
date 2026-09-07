$path = 'C:/Users/manue/Desktop/AutoTable_Prod/AutoTable/ViewModels/DashboardViewModel.cs'
$lines = Get-Content $path
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'Revenue|GetFeePayments|GetActiveTerm|LoadAsync') {
        Write-Output ("{0}: {1}" -f ($i+1), $lines[$i])
    }
}
Write-Output '----- Full LoadAsync region -----'
$start = ($lines | Select-String -Pattern 'public async Task LoadAsync' | Select-Object -First 1).LineNumber
if ($start) { $lines[($start-1)..([Math]::Min($start+79, $lines.Count-1))] }