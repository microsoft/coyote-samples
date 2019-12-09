param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

function Write-Comment([String]$prefix, [String]$text, [String]$color) {
    Write-Host "$prefix " -b "black" -nonewline; Write-Host $text -b "black" -f $color
}

# Runs the specified tool command.
function Invoke-ToolCommand([String]$tool, [String]$command, [String]$error_msg) {
    Invoke-Expression "$tool $command"
    if (-not ($LASTEXITCODE -eq 0)) {
        Write-Error $error_msg
        exit $LASTEXITCODE
    }
}

function BuildProj([String]$proj, [String]$name) {

  $solution = $PSScriptRoot + "\$proj\$proj.sln"
  Write-Host $solution

  Write-Comment -prefix "." -text "Building $name" -color "yellow"
  Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"

  $command = "build -c $configuration $solution"
  $error_msg = "Failed to build $name"
  Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg
  Write-Comment -prefix "." -text "Successfully built $name" -color "green"
}

# gci -r | select -exp FullName

BuildProj -proj "AsyncTasks" -name "the Coyote asynchronous task examples"
BuildProj -proj "StateMachines" -name "the Coyote state machine examples"
