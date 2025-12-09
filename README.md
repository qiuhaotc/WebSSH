# WebSSH

English | [中文](README_CN.md)

WebSSH allows you to SSH to your remote host anytime, anywhere.

## Demonstrate Site

Check <https://webssh.azurewebsites.net>, you can use any login name and password to logged in.

## Usage

### Login

Input the user name and password and captcha to login, user name and password configured in appsettings.json

![Login](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/LoginToServer.gif)

### Management Connection

![Mangement Connection](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/Management.png)

### Private Key Authentication

WebSSH now supports SSH private key authentication in addition to traditional password authentication:

- **Multiple Authentication Types**: Choose between password or private key authentication for each stored session
- **Encrypted Key Support**: Supports private keys protected with a passphrase
- **Multiple Key Formats**: Compatible with RSA, DSA, ECDSA, and ED25519 key types
- **Secure Storage**: Private keys are stored as base64-encoded strings in your browser's local storage
- **Backward Compatible**: Existing password-based sessions continue to work without any changes

For detailed usage instructions, see the [Private Key Authentication documentation](docs/private-key-authentication.md).

### Connected To Server, Running Command

Press the Connect button, connect to the remote server, then run whatever command you want.

Go to management page, will list all available connected servers.

![Connected & Running Command 1](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/Interface.png)

![Connected & Running Command 2](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/Shell.gif)

## Real-time (SignalR) Enhancements

From the latest version, WebSSH replaces periodic HTTP polling with ASP.NET Core SignalR for a truly real-time terminal experience:

| Area | Before | Now (SignalR) |
| ---- | ------ | -------------- |
| Output refresh | 1s / 100ms adaptive polling | Push as soon as data arrives |
| Bandwidth | Repeated empty responses | Only actual output payloads |
| Command send | HTTP GET endpoint | Bi-directional hub method `RunCommand` |
| Initial backlog | Multiple fetches | Single join flush + streaming |
| Reconnect | Full page reliance | Automatic hub reconnect with status messages |

Key benefits:

1. Lower latency for interactive workflows (vim, tail -f, etc.).
2. Reduced server & network overhead – no useless polling cycles.
3. Better UX with connection state (Connected / Reconnecting / Disconnected).
4. Extensible channel for future features (file upload, terminal resize, heartbeat).

Technical notes:

* Hub path: `/shellHub`
* Group isolation: each browser session + shell GUID => SignalR group
* Still keeps an output queue for initial backlog replay on (re)join
* Automatic reconnect enabled; transient drops will not lose buffered output
* Legacy endpoints (`GetShellOutput`, `IsConnected`, `RunShellCommand`) have been removed after migration

Planned (roadmap ideas):

* Terminal resizing sync (cols/rows)
* Structured output channels (stdout/stderr separation)
* Optional rate limiting / flood protection
* Audit log of executed commands
* ~~Secure copy (SCP / SFTP) integration~~ ✅ **Completed**

## File Transfer Features

WebSSH now supports secure file upload and download to/from remote servers via SFTP with a professional tab-based interface:

![Upload Files](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/UploadFiles.png)
![Download Files](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/DownLoadFiles.png)

### File Upload
- **Tab-based Interface**: Separate tabs for Shell Console and File Upload
- **Multiple File Support**: Upload up to 3 files simultaneously (configurable)
- **File Size Limits**: Maximum 10MB per file (configurable)
- **Rate Limiting**: IP-based limiting (20 files per hour, configurable)
- **Real-time Progress**: Live upload status via SignalR
- **SFTP Integration**: Secure transfer using existing SSH connections

### File Download
- **Remote File Browser**: Navigate and browse remote directory structure
- **Multi-file Selection**: Download up to 3 files per operation (configurable)
- **ZIP Archive Support**: Multiple files automatically packaged for download
- **Size Limits**: Maximum 20MB total download size (configurable)
- **Rate Limiting**: IP-based limiting (20 downloads per hour, configurable)
- **Real-time Progress**: Live download status via SignalR

### Configuration
File transfer restrictions can be configured in `appsettings.json`:
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

### Implementation Details
📋 **[Complete Implementation Summary](https://github.com/qiuhaotc/WebSSH/blob/master/docs/implementation-summary.md)** - Detailed technical documentation of all changes, features, and architecture decisions made during development

## Deployment Via Docker

Running your server at port 8070, config the password "your_password" to your own.

```bash
docker pull qiuhaotc/webssh
docker run -d --name webssh -p 8070:8080 -e ShellConfiguration__Users__0__Password="your_password" --restart=always qiuhaotc/webssh
```

## Misc

| Status            |                                                             Value                                                              |
| :---------------- | :----------------------------------------------------------------------------------------------------------------------------: |
| Stars             |              [![Stars](https://img.shields.io/github/stars/qiuhaotc/WebSSH)](https://github.com/qiuhaotc/WebSSH)               |
| Forks             |              [![Forks](https://img.shields.io/github/forks/qiuhaotc/WebSSH)](https://github.com/qiuhaotc/WebSSH)               |
| License           |            [![License](https://img.shields.io/github/license/qiuhaotc/WebSSH)](https://github.com/qiuhaotc/WebSSH)             |
| Issues            |             [![Issues](https://img.shields.io/github/issues/qiuhaotc/WebSSH)](https://github.com/qiuhaotc/WebSSH)              |
| Docker Pulls      |       [![Downloads](https://img.shields.io/docker/pulls/qiuhaotc/webssh.svg)](https://hub.docker.com/r/qiuhaotc/webssh)        |
| Release Downloads | [![Downloads](https://img.shields.io/github/downloads/qiuhaotc/WebSSH/total.svg)](https://github.com/qiuhaotc/WebSSH/releases) |
