using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal interface IRevisionLog
{
    Task RecordAsync(Revision revision, CancellationToken cancellationToken);
}
