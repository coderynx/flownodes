using Engine.Domain.Integrations;
using Xunit;

namespace Engine.UnitTests.Domain;

public sealed class IntegrationIdTests
{
    [Theory]
    [InlineData("flownodes.mqtt")]
    [InlineData("acme-corp.modbus-tcp")]
    [InlineData("acme.hue.bridge")]
    public void TryCreate_ShouldAccept_WhenTheIdIsVendorQualified(string raw)
    {
        // Act
        var valid = IntegrationId.TryCreate(raw, out var id);

        // Assert
        valid.ShouldBeTrue();
        id!.Value.ShouldBe(raw);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("mqtt")]
    [InlineData("Flownodes.Mqtt")]
    [InlineData("flownodes.")]
    [InlineData("flownodes..mqtt")]
    [InlineData("-flownodes.mqtt")]
    [InlineData("flownodes.mqtt-")]
    public void TryCreate_ShouldRefuse_WhenTheIdIsNotVendorQualified(string? raw)
    {
        // Act
        var valid = IntegrationId.TryCreate(raw, out var id);

        // Assert
        valid.ShouldBeFalse();
        id.ShouldBeNull();
    }
}
