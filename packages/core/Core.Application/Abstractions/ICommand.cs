using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;

namespace Core.Application.Abstractions;

/// <summary>
/// A request that changes state and reports only success or failure.
/// </summary>
public interface ICommand : IRequest<Result>;

/// <summary>
/// A request that changes state and returns a value on success.
/// </summary>
/// <typeparam name="TValue">The value returned on success.</typeparam>
public interface ICommand<TValue> : IRequest<Result<TValue>>;
