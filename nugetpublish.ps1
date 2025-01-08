# Specify the NuGet repository URL and API key
$FEED_URL = "https://api.nuget.org/v3/index.json" # Replace with your NuGet feed URL if different
$API_KEY = "oy2c4zz565tuclyxijrakv3xlbzcs2quvkyyhlbkqq7ycm" # Your NuGet API key

# Directory where NuGet packages are located
$PACKAGES_DIRECTORY = "AzureArtifacts" # Replace with the path to your NuGet packages directory

# Ensure the directory exists
if (-Not (Test-Path -Path $PACKAGES_DIRECTORY)) {
    Write-Host "Packages directory does not exist: $PACKAGES_DIRECTORY" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Retrieve a list of all existing packages from the feed
Write-Host "Retrieving existing packages from the feed..."
$existingPackages = & dotnet nuget list source --source $FEED_URL --api-key $API_KEY
if (!$existingPackages) {
    Write-Host "Failed to retrieve packages from the feed." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Publish each package in the directory
Write-Host "Publishing packages..."
$packages = Get-ChildItem -Path $PACKAGES_DIRECTORY -Filter *.nupkg
foreach ($package in $packages) {
    # Extract package metadata
    $packageInfo = [System.IO.Path]::GetFileNameWithoutExtension($package.Name) -split '\.'
    $packageName = $packageInfo[0]
    $packageVersion = $packageInfo[1]

    # Check if a package with the same name and version already exists
    $duplicatePackage = $existingPackages | Where-Object { $_.Id -eq $packageName -and $_.Version -eq $packageVersion }
    if ($null -eq $duplicatePackage) {
        Write-Host "Publishing $($package.Name)..."
        dotnet nuget push $package.FullName --source $FEED_URL --api-key $API_KEY --skip-duplicate
    } else {
        Write-Host "Skipping $($package.Name) (duplicate version)."
    }
}

# Clean up the package directory
Write-Host "Cleaning up package directory..."
Remove-Item -Path "$PACKAGES_DIRECTORY\*" -Force

Write-Host "Publishing complete."

# Pause to keep the window open
Read-Host "Press Enter to close the script"
