<div align="center">
<img alt="LOGO" src="https://github.com/SweetSmellFox/MFAWPF/blob/master/logo.png" width="256" height="256" />

# MFAWPF

</div>

## 基本介绍

- 本项目是一个基于WPF框架开发的用户界面，旨在提供类似于MaaPiCli的功能

## 说明

### 使用需求

- .NET8运行库
- 一个基于maaframework的非集成项目

### 如何使用

#### 自动安装

- 下载项目中workflows/install.yml并修改```项目名称```和```MAAxxx```
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
  "name": "项目名称",
  "version":  "项目版本",
  "url":  "项目链接(目前应该只支持Github)", //比如 https://github.com/{Github账户}/{Github项目}
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
      "checked": true,  //任务默认是否被选中
      "doc": "文档介绍",  //显示在任务设置选项底下，可支持富文本，格式在下方
      "repeatable": true,  //任务可不可以重复运行
      "repeat_count": 1,  //任务默认重复运行次数，需要repeatable为true
    }
  ]
}
```
### `doc`字符串格式：

#### 使用类似`[color:red]`文本内容`[/color]`的标记来定义文本样式。

#### 支持的标记包括：

- `[color:color_name]`：颜色，例如`[color:red]`。

- `[size:font_size]`：字号，例如`[size:20]`。

- `[b]`：粗体。

- `[i]`：斜体。

- `[u]`：下划线。

- `[s]`：删除线。

**注：上面注释内容为文档介绍用，实际运行时不建议写入。**

- 运行

## 开发相关

- 内置 MFATools 可以用来裁剪图片和获取 ROI
- 目前一些地方并没有特别完善,欢迎各位大佬贡献代码
- 注意，由于 `MaaFramework` 于 2.0 移除了Exec Agent，所以目前无法通过注册interface注册Custom Action和Custom Recognition

**注：在MFA中，于Pipeline中任务新增了俩个属性字段，分别为 `focus_tip` 和 `focus_tip_color`。**

- `focus` : *bool*  
  是否启用`focus_tip`。可选，默认false。
- `focus_tip` : *string* | *list<string, >*  
  当执行某任务时，在MFA右侧日志输出的内容。可选，默认空。
- `focus_tip_color` : *string* | *list<string, >*  
  当执行某任务时，在MFA右侧日志输出的内容的颜色。可选，默认为Gray。

## 致谢

### 开源库

- [MaaFramework](https://github.com/MaaAssistantArknights/MaaFramework)：自动化测试框架
- [MaaFramework.Binding.CSharp](https://github.com/MaaXYZ/MaaFramework.Binding.CSharp)：MaaFramework 的 C# 包装
- [HandyControls](https://github.com/ghost1372/HandyControls)：C# WPF 控件库
- [NLog](https://github.com/NLog/NLog)：C# 日志记录库
- [Newtonsoft.Json](https://github.com/CommunityToolkit/dotnet)：C# JSON 库

## 画大饼

### v1.0

- [x] Pipeline的GUI编辑界面
- [x] Support EN

### v1.2

- [ ] <strike>interface.json的GUI编辑界面</strike>
