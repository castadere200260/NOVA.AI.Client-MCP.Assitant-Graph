namespace Y10.AI.Client.Abstractions;

public interface IAiClientFactory
{
    IAiClient GetClient(string provider);
}
