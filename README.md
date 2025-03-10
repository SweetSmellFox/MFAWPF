<div align="center">
<img alt="LOGO" src="https://github.com/SweetSmellFox/MFAWPF/blob/master/logo.png" width="256" height="256" />

# MFAWPF
[English](./README_en.md) | [简体中文](./README.md)

</div>

## 基本介绍

- 本项目是一个基于WPF框架开发的用户界面，旨在提供类似于MaaPiCli的功能

## 说明

### 使用需求

- .NET8运行库
- 一个基于maaframework的非集成项目

### 如何使用

#### 自动安装

- 下载项目中workflows/install.yml并修改```项目名称```,```作者名```,```项目名```,```MAAxxx```
- 将修改后的install.yml替换MAA项目模板.github/workflows/install.yml
- 推送新版本

#### 手动安装

- 下载最新发行版并解压
- 将maafw项目中assets/resource中所有内容复制到MFAWPF/Resource中
- 将maafw项目中assets/interface.json文件复制到MFAWPF/中
- ***修改***刚刚复制的interface.json文件
- 下面是一个例子

 ```
{
  "resource": [
    {
      "name": "官服",
      "path": "{PROJECT_DIR}/resource/base"
    },
    {
      "name": "Bilibili服",
      "path": [
        "{PROJECT_DIR}/resource/base",
        "{PROJECT_DIR}/resource/bilibili"
      ]
    }
  ],
  "task": [
    {
      "name": "任务",
      "entry": "任务"
    }
  ]
}
```

修改为

```
{
  "name": "项目名称", //默认为null
  "version":  "项目版本", //默认为null
  "mirrorchyan_rid":  "项目ID(从Mirror酱下载的必要字段)", //默认为null , 比如 M9A
  "url":  "项目链接(目前应该只支持Github)", //默认为null , 比如 https://github.com/{Github账户}/{Github项目}
  "custom_title": "自定义标题", //默认为null, 使用该字段后，标题栏将只显示custom_title和version
  "resource": [
    {
      "name": "官服",
      "path": "{PROJECT_DIR}/resource/base"
    },
    {
      "name": "Bilibili服",
      "path": [
        "{PROJECT_DIR}/resource/base",
        "{PROJECT_DIR}/resource/bilibili"
      ]
    }
  ],
  "task": [
    {
      "name": "任务",
      "entry": "任务接口",
      "check": true,  //默认为false，任务默认是否被选中
      "doc": "文档介绍",  //默认为null，显示在任务设置选项底下，可支持富文本，格式在下方
      "repeatable": true,  //默认为false，任务可不可以重复运行
      "repeat_count": 1,  //任务默认重复运行次数，需要repeatable为true
    }
  ]
}
```
注意 `default_controller`和`lock_controller`已在v1.2.7.6被废除，

lock_controller可以通过controller的数量来控制，default_controller则通过controller[0]

### `doc`字符串格式：

#### 使用类似`[color:red]`文本内容`[/color]`的标记来定义文本样式。

#### 支持的标记包括：

- `[color:color_name]`：颜色，例如`[color:red]`。

- `[size:font_size]`：字号，例如`[size:20]`。

- `[b]`：粗体。

- `[i]`：斜体。

- `[u]`：下划线。

- `[s]`：删除线。

- `[align:left/center/right]`：居左，居中或者居右，只能在一整行中使用。

**注：上面注释内容为文档介绍用，实际运行时不建议写入。**

- 运行

## 开发相关

- 内置 MFATools 可以用来裁剪图片和获取 ROI
- 目前一些地方并没有特别完善,欢迎各位大佬贡献代码
- 注意，由于 `MaaFramework` 于 2.0 移除了Exec Agent，所以目前无法通过注册interface注册Custom Action和Custom Recognition
- `MFAWPF` 于v1.2.3.3加入动态注册Custom Action和Custom Recognition的功能，目前只支持C#,需要在Resource目录的custom下放置相应的.cs文件, 参考 [文档](./docs/zh_cn/自定义识别_操作.md)
- 在exe同级目录中放置 `logo.ico` 后可以替换窗口的图标
- `MFAWPF` 新增interface多语言支持,在`interface.json`同目录下新建`zh-cn.json`,`zh-tw.json`和`en-us.json`后，doc和任务的name和选项的name可以使用key来指代。MFAWPF会自动根据语言来读取文件的key对应的value。如果没有则默认为key
- `MFAWPF` 会读取`Resource`文件夹的`Announcement.md`作为公告，更新资源时会自动下载一份Changelog作为公告

**注：在MFA中，于Pipeline中任务新增了俩个属性字段，分别为 `focus_tip` 和 `focus_tip_color`。**

- `focus` : *bool*  
  是否启用`focus_tip` 、`focus_succeeded`、 `focus_failed`、 `focus_toast`。可选，默认false。
- `focus_toast` : *string*  
  当执行某任务前，Windows弹窗输出的内容。可选，默认空。
- `focus_tip` : *string* | *list<string, >*  
  当执行某任务前，在MFA右侧日志输出的内容。可选，默认空。
- `focus_tip_color` : *string* | *list<string, >*  
  当执行某任务前，在MFA右侧日志输出的内容的颜色。可选，默认为Gray。
- `focus_succeeded` : *string* | *list<string, >*  
  当执行某任务成功后，在MFA右侧日志输出的内容。可选，默认空。
- `focus_succeeded_color` : *string* | *list<string, >*  
  当执行某任务成功后，在MFA右侧日志输出的内容的颜色。可选，默认为Gray。
- `focus_failed` : *string* | *list<string, >*  
  当执行某任务失败时，在MFA右侧日志输出的内容。可选，默认空。
- `focus_failed_color` : *string* | *list<string, >*  
  当执行某任务失败时，在MFA右侧日志输出的内容的颜色。可选，默认为Gray。
## 致谢

### 开源库

- [MaaFramework](https://github.com/MaaAssistantArknights/MaaFramework)：自动化测试框架
- [MaaFramework.Binding.CSharp](https://github.com/MaaXYZ/MaaFramework.Binding.CSharp)：MaaFramework 的 C# 包装
- [HandyControls](https://github.com/ghost1372/HandyControls)：C# WPF 控件库
- [Serilog](https://github.com/serilog/serilog)：C# 日志记录库
- [Newtonsoft.Json](https://github.com/CommunityToolkit/dotnet)：C# JSON 库

### 开发者

感谢以下开发者对 MFA 作出的贡献：

[![Contributors](https://contrib.rocks/image?repo=SweetSmellFox/MFAWPF&max=1000)](https://github.com/SweetSmellFox/MFAWPF/graphs/contributors)

## 讨论

- MaaFW 交流 QQ 群: 595990173