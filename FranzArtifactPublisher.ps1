# Configuration
$FEED_NAME = "FranzArtifact"
$FEED_URL  = "https://pkgs.dev.azure.com/EntelekheiaSystems/_packaging/AjnaTellus/nuget/v3/index.json"
$PAT       = $env:PAT # Use environment variable for security
$PACKAGES_DIRECTORY = "AzureArtifacts"
# Path to your specific Business project
$PROJECT_PATH = "sources/Franz.Common.Business/Franz.Common.Business.csproj"

# 1. Clean Room Setup
Write-Host "Cleaning workspace..."
Remove-Item -Path $PACKAGES_DIRECTORY -Recurse -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $PACKAGES_DIRECTORY | Out-Null

# 2. Strict Build & Pack
Write-Host "Building and packaging $PROJECT_PATH..."
# Force clean build of the specific project to ensure no 'ghost' binaries
dotnet clean $PROJECT_PATH
dotnet build $PROJECT_PATH -c Release
# Pack ONLY the specific project, not the whole folder
dotnet pack $PROJECT_PATH -c Release -o $PACKAGES_DIRECTORY --no-build

# 3. Verify Metadata before pushing
$nupkg = Get-ChildItem -Path $PACKAGES_DIRECTORY -Filter "*.nupkg" | Select-Object -First 1
if ($null -eq $nupkg) {
    Write-Error "Failed to locate generated nupkg. Build failed."
    exit 1
}
Write-Host "Verifying package: $($nupkg.Name)..."
# (Optional) Add a step here to unzip and check the DLL AssemblyVersion if you want 100% certainty

# 4. Authenticated Publish
$nugetConfigPath = Join-Path $PACKAGES_DIRECTORY "nuget.config"
$nugetConfig = @"
<configuration>
  <packageSources>
    <add key="$FEED_NAME" value="$FEED_URL" />
  </packageSources>
  <packageSourceCredentials>
    <$FEED_NAME>
      <add key="Username" value="anyvalue" />
      <add key="ClearTextPassword" value="$PAT" />
    </$FEED_NAME>
  </packageSourceCredentials>
</configuration>
"@
$nugetConfig | Out-File -FilePath $nugetConfigPath -Encoding UTF8

Write-Host "Publishing to Azure Artifacts..."
dotnet nuget push $nupkg.FullName `
    --source $FEED_NAME `
    --api-key az `
    --skip-duplicate `
    --configfile $nugetConfigPath

Write-Host "Publishing complete."