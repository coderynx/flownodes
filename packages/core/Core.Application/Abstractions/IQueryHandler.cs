using Coderynx.Functional.Options;
using Coderynx.MediatorKit.Abstractions;

namespace Core.Application.Abstractions;

public interface IQueryHandler<in TQuery, TValue> : IRequestHandler<TQuery, Option<TValue>>
    where TQuery : IQuery<TValue>
    where TValue : class;
