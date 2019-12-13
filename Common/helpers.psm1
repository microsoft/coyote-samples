# Builds the specified .NET project
function Invoke-DotnetBuild([String]$dotnet, [String]$solution, [String]$config) {
    Write-Comment -prefix "." -text "Building $solution" -color "yellow"
    Write-Comment -prefix "..." -text "Configuration: $config" -color "white"
  
    $command = "build -c $config $solution"
    $error_msg = "Failed to build $solution"
    Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg
}

# Runs the specified .NET test using the specified framework.
function Invoke-DotnetTest([String]$dotnet, [String]$project, [String]$target, [string]$filter, [string]$framework, [string]$verbosity) {
    Write-Comment -prefix "..." -text "Testing '$project' ($framework)" -color "white"
    if (-not (Test-Path $target)) {
        Write-Error "tests for '$project' ($framework) not found."
        exit
    }

    if (!($filter -eq "")) {
        $command = "test $target --filter $filter -f $framework --no-build -v $verbosity"
    } else {
        $command = "test $target -f $framework --no-build -v $verbosity"
    }

    $error_msg = "Failed to test '$project'"
    Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg
}

# Runs the specified tool command.
function Invoke-ToolCommand([String]$tool, [String]$command, [String]$error_msg) {
    Invoke-Expression "$tool $command"
    if (-not ($LASTEXITCODE -eq 0)) {
        Write-Error $error_msg
        exit $LASTEXITCODE
    }
}

function Write-Comment([String]$prefix, [String]$text, [String]$color) {
    Write-Host "$prefix " -b "black" -nonewline; Write-Host $text -b "black" -f $color
}

function Write-Error([String]$text) {
    Write-Host "Error: $text" -b "black" -f "red"
}
