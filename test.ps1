$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot.Replace('/', '\')
New-Item -ItemType Directory -Force -Path "$root\bin" | Out-Null
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /main:SmokeTests "/out:$root\bin\SmokeTests.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Runtime.Serialization.dll "$root\src\Listik.cs" "$root\tests\SmokeTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test build failed' }
$testData = Join-Path "$root\bin" ('test-data-' + [Guid]::NewGuid().ToString('N'))
try {
    & "$root\bin\SmokeTests.exe" $testData
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
} finally {
    $resolvedTestData = [IO.Path]::GetFullPath($testData)
    $allowedParent = [IO.Path]::GetFullPath((Join-Path $root 'bin'))
    if ([IO.Path]::GetDirectoryName($resolvedTestData) -ne $allowedParent -or [IO.Path]::GetFileName($resolvedTestData) -notmatch '^test-data-[a-f0-9]{32}$') {
        throw 'Unexpected test data path; refusing cleanup'
    }
    if (Test-Path -LiteralPath $resolvedTestData) { Remove-Item -LiteralPath $resolvedTestData -Recurse -Force }
}
