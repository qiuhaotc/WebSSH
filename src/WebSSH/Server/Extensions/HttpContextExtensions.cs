using Microsoft.AspNetCore.Http;
using System.Linq;

namespace WebSSH.Server.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetRealIpAddress(this HttpContext context)
        {
            // 尝试从各种头部获取 IP
            var headerIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                           context.Request.Headers["X-Real-IP"].FirstOrDefault();

            // 如果存在代理头部信息
            if (!string.IsNullOrEmpty(headerIp))
            {
                // X-Forwarded-For 可能包含多个 IP，以逗号分隔，第一个为客户端真实 IP
                return headerIp.Split(',')[0].Trim();
            }

            // 使用连接的远程 IP
            var remoteIp = context.Connection.RemoteIpAddress;
            if (remoteIp != null)
            {
                // 如果是 IPv4-mapped IPv6 地址（格式如 ::ffff:192.168.1.1）
                if (remoteIp.IsIPv4MappedToIPv6)
                {
                    // 转换为 IPv4 格式
                    remoteIp = remoteIp.MapToIPv4();
                }

                return remoteIp.ToString();
            }

            // 没有找到 IP
            return "0.0.0.0";
        }
    }
}
