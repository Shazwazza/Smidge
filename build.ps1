param (	
	[Parameter(Mandatory=$false)]
	[string]
	[AllowEmptyString()]
	$PreReleaseName
)

if(-not $PreReleaseName.StartsWith("-"))
{	
	$PreReleaseName = "-$PreReleaseName"
	Write-Host "pre release name: $PreReleaseName"
}

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName

" PSScriptFilePath = $PSScriptFilePath"

$SolutionRoot = Split-Path -Path $PSScriptFilePath -Parent

$DOTNET = "dotnet"

# Make sure we don't have a release folder for this version already
$BuildFolder = Join-Path -Path $SolutionRoot -ChildPath "build";
$ReleaseFolder = Join-Path -Path $BuildFolder -ChildPath "Release";
if ((Get-Item $ReleaseFolder -ErrorAction SilentlyContinue) -ne $null)
{
	Write-Warning "$ReleaseFolder already exists on your local machine. It will now be deleted."
	Remove-Item $ReleaseFolder -Recurse
}

$SmidgeSln = Join-Path -Path $SolutionRoot -ChildPath "Smidge.sln"
$NugetConfig = Join-Path -Path $SolutionRoot -ChildPath "Nuget.config"

# Read XML
$buildXmlFile = (Join-Path $SolutionRoot "build.xml")
[xml]$buildXml = Get-Content $buildXmlFile

# Iterate projects and update their versions
[System.Xml.XmlElement] $root = $buildXml.get_DocumentElement()
[System.Xml.XmlElement] $project = $null
foreach($project in $root.ChildNodes) {

	$projectPath = Join-Path -Path $SolutionRoot -ChildPath ("src\" + $project.id)
	$csproj = Join-Path -Path $projectPath -ChildPath ($project.id + ".csproj")
	$projectVersion = $project.version;
	$prerelease = $project.prerelease;
	# Override with passed in params
	if(-not [string]::IsNullOrEmpty($PreReleaseName)){
		$prerelease = [string]$PreReleaseName
	}

	Write-Host "Updating verion for $projectPath to ($projectVersion$prerelease)"

	#Update the csproj with the correct info
	[xml]$xmlCsproj = Get-Content $csproj
	$xmlCsproj.Project.PropertyGroup.VersionPrefix = "$projectVersion"
	#Remove all VersionSuffix elements to start
	$xmlCsproj.Project.PropertyGroup.SelectNodes("VersionSuffix") | % {   
		Write-Host "DELETING!"
        $xmlCsproj.Project.PropertyGroup.RemoveChild($_) | Out-Null
    }

	#Set the pre release if t here is one
	if(-not [string]::IsNullOrEmpty($prerelease)){
		$xmlVersionSuffix = $xmlCsproj.CreateElement("VersionSuffix")
		[void]$xmlCsproj.Project.PropertyGroup.AppendChild($xmlVersionSuffix)
		$xmlCsproj.Project.PropertyGroup.VersionSuffix = "$prerelease"	
	}
	
	# Set the copyright
	$DateYear = (Get-Date).year
	$xmlCsproj.Project.PropertyGroup.Copyright = "Copyright © Shannon Deminick $DateYear"
	$xmlCsproj.Save($csproj)

}

# Build the proj in release mode

& $DOTNET --info

& $DOTNET restore --configfile "$NugetConfig"
if (-not $?)
{
	throw "The dotnet restore process returned an error code."
}

& $DOTNET build "$SmidgeSln" --configuration "Release"
if (-not $?)
{
	throw "The dotnet build process returned an error code."
}

# Build the nugets for each proj

foreach($project in $root.ChildNodes) {

	$projectPath = Join-Path -Path $SolutionRoot -ChildPath ("src\" + $project.id)
	$csproj = Join-Path -Path $projectPath -ChildPath ($project.id + ".csproj")
	$projectVersion = $project.version;
	$prerelease = $project.prerelease;
	# Override with passed in params
	if(-not [string]::IsNullOrEmpty($PreReleaseName)){
		$prerelease = [string]$PreReleaseName
	}

	if([string]::IsNullOrEmpty($prerelease))
	{
		& $DOTNET pack "$csproj" --configuration Release --output "$ReleaseFolder"
		if (-not $?)
		{
			throw "The dotnet pack process returned an error code."
		}
	}
	else {
		& $DOTNET pack "$csproj" --configuration Release --output "$ReleaseFolder" --version-suffix $prerelease.TrimStart("-")
		if (-not $?)
		{
			throw "The dotnet pack process returned an error code."
		}
	}
}