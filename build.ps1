param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

Import-Module $PSScriptRoot\Common\helpers.psm1

Write-Comment -prefix "." -text "Building the Coyote samples" -color "yellow"

Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\HelloWorldTasks\HelloWorldTasks.sln" -config $configuration
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\HelloWorldActors\HelloWorldActors.sln" -config $configuration
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\CloudMessaging\CloudMessaging.sln" -config $configuration
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\Mocking\Mocking.sln" -config $configuration
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\Nondeterminism\Nondeterminism.sln" -config $configuration
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\Timers\Timers.sln" -config $configuration
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\Monitors\Monitors.sln" -config $configuration

Write-Comment -prefix "." -text "Successfully built the Coyote samples" -color "green"
