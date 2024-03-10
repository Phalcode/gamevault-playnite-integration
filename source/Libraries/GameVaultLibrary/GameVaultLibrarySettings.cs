using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace GameVaultLibrary
{
    public class GameVaultLibrarySettings : ObservableObject
    {
        private string serverUrl = string.Empty;
        private string version = "";

        [DontSerialize, System.Xml.Serialization.XmlIgnore, System.NonSerialized]
        private string username = "";

        public string ServerUrl { get => serverUrl; set => SetValue(ref serverUrl, value); }

        [DontSerialize, System.Xml.Serialization.XmlIgnore]
        public string Username { get => username; set => SetValue(ref username, value); }

        [DontSerialize, System.Xml.Serialization.XmlIgnore]
        public string Password { get; set; } = "";

        public string Version { get => version; set => SetValue(ref version, value); }

        public string UsernameEncrypted
        {
            get => Convert.ToBase64String(System.Security.Cryptography.ProtectedData.Protect(Encoding.UTF8.GetBytes(Username), null, System.Security.Cryptography.DataProtectionScope.CurrentUser));
            set => Username = Encoding.UTF8.GetString(System.Security.Cryptography.ProtectedData.Unprotect(Convert.FromBase64String(value), null, System.Security.Cryptography.DataProtectionScope.CurrentUser));
        }

        public string PasswordEncrypted
        {
            get => Convert.ToBase64String(System.Security.Cryptography.ProtectedData.Protect(Encoding.UTF8.GetBytes(Password), null, System.Security.Cryptography.DataProtectionScope.CurrentUser));
            set => Password = Encoding.UTF8.GetString(System.Security.Cryptography.ProtectedData.Unprotect(Convert.FromBase64String(value), null, System.Security.Cryptography.DataProtectionScope.CurrentUser));
        }
    }

    public class GameVaultLibrarySettingsViewModel : ObservableObject, ISettings
    {
        private readonly GameVaultLibrary plugin;
        private GameVaultLibrarySettings editingClone { get; set; }

        private GameVaultLibrarySettings settings;
        public GameVaultLibrarySettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public GameVaultLibrarySettingsViewModel(GameVaultLibrary plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<GameVaultLibrarySettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new GameVaultLibrarySettings();
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrEmpty(Settings.ServerUrl))
                errors.Add("Server URL cannot be empty.");

            if (string.IsNullOrEmpty(Settings.Username))
                errors.Add("Username cannot be empty.");

            if (string.IsNullOrEmpty(Settings.Password))
                errors.Add("Password cannot be empty.");

            return true;
        }
    }
}