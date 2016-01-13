# MyVaultBrowser
Seperate vault browser for Autodesk Inventor

## Intro
This addin is basically a hack, it uses win32 p/invoke to get the original vault browser, and uses it with a new dockable window. So the user interface and vault functions are exactly the same as the original vault browser, don't expect there will be more features than the original.

## Feature and Usage
It is simple:
* Open it from View -> Windows -> User Interface if it's closed.
* The original vault browser is hidden when MyVaultBrowser is used.
* The original vault browser is back when MyVaultBrowser is closed.
* Dock or undock as you want.

See http://autode.sk/1PRIwiJ for very simple demonstration.

## How to install
Unzip and copy the folder to either of these locations:
#### 1.For all users
%ALLUSERSPROFILE%\Autodesk\Inventor Addins\
#### 2.For current user only
%APPDATA%\Autodesk\ApplicationPlugins

## How to Build
The code is written with VS2015, some new feature in C# 6 is used in the code, you may need to change these new feature code to build in lower VS versions.
The code is not so elegant as you may expect, because I am just amateur programmer, feel free to fork and modify it as you need.

## License
Copyright (c) 2016 smilinger

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
