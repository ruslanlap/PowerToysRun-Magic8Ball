using System.Text.Json.Serialization;

namespace Community.PowerToys.Run.Plugin.Magic8Ball.Models
{
    /// <summary>
    /// Represents a response from the 8Ball API.
    /// </summary>
    public class EightBallResponse
    {
        /// <summary>
        /// Gets or sets the reading from the 8Ball API.
        /// </summary>
        [JsonPropertyName("reading")]
        public string Reading { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the response (e.g., Positive, Negative, Neutral).
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
}