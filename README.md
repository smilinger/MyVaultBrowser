# MyVaultBrowser

## The Separate Vault Browser for Autodesk Inventor

This addin is for the inventor and vault users who are tired of switching back and forth between the model browser and the vault browser. It will create a separate vault browser, with all the same functions as the original vault browser, and the browser can dock to the either side of the Inventor window, or you can also make it floating inside or outside the inventor window.

## Intro

The idea begins from a thread on Autodesk forum:
<http://forums.autodesk.com/t5/vault-general-discussion/seperate-vault-browser/td-p/5975209>

This addin will not create a whole new vault browser from scratch, it actually use win32 p/invoke to find the original vault browser window, takes it and reuses it with a new dockable window. So the user interface, context menu and all the vault functions are exactly the same as the original vault browser. You don't need to learn anything new to get used to it, and you can go back to the old vault browser anytime, just by closing the new browser window.

## Feature and Usage

* The new MyVaultBrowser window is visible by default for the first time the addin is loaded.
* You can dock or undock the window as you like, just like any other dockable windows in inventor.
* Inventor will remember the visibility, floating position or docking state of the browser window.
* If the window is closed, you can open it from View -> Windows -> User Interface.
* The original vault browser is hidden when MyVaultBrowser is open.
* The original vault browser is back when MyVaultBrowser is closed.
* When startup, the addin will check the status of the vault addin, if vault addin is not loaded, it will ask you to load the vault addin.
* If you unload/reload vault addin manually from the Add-in Manager, MyVaultBrowser will stop working, you need to unload/reload MyVaultBrowser addin to make it work again.
* The default keyboard shortcut setting is removed because it cause problems in some situation, instead now you can add shortcut for MyVaultBrowser in inventor customize settings like normal inventor commands.

See <http://autode.sk/1PRIwiJ> for very simple demonstration.

## How to install

Unzip and copy the folder to either of these locations:

### 1.For all users

%ALLUSERSPROFILE%\Autodesk\Inventor Addins\

### 2.For current user only

%APPDATA%\Autodesk\ApplicationPlugins

You can also use the exe provided to install.

## How to Uninstall

If you installed the add-in manually with zip, just remove the add-in folder.

If you installed the add-in with exe, you can uninstall it with the uninstall.exe in the add-in folder, or uninstall it from Programs and Features in Windows control panel.

## How to Build

The code is written with VS2015, some new feature in C# 6 is used in the code, you may need to change these new feature code to build in lower VS versions. The code is not so elegant as you may expect, feel free to fork and modify it as you need.

## License

Copyright (c) 2016 smilinger

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
