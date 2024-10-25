using System.Text.Json.Serialization;

namespace YoshiMoshi.LabelConverter;

public class ImageData
{
    [JsonPropertyName("id")]
    public int ID { get; set; }
    [JsonPropertyName("file_name")]
    public string FileName { get; set; }
    [JsonPropertyName("width")]
    public int Width { get; set; }
    [JsonPropertyName("height")]
    public int Height { get; set; }
    [JsonIgnore]
    public string FullFileName { get; set; }
}
