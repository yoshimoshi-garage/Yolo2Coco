namespace YoshiMoshi.LabelConverter;

public interface ILabelConverter
{
    static LabelFormat InputFormat { get; }
    static LabelFormat OutputFormat { get; }
}
