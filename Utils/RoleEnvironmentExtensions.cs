using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Utils
{
    public static class RoleEnvironmentExt
    {
        public static string GetRoleConfigSetting(string setting, string defaultValue = null)
        {
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(setting);
            }
            catch (RoleEnvironmentException rex)
            {
                return defaultValue;
            }
        }

        public static int GetRoleConfigSetting(string setting, int defaultValue)
        {
            string settingConfig;
            try
            {
                settingConfig = RoleEnvironment.GetConfigurationSettingValue(setting);
                return int.Parse(settingConfig);
            }
            catch (RoleEnvironmentException rex)
            {
                return defaultValue;
            }
            catch (Exception ex)
            {
                return defaultValue;
            }
        }

        public static bool GetRoleConfigSetting(string setting, bool defaultValue)
        {
            string settingConfig;
            try
            {
                settingConfig = RoleEnvironment.GetConfigurationSettingValue(setting);
                return !string.IsNullOrWhiteSpace(settingConfig) ? bool.Parse(settingConfig) : defaultValue;
            }
            catch (RoleEnvironmentException rex)
            {
                return defaultValue;
            }
            catch (Exception ex)
            {
                return defaultValue;
            }
        }

        public static string GetRoleConfigSetting(this RoleEntryPoint role, string setting, string defaultValue = "")
        {
            return GetRoleConfigSetting(setting, defaultValue);
        }

        public static int GetRoleConfigSetting(this RoleEntryPoint role, string setting, int defaultValue)
        {
            return GetRoleConfigSetting(setting, defaultValue);
        }

        public static bool GetRoleConfigSetting(this RoleEntryPoint role, string setting, bool defaultValue)
        {
            return GetRoleConfigSetting(setting, defaultValue);
        }
    }
}