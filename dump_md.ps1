$i = 0
Get-Content 'C:\Users\manue\Desktop\AutoTable_Prod\AutoTable\Table.md' | ForEach-Object {
    $i++
    '{0,4}: [{1}]' -f $i, $_
}