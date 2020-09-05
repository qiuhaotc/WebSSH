using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;

namespace WebSSH.Playground
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            // tell the builder to look for the appsettings.json file
            builder.AddJsonFile("consolesettings.json", optional: false, reloadOnChange: true);
            builder.AddUserSecrets<Program>();

            var config = builder.Build();
            var section = config.GetSection("DefaultSSHForTest");
            var host = section["Host"];
            var port = Convert.ToInt32(section["Port"]);
            var userName = section["UserName"];
            var password = section["Password"];

            using var client = new SshClient(host, port, userName, password);
            client.Connect();
            using var shellStream = client.CreateShellStream("Terminal", 80, 30, 800, 400, 1000);

            var task = Task.Run(() =>
            {
                while (true)
                {
                    string result;
                    while ((result = shellStream.ReadLine(TimeSpan.FromSeconds(1000))) != null)
                    {
                        Console.WriteLine(result);
                    }

                    Thread.Sleep(100);
                }
            });

            while (true)
            {
                var commandStr = Console.ReadLine();

                if (commandStr == "exit")
                {
                    break;
                }

                shellStream.WriteLine(commandStr);
            }

            task.Dispose();
        }
    }
}
