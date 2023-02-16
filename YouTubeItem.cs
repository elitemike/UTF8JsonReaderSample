using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JsonReader
{
    public class YouTubeItem
    {
        [JsonPropertyName("etag")]
        public string ETag { get; set; }
        [JsonPropertyName("kind")]
        public string Kind { get; set; }
        [JsonPropertyName("id")]
        public YouTubeItemId Id { get; set; }
    }

    public class YouTubeItemId
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; }
        [JsonPropertyName("channelId")]
        public string ChannelId { get; set; }
        [JsonPropertyName("videoId")]
        public string VideoId { get; set; }
    }
}
