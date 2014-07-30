Template builder has been installed into this project. Your build process has been updated to automatically build item templates
from the contents of the ItemTemplates\ folder. A demo template has been installed into that folder as well.

[![Build status](https://ci.appveyor.com/api/projects/status/99qxhy5kmpm2ae0k)](https://ci.appveyor.com/project/sayedihashimi/template-builder)

The following snippet should have been added to your .vsixmanifest file

  <Assets>
    <Asset Type="Microsoft.VisualStudio.ItemTemplate" Path="Output\ItemTemplates"/>
	<Asset Type="Microsoft.VisualStudio.ProjectTemplate" Path="Output\ProjectTemplates" />
  </Assets>
