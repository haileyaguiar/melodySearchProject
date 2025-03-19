using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace melodySearchProject.Models
{
    public class Response
    {
        [JsonPropertyName("hits")]
        public Hit[] hits { get; set; }

        [JsonPropertyName("message")]
        public string message { get; set; }

        [JsonPropertyName("success")]
        public bool success { get; set; }
    }

    public class Hit
    {
        [JsonPropertyName("index")]
        public string index { get; set; }

        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("score")]
        public float score { get; set; }

        [JsonPropertyName("highlight")]
        public Dictionary<string, string[]> highlight { get; set; }

        [JsonPropertyName("source")]
        public Source source { get; set; }
    }

    public class Source
    {
        [JsonPropertyName("name")]
        public string name { get; set; }

        [JsonPropertyName("intervals_text")]
        public string intervals_text { get; set; }

        [JsonPropertyName("measure_map")]
        public string measure_map { get; set; }

        [JsonPropertyName("intervals_as_array")]
        public int[] intervals_as_array { get; set; }

        [JsonPropertyName("measure_map_as_array")]
        public int[] measure_map_as_array { get; set; }

        [JsonPropertyName("file_id")]
        public string file_id { get; set; }
    }
}
