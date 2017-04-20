$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName

" PSScriptFilePath = $PSScriptFilePath"

$SolutionRoot = Split-Path -Path $PSScriptFilePath -Parent
$Csproj = Join-Path -Path $SolutionRoot -ChildPath "test\Smidge.Tests\Smidge.Tests.csproj"

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
& $DOTNET test "$Csproj"
if (-not $?)
{
	throw "The dotnet test process returned an error code."
}