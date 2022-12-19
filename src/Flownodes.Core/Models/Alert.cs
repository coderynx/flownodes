namespace Flownodes.Core.Models;

[GenerateSerializer]
public record Alert([property: Id(0)] string Frn, [property: Id(1)] AlertKind Kind, [property: Id(2)] string Message);