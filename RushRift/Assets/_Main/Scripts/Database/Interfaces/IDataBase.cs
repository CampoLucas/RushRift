using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.DataBase
{
    public interface IDataBase
    {
        bool Enabled();
        UniTask<DBRequestState> SendUsername(string user, Action<int> successCallback, CancellationToken token);
        UniTask<DBRequestState> SendScore(int user, int level, string time, int a1, int a2, int a3, CancellationToken token);// probar de usar bitmask
        UniTask<DBRequestState> GetScore(int level, Action<ScoreList> successCallback, CancellationToken token);// probar de usar bitmask
    }

    public enum DBRequestState
    {
        Success,
        Disabled,
        SavingError,
        SendingError
    }

    [Flags]
    public enum Abilities
    {
        Bronze,
        Silver,
        Gold
    }
}