$i = 0
Get-Content 'C:\Users\manue\Desktop\AutoTable_Prod\AutoTable\Table.md' | ForEach-Object {
    $i++
    if ($i -ge 260 -and $i -le 320) { '{0,4}: [{1}]' -f $i, $_ }
}