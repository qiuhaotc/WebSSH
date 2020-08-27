# WebSSH

WebSSH allows you to SSH to your remote host anytime, anywhere.

## Usage

### Login

Input the user name and password and captcha to login, user name and password configured in appsettings.json

![Login](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/LoginToServer.gif)

### Mangement Connection

![Mangement Connection](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/ManagementConnection.gif)

### Connected To Server, Running Command

![Connected To Server & Running Command](https://raw.githubusercontent.com/qiuhaotc/WebSSH/master/docs/ConnectedAndDisconnected.gif)

## Deployment Via Docker

Running your server at port 8080, config the password "your_password" to your own.

```bash
docker pull qiuhaotc/webssh
docker run -d --name webssh -p 8080:80 ShellConfiguration__Users__0__Password="your_password" --restart=always qiuhaotc/webssh
```
