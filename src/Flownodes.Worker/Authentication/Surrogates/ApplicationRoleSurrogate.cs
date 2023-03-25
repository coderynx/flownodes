using Flownodes.Shared.Authentication.Models;

namespace Flownodes.Worker.Authentication.Surrogates;

[GenerateSerializer]
internal struct ApplicationRoleSurrogate
{
    public ApplicationRoleSurrogate()
    {
    }

    [Id(0)] public string Id { get; init; } = default!;
    [Id(1)] public string? Name { get; init; }
    [Id(2)] public string? NormalizedName { get; init; }
    [Id(3)] public string? ConcurrencyStamp { get; init; }
}

[RegisterConverter]
internal sealed class ApplicationRoleSurrogateConverter : IConverter<ApplicationRole, ApplicationRoleSurrogate>
{
    public ApplicationRole ConvertFromSurrogate(in ApplicationRoleSurrogate surrogate)
    {
        return new ApplicationRole
        {
            Id = surrogate.Id,
            Name = surrogate.Name,
            NormalizedName = surrogate.NormalizedName,
            ConcurrencyStamp = surrogate.ConcurrencyStamp
        };
    }

    public ApplicationRoleSurrogate ConvertToSurrogate(in ApplicationRole value)
    {
        return new ApplicationRoleSurrogate
        {
            Id = value.Id,
            Name = value.Name,
            NormalizedName = value.NormalizedName,
            ConcurrencyStamp = value.ConcurrencyStamp
        };
    }
}