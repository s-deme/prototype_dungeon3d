param(
    [Parameter(Mandatory = $true)]
    [string]$UnityPath
)

$projectPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity executable was not found: $UnityPath"
}

& $UnityPath -batchmode -nographics -quit -projectPath $projectPath -executeMethod ArcaneDepthsBuild.BuildWindows64 -logFile (Join-Path $projectPath 'Builds/Windows/build.log')
if ($LASTEXITCODE -ne 0) {
    throw "Unity build failed with exit code $LASTEXITCODE."
}
