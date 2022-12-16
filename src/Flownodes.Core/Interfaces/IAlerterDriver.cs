using Flownodes.Core.Models;

namespace Flownodes.Core.Interfaces;

public interface IAlerterDriver
{
    Task SendAlertAsync(Alert alert);
}