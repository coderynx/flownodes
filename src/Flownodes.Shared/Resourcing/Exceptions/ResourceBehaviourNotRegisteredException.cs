namespace Flownodes.Shared.Resourcing.Exceptions;

public class ResourceBehaviourNotRegisteredException : Exception
{
    public ResourceBehaviourNotRegisteredException(string behaviourId) : base(
        $"The behaviour {behaviourId} is not registered in the container")
    {
        BehaviourId = behaviourId;
    }

    public string BehaviourId { get; }
}