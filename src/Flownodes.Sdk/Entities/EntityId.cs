namespace Flownodes.Sdk.Entities;

/// <summary>
///     Enumeration of all Flownodes entities.
/// </summary>
public enum Entity
{
    UserManager,
    RoleClaimManager,
    ApiKeyManager,
    TenantManager,
    Tenant,
    AlertManager,
    Alert,
    EventBook,
    ResourceManager,
    ResourceGroup,
    Device,
    DeviceZone,
    DataSource,
    Asset,
    Script,
    Other
}

/// <summary>
///     The EntityID is used to represent an entity in the Flownodes cluster.
/// </summary>
public readonly struct EntityId
{
    private static readonly Dictionary<string, Entity> StringToFlownodesEntity = new()
    {
        { EntityNames.UserManager, Entity.UserManager },
        { EntityNames.RoleClaimManager, Entity.RoleClaimManager },
        { EntityNames.ApiKeyManager, Entity.ApiKeyManager },
        { EntityNames.TenantManager, Entity.TenantManager },
        { EntityNames.Tenant, Entity.Tenant },
        { EntityNames.AlertManager, Entity.AlertManager },
        { EntityNames.Alert, Entity.Alert },
        { EntityNames.EventBook, Entity.EventBook },
        { EntityNames.ResourceManager, Entity.ResourceManager },
        { EntityNames.ResourceGroup, Entity.ResourceGroup },
        { EntityNames.Device, Entity.Device },
        { EntityNames.DeviceZone, Entity.DeviceZone },
        { EntityNames.DataSource, Entity.DataSource },
        { EntityNames.Asset, Entity.Asset },
        { EntityNames.Script, Entity.Script },
        { EntityNames.Other, Entity.Other }
    };

    private static readonly Dictionary<Entity, string> FlownodesEntityToString = new()
    {
        { Entity.UserManager, EntityNames.UserManager },
        { Entity.RoleClaimManager, EntityNames.RoleClaimManager },
        { Entity.ApiKeyManager, EntityNames.ApiKeyManager },
        { Entity.TenantManager, EntityNames.TenantManager },
        { Entity.Tenant, EntityNames.Tenant },
        { Entity.AlertManager, EntityNames.AlertManager },
        { Entity.Alert, EntityNames.Alert },
        { Entity.EventBook, EntityNames.EventBook },
        { Entity.ResourceManager, EntityNames.ResourceManager },
        { Entity.ResourceGroup, EntityNames.ResourceGroup },
        { Entity.Device, EntityNames.Device },
        { Entity.DeviceZone, EntityNames.DeviceZone },
        { Entity.DataSource, EntityNames.DataSource },
        { Entity.Asset, EntityNames.Asset },
        { Entity.Script, EntityNames.Script },
        { Entity.Other, EntityNames.Other }
    };

    /// <summary>
    ///     Creates an EntityID from a string entityKind.
    /// </summary>
    /// <param name="entityKind">The entity kind string to create the ID from.</param>
    /// <param name="firstName">The first part of the ID.</param>
    /// <param name="secondName">The second part of the ID.</param>
    public EntityId(string entityKind, string firstName, string? secondName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(entityKind);
        ArgumentException.ThrowIfNullOrEmpty(firstName);

        EntityKind = KindFromString(entityKind);
        if (IsShort && secondName is not null) throw new ArgumentException($"Passed invalid {nameof(entityKind)}");

        FirstName = firstName;
        SecondName = secondName;
    }

    /// <summary>
    ///     Creates an EntityID from an enum EntityKind.
    /// </summary>
    /// <param name="entityKind">The entityKind enum to create the ID from.</param>
    /// <param name="firstName">The first part of the ID.</param>
    /// <param name="secondName">Te secondo part of the ID.</param>
    public EntityId(Entity entityKind, string firstName, string? secondName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(firstName);

        EntityKind = entityKind;
        if (IsShort && secondName is not null) throw new ArgumentException($"Passed invalid {nameof(entityKind)}");

        FirstName = firstName;
        SecondName = secondName;
    }

    /// <summary>
    ///     Creates an EntityID from a string representation.
    /// </summary>
    /// <param name="id">The string to create the EntityID from.</param>
    public EntityId(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        if (id.Contains('/'))
        {
            var tokens = id.Split('/');
            FirstName = tokens[0];

            var secondName = tokens[1].Split(':');
            EntityKind = KindFromString(secondName[0]);
            if (IsShort) throw new ArgumentException($"Passed invalid {nameof(id)}");

            SecondName = secondName[1];

            return;
        }

        if (!id.Contains(':')) throw new ArgumentException("Passed invalid FlownodesId string");

        var firstName = id.Split(':');
        EntityKind = KindFromString(firstName[0]);
        if (!IsShort) throw new ArgumentException($"Passed invalid {nameof(id)}");

        FirstName = firstName[1];
    }

    /// <summary>
    ///     The string representation of the EntityID.
    /// </summary>
    public string IdString => ToString();

    /// <summary>
    ///     If the EntityID refers to a manager.
    /// </summary>
    // TODO: Review naming.
    public bool IsManager => EntityKind
        is Entity.TenantManager
        or Entity.AlertManager
        or Entity.ResourceManager
        or Entity.EventBook;

    private bool IsShort => IsManager || EntityKind == Entity.Tenant;

    /// <summary>
    ///     The entity to which the EntityID refers to.
    /// </summary>
    public Entity EntityKind { get; }

    /// <summary>
    ///     The first part of the EntityID.
    /// </summary>
    public string FirstName { get; }

    /// <summary>
    ///     The second part of the EntityID.
    /// </summary>
    public string? SecondName { get; }

    /// <summary>
    ///     Gets the string representation of the EntityID.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString()
    {
        return !IsShort
            ? $"{FirstName}/{KindToString(EntityKind)}:{SecondName}"
            : $"{KindToString(EntityKind)}:{FirstName}";
    }

    public static implicit operator string(EntityId id)
    {
        return id.ToString();
    }

    public static explicit operator EntityId(string id)
    {
        return new EntityId(id);
    }

    internal static Entity KindFromString(string kind)
    {
        if (StringToFlownodesEntity.TryGetValue(kind, out var result)) return result;

        throw new ArgumentException($"Provided invalid {nameof(kind)}");
    }

    internal static string KindToString(Entity kind)
    {
        if (FlownodesEntityToString.TryGetValue(kind, out var result)) return result;

        throw new ArgumentException($"Provided invalid {nameof(kind)}");
    }

    public string ToEntityKindString()
    {
        return KindToString(EntityKind);
    }
}