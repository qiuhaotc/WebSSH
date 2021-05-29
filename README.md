# WebSSH

WebSSH allows you to SSH to your remote host anytime, anywhere.

## Usage

### Login

Input the user name and password and captcha to login, user name and password configured in appsettings.json

![Login](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/LoginToServer.gif)

### Mangement Connection

![Mangement Connection](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/ManagementConnection.gif)

### Connected To Server, Running Command

Press connected button, connected to remote server, running whatever command you want.

Go to management page, will list all available connected servers.

Features:

1. Multi lines command
2. Recall command
3. Press "ctrl + c" can run command "^C", support "ctrl + [a-z]"

![Connected & Running Command](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/ConnectedAndRunningCommand.gif)

![Run & Recall Command](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/RunCommandAndRecallCommand.gif)

## Deployment Via Docker

Running your server at port 8080, config the password "your_password" to your own.

```bash
docker pull qiuhaotc/webssh
docker run -d --name webssh -p 8080:80 -e ShellConfiguration__Users__0__Password="your_password" --restart=always qiuhaotc/webssh
```

## Misc

|Status|Value|
|:----|:---:|
|Stars|[![Stars](https://img.shields.io/github/stars/qiuhaotc/WebSSH)](https://github.com/qiuhaotc/WebSSH)
|Forks|[![Forks](https://img.shields.io/github/forks/qiuhaotc/WebSSH)](https://github.com/qiuhaotc/WebSSH)
|License|[![License](https://img.shields.io/github/license/qiuhaotc/WebSSH)](https://github.com/qiuhaotc/WebSSH)
|Issues|[![Issues](https://img.shields.io/github/issues/qiuhaotc/WebSSH)](https://github.com/qiuhaotc/WebSSH)
|Docker Pulls|[![Downloads](https://img.shields.io/docker/pulls/qiuhaotc/webssh.svg)](https://hub.docker.com/r/qiuhaotc/webssh)
|Release Downloads|[![Downloads](https://img.shields.io/github/downloads/qiuhaotc/WebSSH/total.svg)](https://github.com/qiuhaotc/WebSSH/releases)
