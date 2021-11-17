param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

Import-Module $PSScriptRoot\..\Common\helpers.psm1

Write-Comment -prefix "." -text "Building the Coyote samples" -color "yellow"

Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\..\Common\TestDriver\TestDriver.csproj" -config $configuration

Write-Comment -prefix "." -text "Successfully built the Coyote samples" -color "green"
