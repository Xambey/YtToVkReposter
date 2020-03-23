using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;

namespace YtToVkReposter.Services.Interfaces
{
    public interface IYoutubeService
    {
        /// <summary>
        /// Получить информацию о канале по YouTube channel id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ChannelListResponse> GetChannelById(string id);
        /// <summary>
        /// Получить информацию о канале по имени
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<ChannelListResponse> GetChannelByName(string name);
        /// <summary>
        /// Получить список видео плейлиста (канала)
        /// </summary>
        /// <param name="playlistId"></param>
        /// <returns></returns>
        Task<PlaylistItemListResponse> GetVideoList(string playlistId);
    }
}