param (
    [Alias('sp')]
    [parameter(
        HelpMessage = "Name of the existing project (default: Franz.Common).",
        Mandatory = $false)]
    [string]$SourceProjectName = "Franz.Common",
    
    [Alias('tp')]
    [parameter(
        HelpMessage = "Name of the new project (default: Pangea.Framework).",
        Mandatory = $false)]
    [string]$TargetProjectName = "Pangea.Framework" ,
    
    [Alias('odtp')]
    [parameter(
        HelpMessage = "Output directory for the new project. (default: ../)",
        Mandatory = $false)]
    [string]$TargetProjectRootDirectory = "",

    [Alias('ai')]
    [parameter(
        HelpMessage = "Relative path to AssemblyInfo.cs file, e.g. '.\Properties\AssemblyInfo.cs'.`nEmpty string = skip AssemblyInfo.",
        Mandatory = $false)]
    [AllowEmptyString()]
    [string]$RelativePathToAssemblyInfo = ""
)

$SourceProjectName = $SourceProjectName.Trim().Trim("")
$SourceProjectFullPath = "$(Resolve-Path "..")\"
$SourceSolutionFullPath = "$SourceProjectFullPath$SourceProjectName.sln"
$TargetProjectName = $TargetProjectName.Trim().Trim("")

if ($TargetProjectRootDirectory.Trim() -eq "") {    
    $TargetProjectFullPath = "..\"
}
else {
    $TargetProjectFullPath = "$TargetProjectRootDirectory$TargetProjectName\"
}

$TargetSolutionFullPath = "$TargetProjectFullPath$TargetProjectName.sln"

if ( (Test-Path $SourceSolutionFullPath -PathType Leaf) -ne $true) {
    throw "Source solution not found: $SourceSolutionFullPath"
}

function Start-ProceedOrExit {
    param([string]$currentStep)
    if ($?) { Write-Output "$currentStep - OK" } else { Write-Output "Script ERROR! Exiting."; exit 1 } 
}

function Rename-Solution {
    param ($targetProjectFullPath, $sourceProjectName, $targetProjectName)

    # Files
    Get-ChildItem -Path "$targetProjectFullPath" -Include "$sourceProjectName.*" -Recurse -File `
    | ForEach-Object {
        $OldName = $_.Name
        $NewName = $_.Name -replace "^$sourceProjectName\b", "$targetProjectName"
        if ($OldName -ne $NewName) {
            Rename-Item -Path $_.PSPath -NewName $NewName
        }
    }

    # Directories
    Get-ChildItem -Path "$targetProjectFullPath" -Include "$sourceProjectName*" -Recurse -Directory `
    | ForEach-Object {
        $OldName = $_.Name
        $NewName = $OldName -replace "^$sourceProjectName\b", "$targetProjectName"
        if ($OldName -ne $NewName) {
            Rename-Item -Path $_.PSPath -NewName $NewName
        }
    }
}

function Copy-BaseSolution {
    param ($sourceProjectFullPath, $targetProjectFullPath)
    if ($sourceProjectFullPath -ne $targetProjectFullPath) {
        New-Item $targetProjectFullPath -ItemType Directory -Force | Out-Null
        Copy-Item -Path "$sourceProjectFullPath*" $targetProjectFullPath -Recurse -Force -Exclude @(".git","scripts") | Out-Null
        return $targetProjectFullPath
    }
    return $sourceProjectFullPath
}

function Save-CurrentDirectory { Get-Location }
function Restore-CurrentDirectory { param ($loc) Set-Location $loc }

function Rename-ReferencesInSolution {
    param ($targetSolutionFullPath, $sourceProjectName, $targetProjectName)
    $UpdatedSolution = Get-Content -Path $targetSolutionFullPath `
    | ForEach-Object { $_ -replace $sourceProjectName, $targetProjectName }
    Set-Content -Path $targetSolutionFullPath -Value $UpdatedSolution -Encoding UTF8
}

function Rename-AssemblyNameAndRootNamespace {
    param ($targetProjectFullPath, $sourceProjectName, $targetProjectName)
    Get-ChildItem -Path "$targetProjectFullPath\*" -Recurse -Include *.csproj `
    | ForEach-Object {
        (Get-Content $_.FullName) -replace $sourceProjectName, $targetProjectName |
        Set-Content $_.FullName
    }
}

function Rename-NamespacesAndUsings {
    param ($targetProjectFullPath, $sourceProjectName, $targetProjectName)
    Get-ChildItem -Path "$targetProjectFullPath\*" -Recurse -Include *.cs `
    | ForEach-Object {
        (Get-Content $_.FullName) -replace $sourceProjectName, $targetProjectName |
        Set-Content $_.FullName
    }
}

function Rename-AssemblyInfo {
    param ($targetProjectFullPath, $sourceProjectName, $targetProjectName, $relativePath)
    $relativePath = $relativePath.Trim().Trim('"')
    if ($relativePath) {
        $FullPath = "$targetProjectFullPath$relativePath".Trim().Trim('"')
        if (!(Test-Path $FullPath)) { throw "AssemblyInfo not found: $FullPath" }
        else {
            (Get-Content $FullPath) -replace $sourceProjectName, $targetProjectName |
            Set-Content $FullPath
        }
    }
}

Write-Host "===== Starting project rename from $SourceProjectName to $TargetProjectName ====="

$CurrentDir = Save-CurrentDirectory

Start-ProceedOrExit "Copy base solution if target differs"
$SourceProjectFullPath = Copy-BaseSolution $SourceProjectFullPath $TargetProjectFullPath

Start-ProceedOrExit "Renaming files/folders: $SourceProjectName -> $TargetProjectName"
Rename-Solution $TargetProjectFullPath $SourceProjectName $TargetProjectName

Start-ProceedOrExit "Updating references in solution: $TargetSolutionFullPath"
Rename-ReferencesInSolution $TargetSolutionFullPath $SourceProjectName $TargetProjectName

Start-ProceedOrExit "Updating assembly names and root namespaces"
Rename-AssemblyNameAndRootNamespace $TargetProjectFullPath $SourceProjectName $TargetProjectName

Start-ProceedOrExit "Updating namespaces and usings"
Rename-NamespacesAndUsings $TargetProjectFullPath $SourceProjectName $TargetProjectName

Start-ProceedOrExit "Updating AssemblyInfo (if exists)"
Rename-AssemblyInfo $TargetProjectFullPath $SourceProjectName $TargetProjectName $RelativePathToAssemblyInfo

Restore-CurrentDirectory $CurrentDir

Write-Host "===== Finished project rename to $TargetProjectName ====="
