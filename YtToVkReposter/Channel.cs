using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace YtToVkReposter
{
    public class Channel
    {
        public string YtName { get; set; }
        public string VkToken { get; set; }
        public string YtId { get; set; }
        public string VkGroupId { get; set; }
        public string VkUserId { get; set; }

        public bool UpdateOnInit { get; set; } = true;
        public LimitedStack<string> VideoStack { get; } = new LimitedStack<string>(20);
    }
}