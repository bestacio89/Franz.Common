# Define Azure Artifacts feed URL and PAT
$FEED_URL = "https://pkgs.dev.azure.com/AjnaTellus/Franz/_packaging/FranzArtifacts/nuget/v3/index.json"
$PAT = "EMWFEqQv9Ruq6TpjpdJCVpeBJM4NujVB4QQ2AZyBWOhyx5TlButEJQQJ99BAACAAAAAc3GLLAAASAZDOoTeM"

# Directory where NuGet packages will be built
$PACKAGES_DIRECTORY = "AzureArtifacts"

# Perform NuGet packaging
dotnet pack -c Release -o $PACKAGES_DIRECTORY

# List of packages in the directory
$packages = Get-ChildItem -Path $PACKAGES_DIRECTORY -Filter *.nupkg
foreach ($package in $packages) {
    # Extract package metadata using regex
    if ($package.Name -match "^(?<name>.+?)\.(?<version>(\d+\.)+\d+)\.nupkg$") {
        $packageName = $matches.name
        $packageVersion = $matches.version

        # Check if the package already exists in Azure Artifacts
        $existingPackage = & dotnet nuget list source -s $FEED_URL | Select-String "$packageName $packageVersion"

        if ($null -eq $existingPackage) {
            Write-Host "Publishing $($package.Name)..."
            dotnet nuget push $package.FullName --source $FEED_URL --api-key $PAT --skip-duplicate
        } else {
            Write-Host "Skipping $($package.Name) (duplicate version)."
        }
    } else {
        Write-Host "Skipping $($package.Name) (invalid filename format)."
    }
}

# Clean up the packages directory
Remove-Item -Path "$PACKAGES_DIRECTORY\*" -Force -Recurse

# Pause for user interaction
Write-Host "Process completed."
Read-Host "Press Enter to exit"
