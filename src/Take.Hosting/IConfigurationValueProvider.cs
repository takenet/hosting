using System.Text;

namespace Take.Hosting
{
    /// <summary>
    /// Defines a service that provide configuration strings.
    /// </summary>
    public interface IConfigurationValueProvider
    {
        /// <summary>
        /// Gets the value of a configuration string.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string GetConfigurationValue(string name);
    }
}
