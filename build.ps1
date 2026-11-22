$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Definition
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path $csc)) {
    throw '找不到 .NET Framework 4.x 的 csc.exe，请先安装 .NET Framework 4.8。'
}

$sources = @(Get-ChildItem (Join-Path $root 'src\*.cs') | ForEach-Object { $_.FullName })
if ($sources.Count -eq 0) { throw 'src 目录中没有源文件。' }

$out = Join-Path $root 'ddl计时器.exe'
$icon = Join-Path $root 'assets\icon.ico'
$manifest = Join-Path $root 'app.manifest'

$cscArgs = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/codepage:65001',
    '/utf8output',
    ('/out:' + $out),
    ('/win32icon:' + $icon),
    ('/win32manifest:' + $manifest),
    '/r:System.dll',
    '/r:System.Core.dll',
    '/r:System.Drawing.dll',
    '/r:System.Windows.Forms.dll',
    '/r:System.Runtime.Serialization.dll',
    '/r:System.Xml.dll'
) + $sources

& $csc $cscArgs
if ($LASTEXITCODE -ne 0) {
    throw "编译失败（csc 退出码 $LASTEXITCODE）。"
}
Write-Host ('已生成：' + $out)
