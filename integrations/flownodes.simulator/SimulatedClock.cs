namespace Flownodes.Simulator;

internal sealed class SimulatedClock(TimeProvider time)
{
    public DateTimeOffset Now => time.GetUtcNow();
}
