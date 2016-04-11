using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SInnovations.ConfigurationManager;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration
{
    public class ServiceFabricSettingProvider : ISettingsProvider
    {
        public const string ServiceFabricSettingProviderName = "ServiceFabricSettingProvider";
        public string Name { get; private set; } = ServiceFabricSettingProviderName;

        private readonly ServiceInitializationParameters serviceInitializationParameters;

        public ServiceFabricSettingProvider(ServiceInitializationParameters serviceInitializationParameters)
        {
            this.serviceInitializationParameters = serviceInitializationParameters; 
        }
        public bool TryGetSetting(string settingName, out string settingValue)
        {
            try {
                var configurationPackage = this.serviceInitializationParameters.CodePackageActivationContext.GetConfigurationPackageObject("Config");
                var section = settingName.Substring(0, settingName.IndexOf("_"));
                var paramName = settingName.Substring(section.Length + 1);

                var connectionStringParameter = configurationPackage.Settings.Sections[section].Parameters[paramName];
                settingValue = connectionStringParameter.Value;

                return true;
            }
            catch (Exception ex)
            {
                settingValue = null;
                return false;
            }
        }
    }
}
