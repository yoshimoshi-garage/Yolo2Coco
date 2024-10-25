using System.Text.Json.Serialization;

namespace YoshiMoshi.LabelConverter;

public class AnnotationData
{
    [JsonPropertyName("id")]
    public int ID { get; set; }
    [JsonPropertyName("image_id")]
    public int ImageID { get; set; }
    [JsonPropertyName("category_id")]
    public int CategoryID { get; set; }
    [JsonPropertyName("bbox")]
    public List<double> BoundingBox { get; set; }
}
