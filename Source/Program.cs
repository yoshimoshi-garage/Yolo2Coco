using YoshiMoshi.LabelConverter;

internal class Program
{
    private static void Main(string[] args)
    {
        var converter = new YoloToCocoConverter();

        var d = new DirectoryInfo(@"C:\repos\yoshimoshi\carcam\labelled-training-data");
        var dataset = converter.CreateDatasetFromSource(d);
        converter.GenerateSingleOutputDirectoryStructure(dataset, d);
    }
}
