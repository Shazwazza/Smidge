$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName

" PSScriptFilePath = $PSScriptFilePath"

$SolutionRoot = Split-Path -Path $PSScriptFilePath -Parent
$TestsFolder = Join-Path -Path $SolutionRoot -ChildPath "tests/Smidge.Tests";

" Tests folder = $TestsFolder"

Set-Location -Path $TestsFolder

$DNX = "dnx"
$DNVM = "dnvm"

# use the correct version
& $DNVM use 1.0.0-rc1-final

# run them
& $DNX test

if (-not $?)
{
	throw "Tests failed"
}

Set-Location -Path (Get-Item $MyInvocation.MyCommand.Path).Directory