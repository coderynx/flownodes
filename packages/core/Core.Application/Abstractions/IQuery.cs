using Coderynx.Functional.Options;
using Coderynx.MediatorKit.Abstractions;

namespace Core.Application.Abstractions;

/// <summary>
/// A read-only request that returns a value when one is found.
/// </summary>
/// <typeparam name="TValue">The value returned when found.</typeparam>
public interface IQuery<TValue> : IRequest<Option<TValue>>
    where TValue : class;
