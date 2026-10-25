$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot.Replace('/', '\')
New-Item -ItemType Directory -Force -Path "$root/bin" | Out-Null
$compiler = "$env:WINDIR/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
$setupSources = @(Get-ChildItem -LiteralPath "$root\src" -Filter 'Setup*.cs' | ForEach-Object { $_.FullName })
$appSources = @(Get-ChildItem -LiteralPath "$root\src" -Filter '*.cs' | Where-Object { $_.Name -notlike 'Setup*.cs' } | ForEach-Object { $_.FullName })
& $compiler /nologo /target:exe "/out:$root\bin\DrawIcon.exe" /reference:System.Drawing.dll "$root\tools\DrawIcon.cs"
if ($LASTEXITCODE -ne 0) { throw 'Icon generator build failed' }
& "$root\bin\DrawIcon.exe" "$root\bin\Listik.ico" "$root\bin\Listik-icon.png"
if ($LASTEXITCODE -ne 0) { throw 'Icon generation failed' }
& $compiler /nologo /target:winexe "/win32icon:$root\bin\Listik.ico" "/out:$root\bin\Listik.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Runtime.Serialization.dll @appSources
if ($LASTEXITCODE -ne 0) { throw 'Application build failed' }
& $compiler /nologo /target:winexe "/win32icon:$root\bin\Listik.ico" "/out:$root\bin\ListikSetup.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/resource:$root\bin\Listik.exe,Listik.exe" "/resource:$root\bin\Listik-icon.png,Listik-icon.png" @setupSources
if ($LASTEXITCODE -ne 0) { throw 'Installer build failed' }
