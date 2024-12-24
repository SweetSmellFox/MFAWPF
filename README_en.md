<div align="center">
<img alt="LOGO" src="https://github.com/SweetSmellFox/MFAWPF/blob/master/logo.png" width="256" height="256" />

# MFAWPF
[简体中文](./README.md) | [English](./README_en.md)

</div>

## Introduction

- This project is a user interface developed based on the WPF framework, aiming to provide functionality similar to MaaPiCli.

## Requirements

### Usage Requirements

- .NET8 runtime
- A non-integrated project based on the `maaframework`.

### How to Use

#### Automatic Installation

- Download workflows/install.yml from the project and modify the following:
  ```project name```, ```author name```, ```project title```, ```MAAxxx```
- Replace MAA project template .github/workflows/install.yml with the modified install.yml.
- Push the new version.

#### Manual Installation

- Download and extract the latest release.
- Copy all content from `maafw/assets/resource` to `MFAWPF/Resource`.
- Copy the `maafw/assets/interface.json` file to the root directory of `MFAWPF/`.
- ***Modify*** the newly copied `interface.json` file.
- Below is an example:

 ```json
{
  "resource": [
    {
      "name": "Official",
      "path": "{PROJECT_DIR}/resource/base"
    },
    {
      "name": "Bilibili",
      "path": [
        "{PROJECT_DIR}/resource/base",
        "{PROJECT_DIR}/resource/bilibili"
      ]
    }
  ],
  "task": [
    {
      "name": "Task",
      "entry": "Task"
    }
  ]
}
 ```
Modify it as follows:
 ```json
{
  "name": "Project Name", // Default is null
  "version":  "Project Version", // Default is null
  "url":  "Project URL (currently only supports Github)", // Default is null, for example, https://github.com/{GithubAccount}/{GithubRepo}
  "custom_title": "Custom Title", // Default is null, after using this field, the title bar will only show custom_title and version
  "default_controller": "adb", // Default is adb, the default controller after startup, options include adb or win32
  "lock_controller":false, // Default is false, whether to lock the controller; when enabled, users cannot switch between adb and win32
  "resource": [
    {
      "name": "Official",
      "path": "{PROJECT_DIR}/resource/base"
    },
    {
      "name": "Bilibili",
      "path": [
        "{PROJECT_DIR}/resource/base",
        "{PROJECT_DIR}/resource/bilibili"
      ]
    }
  ],
  "task": [
    {
      "name": "Task",
      "entry": "Task Interface",
      "checked": true,  // Default is false, whether the task is selected by default
      "doc": "Documentation",  // Default is null, displayed below the task setting options, supports rich text format (details below)
      "repeatable": true,  // Default is false, whether the task can be repeated
      "repeat_count": 1  // Default task repeat count, requires repeatable to be true
    }
  ]
}
 ```
### `doc`String Formatting：

#### Use tags like`[color:red]`Text Content`[/color]` to define text styles.

#### Supported tags include:

- `[color:color_name]`: Color, such as`[color:red]`.

- `[size:font_size]`: Font size, such as`[size:20]`.

- `[b]`: Bold.

- `[i]`: Italic.

- `[u]`: Underline.

- `[s]`：Strikethrough.

**Note: The above comments are for documentation purposes and are not recommended for actual usage.**

- Run the project

## Development Notes

- The built-in MFATools can be used to crop images and obtain ROIs.
- Some areas are not fully developed yet, and contributions are welcome.
- Note that `MaaFramework`  removed Exec Agent in version 2.0, so it's currently not possible to register Custom Actions and Custom Recognitions via interface registration.
- `MFAWPF` added the function of dynamically registering Custom Action and Custom Recognition in version 1.2.3.3. Currently, only C# is supported. The corresponding.cs files need to be placed in the custom directory of the Resource directory. Refer to [Document](./docs/en_us/CustomRecognition_Action.md)
- Placing `logo.ico` in the same directory as the exe file will replace the window icon.
- `MFAWPF` adds multi-language support for interfaces. After creating `zh-cn.json`,`zh-tw.json` and `en-us.json` in the same directory as `interface.json`, the names of docs and tasks and the names of options can be represented by keys. MFAWPF will automatically read the values corresponding to the keys in the files according to the language. If not, it defaults to the key.
**Note: In MFA, two new fields have been added to the task in the Pipeline, namely `focus_tip` and `focus_tip_color`.**

- `focus` : *bool*  
  Whether to enable `focus_tip`. Optional, default is false.
- `focus_tip` : *string* | *list<string, >*  
  When executing a task, this is the content output on the right-side log in MFA. Optional, default is empty.
- `focus_tip_color` : *string* | *list<string, >*  
  The color of the content output when executing a task. Optional, default is Gray.

## Acknowledgments

### Open Source Libraries

- [MaaFramework](https://github.com/MaaAssistantArknights/MaaFramework)：Automation testing framework
- [MaaFramework.Binding.CSharp](https://github.com/MaaXYZ/MaaFramework.Binding.CSharp)：The csharp binding of MaaFramework
- [HandyControls](https://github.com/ghost1372/HandyControls)：WPF control library
- [Serilog](https://github.com/serilog/serilog)：C# logging library
- [Newtonsoft.Json](https://github.com/CommunityToolkit/dotnet)：C# JSON library

## Roadmap

### v1.0

- [x] GUI interface for editing Pipelines
- [x] Support EN

### v1.2

- [ ] <strike>GUI interface for editing interface.json</strike>
