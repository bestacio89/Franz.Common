param(
    [Parameter(Mandatory = $true)]
    [string]$PackagesDirectory
)

$FEED_URL = "https://pkgs.dev.azure.com/EntelekheiaSystems/_packaging/AjnaTellus/nuget/v3/index.json"

Write-Host "Publishing packages from: $PackagesDirectory"

if (-not (Test-Path $PackagesDirectory)) {
    Write-Error "Package directory not found: $PackagesDirectory"
    exit 1
}

$packages = Get-ChildItem `
    -Path $PackagesDirectory `
    -Filter "*.nupkg" `
    -File |
    Sort-Object Name

if (-not $packages) {
    Write-Error "No .nupkg files found in $PackagesDirectory"
    exit 1
}

Write-Host "Found $($packages.Count) package(s):"

foreach ($pkg in $packages) {
    Write-Host " - $($pkg.Name)"
}

foreach ($pkg in $packages) {

    Write-Host ""
    Write-Host "Publishing $($pkg.Name)..."

    dotnet nuget push $pkg.FullName `
        --source $FEED_URL `
        --api-key az `
        --skip-duplicate

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to publish $($pkg.Name)"
        exit $LASTEXITCODE
    }
}

Write-Host ""
Write-Host "Publishing complete."