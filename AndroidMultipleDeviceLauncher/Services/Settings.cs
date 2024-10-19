using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Shell;

namespace AndroidMultipleDeviceLauncher.Services
{
    public class Settings
    {
        public static string SettingsName = "AndroidMultipleDeviceLauncherSettings";

        private WritableSettingsStore GetWritableSettingsStore()
        {
            var shellSettingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            return shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }

        public void SaveSetting(string collectionPath, string propertyName, string value)
        {
            WritableSettingsStore writableSettingsStore = GetWritableSettingsStore();
            if (writableSettingsStore != null)
            {
                if (!writableSettingsStore.CollectionExists(collectionPath))
                    writableSettingsStore.CreateCollection(collectionPath);

                writableSettingsStore.SetString(collectionPath, propertyName, value);
            }
        }

        public string GetSetting(string collectionPath, string propertyName)
        {
            WritableSettingsStore writableSettingsStore = GetWritableSettingsStore();
            if (writableSettingsStore != null && writableSettingsStore.CollectionExists(collectionPath))
            {
                return writableSettingsStore.GetString(collectionPath, propertyName, defaultValue: null);
            }
            return null;
        }
    }
}
