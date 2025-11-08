using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.DataBase
{
    public interface IDataBase
    {
        UniTask<DBRequestState> SendUsername(string user, Action<int> successCallback, CancellationToken token);
        UniTask<DBRequestState> SendScore(int user, int level, string time, int a1, int a2, int a3, CancellationToken token);
    }

    public enum DBRequestState
    {
        Success,
        SavingError,
        SendingError
    }
}