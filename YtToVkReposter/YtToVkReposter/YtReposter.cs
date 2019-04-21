using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VkNet;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using Video = VkNet.Model.Attachments.Video;

namespace YtToVkReposter
{
    public class YtReposter
    {
        //Links of remote PubSub pages
        private string PathToChannelsConfig { get; set; }
        private string PathToSettingsConfig { get; set; }
        private DateTime startServerTime { get; set; }
        private List<Channel> _channels;
        private Timer _timer;
        private string _ytKey;
        public log4net.ILog Logger;

        public YtReposter(string pathToResources = null)
        {
            string pathToResourcesTemp = string.IsNullOrEmpty(pathToResources) ? (Environment.CurrentDirectory.Contains("bin") ? Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName : Environment.CurrentDirectory) + $"{System.IO.Path.AltDirectorySeparatorChar}Resources{System.IO.Path.AltDirectorySeparatorChar}" : pathToResources;

            PathToChannelsConfig = pathToResourcesTemp + "channels.json";
            PathToSettingsConfig = pathToResourcesTemp + "settings.json";

            _channels = new List<Channel>();

            var logRepository = log4net.LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly());

            //загружаем конфигурацию логировщика
            log4net.Config.XmlConfigurator.Configure(logRepository,
                                                     new System.IO.FileInfo(pathToResourcesTemp + "log4net.config"));
            Logger = log4net.LogManager.GetLogger(typeof(YtReposter));

            startServerTime = DateTime.UtcNow;

            Logger.Info("YtReposter initialized!");
        }

        private void LoadChannelsFromFile()
        {
            if (File.Exists(PathToChannelsConfig))
            {
                var json = JsonConvert.DeserializeObject<List<Channel>>(File.ReadAllText(PathToChannelsConfig));
                if (json != null)
                {
                    foreach (var item in json)
                    {
//                        if (_channels.FirstOrDefault(x => x.YtName == item.Key) == null)
//                        {
//                            var channel = new Channel()
//                            {
//                                YtName = item.Key,
//                                VkToken = item.Value["VkToken"].ToObject<string>(),
//                                YtId = item.Value["YtId"].ToObject<string>(),
//                                VkGroupId = item.Value["VkGroupId"].ToObject<string>(),
//                                VkUserId = item.Value["VkUserId"].ToObject<string>()
//                            };
//                            channel.VideoStack.PushRange(item.Value["VideoStack"].Values<string>());
//                            _channels.Add(channel);
//
//                            Logger.Info($"Channel {item.Key} added");
//                        }
                        if (!_channels.Exists(x => x.YtName == item.YtName))
                        {
                            _channels.Add(item);
                            Logger.Info($"Channel {item.YtName} added");
                        }
                    }
                }
            }
            else
            {
                Logger.Info("Channels configuration not found! Default configuration is creating...");

                Directory.CreateDirectory(PathToChannelsConfig.Remove(PathToChannelsConfig.LastIndexOf(System.IO.Path.AltDirectorySeparatorChar)));
                File.Create(PathToChannelsConfig);

                Logger.Info("Channels configuration has created");
            }
        }

