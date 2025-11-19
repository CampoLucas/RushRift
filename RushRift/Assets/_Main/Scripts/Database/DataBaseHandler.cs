using Game.DataBase.DB;
using Game.DesignPatterns.Observers;

namespace Game.DataBase
{
    public static class DataBaseHandler
    {
        public static IDataBase DB { get; private set; }
        public static ISubject<DBRequestState, string> ErrorFallback = new Subject<DBRequestState, string>();

        public static void Init()
        {
            if (HasInternet())
            {
                DB = new ServerDB("192.168.1.197");
            }
            else
            {
                // Set to offline data base
                //if (OfflineDataBaseEnabled()) {

                // If there is no offline database
                DB = new ServerDB("");
            }
            // si  esta online setea DB a la database, sino a la ofline DB
        }

        private static bool HasInternet()
        {
            //ping database
            return true;
        }
    }
}