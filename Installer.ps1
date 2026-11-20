param([string]$TargetDirectory=(Join-Path $env:LOCALAPPDATA 'Programs\CaptureStudio'),[switch]$NoLaunch,[switch]$NoShortcut)
$ErrorActionPreference='Stop'
try {
 $target=[IO.Path]::GetFullPath($TargetDirectory)
 $null=New-Item -ItemType Directory -Path $target -Force
 foreach($name in @('CaptureStudio.exe','CaptureStudio.exe.config','CaptureStudio.ico','ocr.ps1','README.md')) {
  $source=Join-Path $PSScriptRoot $name
  $destination=Join-Path $target $name
  if([IO.Path]::GetFullPath($source) -ne [IO.Path]::GetFullPath($destination)) {Copy-Item -LiteralPath $source -Destination $destination -Force}
 }
 if(!$NoShortcut) {
  $shell=New-Object -ComObject WScript.Shell
  $shortcutPath=Join-Path ([Environment]::GetFolderPath('Programs')) 'Capture Studio.lnk'
  $shortcut=$shell.CreateShortcut($shortcutPath)
  $shortcut.TargetPath=Join-Path $target 'CaptureStudio.exe'
  $shortcut.WorkingDirectory=$target
  $shortcut.IconLocation=(Join-Path $target 'CaptureStudio.exe')+',0'
  $shortcut.Description='Capture Studio - captures et annotations'
  $shortcut.Save()
  Add-Type -AssemblyName System.Windows.Forms,System.Drawing
  $application=[Reflection.Assembly]::LoadFrom((Join-Path $target 'CaptureStudio.exe'))
  $application.GetType('Studio').GetMethod('StampShortcut').Invoke($null,@($shortcutPath.PSObject.BaseObject)) | Out-Null
 }
 Write-Output "Capture Studio installe dans : $target"
 if(!$NoLaunch){Start-Process -FilePath (Join-Path $target 'CaptureStudio.exe') -WorkingDirectory $target}
} catch {Write-Error ('Installation impossible. Fermez Capture Studio avant une mise a jour. '+$_.Exception.Message);exit 1}
