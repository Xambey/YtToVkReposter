using System;
using System.Text;
using System.Threading;
using Google.Apis.Auth;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace YtToVkReposter
{
    class Program
    {
        private static YtReposter _server;
        
        private static void  Run()
        {
            _server = new YtReposter();

            _server.Logger.Info("Welcome to YtToVkReposter! Enter '!start' for start a server ...");
            _server.Logger.Info("> ");

            _server.Start();

            while (true)
            {
                try
                {
                    string str;
                    str = Console.ReadLine();
                    switch (str)
                    {
                        case "!start":
                            _server.Start();
                        
                            break;
                        case "!shutdown":
                            _server.Shutdown();
                            break;
                        case "!help":
                            _server.Logger.Info(
                                "List commands:\n!start - start a server;\n!shutdown - shut down a server;\n!add - add channel;\n!remove - remove channel;\n!channels - info about channels;\n!exit - exit;");
                            break;
                        case "!channels":
                            _server.Logger.Info($"List channels:");
                            foreach (var channel in _server.GetChannels())
                            {
                                _server.Logger.Info(
                                    $"{channel.YtName}\n\tVK token:{channel.VkToken};\n\tYouTube id:{channel.YtId};\n\tVkGroupId:{channel.VkGroupId};\n\tVkUserId:{channel.VkUserId};");
                                _server.Logger.Info("\tVideo:");
                                foreach (var video in channel.VideoStack)
                                {
                                    _server.Logger.Info($"\t\t{video}");
                                }
                                _server.Logger.Info("");
                            }
                            break;
                        case "!add":
                            string nameOrId, token, groupId, userId;

                            _server.Logger.Info("Enter the name of the channel?(y/n): ");
                            bool flag = Console.ReadLine() == "y";

                            if (flag)
                            {
                                do
                                {
                                    _server.Logger.Info("Enter channel name on YouTube: ");
                                    nameOrId = Console.ReadLine();
                                } while (string.IsNullOrEmpty(nameOrId));

                            }
                            else
                            {
                                do
                                {
                                    _server.Logger.Info("Enter id of channel on YouTube: ");
                                    nameOrId = Console.ReadLine();
                                } while (string.IsNullOrEmpty(nameOrId));
                            }
                            do
                            {
                                _server.Logger.Info("Enter VK token: ");
                                token = Console.ReadLine();
                            } while (string.IsNullOrEmpty(token));

                            do
                            {
                                _server.Logger.Info("Enter VK Group id: ");
                                groupId = Console.ReadLine();
                            } while (string.IsNullOrEmpty(groupId));
                            
                            do
                            {
                                _server.Logger.Info("Enter VK User id: ");
                                userId = Console.ReadLine();
                            } while (string.IsNullOrEmpty(userId));

                            _server.AddChannel(nameOrId, token, groupId, userId, flag);
                            break;
                        case "!remove":
                            _server.Logger.Info("Enter channel name: ");
                            var rmname = Console.ReadLine();
                            _server.RemoveChannel(rmname);
                            break;
                        case "!exit":
                            return;
                    }
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        _server.Logger.Error($"{ex.InnerException.Message} : {ex.InnerException.StackTrace}");
                    }
                    _server.Logger.Error($"{ex.Message} : {ex.StackTrace}");
                }
            }
        }
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            Run();
        }
    }
}