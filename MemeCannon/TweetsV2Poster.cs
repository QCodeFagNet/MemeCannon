using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using Newtonsoft.Json;
using Tweetinvi;
using Tweetinvi.Core.Web;
using Tweetinvi.Models;
using Tweetinvi.Models.DTO;

namespace MemeCannon
{
    //https://www.thatsoftwaredude.com/content/6289/how-to-post-a-tweet-using-c-for-single-user
    //https://github.com/linvi/tweetinvi/issues/1147
    /// <summary>A way to still use TweetInvi to post tweets</summary>
    public class TweetsV2Poster
    {
        // ----------------- Fields ----------------

        private readonly ITwitterClient client;

        // ----------------- Constructor ----------------

        public TweetsV2Poster(ITwitterClient client)
        {
            this.client = client;
        }

        public Task<ITwitterResult> PostTweet(TweetV2PostRequest tweetParams)
        {
            return this.client.Execute.AdvanceRequestAsync(
                (ITwitterRequest request) =>
                {
                    var jsonBody = this.client.Json.Serialize(tweetParams);

                    // Technically this implements IDisposable,
                    // but if we wrap this in a using statement,
                    // we get ObjectDisposedExceptions,
                    // even if we create this in the scope of PostTweet.
                    //
                    // However, it *looks* like this is fine.  It looks
                    // like Microsoft's HTTP stuff will call
                    // dispose on requests for us (responses may be another story).
                    // See also: https://stackoverflow.com/questions/69029065/does-stringcontent-get-disposed-with-httpresponsemessage
                    var content = new System.Net.Http.StringContent(jsonBody, Encoding.UTF8, "application/json");

                    request.Query.Url = "https://api.twitter.com/2/tweets";
                    request.Query.HttpMethod = Tweetinvi.Models.HttpMethod.POST;
                    request.Query.HttpContent = content;
                }
            );
        }
    }

    /// <summary>
    /// There are a lot more fields according to:
    /// https://developer.twitter.com/en/docs/twitter-api/tweets/manage-tweets/api-reference/post-tweets
    /// but these are the ones we care about for our use case.
    /// </summary>
    public class TweetV2PostRequest
    {
        [JsonProperty("media")]
        public TweetV2Media Media { get; set; }
        /// <summary>
        /// The text of the tweet to post.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class TweetV2Media
    {
        //[JsonProperty("expires_after_secs")]
        //public int ExpiresAfterSecs { get; set; }
        //[JsonProperty("image")]
        //public TweetV2Image Image { get; set; } = null;
        [JsonProperty("media_ids")]
        public string[] MediaId { get; set; }
        //[JsonProperty("media_key")]
        //public string MediaKey { get; set; }
        //[JsonProperty("media_id_string")]
        //public string MediaIdString { get; set; }
        //[JsonProperty("size")]
        //public int Size { get; set; }
    }

    public class TweetV2Image
    {
        [JsonProperty("h")]
        public int Height { get; set; }
        [JsonProperty("image_type")]
        public string ImageType { get; set; }
        [JsonProperty("w")]
        public int Width { get; set; }
    }
}