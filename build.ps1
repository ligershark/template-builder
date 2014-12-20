[cmdletbinding(DefaultParameterSetName ='build',SupportsShouldProcess = $true)]
param(
    [Parameter(Position=0)]
    [Parameter(ParameterSetName='build')]
    [string]$configuration = 'Release',

    [Parameter(ParameterSetName='build')]
    [string]$visualStudioVersion='11.0',

    [Parameter(ParameterSetName='build')]
    [switch]$cleanBeforeBuild,

    [Parameter(ParameterSetName='build')]
    [bool]$installPsbuildIfMissing = $true,

    [Parameter(ParameterSetName='build')]
    [string]$psbuildInstallUrl='https://raw.github.com/ligershark/psbuild/master/src/GetPSBuild.ps1',

    [Parameter(ParameterSetName='build')]
    [Parameter(ParameterSetName='publishToNuGet')]
    [switch]$publishToNuget,

    [Parameter(ParameterSetName='publishToNuGet')]
    [string]$nugetApiKey = ($env:NuGetApiKey)
    )

function Get-ScriptDirectory{
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $Invocation.MyCommand.Path
}

$scriptDir = ((Get-ScriptDirectory) + "\")

$buildproj = (get-item (Join-Path $scriptDir 'build.proj'))

function Filter-String{
[cmdletbinding()]
    param(
        [Parameter(Position=0,Mandatory=$true,ValueFromPipeline=$true)]
        [string[]]$message
    )
    process{
        foreach($msg in $message){
            if($nugetApiKey){
                $msg = $msg.Replace($nugetApiKey,'REMOVED-FROM-LOG')
            }

            $msg
        }
    }
}

function Write-Message{
    [cmdletbinding()]
    param(
        [Parameter(Position=0,Mandatory=$true,ValueFromPipeline=$true)]
        [string[]]$message
    )
    process{
        Filter-String -message $message | Write-Verbose
    }
}

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
            'Attempting to download psbuild install script from [{0}]' -f  $psbuildInstallUrl | Write-Message
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


<#
.SYNOPSIS
    If nuget is in the tools
    folder then it will be downloaded there.
#>
function Get-Nuget{
    [cmdletbinding()]
    param(
        $toolsDir = ("$env:LOCALAPPDATA\LigerShark\tools\"),
        $nugetDownloadUrl = 'http://nuget.org/nuget.exe'
    )
    process{
        $nugetDestPath = Join-Path -Path $toolsDir -ChildPath nuget.exe

        if(!(Test-Path $nugetDestPath)){
            $nugetDir = ([System.IO.Path]::GetDirectoryName($nugetDestPath))
            if(!(Test-Path $nugetDir)){
                New-Item -Path $nugetDir -ItemType Directory | Out-Null
            }

            'Downloading nuget.exe' | Write-Message
            (New-Object System.Net.WebClient).DownloadFile($nugetDownloadUrl, $nugetDestPath)

            # double check that is was written to disk
            if(!(Test-Path $nugetDestPath)){
                throw 'unable to download nuget'
            }
        }

        # return the path of the file
        $nugetDestPath
    }
}

function PublishNuGetPackage{
    [cmdletbinding(SupportsShouldProcess=$true)]
    param(
        [Parameter(Mandatory=$true,ValueFromPipeline=$true)]
        [string[]]$nugetPackage,

        [Parameter(Mandatory=$true)]
        $nugetApiKey
    )
    process{
        foreach($pkg in $nugetPackage){
            $pkgPath = (get-item $pkg).FullName
            $cmdArgs = @('push',$pkgPath,$nugetApiKey,'-NonInteractive')

            'Publishing nuget package [{0}]' -f $pkgPath | Write-Message

            $filteredCmd = Filter-String ('Publishing nuget package with the following args: [nuget.exe {0}]' -f ($cmdArgs -join ' '))
            if($PSCmdlet.ShouldProcess($env:COMPUTERNAME, $filteredCmd)){
                &(Get-Nuget) $cmdArgs
            }
        }
    }
}

function Clean{
    [cmdletbinding()]
    param()
    process{
        'Clean started' | Write-Message
        Invoke-MSBuild -projectsToBuild $buildproj.FullName -targets Clean -properties @{
            'Configuration'=$configuration
            'RestorePackages'='true'
        }
    }
}

function Build{
    [cmdletbinding()]
    param()
    process{
        'Build started' | Write-Message
        

        # MSBuild.exe build.proj /p:Configuration=Release /p:VisualStudioVersion=11.0 /p:RestorePackages=true /flp1:v=d;logfile=build.d.log /flp2:v=diag;logfile=build.diag.log

        Invoke-MSBuild -projectsToBuild $buildproj.FullName -properties @{
            'Configuration'=$configuration
            'RestorePackages'='true'
        }
    }
}





# Begin script here
try{
    EnsurePsbuildInstalled -psbuildInstallUrl  $psbuildInstallUrl -installPsbuildIfMissing $installPsbuildIfMissing

    if($cleanBeforeBuild -or $publishToNuget){
        Clean
    }
    
    Build

    if($publishToNuget){
        # get the nuget pkg from the output folder
        $outputroot = (get-item (join-path ($buildproj.Directory.FullName) 'OutputRoot\')).FullName
        $package = (Get-ChildItem $outputroot 'TemplateBuilder.*.nupkg')
        if($package.count > 1){
            throw ('Found more than one file [{0} found] matching ''TemplateBuilder.*.nupkg'' in the output folder [{1}] ' -f $package.count, $outputroot)
        }
        # publish the nuget package
        PublishNuGetPackage -nugetPackage $package.FullName -nugetApiKey $nugetApiKey
    }
}
catch{
    "An error has occurred.`nError: [{0}]" -f ($_.Exception) | Write-Warning
}
