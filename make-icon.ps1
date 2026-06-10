Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap(1024, 1024)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::FromArgb(255, 12, 12, 14))
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 90, 31))
$g.FillEllipse($brush, 112, 112, 800, 800)
$font = New-Object System.Drawing.Font("Arial", 380, [System.Drawing.FontStyle]::Bold)
$tb = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$sf = New-Object System.Drawing.StringFormat
$sf.Alignment = [System.Drawing.StringAlignment]::Center
$sf.LineAlignment = [System.Drawing.StringAlignment]::Center
$rect = New-Object System.Drawing.RectangleF(0, 50, 1024, 1024)
$g.DrawString("L", $font, $tb, $rect, $sf)
$g.Dispose()
$bmp.Save("C:\Users\JosefAxen\Gym\LockIn\Resources\AppIcon\appicon.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "Done"
