namespace Fpl.Client.Clients;

internal class FplApiException : Exception
{
    public FplApiException(string message) : base(message)
    {
    }
}
