namespace Flownodes.Shared.Exceptions;

public class BehaviourNotRegisteredException : Exception
{
    public BehaviourNotRegisteredException(string behaviourId) : base(
        $"The behaviour {behaviourId} is not registered in the container")
    {
        BehaviourId = behaviourId;
    }

    public string BehaviourId { get; }
}