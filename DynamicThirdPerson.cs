namespace Oxide.Plugins
{
    [Info("Dynamic Third Person", "VisEntities", "1.0.0")]
    [Description("Automatically switches between first-person and third-person views when aiming with a weapon.")]
    public class DynamicThirdPerson : RustPlugin
    {
        #region Fields

        private static DynamicThirdPerson _plugin;

        #endregion Fields

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            _plugin = null;
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.IsDown(BUTTON.FIRE_SECONDARY))
            {
                // Only apply the third-person view toggle to non-admin players
                if (!player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin))
                {
                    ToggleThirdPerson(player, false);
                }
            }
            else if (input.WasDown(BUTTON.FIRE_SECONDARY))
            {
                // Only apply the third-person view toggle to non-admin players
                if (!player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin))
                {
                    ToggleThirdPerson(player, true);
                }
            }
        }

        private void ToggleThirdPerson(BasePlayer player, bool enable)
        {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
            player.SendNetworkUpdateImmediate();

            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, enable);
            if (enable)
            {
                player.Command("camoffset", "0.0, 1.0, 0.0");
                player.Command("camfov", "106.1227");
                player.Command("camdist", "2");
            }

            player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
            player.SendNetworkUpdateImmediate();
        }

        #endregion Oxide Hooks
    }
}