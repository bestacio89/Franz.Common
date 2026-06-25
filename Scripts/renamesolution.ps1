param (
    [Alias('sp')]
    [string]$SourceProjectName = "Franz.Common",

    [Alias('tp')]
    [string]$TargetProjectName = "Pangea.Framework",

    [Alias('root')]
    [string]$TargetProjectRootDirectory = ".."
)

$ErrorActionPreference = "Stop"

$SourceRoot = (Resolve-Path "..").Path
$TargetRoot = Join-Path $TargetProjectRootDirectory $TargetProjectName

$SourceSolution = Join-Path $SourceRoot "$SourceProjectName.sln"
$TargetSolution = Join-Path $TargetRoot "$TargetProjectName.sln"

Write-Host "== Migrating $SourceProjectName → $TargetProjectName =="

# =========================================================
# 1. COPY STRUCTURE (NO MODIFICATION)
# =========================================================
if (-not (Test-Path $TargetRoot)) {
    New-Item -ItemType Directory -Path $TargetRoot | Out-Null
}

Copy-Item -Path "$SourceRoot/*" -Destination $TargetRoot -Recurse -Force

# =========================================================
# 2. RENAME FILES + FOLDERS (SCOPED ONLY)
# =========================================================
Get-ChildItem $TargetRoot -Recurse | ForEach-Object {

    $newName = $_.Name -replace "^$SourceProjectName", $TargetProjectName

    if ($_.Name -ne $newName) {
        Rename-Item $_.FullName $newName
    }
}

# =========================================================
# 3. UPDATE CSPROJ METADATA ONLY
# =========================================================
Get-ChildItem $TargetRoot -Recurse -Filter *.csproj | ForEach-Object {

    (Get-Content $_.FullName) `
        -replace "<RootNamespace>.*</RootNamespace>", "<RootNamespace>$TargetProjectName</RootNamespace>" `
        -replace "<AssemblyName>.*</AssemblyName>", "<AssemblyName>$TargetProjectName</AssemblyName>" |
        Set-Content $_.FullName -Encoding UTF8
}

# =========================================================
# 4. UPDATE SOLUTION FILE (SCOPED REPLACE ONLY)
# =========================================================
if (Test-Path $TargetSolution) {
    (Get-Content $TargetSolution) `
        -replace $SourceProjectName, $TargetProjectName |
        Set-Content $TargetSolution -Encoding UTF8
}

# =========================================================
# 5. NAMESPACE MIGRATION (LIMITED SCOPE ONLY)
# =========================================================
Get-ChildItem $TargetRoot -Recurse -Include *.cs | ForEach-Object {

    $content = Get-Content $_.FullName -Raw

    # ONLY replace namespace declarations, not whole file
    $content = $content -replace "namespace $SourceProjectName", "namespace $TargetProjectName"

    Set-Content $_.FullName $content -Encoding UTF8
}

Write-Host "== Migration completed safely =="