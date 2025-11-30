# LLM-NameDiscussion

一个使用 .NET 10 开发的示例应用，用于基于 LLM 进行“名称讨论”。

## 特性
- .NET 10 控制台应用
- 通过 `APIKey` 配置外部 LLM 服务密钥
- 使用 `NameDiscussion` 驱动讨论流程，`DiscussionProgress` 跟踪进度

## 目录结构
- `Program.cs`：程序入口
- `APIKey.cs`：读取与管理 API 密钥
- `NameDiscussion.cs`：名称讨论逻辑
- `DiscussionProgress.cs`：讨论进度数据结构
- `LLM-NameDiscussion.csproj`：项目文件

## 前置条件
- .NET SDK 10
- 可选：外部 LLM 服务账号与 API Key

## 快速开始
1. 克隆仓库：
   ```bash
   git clone https://github.com/lllcy/LLM-NameDiscussion.git
   cd LLM-NameDiscussion
   ```
2. 配置密钥：在 `APIKey.cs` 或环境变量中设置你的 API Key。
3. 构建与运行：
   ```bash
   dotnet build
   dotnet run
   ```

## 配置
- 环境变量：
  - `LLM_API_KEY`：外部 LLM 服务的 API Key
- 或在 `APIKey.cs` 中实现从文件或安全存储加载密钥。

## 使用说明
- 运行后按照控制台提示输入候选名称或参数。
- `NameDiscussion` 将组织多轮讨论并输出建议与结论。
- `DiscussionProgress` 用于在多次运行之间记录或展示状态（视实现而定）。


