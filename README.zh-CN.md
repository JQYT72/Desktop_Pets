# 桌宠 Desktop Pets

基于 .NET 8 和 WPF 的 Windows 桌面宠物应用。导入自己的图片或 Codex 生成的桌宠包，即可在桌面上拥有一个透明无边框、支持拖拽交互和 spritesheet 动画的桌宠。

[English](README.md)

## 功能特性

- 透明、无边框、置顶的 WPF 桌宠窗口。
- 内置示例桌宠图片。
- 鼠标拖拽移动桌宠。
- 点击弹跳反馈与空闲伪动画。
- 拖拽导入 PNG、JPG、JPEG、WEBP 格式图片。
- 拖拽导入 `.codex/pets/<pet-id>/` 下生成的 Codex 桌宠文件夹或 `pet.json` 文件。
- 导入的素材会被复制到受管理的用户数据目录。
- 重启后恢复上次使用的桌宠和窗口位置。
- 右键菜单提供退出功能。

## 环境要求

- Windows 10/11。
- .NET 8 SDK。

可通过以下命令安装 SDK：

```powershell
winget install --id Microsoft.DotNet.SDK.8 --exact
```

## 运行

```powershell
dotnet run --project src/DesktopPets.App/DesktopPets.App.csproj
```

## 使用管理界面

右键点击桌宠，选择 `管理桌宠` 或 `导入桌宠包`。

管理窗口支持：

- 选择 `pet.json`
- 选择 `spritesheet.webp`
- 同时导入这两个文件并播放桌宠
- 通过操作按钮预览每个 Codex 动画状态
- 将桌宠文件夹拖入窗口
- 打开 `%APPDATA%/DesktopPets`

右键点击桌宠并打开 `Animations`，可直接从桌宠窗口触发以下状态：

- `Idle`
- `Run Right`
- `Run Left`
- `Wave`
- `Jump`
- `Failed`
- `Waiting`
- `Working`
- `Review`

## 交互行为

- 鼠标悬停在桌宠上会触发挥手打招呼。
- 连续点击桌宠可循环切换所有 Codex 动画状态。
- 向左或向右拖拽时播放对应方向的奔跑动画。
- 开启 Auto Walk 后，动画桌宠会在最后停放位置附近缓慢左右游走。
- 管理窗口提供 `Auto Walk` 开关和 `Back To Idle` 按钮。

## 测试

```powershell
dotnet build
dotnet test
```

## 发布单文件 EXE

创建自包含的 Windows x64 可执行文件：

```powershell
dotnet publish src/DesktopPets.App/DesktopPets.App.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish/win-x64 /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=None /p:DebugSymbols=false
```

输出为：

```text
artifacts/publish/win-x64/DesktopPets.App.exe
```

该可执行文件已包含 .NET 运行时，大多数 Windows 用户双击即可运行，无需安装 SDK。

## 用户数据

长期用户数据保存在安装目录之外：

```text
%APPDATA%/DesktopPets/
  config.json
  pets/
    generated/
```

每个导入的桌宠按如下结构保存：

```text
pets/generated/<pet-id>/
  pet.json
  source/input.<ext>
  sprites/idle.<ext>
```

导入后应用不再依赖原始拖入文件的路径。

## Codex 动画桌宠

Codex 生成的桌宠可直接导入。将整个桌宠文件夹或其 `pet.json` 文件拖入桌宠窗口即可：

```text
C:/Users/Administrator/.codex/pets/guagua-cat/
  pet.json
  spritesheet.webp
```

应用会将桌宠包复制到 `%APPDATA%/DesktopPets/pets/generated/`，并播放标准 Codex spritesheet 各行动画：

也可以将 `pet.json` 和 `spritesheet.webp` 一起拖入窗口，两者不必事先位于同一文件夹，只要 `pet.json` 引用了对应的 spritesheet 文件名即可。

- `idle` 站立时循环播放。
- `waving` 点击时播放。
- `running-right` / `running-left` 拖拽时播放。
