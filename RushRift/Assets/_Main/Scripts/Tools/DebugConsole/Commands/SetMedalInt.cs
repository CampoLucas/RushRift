namespace Game.Tools.DebugCommands
{
    public class SetMedalInt : DebugCommand<int>
    {
        public SetMedalInt() : base("set_medal", 
            "Toggles a medal upgrade from the level. It is removed when changing level or restarting.", 
            "set_medal <medal_number> (options: 1, 2, 3)")
        {
            _command = SetMedal;
        }
        
        private bool SetMedal(int arg)
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