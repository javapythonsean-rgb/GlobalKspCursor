param(
    [string]$Ksp2Root = 'C:\KSP2\Kerbal.Space.Program.2.v0.2.2.0.32913\Kerbal.Space.Program.2.v0.2.2.0.32913'
)

$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$managed = Join-Path $Ksp2Root 'KSP2_x64_Data\Managed'
$output = Join-Path $projectRoot 'dist\GlobalKspCursor'
$dotnet = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Data\DotNetSdk\dotnet.exe'
$compiler = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Data\DotNetSdk\sdk\8.0.318\Roslyn\bincore\csc.dll'

if (!(Test-Path -LiteralPath $dotnet) -or !(Test-Path -LiteralPath $compiler)) {
    throw 'Unity Roslyn compiler was not found.'
}

if (!(Test-Path -LiteralPath $managed)) {
    throw "KSP 2 managed assembly folder not found: $managed"
}

New-Item -ItemType Directory -Path $output -Force | Out-Null

& $dotnet $compiler /nologo /target:library /optimize+ /nostdlib+ /langversion:latest `
    /out:"$(Join-Path $output 'GlobalKspCursor.dll')" `
    /reference:"$(Join-Path $managed 'mscorlib.dll')" `
    /reference:"$(Join-Path $managed 'System.dll')" `
    /reference:"$(Join-Path $managed 'System.Core.dll')" `
    /reference:"$(Join-Path $managed 'Assembly-CSharp.dll')" `
    /reference:"$(Join-Path $managed 'SpaceWarp2.dll')" `
    /reference:"$(Join-Path $managed 'netstandard.dll')" `
    /reference:"$(Join-Path $managed 'UnityEngine.AssetBundleModule.dll')" `
    /reference:"$(Join-Path $managed 'UnityEngine.CoreModule.dll')" `
    /reference:"$(Join-Path $managed 'UnityEngine.InputLegacyModule.dll')" `
    /reference:"$(Join-Path $managed 'UnityEngine.IMGUIModule.dll')" `
    "$(Join-Path $projectRoot 'src\GlobalKspCursorMod.cs')"

if ($LASTEXITCODE -ne 0) {
    throw "C# compilation failed with exit code $LASTEXITCODE"
}

Copy-Item -LiteralPath (Join-Path $projectRoot 'swinfo.json') -Destination (Join-Path $output 'swinfo.json') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination (Join-Path $output 'LICENSE') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $output 'README.md') -Force

# Remove only the stale stock asset from older local builds; never ship it.
$oldAsset = Join-Path $output 'assets\cursor_arrow@2x.png'
if (Test-Path -LiteralPath $oldAsset) {
    Remove-Item -LiteralPath $oldAsset -Force
}

Write-Output $output
