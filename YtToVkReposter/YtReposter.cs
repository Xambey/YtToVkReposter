using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using YtToVkReposter.Services;
using YtToVkReposter.Settings;
using YtToVkReposter.Static;
using SearchResult = Google.Apis.YouTube.v3.Data.SearchResult;
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
        private YtToReposterSettings _settings;
        private YoutubeService _youtubeService;
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
                try
                {
                    _settings = JsonConvert.DeserializeObject<YtToReposterSettings>(File.ReadAllText(PathToSettingsConfig));
                    Validator.ValidateObject(_settings, new ValidationContext(_settings));

                    Logger.Info("Settings loaded");
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Warn("Token not found at the settings", ex);
                    return false;
                }
            }

            Logger.Info("Settings configuration not found! Default configuration is creating...(Token is empty)");

            Directory.CreateDirectory(PathToSettingsConfig.Remove(PathToSettingsConfig.LastIndexOf(System.IO.Path.AltDirectorySeparatorChar)));
            File.Create(PathToSettingsConfig);

            Logger.Info("Settings configuration has created");
            return false;
        }
        private void UpdateChannelsInFile()
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

                Logger.Info($"Channels configuration updated in file");
            }
            else
            {
                Logger.Info("WriteChannelToFile ERROR");
                Logger.Error("WriteChannelToFile - Configuration file not found!");
            }
        }
        public async Task AddChannel(string ytNameOrId, string vkToken, string groupId, string userId, bool isYtName = true)
        {
            if (isYtName && _channels.FirstOrDefault(x => x.YtName == ytNameOrId) != null)
                throw  new Exception("Error. Channel already exists");
            // else if (!isYtName && !IsYtChannelIdExist(ytNameOrId))
            //     throw  new Exception("Error. Channel with the id doesn't exist");
            
            var channel = new Channel()
            {
                YtName = isYtName ? ytNameOrId : await GetYtNameByChannelId(ytNameOrId), 
                VkToken = vkToken, 
                VkGroupId = groupId, 
                VkUserId = userId, 
                YtId = isYtName ? GetYtChannelId(ytNameOrId) : ytNameOrId
            };
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
                channel.YtName = await GetYtNameByChannelId(name);
            }
            while (true)
            {
                try
                {
                    VkApi test = new VkApi();
                    test.Authorize(new ApiAuthParams() {
                        AccessToken = channel.VkToken, 
                        ApplicationId = _settings.ApplicationId,
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

            // using (var ytApi = new YouTubeService(new BaseClientService.Initializer() {ApiKey = _ytKey}))
            // {
            //     var searchListRequest = ytApi.Videos.List("snippet");
            //     searchListRequest.ChannelId = channel.YtId;
            //     searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
            //     searchListRequest.Type = "video";
            //     //searchListRequest.MaxResults = 20;
            //     searchListRequest.PublishedAfter = DateTime.UtcNow.Subtract(TimeSpan.FromHours(24));
            //     
            //     channel.VideoStack.PushRange(searchListRequest.Execute().Items.Select(x => x.Id.VideoId)); 
            // }
            var videos = _youtubeService.GetVideoList(channel.PlaylistId);
            
            _channels.Add(channel);

            Logger.Info($"Channel {channel.YtName} has been added");
            
            UpdateChannelsInFile();
        }

        public void RemoveChannel(string name)
        {
            var channel = _channels.FirstOrDefault(x => x.YtName == name);
            if (channel != null)
            {
                _channels.Remove(channel);

                Logger.Info($"Channel {channel.YtName} has been removed");
                UpdateChannelsInFile();
            } 
        }

        private async Task<string> GetYtNameByChannelId(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(_ytKey))
                    throw new Exception("Api key is null or empty");
                var response = await _youtubeService.GetChannelById(id);
                
                if (response.Items.Any())
                {
                    return response.Items[0].Snippet.Title;
                }

                throw new InvalidDataException($"Snippet fo channel with id = {id} not found");
            }
            catch (Exception ex)
            {
                var mes = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                throw new Exception($"Trying youtube channel name by id ({id}) has been failded : {mes}");
            }
        }
        // private bool IsYtChannelIdExist(string id)
        // {
        //     try
        //     {
        //         string key;
        //         do
        //         {
        //             Logger.Info("Enter api key: ");
        //             key = Console.ReadLine();
        //         } while (string.IsNullOrEmpty(key));
        //
        //         using (var service = new YouTubeService(new BaseClientService.Initializer() {ApiKey = key}))
        //         {
        //             var list = service.Channels.List("Id");
        //             list.Id = id;
        //             var request = list.Execute();
        //             if (request.Items.Count == 1)
        //             {
        //                 _ytKey = key;
        //                 return true;
        //             }
        //             return false;
        //         }       
        //     }
        //     catch (Exception ex)
        //     {
        //         var mes = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        //         throw new Exception($"Checking youtube channel id ({id}) has been failded : {mes}");
        //     }
        // }
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
                throw new Exception($"Getting youtube channel groupId ({userName}) has been failed : {mes}");
            }
            return null;
        }

        /// <summary>
        /// Обновляет списки видео в конфигурации на последние (чтобы не постить старое)
        /// </summary>
        private async Task InitChannelsFromFile()
        {
            Logger.Info("Init update channels...");
            //Загружаем каналы
            LoadChannelsFromFile();
            foreach (var channel in _channels)
            {
                if (String.IsNullOrEmpty(channel.PlaylistId))
                {
                    var channelDetails = await _youtubeService.GetChannelById(channel.YtId);
                    channel.PlaylistId = channelDetails.Items[0].ContentDetails.RelatedPlaylists.Uploads;
                }
                if (channel.UpdateOnInit)
                {
                    // var searchListRequest = ytApi.Search.List("snippet");
                    // searchListRequest.ChannelId = channel.YtId;
                    // searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                    // searchListRequest.Type = "video";
                    // //searchListRequest.MaxResults = 20;
                    // searchListRequest.PublishedAfter = DateTime.UtcNow.Subtract(TimeSpan.FromHours(24));
                    //
                    // var items = searchListRequest.Execute().Items;
                    // var results = items.Take(20).Select(sn => sn.Id.VideoId).Reverse().ToList();
                    //
                    // channel.VideoStack.Clear();
                    // channel.VideoStack.PushRange(results);
                    //
                    //
                    var list = await _youtubeService.GetVideoList(channel.PlaylistId);
                    channel.VideoStack.Clear();
                    channel.VideoStack.PushRange(list.Items.Select(x => x.Snippet.ResourceId.VideoId));
                    Thread.Sleep(300);
                }
            }
            
            UpdateChannelsInFile();
        }

        public async void Start()
        {
            try
            {
                if (_timer == null)
                {
                    Logger.Info("Server has been started");

                    Logger.Info("Enter Api key (30 seconds waiting): ");

                    if (LoadSettingsFromFile())
                    {
                        _youtubeService = new YoutubeService(_settings.Token);

                        await InitChannelsFromFile();

                        _timer = new Timer(HandlerRepostMessage, null, _settings.Timeout,
                            _settings.Timeout);
                        Logger.Info("Repost handler started!");
                    }
                    else
                    {
                        throw new Exception(
                            "Fatal Error. Settings (/Resources/settings.json) not found! You should add settings.json and restart service!");
                    }

                }
            } 
            catch (Exception ex)
            {
                var mes = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Logger.Warn($"{DateTime.UtcNow.AddHours(3).ToShortTimeString()} Repost error: {ex.GetType()} {mes}");
                Logger.Error($"{DateTime.UtcNow.AddHours(3).ToShortTimeString()} Repost error: {ex.ToString()}");
                Logger.Info($"{DateTime.UtcNow.AddHours(3).ToShortTimeString()} Repost error {ex.GetType()} {mes}");
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

                Logger.Info("Server has been shutdown!");
            }        
        }
        private async void HandlerRepostMessage(object state)
        {
            Channel current = null;
            try
            {
                foreach (var channel in _channels)
                {
                    current = channel;
                    var response = await _youtubeService.GetVideoList(channel.PlaylistId);
                        
                    if (response.Items.Any())
                    {
                        //Список первых 5 публикации ожидаемых к публикации, которые не содержатся в текущем стеке видео
                        var lastVideos = response.Items
                            .Where(p => !channel.VideoStack.Contains(p.Snippet.ResourceId.VideoId))
                            .Select(x => x.Snippet)
                            .ToList();

                        if (lastVideos.Any())
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

                            //Список видео, которые уже публиковались в этой группе от других каналов и есть в lastVideos
                            var channelVideosFromSameGroupVk = _channels
                                .Where(ch => ch.VkGroupId == channel.VkGroupId && ch != channel && ch.VideoStack != null)
                                .SelectMany(ch => ch.VideoStack.Select(x => x))
                                .Intersect(lastVideos.Select(x => x.ResourceId.VideoId)) //дубликаты
                                .ToList();
                            //Список видео id для публикации
                            var newVideos = lastVideos.Select(x => x.ResourceId.VideoId).Except(channelVideosFromSameGroupVk).ToList();

//                                //Добавляем новые видео в стек video id
//                                channel.VideoStack.PushRange(newVideos);    

                            var info = newVideos
                                .Select(x => lastVideos.First(s => s.ResourceId.VideoId == x)).ToList();
                                
                            foreach (var item in info)
                            {
                                VkApi api = new VkApi();

                                Thread.Sleep(3000);
                                var sVideo = await UploadVideoToVk(api, channel, item);
                                Thread.Sleep(1000);


                                if (sVideo != null && sVideo.Player.Host.Contains("youtube"))
                                {
                                    api.Wall.Post(new WallPostParams()
                                    {
                                        FromGroup = true,
                                        OwnerId = -(int.Parse(channel.VkGroupId)),
                                        Message = item.Title,
                                        Attachments = new List<MediaAttachment>()
                                        {
                                            new Video()
                                            {
                                                OwnerId = -(int.Parse(channel.VkGroupId)),
                                                Id = sVideo?.Id
                                            }
                                        }
                                    });
                                    channel.VideoStack.Push(item.ResourceId.VideoId);    
                                    Logger.Info(
                                        $"{DateTime.UtcNow.AddHours(3).ToShortTimeString()} Video '{item.Title}'(VkId={sVideo?.Id} YtId={sVideo?.Player}) has been reposted to Vk group of {channel.YtName}");
                                    //Обновляем конфигурацию канала (videoId stack) в channels.json
                                    UpdateChannelsInFile();
                                }
                                else
                                {
                                    Logger.Error($"{DateTime.UtcNow.AddHours(3).ToShortTimeString()} ERROR. Video '{item.Title}'(VkId={sVideo?.Id} YtId={sVideo?.Player}) not uploaded to Vk group of {channel.YtName}");
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
                Logger.Info($"{DateTime.UtcNow.AddHours(3).ToShortTimeString()} {current?.YtName} Repost error {ex.GetType()} {mes}");
            }
        }

        private async Task<Video> UploadVideoToVk(VkApi api, Channel channel, PlaylistItemSnippet item)
        {
            await api.AuthorizeAsync(new ApiAuthParams()
            {
                AccessToken = channel.VkToken,
                ApplicationId = _settings.ApplicationId,
                UserId = long.Parse(channel.VkUserId)
            });
            if (!api.IsAuthorized)
                throw new Exception(
                    $"Vk api client not authorized. YtName = {channel.YtName}, Video Id = {item.ResourceId.VideoId}");
            var sVideo = api.Video.Save(new VideoSaveParams()
            {
                Name = item.Title,
                Link = $"https://www.youtube.com/watch?v={item.ResourceId.VideoId}",
                Wallpost = false,
                GroupId = int.Parse(channel.VkGroupId)
            });
            using (HttpClient client = new HttpClient())
            using (var message = new HttpRequestMessage(HttpMethod.Get, sVideo.UploadUrl))
            {
                var response = await client.SendAsync(message);
                if (!response.IsSuccessStatusCode)
                    throw new Exception(
                        $"Error loading video {response.StatusCode}: {response.ReasonPhrase}: Yt id={item.ResourceId.VideoId} Vk id={sVideo.Id}");
                Logger.Info($"Video uploaded to vk. Yt id={item.ResourceId.VideoId} Vk id={sVideo.Id} Status: {response.StatusCode} Reason: {response.ReasonPhrase}");
            }

            Thread.Sleep(300);
            
            var uploadedVideo = api.Video.Get(new VideoGetParams
            {
                Videos = new[]
                {
                    new Video()
                    {
                        OwnerId = -long.Parse(channel.VkGroupId),
                        Id = sVideo.Id
                    }
                },
                OwnerId = -long.Parse(channel.VkGroupId),
                Extended = true
            }).FirstOrDefault();
            return uploadedVideo;
        }
    }
}