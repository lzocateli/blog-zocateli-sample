using Microsoft.Extensions.Configuration;

namespace BlogSamples.Logging;

public sealed class DynamicLogLevelConfigurationProvider : ConfigurationProvider
{
    public override void Load()
    {
        // Starts empty — values are set dynamically via Update()
    }

    public void Update(string key, string value)
    {
        if (value is null)
        {
            Data.Remove(key);
        }
        else
        {
            Data[key] = value;
        }

        OnReload();
    }

    public void Clear()
    {
        Data.Clear();
        OnReload();
    }
}
