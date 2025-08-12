namespace Avs.Messaging.Core;

/// <summary>
/// Generic message handler
/// </summary>
/// <typeparam name="T">Type of message to handle</typeparam>
public delegate Task MessageHandlerDelegate<T>(MessageContext<T> context);