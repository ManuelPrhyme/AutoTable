$i = 0
Get-Content 'C:\Users\manue\Desktop\AutoTable_Prod\AutoTable\Views\FeeCollectionView.xaml' | ForEach-Object {
    $i++
    if ($i -ge 62 -and $i -le 152) { '{0,3}: [{1}]' -f $i, $_ }
}