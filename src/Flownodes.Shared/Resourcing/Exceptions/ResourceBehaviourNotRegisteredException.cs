namespace Flownodes.Shared.Resourcing.Exceptions;

[GenerateSerializer]
public class ResourceBehaviourNotRegisteredException : Exception
{
    public ResourceBehaviourNotRegisteredException(string behaviourId) : base(
        $"The behaviour {behaviourId} is not registered in the container")
    {
        BehaviourId = behaviourId;
    }

    [Id(0)] public string BehaviourId { get; }
}