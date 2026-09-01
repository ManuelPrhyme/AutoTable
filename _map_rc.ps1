$c = Get-Content 'Views\ReportCardsView.xaml.cs'
Write-Output ('TOTAL LINES: ' + $c.Count)
Write-Output '=== method signatures & key refs ==='
$patterns = @(
    '^(\s)*(public|private|protected|internal)(\s)+(async\s+)?(static\s+)?(sealed\s+)?(partial\s+)?(override\s+)?(void|Task|Task<|bool|int|string|List<|IReadOnlyList<|byte\[\]|UIElement|ReportCardSheetView|PrintDocument|IPrintDocumentSource)',
    'ShowPreviewAndPrintAsync',
    'QuestReportGenerator',
    'ContentDialog',
    'BuildSheetAsync',
    '_previewViewbox',
    'BuildSheetsAsync',
    'ShowPrintDialog'
)
for ($i = 0; $i -lt $c.Count; $i++) {
    $line = $c[$i]
    foreach ($pat in $patterns) {
        if ($line -match $pat) {
            Write-Output (($i+1).ToString().PadLeft(5) + '| ' + $line.Trim()) -replace '[[\]{}]','']
            break
        }
    }
}