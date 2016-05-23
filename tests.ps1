$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName

" PSScriptFilePath = $PSScriptFilePath"

$SolutionRoot = Split-Path -Path $PSScriptFilePath -Parent
$ProjectJsonPath = Join-Path -Path $SolutionRoot -ChildPath "tests\Smidge.Tests\project.json"

$DOTNET = "dotnet"

& $DOTNET restore "$ProjectJsonPath"
if (-not $?)
{
	throw "The dotnet restore process returned an error code."
}

# run them
& $DOTNET -p "$ProjectJsonPath" test
if (-not $?)
{
	throw "The dotnet test process returned an error code."
}