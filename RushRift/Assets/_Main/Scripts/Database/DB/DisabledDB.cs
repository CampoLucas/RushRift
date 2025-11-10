using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.DataBase.DB
{
    public class DisabledDB : IDataBase
    {
        public bool Enabled()
        {
            return false;
        }

        public async UniTask<DBRequestState> SendUsername(string user, Action<int> successCallback, CancellationToken token)
        {
            return DBRequestState.Disabled;
        }

        public async UniTask<DBRequestState> SendScore(int user, int level, string time, int a1, int a2, int a3, CancellationToken token)
        {
            return DBRequestState.Disabled;
        }

        public async UniTask<DBRequestState> GetScore(int level, Action<ScoreList> successCallback, CancellationToken token)
        {
            return DBRequestState.Disabled;
        }
    }
}