[cmdletbinding(DefaultParameterSetName ='build')]
param(
    [Parameter(Position=0)]
    [Parameter(ParameterSetName='build')]
    [string]$configuration = 'Release',

    [Parameter(ParameterSetName='build')]
    [string]$visualStudioVersion='11.0',

    [Parameter(ParameterSetName='build')]
    [bool]$installPsbuildIfMissing = $true,

    [Parameter(ParameterSetName='build')]
    [string]$psbuildInstallUrl='https://raw.github.com/ligershark/psbuild/master/src/GetPSBuild.ps1',

    [Parameter(ParameterSetName='build')]
    [Parameter(ParameterSetName='publishToNuGet')]
    [switch]$publishToNuget
    )

function Get-ScriptDirectory
{
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $Invocation.MyCommand.Path
}

$scriptDir = ((Get-ScriptDirectory) + "\")

$buildproj = (get-item (Join-Path $scriptDir 'build.proj'))

<#
.SYNOPSIS 
    This will throw an error if the psbuild module is not installed and available.
#>
function EnsurePsbuildInstalled{
    [cmdletbinding()]
    param(
        [bool]$installPsbuildIfMissing,
        [string]$psbuildInstallUrl='https://raw.github.com/ligershark/psbuild/master/src/GetPSBuild.ps1'
    )
    process{
        if($installPsbuildIfMissing -and !(Get-Module -listAvailable 'psbuild')){(new-object Net.WebClient).DownloadString("https://raw.github.com/ligershark/psbuild/master/src/GetPSBuild.ps1") | iex
            'Attempting to download psbuild install script from [{0}]' -f  $psbuildInstallUrl | Write-Verbose
            (new-object Net.WebClient).DownloadString($psbuildInstallUrl) | iex
        }

        if(!(Get-Module -listAvailable 'psbuild')){
            $msg = ('psbuild is required for this script, but it does not look to be installed. Get psbuild from here: https://aka.ms/psbuild')
            throw $msg
        }

        if(!(Get-Module 'psbuild')){
            # add psbuild to the currently loaded session modules
            import-module psbuild -Global;
        }
    }
}

function DoBuild{
    [cmdletbinding()]
    param()
    process{
        EnsurePsbuildInstalled -psbuildInstallUrl  $psbuildInstallUrl -installPsbuildIfMissing $installPsbuildIfMissing

        # MSBuild.exe build.proj /p:Configuration=Release /p:VisualStudioVersion=11.0 /p:RestorePackages=true /flp1:v=d;logfile=build.d.log /flp2:v=diag;logfile=build.diag.log

        Invoke-MSBuild -projectsToBuild $buildproj.FullName -properties @{
            'Configuration'=$configuration
            'RestorePackages'='true'
        }

        if($publishToNuget){
            throw 'publishing to nuget not supported yet'
        }
    }
}





# Begin script here
DoBuild