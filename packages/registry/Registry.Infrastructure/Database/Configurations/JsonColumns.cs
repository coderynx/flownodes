using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Registry.Infrastructure.Database.Configurations;

internal static class JsonColumns
{
    private static readonly ValueConverter<JsonElement, string> Converter = new(
        element => element.GetRawText(),
        text => JsonDocument.Parse(text, default(JsonDocumentOptions)).RootElement.Clone());

    private static readonly ValueComparer<JsonElement> Comparer = new(
        (first, second) => first.GetRawText() == second.GetRawText(),
        element => element.GetRawText().GetHashCode(),
        element => element.Clone());

    public static PropertyBuilder<JsonElement> IsJsonb(this PropertyBuilder<JsonElement> property) =>
        property.HasConversion(Converter, Comparer).HasColumnType("jsonb");

    public static PropertyBuilder<JsonElement?> IsJsonb(this PropertyBuilder<JsonElement?> property) =>
        property.HasConversion(Converter, Comparer).HasColumnType("jsonb");
}
