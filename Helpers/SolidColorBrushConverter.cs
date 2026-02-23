using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace zxeltor.StoCombat.Realtime.Helpers;

public class SolidColorBrushConverter : JsonConverter<SolidColorBrush>
{
    public override SolidColorBrush Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hexValue = reader.GetString();
        if (string.IsNullOrWhiteSpace(hexValue))
        {
            return Brushes.Beige; // Or any default color
        }
        // Use BrushConverter to create a SolidColorBrush from the hex string
        return (SolidColorBrush)(new BrushConverter().ConvertFrom(hexValue) ?? Brushes.Beige);
    }

    public override void Write(Utf8JsonWriter writer, SolidColorBrush value, JsonSerializerOptions options)
    {
        // Write the brush's color as a hex string (e.g., "#RRGGBB" or "#AARRGGBB")
        writer.WriteStringValue(value.Color.ToString());
    }
}

