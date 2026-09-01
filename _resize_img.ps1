Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile('c:\Users\manue\Desktop\Desktop_Apps\AutoTable\report final.png')
Write-Output "Original: $($img.Width) x $($img.Height)"
$maxW = 300
$ratio = [double]$maxW / $img.Width
$newW = $maxW
$newH = [int]($img.Height * $ratio)
$resized = $img.GetThumbnailImage($newW, $newH, $null, [IntPtr]::Zero)
$ms = New-Object System.IO.MemoryStream
$resized.Save($ms, [System.Drawing.Imaging.ImageFormat]::Jpeg)
$b64 = [Convert]::ToBase64String($ms.ToArray())
Set-Content -Path "_img_b64_small.txt" -Value $b64
Write-Output "Resized: $newW x $newH"
Write-Output "Base64 length: $($b64.Length) chars"
$img.Dispose()
$resized.Dispose()
$ms.Dispose()