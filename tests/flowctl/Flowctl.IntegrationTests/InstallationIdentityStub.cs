using Engine.Application.Installations;
using Engine.Domain.Installations;

namespace Flowctl.IntegrationTests;

/// <summary>
/// The Installation the test Silo reports. Generating and keeping the identifier is the Engine's own tests' business.
/// </summary>
public sealed class InstallationIdentityStub : IInstallationIdentity
{
    public static readonly Guid Id = Guid.Parse("3f2b8c1e-6a47-4d59-9b0e-7c5d1a2e4f68");
    public Installation Current { get; } = new(Id, "0.1.0");
}
