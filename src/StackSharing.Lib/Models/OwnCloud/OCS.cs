using Newtonsoft.Json;

namespace StackSharing.Lib.Models.OwnCloud
{
    /// <summary>
	/// OCS API Response.
	/// </summary>
	[JsonObject("ocs")]
    public class OCS
    {
        /// <summary>
        /// Gets or sets the meta information.
        /// </summary>
        /// <value>The meta.</value>
        [JsonProperty("meta")]
        public Meta Meta { get; set; }

        /// <summary>
        /// Gets or sets the data payload.
        /// </summary>
        /// <value>The data.</value>
        [JsonProperty("data")]
        public object Data { get; set; }
    }
}