        private bool LoadSettingsFromFile()
        {
            if (File.Exists(PathToSettingsConfig))
            {
                var json = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(PathToSettingsConfig));
                if (json != null)
                {
                    Logger.Info("Settings configuration found");

                    if(json.TryGetValue("Token", out JToken token))
                    {
                        _ytKey = token.Value<string>();

                        return true;
                    }
                    else
                    {
                        Logger.Warn("Token not found at the settings");
                    }
                }
            }
            else
            {
                Logger.Info("Settings configuration not found! Default configuration is creating...(Token is empty)");

                Directory.CreateDirectory(PathToSettingsConfig.Remove(PathToSettingsConfig.LastIndexOf(System.IO.Path.AltDirectorySeparatorChar)));
                File.Create(PathToSettingsConfig);

                Logger.Info("Settings configuration has created");
            }
            return false;
        }
        private void UpdateChannelInFile(Channel channel)
        {
            if (File.Exists(PathToChannelsConfig))
            {
//                var jchannel = new JObject();
//                jchannel.Add("YtId", new JValue(channel.YtId));
//                jchannel.Add("VkToken", new JValue(channel.VkToken));
//                jchannel.Add("VkGroupId", new JValue(channel.VkGroupId));
//                jchannel.Add("VkUserId", new JValue(channel.VkUserId));
//                
//                var ar = new JArray(channel.VideoStack.Select(x => new JValue(x)));
//                jchannel.Add("VideoStack", ar);
//                var ch = json.SelectToken($".{channel.YtName}", false);
//                if (ch != null)
//                {
//                    ch["YtId"] = new JValue(channel.YtId);
//                    ch["VkToken"] = new JValue(channel.VkToken);
//                    ch["VideoStack"] = new JArray(channel.VideoStack.Select(x => new JValue(x)));
//                    ch["VkGroupId"] = new JValue(channel.VkGroupId);
//                }
//                else
//                    json.Add(channel.YtName, jchannel);

                File.WriteAllText(PathToChannelsConfig, JsonConvert.SerializeObject(_channels, Formatting.Indented));

                Logger.Info($"Channel {channel.YtName} configuration updated");
            }
            else
            {
                Logger.Error("WriteChannelToFile - Configuration file not found!");
            }
        }
        public void AddChannel(string ytNameOrId, string vkToken, string groupId, string userId, bool isYtName = true)
        {
            if (isYtName && _channels.FirstOrDefault(x => x.YtName == ytNameOrId) != null)
                throw  new Exception("Error. Channel already exists");
            else if (!isYtName && !IsYtChannelIdExist(ytNameOrId))
                throw  new Exception("Error. Channel with the id doesn't exist");
            var channel = new Channel() {YtName = isYtName ? ytNameOrId : GetYtNameByChannelId(ytNameOrId), VkToken = vkToken, VkGroupId = groupId, VkUserId = userId, YtId = isYtName ? GetYtChannelId(ytNameOrId) : ytNameOrId};
            while (channel.YtId == null)
            {
                Logger.Error("Channel not found\nEnter user name: ");

                string name;
                do
                {
                    name = Console.ReadLine();
                } while (string.IsNullOrEmpty(name));
                channel.YtId = GetYtChannelId(name);
                channel.YtName = name;
            }
            while (channel.YtName == null)
            {
                Logger.Error("Channel not found\nEnter channel id: ");

                string name;
                do
                {
                    name = Console.ReadLine();
                } while (string.IsNullOrEmpty(name));
                channel.YtId = name;
                channel.YtName = GetYtNameByChannelId(name);
            }
            while (true)
            {
                try
                {
                    VkApi test = new VkApi();
                    test.Authorize(new ApiAuthParams() {
                        AccessToken = channel.VkToken, 
                        ApplicationId = 6321454,
                        UserId = long.Parse(channel.VkUserId)                      
                    });
                    if (!test.IsAuthorized)
                        throw new Exception("Error Vk authorization data not valid");
                    break;
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                        Logger.Error(ex.Message + Environment.NewLine + ex.InnerException.StackTrace);
                    else
                        Logger.Error(ex.Message);

                    string vk;
                    do
                    {
                        Logger.Error("Enter vk group token: ");
                        vk = Console.ReadLine();
                    } while (string.IsNullOrEmpty(vk));
                    channel.VkToken = vk;
                }
            }
            
            if (_channels.FirstOrDefault(x => x.YtName == channel.YtName) != null)
                return;

            using (var ytApi = new YouTubeService(new BaseClientService.Initializer() {ApiKey = _ytKey}))
            {
                var searchListRequest = ytApi.Search.List("snippet");
                searchListRequest.ChannelId = channel.YtId;
                searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                searchListRequest.Type = "video";
                searchListRequest.PublishedAfter = DateTime.Today.Subtract(TimeSpan.FromDays(1));
                channel.VideoStack.PushRange(searchListRequest.Execute().Items.Select(x => x.Id.VideoId)); 
            }
            _channels.Add(channel);
            UpdateChannelInFile(channel);

            Logger.Info($"Channel {channel.YtName} has been added");
        }

        public void RemoveChannel(string name)
        {
            var channel = _channels.FirstOrDefault(x => x.YtName == name);
            if (channel != null)
            {
                _channels.Remove(channel);
                UpdateChannelInFile(channel);

                Logger.Info($"Channel {channel.YtName} has been removed");
            } 
        }

        private string GetYtNameByChannelId(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(_ytKey))
                    throw new Exception("Api key is null or empty");
                using (var service = new YouTubeService(new BaseClientService.Initializer() {ApiKey = _ytKey}))
                {
                    var list = service.Channels.List("snippet");
                    list.Id = id;
                    var request = list.Execute();
                    if (request.Items.Count == 1)
                    {
                        return request.Items.First().Snippet.Title;
                    }
                    return null;
                }       
            }
            catch (Exception ex)
            {
                var mes = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                throw new Exception($"Trying youtube channel name by id ({id}) has been failded : {mes}");
            }
        }
        private bool IsYtChannelIdExist(string id)
        {
            try
            {
                string key;
                do
                {
                    Logger.Info("Enter api key: ");
                    key = Console.ReadLine();
                } while (string.IsNullOrEmpty(key));

                using (var service = new YouTubeService(new BaseClientService.Initializer() {ApiKey = key}))
                {
                    var list = service.Channels.List("Id");
                    list.Id = id;
                    var request = list.Execute();
                    if (request.Items.Count == 1)
                    {
                        _ytKey = key;
                        return true;
                    }
                    return false;
                }       
            }
            catch (Exception ex)
            {
                var mes = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                throw new Exception($"Checking youtube channel id ({id}) has been failded : {mes}");
            }
        }
        private string GetYtChannelId(string userName)
        {
            try
            {
                string key;
                do
                {
                    Logger.Info("Enter api key: ");
                    key = Console.ReadLine();
                } while (string.IsNullOrEmpty(key));

                using (var service = new YouTubeService(new BaseClientService.Initializer() {ApiKey = key}))
                {
                    var list = service.Channels.List("Id");
                    list.ForUsername = userName;
                    var request = list.Execute();
                    if (request.Items.Count > 0)
                    {
                        _ytKey = key;
                        return request.Items.FirstOrDefault(x => x.Id != null)?.Id;
                    }
                }       
            }
            catch (Exception ex)
            {
                var mes = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                throw new Exception($"Getting youtube channel groupId ({userName}) has been failded : {mes}");
            }
            return null;
        }

        /// <summary>
        /// Обновляет списки видео в конфигурации на последние (чтобы не постить старое)
        /// </summary>
        private void InitChannelsFromFile()
        {
            Logger.Info("Init update channels...");
            //Загружаем каналы
            LoadChannelsFromFile();

            using (var ytApi = new YouTubeService(new BaseClientService.Initializer() { ApiKey = _ytKey }))
            {
                foreach (var channel in _channels)
                {
                    if (channel.UpdateOnInit)
                    {
                        var searchListRequest = ytApi.Search.List("snippet");
                        searchListRequest.ChannelId = channel.YtId;
                        searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                        searchListRequest.Type = "video";
                        searchListRequest.MaxResults = 20;
                        searchListRequest.PublishedAfter = DateTime.Today.Subtract(TimeSpan.FromDays(1));

                        var results = searchListRequest.Execute().Items.Take(20).Select(sn => sn.Id.VideoId).ToList();

                        channel.VideoStack.Clear();
                        channel.VideoStack.PushRange(results);

                        UpdateChannelInFile(channel);

                        Thread.Sleep(300);
                    }
                }
            }
        }

        public void Start()
        {
            if (_timer == null)
            {
                Logger.Info("Server has been started");

                Logger.Info("Enter Api key (30 seconds waiting): ");

                if (LoadSettingsFromFile()) {
                    Logger.Info("Api Key loaded from settings.json...");

                    InitChannelsFromFile();

                    _timer = new Timer(HandlerRepostMessage, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
                    Logger.Info("Repost handler started!");
                }
                else
                {
                    throw new Exception("Fatal Error. Settings (/Resources/settings.json) not found! You should add settings.json and restart service!");
                }
                
            }
        }

        public IEnumerable<Channel> GetChannels() => _channels;

        public void Shutdown()
        {
            if (_timer != null)
            {
                _channels = null;
                _timer.Dispose();
                _timer = null;

                Logger.Info("Server has been shut down!");
            }        
        }
        private async void HandlerRepostMessage(object state)
        {
            Channel current = null;
            try
            {
                using (var ytApi = new YouTubeService(new BaseClientService.Initializer() {ApiKey = _ytKey}))
                {
                    foreach (var channel in _channels)
                    {
                        current = channel;
                        var searchListRequest = ytApi.Search.List("snippet");
                        searchListRequest.ChannelId = channel.YtId;
                        searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                        searchListRequest.Type = "video";
                        searchListRequest.PublishedAfter = DateTime.Today.Subtract(TimeSpan.FromDays(1)); //Видео за сутки

                        var searchListResult = searchListRequest.Execute();
                        if (searchListResult.Items.Count > 0)
                        {
                            var source = searchListResult.Items.Reverse().ToList();
                            //Список первых 20 публикации ожидаемых к публикации, которые не содержатся в текущем стеке видео
                            var lastShippets = source.Take(20).Where(p => !channel.VideoStack.Contains(p.Id.VideoId)).ToList();

                            if (lastShippets.Any())
                            {
                                /*
                                if (channel.ytname == "blacksilverchannel")
                                {
                                    var storage = _channels.firstordefault(x => x.ytname == "blacksilverufa")
                                        ?.videostack;
                                    if (storage != null)
                                    {
                                        var remove = searchresults.where(x => storage.contains(x.id.videoid)).select(x => x.id.videoid);
                                        var enumerable1 = remove as ilist<string> ?? remove.tolist();
                                        if(enumerable1.any())
                                            foreach (var rem in enumerable1)
                                            {
                                                searchresults.remove(searchresults.first(x => x.id.videoid == rem));
                                            }
                                    }
                                }
                                */

                                //Список id видео к публикации
                                var lastVideos = lastShippets.Select(x => x.Id.VideoId).ToList();

                                //Список видео, которые уже публиковались в этой группе от других каналов и есть в lastVideos
                                var channelVideosFromSameGroupVk = _channels
                                    .Where(ch => ch.VkGroupId == channel.VkGroupId && ch != channel && ch.VideoStack != null)
                                    .SelectMany(ch => ch.VideoStack.Select(x => x))
                                    .Intersect(lastVideos)
                                    .ToList();
                                //Список видео id для публикации
                                var newVideos = lastVideos.Except(channelVideosFromSameGroupVk).ToList();

                                //Добавляем новые видео в стек video id
                                channel.VideoStack.PushRange(newVideos);

                                //Обновляем конфигурацию канала (videoId stack) в channels.json
                                UpdateChannelInFile(channel);

                                var info = newVideos.Select(x => lastShippets.First(s => s.Id.VideoId == x)).ToDictionary(p => p);
                                VkApi api = new VkApi();
                              
                                foreach (var item in info)
                                {
                                    await api.AuthorizeAsync(new ApiAuthParams()
                                    {
                                        AccessToken = channel.VkToken,
                                        ApplicationId = 6321454,
                                        UserId = long.Parse(channel.VkUserId)
                                    });
                                    if (!api.IsAuthorized)
                                        throw new Exception(
                                            $"Vk api client not authorized. YtName = {channel.YtName}, Video Id = {item.Value.Id.VideoId}");
                                    var sVideo = api.Video.Save(new VideoSaveParams()
                                    {
                                        Name = item.Value.Snippet.Title,
                                        Link = $"https://www.youtube.com/watch?v={item.Key.Id.VideoId}",
                                        Wallpost = false,
                                        GroupId = int.Parse(channel.VkGroupId)
                                    });
                                    using (HttpClient client = new HttpClient())
                                    using (var message = new HttpRequestMessage(HttpMethod.Post, sVideo.UploadUrl))
                                    {
                                        var response = await client.SendAsync(message);
                                        if (!response.IsSuccessStatusCode)
                                            throw new Exception(
                                                $"Error loading video {response.StatusCode}: {response.ReasonPhrase}");
                                    }

                                    api.Wall.Post(new WallPostParams()
                                    {
                                        FromGroup = true,
                                        OwnerId = -(int.Parse(channel.VkGroupId)),
                                        Attachments = new List<MediaAttachment>()
                                        {
                                            new Video()
                                            {
                                                OwnerId = -(int.Parse(channel.VkGroupId)),
                                                Id = sVideo.Id,
                                                Player = new Uri($"https://www.youtube.com/watch?v={item.Key.Id.VideoId}"),
                                                Processing = false
                                            }
                                        },
                                        Services = null
                                    });
                                    Logger.Info(
                                        $"{DateTime.UtcNow.AddHours(3).ToShortTimeString()} Video '{item.Value.Snippet.Title}' has been reposted to Vk group of {channel.YtName}");
                                    Thread.Sleep(500);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var mes = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Logger.Warn($"{DateTime.UtcNow.AddHours(3).ToShortTimeString()} {current?.YtName} Repost error: {ex.GetType()} {mes}");
                Logger.Error($"{DateTime.UtcNow.AddHours(3).ToShortTimeString()} {current?.YtName} Repost error: {ex.ToString()}");
            }
        }
    }
}