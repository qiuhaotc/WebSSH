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

![管理连接](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/ManagementConnection.gif)

### 连接服务器并执行命令

点击 “Connect” 按钮后建立到远程服务器的 SSH 会话，可直接执行任意命令。

回到管理页面可以看到所有当前仍保持连接的会话。

功能特性：

1. 支持多行命令输入（切换模式）
2. 单行模式下支持历史命令回显：方向键 上/下 切换历史
3. 支持组合键：输入 `ctrl + c` 发送 `^C`，也支持 `ctrl + [a-z]`

![连接与运行命令](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/ConnectedAndRunningCommand.gif)

![运行与历史回调](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/RunCommandAndRecallCommand.gif)

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

## 文件上传功能

WebSSH 现已支持通过 SFTP 安全上传文件到远程服务器，提供专业的标签式界面：

### 主要功能
- **标签式界面**：Shell 控制台和文件上传分别使用独立标签页
- **多文件支持**：一次最多上传 3 个文件（可配置）
- **文件大小限制**：每个文件最大 10MB（可配置）
- **速率限制**：基于 IP 地址限制（每小时 20 个文件，可配置）
- **实时进度**：通过 SignalR 显示实时上传状态
- **SFTP 集成**：使用现有 SSH 连接进行安全传输

### 在线演示
🎯 **[交互式演示](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/demo.html)** - 体验文件上传界面

### 配置说明
可在 `appsettings.json` 中配置上传限制：
```json
{
  "ShellConfiguration": {
    "MaxFilesPerUpload": 3,
    "MaxFileSizeMB": 10,
    "MaxFilesPerHour": 20
  }
}
```

### 实现详情
📋 **[完整实现摘要](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/implementation-summary.md)** - 详细的技术文档，包含开发过程中的所有更改、功能特性和架构决策

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
