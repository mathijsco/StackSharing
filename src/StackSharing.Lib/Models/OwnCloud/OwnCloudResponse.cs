using Newtonsoft.Json;

namespace StackSharing.Lib.Models.OwnCloud
{
    public class OwnCloudResponse
    {
        /// <summary>
        /// Gets or sets the OwnCloud.
        /// </summary>
        /// <value>
        /// The ocs.
        /// </value>
        [JsonProperty("ocs")]
        public OCS OCS { get; set; }
    }
}
