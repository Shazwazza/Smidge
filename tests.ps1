$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName

" PSScriptFilePath = $PSScriptFilePath"

$SolutionRoot = Split-Path -Path $PSScriptFilePath -Parent
$ProjectJsonPath = Join-Path -Path $SolutionRoot -ChildPath "test\Smidge.Tests\project.json"

$DOTNET = "dotnet"

& $DOTNET --info

& $DOTNET restore

#& $DOTNET restore "$ProjectJsonPath"
#if (-not $?)
#{
#	throw "The dotnet restore process returned an error code."
#}

#& $DOTNET build "$ProjectJsonPath"
#if (-not $?)
#{
#	throw "The dotnet build process returned an error code."
#}

# run them
& $DOTNET test "$ProjectJsonPath"
if (-not $?)
{
	throw "The dotnet test process returned an error code."
}