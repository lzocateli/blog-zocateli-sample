using Microsoft.Extensions.Configuration;

namespace SampleApi.Logging;

public sealed class DynamicLogLevelConfigurationSource : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DynamicLogLevelConfigurationProvider();
    }
}
