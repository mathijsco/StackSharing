using Newtonsoft.Json;

namespace StackSharing.Lib.Models.OwnCloud
{
    /// <summary>
	/// OCS API Meta information.
	/// </summary>
	[JsonObject("meta")]
    public class Meta
    {
        /// <summary>
        /// Gets or sets the response status.
        /// </summary>
        /// <value>The status.</value>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the response status code.
        /// </summary>
        /// <value>The status code.</value>
        [JsonProperty("statuscode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the response status message.
        /// </summary>
        /// <value>The message.</value>
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}