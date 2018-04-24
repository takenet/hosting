using System;
using System.Collections.Generic;

namespace Take.Hosting
{
    public class DictionaryConfigurationValueProvider : IConfigurationValueProvider
    {
        public IDictionary<string, string> Dictionary { get; }

        public DictionaryConfigurationValueProvider()
            : this(new Dictionary<string, string>())
        {

        }

        public DictionaryConfigurationValueProvider(IDictionary<string, string> dictionary)
        {
            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        public string GetConfigurationValue(string name)
        {
            string value;
            Dictionary.TryGetValue(name, out value);
            return value;
        }
    }
}