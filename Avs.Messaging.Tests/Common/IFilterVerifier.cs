namespace Avs.Messaging.Tests.Common;

public interface IFilterVerifier
{
    void VerifyBeforeAction(string input);
    void VerifyAfterAction(string input);
}