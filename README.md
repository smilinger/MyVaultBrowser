# MyVaultBrowser
Seperate vault browser for Autodesk Inventor

## Intro
This addin is basically a hack, it uses win32 p/invoke to get the original vault browser, and uses it with a new dockable window. So the user interface and vault functions are exactly the same as the original vault browser, don't expect there will be more features than the original.

## Feature and Usage
* The new MyVaultBrowser window is visible by default for the first time the addin is loaded. After that Inventor should remember its visibility, floating position or docking state.
* You can dock or undock the window as you like, just like any other dockable windows in inventor.
* Open it from View -> Windows -> User Interface if it's closed.
* The original vault browser is hidden when MyVaultBrowser is open.
* The original vault browser is back when MyVaultBrowser is closed.
* From v0.9.3, MyVaultBrowser also supports keyboard shortcut to open or close the browser, the default keyboard shortcut is "Ctrl+`", you can use other shortcuts as you like, for example, "Ctrl+1", "Alt+A", "Ctrl+Alt+Z", but be careful not use those already assigned to other commands in inventor. You can also use alias type shortcuts like "B" or "BB", however alias type shortcuts may not work sometimes. You need to manually modify the config file to change shortcuts, the setting will be saved to the following location when the addin is loaded the first time:
  * For Inventor 2014
    * %LOCALAPPDATA%\Autodesk,_Inc\DefaultDomain_Path_ow5451lkj52xbizxdtghrf2pdfathyhr\Autodesk®_Inventor®_2014\user.config
  * For Inventor 2015
    * %LOCALAPPDATA%\Autodesk,_Inc\DefaultDomain_Path_dyn3yltervsx4dgsvto5pwd10whykmwn\Autodesk®_Inventor®_2015\user.config
  * For Inventor 2016
    * %LOCALAPPDATA%\Autodesk,_Inc\DefaultDomain_Path_wts00mmfdaa1a2jhamx4xvzf21fh4mec\Autodesk_Inventor_2016\user.config

  ```xml
    ...
      <userSettings>
          <MyVaultBrowser.Properties.Settings>
              <setting name="ShortCut" serializeAs="String">
                  <value>B</value>
              </setting>
          </MyVaultBrowser.Properties.Settings>
    ...
  ```
  Actually it is the same file where inventor store its ilogic configuration, the folder name may be different sometimes in different machines.
* When startup, the addin will check the status of the vault addin, if vault addin is not loaded, it will ask user to load the vault addin.
* If you unload/reload vault addin manually from the Add-in Manager, MyVaultBrowser will stop working, you need to unload/reload MyVaultBrowser addin to make it work again.

See http://autode.sk/1PRIwiJ for very simple demonstration.

## How to install
Unzip and copy the folder to either of these locations:
#### 1.For all users
%ALLUSERSPROFILE%\Autodesk\Inventor Addins\
#### 2.For current user only
%APPDATA%\Autodesk\ApplicationPlugins

## How to Build
The code is written with VS2015, some new feature in C# 6 is used in the code, you may need to change these new feature code to build in lower VS versions.
The code is not so elegant as you may expect, feel free to fork and modify it as you need.

## License
Copyright (c) 2016 smilinger

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
