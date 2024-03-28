Get-ChildItem |
Foreach-Object {
    $filePath = $_.FullName + "\" + $_.Name + ".csproj.user"
    $profilePath = $_.FullName + "\Properties\PublishProfiles\FolderProfile.pubxml"
    $content = @'
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <_LastSelectedProfileId>
'@ + $profilePath + @'
</_LastSelectedProfileId>
  </PropertyGroup>
</Project>
'@
    New-Item $filePath -Value $content -Force
}