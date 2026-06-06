$FEED_URL = "https://pkgs.dev.azure.com/EntelekheiaSystems/_packaging/AjnaTellus/nuget/v3/index.json"
$PACKAGES_DIRECTORY = "$(System.DefaultWorkingDirectory)/packages"

Write-Host "Publishing packages from: $PACKAGES_DIRECTORY"

$packages = Get-ChildItem -Path $PACKAGES_DIRECTORY -Filter "*.nupkg" -Recurse |
            Sort-Object Name

if (-not $packages) {
    Write-Error "No NuGet packages found to publish."
    exit 1
}

foreach ($pkg in $packages) {

    Write-Host "Publishing $($pkg.Name)..."

    dotnet nuget push $pkg.FullName `
        --source $FEED_URL `
        --api-key az `
        --skip-duplicate
}

Write-Host "All packages published successfully."