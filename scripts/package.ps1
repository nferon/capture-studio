param([string]$CompilerPath='C:\Program Files (x86)\Inno Setup 6\ISCC.exe')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$dist=Join-Path $repo 'dist';$artifacts=Join-Path $repo 'artifacts'
$null=New-Item -ItemType Directory -Path $artifacts -Force
$version=[regex]::Match((Get-Content (Join-Path $repo 'VersionInfo.cs') -Raw),'ReleaseVersion="([0-9.]+)"').Groups[1].Value
if(!$version){throw 'Version absente'}
if(!(Test-Path -LiteralPath $CompilerPath)){throw 'Compilateur Inno Setup introuvable'}
& $CompilerPath "/DAppVersion=$version" "/DSourceDir=$dist" "/DOutputDir=$artifacts" (Join-Path $repo 'installer\CaptureStudio.iss')
if($LASTEXITCODE -ne 0){throw 'Compilation de l installateur impossible'}
$files=@('CaptureStudio.exe','CaptureStudio.exe.config','CaptureStudio.ico','ocr.ps1','README.md','Installer.cmd','Installer.ps1') | ForEach-Object {Join-Path $dist $_}
Compress-Archive -LiteralPath $files -DestinationPath (Join-Path $artifacts "CaptureStudio-Windows-$version.zip") -Force
Get-ChildItem -LiteralPath $artifacts -File | Where-Object {$_.Extension -in '.zip','.exe'} | ForEach-Object {"$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant())  $($_.Name)"} | Set-Content -LiteralPath (Join-Path $artifacts 'SHA256SUMS.txt') -Encoding ascii
