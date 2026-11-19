namespace Rackroom.Application.Abstractions;

public interface ICurrentGymContext
{
    Guid? GymId { get; }
}
