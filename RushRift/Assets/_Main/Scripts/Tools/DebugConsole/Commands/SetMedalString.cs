namespace Game.Tools.DebugCommands
{
    public class SetMedalString : DebugCommand<string>
    {
        public SetMedalString() : base("set_medal", 
            "Toggles a medal upgrade from the level. It is removed when changing level or restarting.", 
            "set_medal <medal> (options: 'bronze' 'silver' 'gold')")
        {
            _command = SetMedal;
        }
        
        private bool SetMedal(string arg)
        {
            if (PlayerSpawner.Instance.TryGet(out var spwManager) && 
                GlobalLevelManager.CurrentLevel.TryGet(out var lvl))
            {
                return spwManager.SetUpgrade(lvl, arg);
            }

            return false;
        }
    }
}