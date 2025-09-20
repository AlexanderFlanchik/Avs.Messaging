namespace Avs.Messaging.Mediator;

public interface IRequest { }

public interface IRequest<out TResponse> : IRequest { }