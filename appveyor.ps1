if($env:APPVEYOR_REPO_BRANCH -eq "releasetemplatebuilder"){
    .\build.ps1 -publishTemplateBuilderToNuget -updateNuget
} 
elseif($env:APPVEYOR_REPO_BRANCH -eq "releasefilereplacer"){
    .\build.ps1 -publishFileReplacerToNuget -updateNuget
}
elseif($env:APPVEYOR_REPO_BRANCH -eq "releaseallnugetpkg"){
    .\build.ps1 -publishFileReplacerToNuget -publishFileReplacerToNuget -updateNuget
}
else {
    .\build.ps1 -updateNuget
}