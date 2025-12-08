# WebSSH

[English](README.md) | 中文

WebSSH 让你可以随时随地通过浏览器 SSH 到远程主机。

## 演示站点

访问 <https://webssh.azurewebsites.net>，可使用任意用户名与密码登录（演示环境不做真实校验）。

## 使用说明

### 登录

输入用户名、密码与验证码进行登录，用户名与密码来源于服务器端 `appsettings.json` 配置。

![登录](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/LoginToServer.gif)

### 管理连接

![管理连接](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/Management.png)

### 密钥认证

WebSSH 支持使用 SSH 私钥进行身份验证，除了传统的密码认证方式外：

- **多种认证方式**：每个保存的会话可以选择使用密码或私钥认证
- **加密密钥支持**：支持使用密码短语保护的私钥
- **多种密钥格式**：兼容 RSA、DSA、ECDSA 和 ED25519 密钥类型
- **安全存储**：私钥以 base64 编码格式存储在浏览器本地存储中
- **向后兼容**：现有的基于密码的会话无需任何更改即可继续工作

详细使用说明请参阅 [私钥认证文档](docs/private-key-authentication.md)。

### 连接服务器并执行命令

点击 "Connect" 按钮后建立到远程服务器的 SSH 会话，可直接执行任意命令。

回到管理页面可以看到所有当前仍保持连接的会话。

![连接与运行命令](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/Interface.png)

## 实时特性（SignalR 改造）

最新版本已将原来的定时 HTTP 轮询输出，升级为基于 ASP.NET Core SignalR 的实时推送：

| 模块 | 以前（轮询） | 现在（SignalR 推送） |
| ---- | ------------ | -------------------- |
| 输出刷新 | 1 秒 / 100ms 自适应轮询 | 数据到达立即推送 |
| 带宽占用 | 频繁空响应 | 只发送真实输出内容 |
| 命令发送 | HTTP GET | Hub 双向方法 `RunCommand` |
| 初始 backlog | 多次取增量 | 加入组后一次性补齐 + 后续流式 |
| 断线恢复 | 依赖刷新页面 | 自动重连并显示状态 |

优势：

1. 更低延迟，适合 tail -f、交互式工具 (vim、htop 等)
2. 降低服务器与网络负载，无空轮询请求
3. 可见的连接状态（已连接 / 重连中 / 已断开）
4. 预留扩展通道（文件上传、窗口尺寸同步、心跳等）

技术要点：

* Hub 路径：`/shellHub`
* 分组隔离：浏览器会话 + Shell GUID 组成独立组
* 仍保留输出队列以便重连 / 新加入时回放 backlog
* 启用自动重连，临时网络抖动不会丢失已缓冲的输出
* 已移除旧接口：`GetShellOutput`、`IsConnected`、`RunShellCommand`

后续规划（Roadmap）：

* 终端大小（列 / 行）同步
* 标准输出 / 错误输出分离
* 速率限制与洪泛防护
* 命令审计日志
* ~~文件传输（SCP / SFTP）~~ ✅ **已完成**

## 文件传输功能

WebSSH 现已支持通过 SFTP 与远程服务器安全传输文件，提供专业的标签式界面：

![文件上传](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/UploadFiles.png)
![文件下载](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/DownLoadFiles.png)

### 文件上传

- **标签式界面**：Shell 控制台和文件上传分别使用独立标签页
- **多文件支持**：一次最多上传 3 个文件（可配置）
- **文件大小限制**：每个文件最大 10MB（可配置）
- **速率限制**：基于 IP 地址限制（每小时 20 个文件，可配置）
- **实时进度**：通过 SignalR 显示实时上传状态
- **SFTP 集成**：使用现有 SSH 连接进行安全传输

### 文件下载

- **远程文件浏览**：导航和浏览远程目录结构
- **多文件选择**：每次操作下载最多 3 个文件（可配置）
- **ZIP 压缩支持**：多文件自动打包下载
- **大小限制**：最大 20MB 总下载大小（可配置）
- **速率限制**：基于 IP 地址限制（每小时 20 次下载，可配置）
- **实时进度**：通过 SignalR 显示实时下载状态

### 配置说明

可在 `appsettings.json` 中配置文件传输限制：

```json
{
  "ShellConfiguration": {
    "MaxFilesPerUpload": 3,
    "MaxFileSizeMB": 10,
    "MaxFilesPerHour": 20,
    "MaxFilesPerDownload": 3,
    "MaxDownloadSizeMB": 20,
    "MaxDownloadsPerHour": 20
  }
}
```

### 实现详情

📋 **[完整实现摘要](https://github.com/qiuhaotc/WebSSH/blob/master/docs/implementation-summary.md)** - 详细的技术文档，包含开发过程中的所有更改、功能特性和架构决策

## 通过 Docker 部署

示例：对外监听 8070 端口，并设置第一个用户的密码为 `your_password`。

```bash
docker pull qiuhaotc/webssh
docker run -d --name webssh -p 8070:8080 -e ShellConfiguration__Users__0__Password="your_password" --restart=always qiuhaotc/webssh
```

## 其他信息

| 状态 | 值 |
| :-- | :--: |
| Stars | [![Stars](https://img.shields.io/github/stars/qiuhaotc/WebSSH)](https://github.com/qiuhaotc/WebSSH) |
| Forks | [![Forks](https://img.shields.io/github/forks/qiuhaotc/WebSSH)](https://github.com/qiuhaotc/WebSSH) |
| License | [![License](https://img.shields.io/github/license/qiuhaotc/WebSSH)](https://github.com/qiuhaotc/WebSSH) |
| Issues | [![Issues](https://img.shields.io/github/issues/qiuhaotc/WebSSH)](https://github.com/qiuhaotc/WebSSH) |
| Docker Pulls | [![Downloads](https://img.shields.io/docker/pulls/qiuhaotc/webssh.svg)](https://hub.docker.com/r/qiuhaotc/webssh) |
| Release Downloads | [![Downloads](https://img.shields.io/github/downloads/qiuhaotc/WebSSH/total.svg)](https://github.com/qiuhaotc/WebSSH/releases) |
