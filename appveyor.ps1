if($env:APPVEYOR_REPO_BRANCH -eq "releasetemplatebuilder"){
    .\build.ps1 -publishTemplateBuilderToNuget
} 
elseif( -eq "releasefilereplacer"){
    .\build.ps1 -publishFileReplacerToNuget
}
elseif($env:APPVEYOR_REPO_BRANCH -eq "releaseallnugetpkg"){
    .\build.ps1 -publishFileReplacerToNuget -publishFileReplacerToNuget
}
else {
    .\build.ps1
}