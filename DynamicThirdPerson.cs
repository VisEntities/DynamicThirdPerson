/*
 * Copyright (C) 2024 Game4Freak.io
 * Your use of this mod indicates acceptance of the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Dynamic Third Person", "VisEntities", "1.3.0")]
    [Description("Automatically puts players in 3d person when performing certain actions.")]
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

            [JsonProperty("Third Person Mode")]
            [JsonConverter(typeof(StringEnumConverter))]
            public ThirdPersonMode ThirdPersonMode { get; set; }

            [JsonProperty("Vehicle Short Prefab Names")]
            public List<string> VehicleShortPrefabNames { get; set; }

            [JsonProperty("Chat Command To Toggle Third Person")]
            public string ChatCommandToToggleThirdPerson { get; set; }

            [JsonProperty("Camera")]
            public CameraConfig Camera { get; set; }
        }

        private class CameraConfig
        {
            [JsonProperty("Offset")]
            public string Offset { get; set; }

            [JsonProperty("Field Of View")]
            public string FieldOfView { get; set; }

            [JsonProperty("Distance")]
            public string Distance { get; set; }
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

            if (string.Compare(_config.Version, "1.1.0") < 0)
            {
                _config.Camera = defaultConfig.Camera;
            }

            if (string.Compare(_config.Version, "1.2.0") < 0)
            {
                _config.VehicleShortPrefabNames = defaultConfig.VehicleShortPrefabNames;
                _config.ChatCommandToToggleThirdPerson = defaultConfig.ChatCommandToToggleThirdPerson;
            }

            if (string.Compare(_config.Version, "1.3.0") < 0)
            {
                _config.ThirdPersonMode = defaultConfig.ThirdPersonMode;
            }

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                ThirdPersonMode = ThirdPersonMode.AlwaysThirdPerson,
                VehicleShortPrefabNames = new List<string>
                {
                    "1module_cockpit",
                    "1module_cockpit_armored",
                    "1module_cockpit_with_engine",
                    "motorbike",
                    "motorbike_sidecar",
                    "pedalbike",
                    "rhib",
                    "rowboat"
                },
                ChatCommandToToggleThirdPerson = "3rd",
                Camera = new CameraConfig
                {
                    Offset = "0.0, 1.0, 0.0",
                    FieldOfView = "106.1227",
                    Distance = "2"
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
            cmd.AddChatCommand(_config.ChatCommandToToggleThirdPerson, this, nameof(cmdToggleThirdPerson));

            switch (_config.ThirdPersonMode)
            {
                case ThirdPersonMode.AlwaysThirdPerson:
                    {
                        Unsubscribe(nameof(OnEntityMounted));
                        Unsubscribe(nameof(OnEntityDismounted));
                        break;
                    }
                case ThirdPersonMode.VehicleOnly:
                    {
                        Unsubscribe(nameof(OnPlayerInput));
                        break;
                    }
                case ThirdPersonMode.CommandOnly:
                    {
                        Unsubscribe(nameof(OnPlayerInput));
                        Unsubscribe(nameof(OnEntityMounted));
                        Unsubscribe(nameof(OnEntityDismounted));
                        break;
                    }
            }
        }
        
        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player != null && !player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin))
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

        private void OnEntityMounted(BaseMountable mountable, BasePlayer player)
        {
            if (player == null || !PermissionUtil.HasPermission(player, PermissionUtil.USE))
                return;

            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin))
                return;

            BaseVehicle vehicle = mountable.GetParentEntity() as BaseVehicle;
            if (vehicle == null || !_config.VehicleShortPrefabNames.Contains(vehicle.ShortPrefabName))
                return;

            ToggleThirdPerson(player, true);
        }

        private void OnEntityDismounted(BaseMountable mountable, BasePlayer player)
        {
            if (player == null || !PermissionUtil.HasPermission(player, PermissionUtil.USE))
                return;

            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin))
                return;

            BaseVehicle vehicle = mountable.GetParentEntity() as BaseVehicle;
            if (vehicle == null || !_config.VehicleShortPrefabNames.Contains(vehicle.ShortPrefabName))
                return;

            ToggleThirdPerson(player, false);
        }

        #endregion Oxide Hooks

        #region Third Person Mode

        public enum ThirdPersonMode
        {
            AlwaysThirdPerson,
            VehicleOnly,
            CommandOnly
        }

        #endregion Third Person Mode

        #region Third Person Toggle

        private void ToggleThirdPerson(BasePlayer player, bool enable)
        {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
            player.SendNetworkUpdateImmediate();

            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, enable);
            if (enable)
            {
                player.Command("camoffset", _config.Camera.Offset);
                player.Command("camfov", _config.Camera.FieldOfView);
                player.Command("camdist", _config.Camera.Distance);
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

        #region Commands
        
        private void cmdToggleThirdPerson(BasePlayer player, string cmd, string[] args)
        {
            if (player == null || !PermissionUtil.HasPermission(player, PermissionUtil.USE))
            {
                SendMessage(player, Lang.NoPermission);
                return;
            }

            if (_config.ThirdPersonMode != ThirdPersonMode.CommandOnly)
            {
                SendMessage(player, Lang.ThirdPersonCommandDisabled);
                return;
            }

            if (args.Length > 0)
            {
                SendMessage(player, Lang.CommandSyntaxError, "/" + _config.ChatCommandToToggleThirdPerson);
                return;
            }

            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin))
            {
                SendMessage(player, Lang.AdminToggleDenied);
                return;
            }

            bool isThirdPerson = player.HasPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode);
            ToggleThirdPerson(player, !isThirdPerson);

            string viewPerspective;
            if (isThirdPerson)
                viewPerspective = "1st person";
            else
                viewPerspective = "3rd person";

            SendMessage(player, Lang.ToggleSuccess, viewPerspective);
        }

        #endregion Commands

        #region Localization

        private class Lang
        {
            public const string NoPermission = "NoPermission";
            public const string ToggleSuccess = "ToggleSuccess";
            public const string AdminToggleDenied = "AdminToggleDenied";
            public const string CommandSyntaxError = "CommandSyntaxError";
            public const string ThirdPersonCommandDisabled = "ThirdPersonCommandDisabled";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.NoPermission] = "You do not have permission to use this command.",
                [Lang.ToggleSuccess] = "You have switched to {0} view.",
                [Lang.AdminToggleDenied] = "You cannot use this command as an admin.",
                [Lang.CommandSyntaxError] = "Syntax error. Correct usage: {0}.",
                [Lang.ThirdPersonCommandDisabled] = "You cannot use this command in the current mode."
            }, this, "en");
        }

        private void SendMessage(BasePlayer player, string messageKey, params object[] args)
        {
            string message = lang.GetMessage(messageKey, this, player.UserIDString);
            if (args.Length > 0)
                message = string.Format(message, args);

            SendReply(player, message);
        }

        #endregion Localization
    }
}