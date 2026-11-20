$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$b=New-Object Drawing.Bitmap 64,64
$g=[Drawing.Graphics]::FromImage($b)
$g.SmoothingMode='AntiAlias'
$g.Clear([Drawing.Color]::Transparent)
$brush=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(108,77,235))
$g.FillEllipse($brush,1,1,62,62)
$pen=New-Object Drawing.Pen ([Drawing.Color]::White),3
$g.DrawRectangle($pen,12,20,40,28)
$g.DrawEllipse($pen,24,25,16,16)
$g.DrawLine($pen,23,15,41,15)
$g.Dispose();$brush.Dispose();$pen.Dispose()
$stream=New-Object IO.MemoryStream
$b.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
$png=$stream.ToArray();$stream.Dispose();$b.Dispose()
$file=[IO.File]::Create((Join-Path $PSScriptRoot 'CaptureStudio.ico'))
$writer=New-Object IO.BinaryWriter $file
try {
 $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]1)
 $writer.Write([byte]64);$writer.Write([byte]64);$writer.Write([byte]0);$writer.Write([byte]0)
 $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$png.Length);$writer.Write([uint32]22);$writer.Write($png)
} finally {$writer.Dispose()}
