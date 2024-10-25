using System.Text.Json;
using System.Text.Json.Serialization;

namespace YoshiMoshi.LabelConverter;

public class CocoDataset
{
    [JsonPropertyName("images")]
    public List<ImageData> Images { get; set; } = new List<ImageData>();
    [JsonPropertyName("annotations")]
    public List<AnnotationData> Annotations { get; set; } = new List<AnnotationData>();
    [JsonPropertyName("categories")]
    public List<Category> Categories { get; set; } = new List<Category>();

    public void WriteToJsonFile(string cocoOutputPath)
    {
        using (FileStream fs = new FileStream(cocoOutputPath, FileMode.Create, FileAccess.Write))
        using (Utf8JsonWriter writer = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject(); // Start of the JSON root object

            // Write the images array
            writer.WritePropertyName("images");
            writer.WriteStartArray();
            foreach (var image in Images)
            {
                writer.WriteStartObject();
                writer.WriteNumber("id", image.ID);
                writer.WriteString("file_name", image.FileName);
                writer.WriteNumber("width", image.Width);
                writer.WriteNumber("height", image.Height);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            // Write the annotations array
            writer.WritePropertyName("annotations");
            writer.WriteStartArray();
            foreach (var annotation in Annotations)
            {
                writer.WriteStartObject();
                writer.WriteNumber("id", annotation.ID);
                writer.WriteNumber("image_id", annotation.ImageID);
                writer.WriteNumber("category_id", annotation.CategoryID);
                writer.WriteStartArray("bbox");
                foreach (var value in annotation.BoundingBox)
                {
                    writer.WriteNumberValue(value);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            // Write the categories array
            writer.WritePropertyName("categories");
            writer.WriteStartArray();
            foreach (var category in Categories)
            {
                writer.WriteStartObject();
                writer.WriteNumber("id", category.ID);
                writer.WriteString("name", category.Name);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject(); // End of the JSON root object
        }

    }
}
