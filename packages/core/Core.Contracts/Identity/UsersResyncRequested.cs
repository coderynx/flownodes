namespace Core.Contracts.Identity;

/// <summary>
/// Sent by the Registry and the Engine to have Identity publish Users again as <see cref="UserAccessPublished"/>
/// (ADR-0026): once when the module starts, and when a User its replica has no record of calls it.
/// </summary>
/// <param name="UserId">The id of the one User to publish again, or <see langword="null"/> for every User.</param>
public sealed record UsersResyncRequested(Guid? UserId = null);
