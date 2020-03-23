using System;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YtToVkReposter.Services.Interfaces;
using YtToVkReposter.Static;

namespace YtToVkReposter.Services
{
    public class YoutubeService : IYoutubeService
    {
        private readonly string _apiKey;

        public YoutubeService(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("Youtube service must have api key");
            }

            _apiKey = apiKey;
        }
        
        public async Task<ChannelListResponse> GetChannelById(string id)
        {
            using (var api = new YouTubeService(new BaseClientService.Initializer() {ApiKey = _apiKey}))
            {
                var request = api.Channels.List(String.Join(',', new [] { Part.ContentDetails, Part.Id }));
                request.Id = id;
                return await request.ExecuteAsync();
            }
        }

        public async Task<ChannelListResponse> GetChannelByName(string name)
        {
            using (var api = new YouTubeService(new BaseClientService.Initializer() {ApiKey = _apiKey}))
            {
                var request = api.Channels.List(String.Join(',', new [] { Part.ContentDetails, Part.Id }));
                request.ForUsername = name;
                return await request.ExecuteAsync();
            }
        }

        public async Task<PlaylistItemListResponse> GetVideoList(string playlistId)
        {
            using (var api = new YouTubeService(new BaseClientService.Initializer() {ApiKey = _apiKey}))
            {
                var request = api.PlaylistItems.List(Part.Snippet);
                request.PlaylistId = playlistId;
                return await request.ExecuteAsync();
            }
        }
    }
}