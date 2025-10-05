using System;

namespace Game.Saves.Interfaces
{
    public interface ISaveMigration<T> where T : BaseSaveData
    {
        Version FromVersion { get; }
        Version ToVersion { get; }
        T Apply(T oldData);
    }
}