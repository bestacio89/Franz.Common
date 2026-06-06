param(
    [Parameter(Mandatory = $true)]
    [string]$PackagesDirectory
)

$FEED_URL = "https://api.nuget.org/v3/index.json"
$API_KEY = $env:NUGET_API_KEY

if ([string]::IsNullOrWhiteSpace($API_KEY)) {
    Write-Error "NUGET_API_KEY environment variable is not set."
    exit 1
}

if (-not (Test-Path $PackagesDirectory)) {
    Write-Error "Package directory not found: $PackagesDirectory"
    exit 1
}

$packages = Get-ChildItem `
    -Path $PackagesDirectory `
    -Filter "*.nupkg" `
    -File |
    Sort-Object Name

if ($packages.Count -eq 0) {
    Write-Error "No packages found in $PackagesDirectory"
    exit 1
}

Write-Host "Found $($packages.Count) package(s)"

foreach ($package in $packages) {

    Write-Host "Publishing $($package.Name)..."

    dotnet nuget push $package.FullName `
        --source $FEED_URL `
        --api-key $API_KEY `
        --skip-duplicate

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed publishing $($package.Name)"
        exit $LASTEXITCODE
    }
}

Write-Host "NuGet publishing complete."