$f = 'C:\Users\manue\Desktop\AutoTable_Prod\AutoTable\ViewModels\DashboardViewModel.cs'
$lines = Get-Content $f
Write-Host ('TOTAL LINES: ' + $lines.Count)
$i = 0
foreach ($l in $lines) {
    $i++
    if ($l -match 'Revenue|Payment|Term|ActiveTerm|LoadAsync|Toasts') {
        Write-Host ("{0}: {1}" -f $i, $l)
    }
}
