using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YoshiMoshi.LabelConverter;

public enum LabelFormat
{
    Yolo5,
    Coco
}

public interface ILabelConverter
{
    static LabelFormat InputFormat { get; }
    static LabelFormat OutputFormat { get; }
}

public class YoloToCocoConverter : ILabelConverter
{
    public static LabelFormat InputFormat => LabelFormat.Yolo5;
    public static LabelFormat OutputFormat => LabelFormat.Coco;

    public YoloToCocoConverter()
    {
    }

    private (int Width, int Height) GetImageDimensions(FileInfo imageFile)
    {
        using var image = Image.FromFile(imageFile.FullName);
        return (image.Width, image.Height);
    }

    public void GenerateSingleOutputDirectoryStructure(CocoDataset dataset, DirectoryInfo rootOutputFolder)
    {
        var root = new DirectoryInfo(Path.Combine(rootOutputFolder.FullName, "coco_data"));
        if (!root.Exists) root.Create();

        foreach (var image in dataset.Images)
        {
            Interop.CreateFileHardLink(
                Path.Combine(root.FullName, image.FileName),
                image.FullFileName);

        }

        dataset.WriteToJsonFile(Path.Combine(root.FullName, "coco_train.json"));
    }

    public void GenerateOutputDirectoryStructureWithSubfolders(CocoDataset dataset, DirectoryInfo rootOutputFolder)
    {
        var trainingPercentage = 0.7;
        var validationPercentage = 0.2;

        // create the coco structure
        var root = new DirectoryInfo(Path.Combine(rootOutputFolder.FullName, "coco"));
        var annotationsFolder = new DirectoryInfo(Path.Combine(root.FullName, "annotations"));
        var imagesRoot = new DirectoryInfo(Path.Combine(root.FullName, "images"));
        var trainingImageFolder = new DirectoryInfo(Path.Combine(imagesRoot.FullName, "train"));
        var validationImageFolder = new DirectoryInfo(Path.Combine(imagesRoot.FullName, "val"));
        var testImageFolder = new DirectoryInfo(Path.Combine(imagesRoot.FullName, "test"));

        if (!root.Exists) root.Create();
        if (!annotationsFolder.Exists) annotationsFolder.Create();
        if (!imagesRoot.Exists) imagesRoot.Create();
        if (!trainingImageFolder.Exists) trainingImageFolder.Create();
        if (!validationImageFolder.Exists) validationImageFolder.Create();
        if (!testImageFolder.Exists) testImageFolder.Create();

        // TODO: making these hard links would be *WAY* better

        var trainDataset = new CocoDataset();
        var validationDataset = new CocoDataset();
        var testDataset = new CocoDataset();

        foreach (var category in dataset.Categories)
        {
            var annotations = dataset.Annotations.Where(a => a.CategoryID == category.ID).ToArray();
            var annotationImageIDs = annotations.Select(c => c.ImageID).ToHashSet();
            var images = dataset.Images.Where(i => annotationImageIDs.Contains(i.ID)).ToArray();

            var firstValidationIndex = (int)(images.Length * trainingPercentage);
            var firstTestIndex = firstValidationIndex + (int)(images.Length * validationPercentage);

            for (var index = 0; index < images.Length; index++)
            {
                if (index < firstValidationIndex)
                {
                    if (!trainDataset.Categories.Contains(category))
                    {
                        trainDataset.Categories.Add(category);
                    }
                    trainDataset.Images.Add(images[index]);
                    trainDataset.Annotations.AddRange(annotations.Where(a => a.ImageID == images[index].ID));

                    Interop.CreateFileHardLink(
                        Path.Combine(trainingImageFolder.FullName, images[index].FileName),
                        images[index].FullFileName);

                    Console.WriteLine($"Linking image {images[index].FileName} to {trainingImageFolder.Name}");
                }
                else if (index < firstTestIndex)
                {
                    if (!validationDataset.Categories.Contains(category))
                    {
                        validationDataset.Categories.Add(category);
                    }
                    validationDataset.Images.Add(images[index]);
                    validationDataset.Annotations.AddRange(annotations.Where(a => a.ImageID == images[index].ID));

                    Interop.CreateFileHardLink(
                        Path.Combine(validationImageFolder.FullName, images[index].FileName),
                        images[index].FullFileName);

                    Console.WriteLine($"Linking image {images[index].FileName} to {validationImageFolder.Name}");
                }
                else
                {
                    if (!testDataset.Categories.Contains(category))
                    {
                        testDataset.Categories.Add(category);
                    }
                    testDataset.Images.Add(images[index]);
                    testDataset.Annotations.AddRange(annotations.Where(a => a.ImageID == images[index].ID));

                    Interop.CreateFileHardLink(
                        Path.Combine(testImageFolder.FullName, images[index].FileName),
                        images[index].FullFileName);

                    Console.WriteLine($"Copy image {images[index].FileName} to {testImageFolder.Name}");
                }
            }
        }

        trainDataset.WriteToJsonFile(Path.Combine(annotationsFolder.FullName, "instances_train.json"));
        validationDataset.WriteToJsonFile(Path.Combine(annotationsFolder.FullName, "instances_val.json"));
        testDataset.WriteToJsonFile(Path.Combine(annotationsFolder.FullName, "instances_test.json"));
    }

