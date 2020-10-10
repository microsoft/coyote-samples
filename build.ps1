param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

$ScriptDir = $PSScriptRoot
Import-Module $PSScriptRoot\Common\helpers.psm1

$dotnet_path = FindDotNet($dotnet)
$sdk_version = FindDotNetSdk($dotnet_path);

if ($null -eq $sdk_version)
{
    Write-Comment -text "The global.json file is pointing to version: $sdk_version but no matching version was found" -color "yellow"
    Write-Comment -text "Please install .NET SDK version $sdk_version from https://dotnet.microsoft.com/download/dotnet-core." -color "yellow"
    exit 1
}

Write-Comment -text "Using .NET SDK version $sdk_version" -color yellow

Write-Comment -prefix "." -text "Building the Coyote samples" -color "yellow"

Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\HelloWorldTasks\HelloWorldTasks.sln" -config $configuration
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\HelloWorldActors\HelloWorldActors.sln" -config $configuration
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\CloudMessaging\CloudMessaging.sln" -config $configuration
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\Timers\Timers.sln" -config $configuration
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\Monitors\Monitors.sln" -config $configuration
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\CoffeeMachineActors\CoffeeMachineActors.sln" -config $configuration
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\CoffeeMachineTasks\CoffeeMachineTasks.sln" -config $configuration
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\DrinksServingRobotActors\DrinksServingRobotActors.sln" -config $configuration
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\BoundedBuffer\BoundedBuffer.sln" -config $configuration

Write-Comment -prefix "." -text "Successfully built the Coyote samples" -color "green"
