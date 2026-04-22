using System.Collections.Generic;
using Game.Saves.Interfaces;

namespace Game.Saves
{
    public static class SaveMigrations
    {
        public static List<ISaveMigration<SaveData>> GameMigrations = new List<ISaveMigration<SaveData>>()
        {
            // ToDo: add more migrations here
        };
        
        public static List<ISaveMigration<SettingsData>> SettingsMigrations = new List<ISaveMigration<SettingsData>>()
        {
            // ToDo: add more migrations here
        };
    }
}