    public CocoDataset CreateDatasetFromSource(DirectoryInfo rootInputFolder)
    {
        var cocoDataset = new CocoDataset();
        var categoryID = 0;
        var imageID = 1;
        var annotationID = 1;

        // get all input folder names - these will be categories
        foreach (var di in rootInputFolder.EnumerateDirectories())
        {
            if (di.Name.StartsWith("coco")) continue;

            Console.WriteLine($"{di.Name}");

            cocoDataset.Categories.Add(
                new Category
                {
                    ID = categoryID,
                    Name = di.Name
                });

            // check each label file
            foreach (var labelFile in di.EnumerateFiles("*.txt"))
            {
                // does it have an associated image?
                var imageFile = new FileInfo(
                    Path.Combine(labelFile.DirectoryName,
                    $"{Path.GetFileNameWithoutExtension(labelFile.Name)}.png"));
                var dimensions = GetImageDimensions(imageFile);
                if (imageFile.Exists)
                {
                    Console.WriteLine($"  {imageFile.Name}");

                    // open the image and get its dimensions

                    var imageData = new ImageData
                    {
                        ID = imageID,
                        FileName = imageFile.Name,
                        Width = dimensions.Width,
                        Height = dimensions.Height,
                        FullFileName = imageFile.FullName
                    };

                    // read the yolo labels
                    var yoloLabels = File.ReadAllLines(labelFile.FullName);

                    if (yoloLabels.Any())
                    {
                        // only parse if labels exist
                        foreach (var label in yoloLabels)
                        {
                            var parts = label.Split(' ');
                            int classId = int.Parse(parts[0]);
                            double xCenter = double.Parse(parts[1]);
                            double yCenter = double.Parse(parts[2]);
                            double width = double.Parse(parts[3]);
                            double height = double.Parse(parts[4]);

                            // Denormalize to pixel values
                            double xMin = (xCenter - (width / 2)) * dimensions.Width;
                            double yMin = (yCenter - (height / 2)) * dimensions.Height;
                            double absWidth = width * dimensions.Width;
                            double absHeight = height * dimensions.Height;

                            // Create COCO annotation
                            var annotation = new AnnotationData
                            {
                                ID = annotationID++,
                                ImageID = imageID,
                                CategoryID = categoryID,
                                BoundingBox = new List<double> { xMin, yMin, absWidth, absHeight }
                            };

                            cocoDataset.Annotations.Add(annotation);
                        }

                        imageID++;

                        cocoDataset.Images.Add(imageData);
                    }

                }
            }

            categoryID++;
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        Console.WriteLine("Processed");
        Console.WriteLine($"  {cocoDataset.Categories.Count} categories");
        Console.WriteLine($"  {cocoDataset.Annotations.Count} annotations");
        Console.WriteLine($"  {cocoDataset.Images.Count} images");

        return cocoDataset;
    }
}

internal static class Interop
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    public static bool CreateFileHardLink(string linkFileName, string existingFileName)
    {
        return CreateHardLink(linkFileName, existingFileName, IntPtr.Zero);
    }
}

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

public class Category
{
    [JsonPropertyName("id")]
    public int ID { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
}