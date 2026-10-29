namespace Identity.Domain.Users;

/// <summary>
/// What Identity keeps of a User it deleted, so a resync can tell the other modules about the deletion even if they
/// missed its message.
/// </summary>
public sealed class DeletedUser
{
    private DeletedUser()
    {
    }

    /// <summary>
    /// Gets the immutable id the User had.
    /// </summary>
    public Guid Id { get; private set; }
    /// <summary>
    /// Gets the sequence number of the deletion message, the last published about the User.
    /// </summary>
    public long Seq { get; private set; }

    /// <summary>
    /// Records the deletion of a User.
    /// </summary>
    /// <param name="user">The User being deleted, whose sequence number has advanced to the deletion message's.</param>
    /// <returns>The tombstone of the User.</returns>
    public static DeletedUser Create(User user) =>
        new()
        {
            Id = user.Id,
            Seq = user.Seq
        };
}
