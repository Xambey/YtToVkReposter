using System;
using System.ComponentModel.DataAnnotations;

namespace YtToVkReposter.Settings
{
    public class YtToReposterSettings
    {
        /// <summary>
        /// Токен апи Youtube
        /// </summary>
        [Required]
        public string Token { get; set; }
        /// <summary>
        /// Id приложения в ВК
        /// </summary>
        [Required]
        public ulong ApplicationId { get; set; }        
        /// <summary>
        /// Таймаут обновления данных
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    }
}