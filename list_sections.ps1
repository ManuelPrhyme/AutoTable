$lines = Get-Content 'C:\Users\manue\Desktop\AutoTable_Prod\AutoTable\Table.md'
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^##\s') { Write-Output ("{0,4}| {1}" -f ($i + 1), $lines[$i]) }
}