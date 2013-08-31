Template builder has been installed into this project. Your build process has been updated to automatically build item templates
from the contents of the ItemTemplates\ folder. A demo template has been installed into that folder as well.

You should add the following XML element in your .vsixmanifest file.

  <Assets>
    <Asset Type="Microsoft.VisualStudio.ItemTemplate" Path="Output\ItemTemplates" xdt:Transform="Remove" xdt:Locator="Match(Path)" />
  </Assets>