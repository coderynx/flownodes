namespace Registry.Domain.Resources;

/// <summary>
/// Who made the change a Revision records.
/// </summary>
/// <param name="UserId">The immutable id of the User.</param>
/// <param name="Username">
/// The User's username when the change was made, as the Registry's replica knew it; <see langword="null"/> when
/// Identity had not reported it yet.
/// </param>
public sealed record RevisionAuthor(Guid UserId, string? Username);
