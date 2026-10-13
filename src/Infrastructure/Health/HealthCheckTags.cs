namespace Infrastructure.Health;

public static class HealthCheckTags
{
    /// <summary>Checks que indicam se a aplicacao pode receber trafego (dependencias criticas).</summary>
    public const string Ready = "ready";
}
