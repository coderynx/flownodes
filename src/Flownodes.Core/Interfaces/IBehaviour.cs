using Flownodes.Core.Models;

namespace Flownodes.Core.Interfaces;

public interface IBehaviour
{
    Task OnSetupAsync(ResourceContext context);
}