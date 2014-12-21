[cmdletbinding(SupportsShouldProcess=$true)]
param()

function Enable-GetNuGet{
    [cmdletbinding()]
    param($toolsDir = "$env:LOCALAPPDATA\LigerShark\tools\getnuget\",
        $getNuGetDownloadUrl = 'https://raw.githubusercontent.com/sayedihashimi/publish-module/master/getnuget.psm1')
    process{
        if(!(get-module 'getnuget')){
            if(!(Test-Path $toolsDir)){ New-Item -Path $toolsDir -ItemType Directory -WhatIf:$false }

            $expectedPath = (Join-Path ($toolsDir) 'getnuget.psm1')
            if(!(Test-Path $expectedPath)){
                'Downloading [{0}] to [{1}]' -f $getNuGetDownloadUrl,$expectedPath | Write-Verbose
                (New-Object System.Net.WebClient).DownloadFile($getNuGetDownloadUrl, $expectedPath)
                if(!$expectedPath){throw ('Unable to download getnuget.psm1')}
            }

            'importing module [{0}]' -f $expectedPath | Write-Verbose
            Import-Module $expectedPath -DisableNameChecking -Force -Scope Global
        }
    }
}

Enable-GetNuGet
'trying to load file replacer' | Write-Output
Enable-NuGetModule -name 'file-replacer' -version '0.2.0-beta'

$folder = pwd
$include = '*.txt'
# In case the script is in the same folder as the files you are replacing add it to the exclude list
$exclude = "$($MyInvocation.MyCommand.Name);"
$replacements = @{
    'to-be-replaced'='replaced-with-this'
}

'replacing in files'|write-output
Replace-TextInFolder -folder $folder -include $include -exclude $exclude -replacements $replacements
'done replacing'|write-output

