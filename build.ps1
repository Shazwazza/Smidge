param (
	[Parameter(Mandatory=$true)]
	[ValidatePattern("^\d+\.\d+\.(?:\d+\.\d+$|\d+$)|^\d+\.\d+\.\d+-(\w|-)+$")]
	[string]
	$ReleaseVersionNumber,
	[Parameter(Mandatory=$false)]
	[string]
	[AllowEmptyString()]
	$PreReleaseName
)

if([string]::IsNullOrEmpty($PreReleaseName) -And $ReleaseVersionNumber.Contains("-"))
{
	$parts = $ReleaseVersionNumber.Split("-")
	$ReleaseVersionNumber = $parts[0]
	$PreReleaseName = "-" + $parts[1]
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

	Write-Host "Updating verion for $projectPath to $($project.version) ($projectVersion)($prerelease)"

	#Update the csproj with the correct info
	$xmlCsproj = [xml](Get-Content $csproj)
	$xmlCsproj.Project.PropertyGroup.VersionPrefix = "$projectVersion"
	if([string]::IsNullOrEmpty($prerelease)){
		# Remove this node if it exists otherwise everything will break	
		$xmlVersionSuffix = $xmlCsproj.Project.PropertyGroup.SelectSingleNode("VersionSuffix")	
		if($xmlVersionSuffix -ne $null){
			$xmlCsproj.Project.PropertyGroup.RemoveChild($xmlVersionSuffix)
		}
	}
	else {
		$xmlVersionSuffix = $xmlCsproj.CreateElement("VersionSuffix")
		$xmlCsproj.Project.PropertyGroup.AppendChild($xmlVersionSuffix)
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