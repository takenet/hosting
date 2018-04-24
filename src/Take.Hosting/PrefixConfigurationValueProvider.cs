using System;

namespace Take.Hosting
{
    /// <summary>
    /// Search values in an underlying provider putting a prefix in the key names.
    /// </summary>
    public class PrefixConfigurationValueProvider : IConfigurationValueProvider
    {
        private readonly IConfigurationValueProvider _underlyinConfigurationValueProvider;
        private readonly string _prefix;

        public PrefixConfigurationValueProvider(IConfigurationValueProvider underlyinConfigurationValueProvider,
            string prefix)
        {            
            _underlyinConfigurationValueProvider = underlyinConfigurationValueProvider ?? throw new ArgumentNullException(nameof(underlyinConfigurationValueProvider));
            if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentNullException(nameof(prefix));
            _prefix = prefix;
        }

        public string GetConfigurationValue(string name)
        {
            return _underlyinConfigurationValueProvider.GetConfigurationValue($"{_prefix}{name}") ??
                   _underlyinConfigurationValueProvider.GetConfigurationValue($"{name}");
        }
    }
}