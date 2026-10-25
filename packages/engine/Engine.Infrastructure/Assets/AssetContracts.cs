namespace Engine.Infrastructure.Assets;

[GenerateSerializer]
[Immutable]
internal sealed record AssetTypeInfo([property: Id(0)] bool Exists, [property: Id(1)] IReadOnlyList<string> DataPoints);
