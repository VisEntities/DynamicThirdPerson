using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Dynamic Third Person", "VisEntities", "1.0.0")]
    [Description("Allows players to switch between first-person and third-person perspectives.")]
    public class DynamicThirdPerson : RustPlugin
    {
        #region Fields

        private static DynamicThirdPerson _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Camera Offset")]
            public string CameraOffset { get; set; }

            [JsonProperty("Camera Field Of View")]
            public string CameraFieldOfView { get; set; }

            [JsonProperty("Camera Distance")]
            public string CameraDistance { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                CameraOffset = "0.0, 1.0, 0.0",
                CameraFieldOfView = "106.1227",
                CameraDistance = "2"
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player != null)
                    ToggleThirdPerson(player, false);
            }

            _config = null;
            _plugin = null;
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || !PermissionUtil.HasPermission(player, PermissionUtil.USE))
                return;

            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin))
                return;

            if (input.IsDown(BUTTON.FIRE_SECONDARY))
            {
                ToggleThirdPerson(player, false);
            }
            else if (input.WasDown(BUTTON.FIRE_SECONDARY))
            {
                 ToggleThirdPerson(player, true);
            }
        }

        #endregion Oxide Hooks

        # region Third Person Toggle

        private void ToggleThirdPerson(BasePlayer player, bool enable)
        {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
            player.SendNetworkUpdateImmediate();

            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, enable);
            if (enable)
            {
                player.Command("camoffset", _config.CameraOffset);
                player.Command("camfov", _config.CameraFieldOfView);
                player.Command("camdist", _config.CameraDistance);
            }

            player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
            player.SendNetworkUpdateImmediate();
        }

        #endregion Third Person Toggle

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "dynamicthirdperson.use";
            private static readonly List<string> _permissions = new List<string>
            {
                USE,
            };

            public static void RegisterPermissions()
            {
                foreach (var permission in _permissions)
                {
                    _plugin.permission.RegisterPermission(permission, _plugin);
                }
            }

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }
        }

        #endregion Permissions
    }
}