$ErrorActionPreference = 'Continue'
$vm = 'C:\Users\manue\Desktop\AutoTable_Prod\AutoTable\ViewModels\DashboardViewModel.cs'
$view = 'C:\Users\manue\Desktop\AutoTable_Prod\AutoTable\Views\DashboardView.xaml'
Write-Host '=== DashboardViewModel.cs ==='
$m = Select-String -Path $vm -Pattern 'Revenue|Collected|ActiveTerm|FeePayment|GetFee'
if ($m) { $m | ForEach-Object { Write-Host ($_.LineNumber.ToString() + ': ' + $_.Line.Trim()) } } else { Write-Host 'NO MATCHES' }
Write-Host '=== DashboardView.xaml ==='
$m2 = Select-String -Path $view -Pattern 'Revenue|Collected'
if ($m2) { $m2 | ForEach-Object { Write-Host ($_.LineNumber.ToString() + ': ' + $_.Line.Trim()) } } else { Write-Host 'NO MATCHES' }
