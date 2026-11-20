Add-Type -AssemblyName PresentationCore,WindowsBase
foreach($brand in @('discord','whatsapp')){
 [xml]$svg=Get-Content -LiteralPath (Join-Path $PSScriptRoot ($brand+'.svg')) -Raw
 $geometry=[Windows.Media.Geometry]::Parse([string]$svg.svg.path.d)
 $visual=New-Object Windows.Media.DrawingVisual
 $drawing=$visual.RenderOpen()
 $drawing.PushTransform((New-Object Windows.Media.ScaleTransform (64/24),(64/24)))
 $drawing.DrawGeometry([Windows.Media.Brushes]::White,$null,$geometry)
 $drawing.Close()
 $bitmap=New-Object Windows.Media.Imaging.RenderTargetBitmap 64,64,96,96,([Windows.Media.PixelFormats]::Pbgra32)
 $bitmap.Render($visual)
 $encoder=New-Object Windows.Media.Imaging.PngBitmapEncoder
 $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
 $stream=[IO.File]::Create((Join-Path $PSScriptRoot ($brand+'.png')))
 try{$encoder.Save($stream)}finally{$stream.Dispose()}
}
