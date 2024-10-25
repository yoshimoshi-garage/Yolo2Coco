using System.Text.Json.Serialization;

namespace YoshiMoshi.LabelConverter;

public class Category
{
    [JsonPropertyName("id")]
    public int ID { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
}