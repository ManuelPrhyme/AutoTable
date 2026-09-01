Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile('c:\Users\manue\Desktop\Desktop_Apps\AutoTable\report final.png')
Write-Output "Original: $($img.Width) x $($img.Height)"
$maxW = 600
$ratio = [double]$maxW / $img.Width
$newW = $maxW
$newH = [int]($img.Height * $ratio)
$resized = $img.GetThumbnailImage($newW, $newH, $null, [IntPtr]::Zero)

# Save as JPEG for smaller file
$jpegPath = 'c:\Users\manue\Desktop\Desktop_Apps\AutoTable\_report_final_resized.jpg'
$ms = New-Object System.IO.MemoryStream
$resized.Save($ms, [System.Drawing.Imaging.ImageFormat]::Jpeg)
[System.IO.File]::WriteAllBytes($jpegPath, $ms.ToArray())
Write-Output "Saved resized JPEG: $jpegPath"
Write-Output "File size: $((Get-Item $jpegPath).Length) bytes"
$ms.Dispose()
$img.Dispose()
$resized.Dispose()