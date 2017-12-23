# MyVaultBrowser

## 独立的 Vault 浏览器

你是否已经厌烦了反复在模型浏览器和Vault浏览器之间切换？如果是，那么这个App就是为你准备的！安装之后，这个App将创建一个新的独立的Vault浏览器窗口，包含原Vault浏览器的所有功能，并且你可随心所欲的将这个浏览器窗口停靠在Inventor界面的左边或者右边，或者漂浮在Inventor界面的外面。

## 介绍

该插件的想法来源于Autodesk论坛上的一个帖子：
<http://forums.autodesk.com/t5/vault-general-discussion/seperate-vault-browser/td-p/5975209>

这个插件实际上并没有从头开始创建一个全新的Vault浏览器，它直接利用了原有的Vault浏览器，将它套用在一个新的可停靠窗口中。所以，用户界面、右键菜单和对应的Vault功能，和原来一模一样。完全不需要重新学习和熟悉它。如果你想回到原来的Vault浏览器，那也很简单，任何时候，只要关闭新的浏览器窗口就可以了。

## 功能和使用

* 第一次启动插件时，新的MyVaultBrowser窗口默认自动显示。
* 和Inventor中其它可停靠窗口一样，你可以随意使其停靠或者漂浮。
* Inventor会记住窗口的可见性、漂浮和停靠位置。
* 如果窗口被关闭，可以从视图->窗口->用户界面再打开。
* MyVaultBrowser窗口开启时，原Vault浏览器将自动隐藏。
* MyVaultBrowser窗口关闭时，原Vault浏览器将恢复显示。
* 启动时，插件会对Vault插件的状态进行检测，如果Vault插件未加载，会提示你进行加载。
* 如果你手动从附加模块管理器里卸载/加载了Vault插件，MyVaultBrowser将停止工作。你必须重新卸载/加载MyVaultBrowser插件才能使其恢复工作。
* 默认的快捷键设置已移除，因为在某些情况下可能引起问题，现在可以直接在Inventor自定义设置中为MyVaultBrowser设置快捷键。

简单的演示：<http://autode.sk/1PRIwiJ>。

## 如何安装

解压后将文件夹复制到以下任一位置：

### 1.对所有用户

%ALLUSERSPROFILE%\Autodesk\Inventor Addins\

### 2.仅对当前用户

%APPDATA%\Autodesk\ApplicationPlugins

或者使用提供的exe直接安装。

## 如何卸载

如果是手动使用zip文件安装的，直接删除插件文件夹。

如果使用exe安装，可以直接从控制面板“程序和功能”进行卸载，也可以运行插件文件夹中的 uninstall.exe 进行卸载。

## 如何编译

代码是用VS2015写的，其中用到了一些C# 6的新功能，如果你使用旧版本的Visual Studio，可能需要将这些新功能的代码改掉之后才能编译。代码写的肯定有不够完美的地方，请随意按照你的喜好进行修改。

## 许可协议

Copyright (c) 2016 smilinger

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
