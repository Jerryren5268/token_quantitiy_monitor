$ErrorActionPreference = 'Stop'
$petCompiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$petSources = Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object FullName
$petIcon = Join-Path $PSScriptRoot 'cat.ico'
$petExtra = @()
if (Test-Path -LiteralPath $petIcon) { $petExtra += ('/win32icon:' + $petIcon) }
& $petCompiler /nologo /target:winexe /codepage:65001 /optimize+ ('/out:' + (Join-Path $PSScriptRoot 'LMServicePet.exe')) /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Core.dll $petExtra $petSources
if ($LASTEXITCODE -ne 0) { throw 'Pixel pet compilation failed.' }
