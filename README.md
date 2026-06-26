# FeatureTool

🎉 基于 [ViVe](https://github.com/thebookisclosed/ViVe) 的 Windows 特性配置图形工具，支持查询、启用 / 禁用 / 重置 A/B 特性，重启后生效。

[![.NET](https://github.com/CuzTeam/FeatureTool/actions/workflows/build.yml/badge.svg)](https://github.com/CuzTeam/FeatureTool/actions/workflows/build.yml)
[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](LICENSE)

## 功能

- 🔍 全量查询 Runtime 特性配置（按 FeatureId 去重，显示生效优先级）
- 🎛 三态切换：启用 / 禁用 / 默认（重置）
- 💾 Runtime + Boot 双写：立即生效且重启保留
- ➕ 手动添加特性 ID（字典未收录的也可手动管理，注释存 `~/.feature-ids.json`）
- 🗂 内置 [FeatureDictionary.pfs](https://github.com/thebookisclosed/ViVe)（17000+ 特性名映射）
- ⚙️ 配置页：可选显示不可配置项
- ℹ️ 关于页：应用信息、协议、仓库链接

## 截图

> TODO

## 下载

前往 [Releases](https://github.com/CuzTeam/FeatureTool/releases) 下载最新版本。

## 从源码构建

需要：
- Windows 10 17763+ / Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Windows App SDK 2.2](https://learn.microsoft.com/windows/apps/windows-app-sdk/)

```powershell
git clone https://github.com/CuzTeam/FeatureTool.git
cd FeatureTool
dotnet build -c Release -p:Platform=x64
```

构建产物在 `bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\`。

## 使用

1. 以管理员身份运行 `FeatureTool.exe`（程序内置 UAC 提权，双击会弹窗确认）
2. 在搜索框输入特性名称或 ID 查找
3. 点击列表项右侧的开关切换状态：
   - ✅ 启用（Enabled）
   - ☑️ 禁用（Disabled）
   - ➖ 默认（Default，重置为系统默认）
4. 重启后配置完全生效

### 添加特性

点击搜索框右侧的 ➕ 按钮，输入特性 ID，选择启用 / 禁用（可选填注释）。注释保存在 `~/.feature-ids.json`。

## 技术细节

- 集成方式：ViVe 源码移植到 `FeatureTool/ViVe/`（.NET 8 重编译，AGPL-3.0 兼容 GPL-3.0）
- 写入策略：`SetFeatureConfigurations(Runtime)` 立即生效 + `SetFeatureConfigurations(Boot)` 持久化 + `SetBootFeatureConfigurationState(BootPending)` 标记重启应用
- 优先级：写入用 `User`（8），不可变优先级（ImageDefault / EKB / ImageDefaultEditionOverride / Security / ImageOverride）的项在 UI 中置灰不可改
- 去重：同一 FeatureId 多条优先级配置，取数值最大的（即实际生效的）

## 协议

本项目基于 [AGPL-3.0](LICENSE) 协议开源。

ViVe 库由 [@thebookisclosed](https://github.com/thebookisclosed) 开发，基于 GPL-3.0 协议，已与 AGPL-3.0 兼容。

## 致谢

- [@thebookisclosed](https://github.com/thebookisclosed) - [ViVe](https://github.com/thebookisclosed/ViVe)
