[cmdletbinding(DefaultParameterSetName ='build',SupportsShouldProcess = $true)]
param(
    [Parameter(Position=0)]
    [Parameter(ParameterSetName='build')]
    [string]$configuration = 'Release',

    [Parameter(ParameterSetName='build')]
    [string]$visualStudioVersion='12.0',

    [Parameter(ParameterSetName='build')]
    [switch]$cleanBeforeBuild,

    [Parameter(ParameterSetName='build')]
    [bool]$installPsbuildIfMissing = $true,

    [Parameter(ParameterSetName='build')]
    [string]$psbuildInstallUrl='https://raw.github.com/ligershark/psbuild/master/src/GetPSBuild.ps1',

    [Parameter(ParameterSetName='build')]
    [Parameter(ParameterSetName='publishToNuGet')]
    [switch]$publishTemplateBuilderToNuget,

    [Parameter(ParameterSetName='build')]
    [Parameter(ParameterSetName='publishToNuGet')]
    [switch]$publishFileReplacerToNuget,

    [Parameter(ParameterSetName='build')]
    [switch]$updateNuget,

    [Parameter(ParameterSetName='publishToNuGet')]
    [string]$nugetApiKey = ($env:NuGetApiKey),

    [Parameter(ParameterSetName='setBuildProps')]
    [switch]$setMSBuildOverrides,

    [Parameter(ParameterSetName='clearBuildProps')]
    [switch]$clearMSBuildOverrides

)

function Get-ScriptDirectory{
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $Invocation.MyCommand.Path
}

$scriptDir = ((Get-ScriptDirectory) + "\")

$buildproj = (get-item (Join-Path $scriptDir 'main-build.proj'))

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
        if($installPsbuildIfMissing -and !(Get-Module -listAvailable 'psbuild')){
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
    This will return the path to nuget.exe. If it is not in the tools
    folder then it will be downloaded there and then the path will 
    be returned.
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
                if($LASTEXITCODE -ne 0){
                    throw ('nuget.exe failed with the following error code [{0}]' -f $LASTEXITCODE)
                }
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
    begin{
        if($updateNuget){
            & (Get-Nuget) update self
        }
    }
    process{
        'Build started' | Write-Message
        

        # MSBuild.exe main-build.proj /p:Configuration=Release /p:VisualStudioVersion=11.0 /p:RestorePackages=true /flp1:v=d;logfile=build.d.log /flp2:v=diag;logfile=build.diag.log

        Push-Location
        try{
            'Restoring NuGet packages for directory [{0}]' -f (join-path $buildproj.Directory.FullName src) | Write-Host -ForegroundColor Green
            Set-Location (join-path $buildproj.Directory.FullName src)
            &(Get-Nuget) restore
        }
        finally{
            Pop-Location
        }

        Invoke-MSBuild -projectsToBuild $buildproj.FullName -properties @{
            'Configuration'=$configuration
            'RestorePackages'='true'
        }
    }
}

function Set-MSBuildOverrides{
    [cmdletbinding()]
    param(
        [System.IO.FileInfo]$targetsPath = (Join-Path $scriptDir 'tools\ligershark.templates.targets'),
        [System.IO.DirectoryInfo]$tasksRoot = (Join-Path $scriptDir 'OutputRoot')
    )
    process{
        'Setting msbuild override env vars' | Write-Output
        '    [TemplateBuilderTargets]=[{0}]' -f $targetsPath | Write-Output
        '    [ls-TasksRoot]=[{0}]' -f $tasksRoot | Write-Output
        $env:TemplateBuilderTargets = $targetsPath
        ${env:ls-TasksRoot} = "$tasksRoot\"
    }
}

function Clear-MSBuildOverrides{
    [cmdletbinding()]
    param()
    process{
        Remove-Item -Path env:TemplateBuilderTargets
        Remove-Item -Path 'env:ls-TasksRoot'
    }
}

if($setMSBuildOverrides){
    Set-MSBuildOverrides
}
elseif($clearMSBuildOverrides){
    Clear-MSBuildOverrides
}
else{
    # Begin build script here
    try{
        EnsurePsbuildInstalled -psbuildInstallUrl  $psbuildInstallUrl -installPsbuildIfMissing $installPsbuildIfMissing

        if($cleanBeforeBuild -or $publishToNuget){
            Clean
        }

        Build

        $outputroot = (get-item (join-path ($buildproj.Directory.FullName) 'OutputRoot\')).FullName
        $nupkgToPublish = @()
        if($publishTemplateBuilderToNuget){
            $package = (Get-ChildItem $outputroot 'TemplateBuilder.*.nupkg')
            if($package.count -gt 1){
                throw ('Found more than one file [{0} found] matching ''TemplateBuilder.*.nupkg'' in the output folder [{1}] ' -f $package.count, $outputroot)
            }
            $nupkgToPublish += ($package.FullName)
        }
        if($publishFileReplacerToNuget){
            $package = (Get-ChildItem $outputroot 'file-replacer.*.nupkg')
            if($package.count -gt 1){
                throw ('Found more than one file [{0} found] matching ''file-replacer.*.nupkg'' in the output folder [{1}] ' -f $package.count, $outputroot)
            }
            $nupkgToPublish += ($package.FullName)
        }

        if($nupkgToPublish.Length -gt 0){
            'nupkgToPublish: [{0}]' -f $nupkgToPublish | Write-Verbose
            # publish the nuget package
            PublishNuGetPackage -nugetPackage $nupkgToPublish -nugetApiKey $nugetApiKey
        }
    }
    catch{
       throw ("An error has occurred.`nError: [{0}]" -f ($_.Exception))
    }
}