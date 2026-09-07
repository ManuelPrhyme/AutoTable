$lines = Get-Content 'C:\Users\manue\Desktop\AutoTable_Prod\AutoTable\Table.md'
Write-Output '===== 75-90 ====='
for ($i = 74; $i -le 89; $i++) { Write-Output ("{0,4}| {1}" -f ($i + 1), $lines[$i]) }
Write-Output '===== 195-215 (§4 even columns) ====='
for ($i = 194; $i -le 214; $i++) { Write-Output ("{0,4}| {1}" -f ($i + 1), $lines[$i]) }
Write-Output '===== 276-305 (§7b) ====='
for ($i = 275; $i -le 304; $i++) { Write-Output ("{0,4}| {1}" -f ($i + 1), $lines[$i]) }
Write-Output '===== 9-15 (§0) ====='
for ($i = 8; $i -le 14; $i++) { Write-Output ("{0,4}| {1}" -f ($i + 1), $lines[$i]) }
Write-Output '===== 364-378 (§8 checklist) ====='
for ($i = 363; $i -le 377; $i++) { Write-Output ("{0,4}| {1}" -f ($i + 1), $lines[$i]) }