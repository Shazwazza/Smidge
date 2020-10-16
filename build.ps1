param (
	[Parameter(Mandatory=$false)]
	[ValidatePattern("^\d+\.\d+\.(?:\d+\.\d+$|\d+$)|^\d+\.\d+\.\d+-(\w|-|\.)+$")]
	[string]
	$ReleaseVersionNumber,
	[Parameter(Mandatory=$false)]
	[string]
	[AllowEmptyString()]
	$PreReleaseName
)

# NOTE, the $ReleaseVersionNumber really doesn't do anything anymore
if(( -not [string]::IsNullOrEmpty($ReleaseVersionNumber)) -And [string]::IsNullOrEmpty($PreReleaseName) -And $ReleaseVersionNumber.Contains("-"))
{	
	$parts = $ReleaseVersionNumber.Split("-")
	$ReleaseVersionNumber = $parts[0]
	$PreReleaseName = $parts[1]
	Write-Host "Version parts split: ($ReleaseVersionNumber) and ($PreReleaseName)"
}

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName

" PSScriptFilePath = $PSScriptFilePath"

$SolutionRoot = Split-Path -Path $PSScriptFilePath -Parent
$BuildConfig = "Release"

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
	
	#Update the csproj with the correct info
	[xml]$xmlCsproj = Get-Content $csproj

	# Set the copyright
	$DateYear = (Get-Date).year
	$xmlCsproj.Project.PropertyGroup[0].Copyright = "Copyright $([char]0x00A9) Shannon Deminick $DateYear"
	$xmlCsproj.Save($csproj)

}

& $DOTNET --info

# Build the project and nugets for each proj

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
		& $DOTNET pack "$csproj" --configuration "$BuildConfig" --output "$ReleaseFolder" -p:PackageVersion="$projectVersion"
		if (-not $?)
		{
			throw "The dotnet pack process returned an error code."
		}
	}
	else {
		& $DOTNET pack "$csproj" --configuration "$BuildConfig" --output "$ReleaseFolder" -p:PackageVersion="$projectVersion-$prerelease" -p:FileVersion="$projectVersion-$prerelease"
		if (-not $?)
		{
			throw "The dotnet pack process returned an error code."
		}
	}
}