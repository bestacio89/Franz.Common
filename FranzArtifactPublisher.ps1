# Specify the Azure Artifacts feed URL and PAT
$FEED_URL = "https://pkgs.dev.azure.com/AjnaTellus/Franz/_packaging/FranzArtifact/nuget/v3/index.json"
$PAT = "e4ugb7pl5qgpyoebosfko42ruem6wwmlqamwzkyl477amhc2byvq"

# Directory where NuGet packages will be built
$PACKAGES_DIRECTORY = "AzureArtifacts"

# Perform NuGet packaging
dotnet pack -c Release -o $PACKAGES_DIRECTORY

# Get the list of packages in the feed
$existingPackages = Get-PackageSource -ProviderName NuGet -Location $FEED_URL | Get-Package -AllVersions

# Publish packages to Azure Artifacts feed
$packages = Get-ChildItem -Path $PACKAGES_DIRECTORY -Filter *.nupkg
foreach ($package in $packages) {
    # Extract package metadata
    $packageInfo = [System.IO.Path]::GetFileNameWithoutExtension($package.Name) -split '\.'
    $packageName = $packageInfo[0]
    $packageVersion = $packageInfo[1]

    # Check if a package with the same version already exists in the feed
    $duplicatePackage = $existingPackages | Where-Object { $_.Id -eq $packageName -and $_.Version -eq $packageVersion }

    if ($null -eq $duplicatePackage) {
        Write-Host "Publishing $($package.Name)..."
        dotnet nuget push $package.FullName --source $FEED_URL --api-key $PAT --interactive
    } else {
        Write-Host "Skipping $($package.Name) (duplicate version)."
    }
}

# Remove all files from the directory
Remove-Item -Path "$PACKAGES_DIRECTORY\*" -Force

# Pause the script to keep the PowerShell window open
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

pause