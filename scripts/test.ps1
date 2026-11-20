param([string]$BinaryDirectory=(Join-Path $PSScriptRoot '..\dist'),[switch]$IncludeOcr,[switch]$IncludeNetwork)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$bin=[IO.Path]::GetFullPath($BinaryDirectory)
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$tests=@('VerifyUpdates','VerifyCaptureDelay','VerifyClearCapture','VerifyExportShortcuts')
if($IncludeOcr){$tests+='VerifyOcrSelection'}
if($IncludeNetwork){$tests+='VerifyLiveUpdates'}
foreach($test in $tests){
 & $compiler /nologo /target:exe /out:"$bin\$test.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Net.Http.dll /r:"$bin\CaptureStudio.exe" (Join-Path $repo "tests\$test.cs")
 if($LASTEXITCODE -ne 0){throw "Compilation du test $test impossible"}
 Copy-Item -LiteralPath (Join-Path $bin 'CaptureStudio.exe.config') -Destination (Join-Path $bin "$test.exe.config") -Force
 Push-Location $bin
 try{& (Join-Path $bin "$test.exe");if($LASTEXITCODE -ne 0){throw "Echec de $test"}}finally{Pop-Location}
}

