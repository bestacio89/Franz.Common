# Specify the NuGet repository URL
$FEED_URL = "https://api.nuget.org/v3/index.json"

# SECURE: Pull API Key from Environment Variable
# Set this in your terminal with: $env:NUGET_API_KEY = "your-key-here"
$API_KEY = $env:NUGET_API_KEY

if ([string]::IsNullOrWhiteSpace($API_KEY)) {
    Write-Host "Error: NUGET_API_KEY environment variable is not set." -ForegroundColor Red
    exit 1
}

$PACKAGES_DIRECTORY = "NuGetPackages"

if (-Not (Test-Path -Path $PACKAGES_DIRECTORY)) {
    Write-Host "Packages directory does not exist: $PACKAGES_DIRECTORY" -ForegroundColor Red
    exit 1
}

Write-Host "Packaging projects into NuGet packages..."
# Added explicit clean to ensure we don't pick up 'ghost' files
dotnet clean
dotnet pack -c Release -o $PACKAGES_DIRECTORY

Write-Host "Publishing packages to NuGet feed..."
$packages = Get-ChildItem -Path $PACKAGES_DIRECTORY -Filter *.nupkg

foreach ($package in $packages) {
    # Using 'dotnet nuget push' directly is safer than listing/checking via CLI
    # --skip-duplicate handles the 'already exists' check automatically on the server side
    Write-Host "Pushing $($package.Name)..."
    dotnet nuget push $package.FullName `
        --source $FEED_URL `
        --api-key $API_KEY `
        --skip-duplicate
}

Write-Host "Cleaning up package directory..."
Remove-Item -Path "$PACKAGES_DIRECTORY\*" -Force

Write-Host "Publishing complete."