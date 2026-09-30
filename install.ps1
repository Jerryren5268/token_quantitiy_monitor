param([string]$ProfilePath = [Environment]::GetFolderPath('UserProfile'))
$ErrorActionPreference = 'Stop'
$destination = Join-Path $ProfilePath 'AppData\Local\LMServiceQuota\App'
$desktop = Join-Path $ProfilePath 'Desktop'
$startup = Join-Path $ProfilePath 'AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup'
foreach ($petProcess in @(Get-Process -Name LMServicePet -ErrorAction SilentlyContinue)) {
    if ($petProcess.Path -eq (Join-Path $destination 'LMServicePet.exe')) {
        throw '请先右键正在运行的小猫，选择“退出桌宠”，再重新安装。'
    }
}
New-Item -ItemType Directory -Path $destination -Force | Out-Null
foreach ($file in @('LMServicePet.exe','LMServicePet.exe.config','account_service.py','cat.ico','使用说明.md')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination (Join-Path $destination $file) -Force
}
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'runtime')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'runtime') -Destination $destination -Recurse -Force
}
$shell = New-Object -ComObject WScript.Shell
foreach ($folder in @($desktop,$startup)) {
    $shortcut = $shell.CreateShortcut((Join-Path $folder 'LMService 额度.lnk'))
    $shortcut.TargetPath = Join-Path $destination 'LMServicePet.exe'
    $shortcut.Arguments = ''
    $shortcut.WorkingDirectory = $destination
    $shortcut.IconLocation = (Join-Path $destination 'LMServicePet.exe') + ',0'
    $shortcut.Description = 'LMService 像素猫 — 点击查看额度'
    $shortcut.WindowStyle = 1
    $shortcut.Save()
}
Write-Output $destination
