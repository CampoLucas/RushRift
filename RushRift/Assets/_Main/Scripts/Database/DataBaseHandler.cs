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
                DB = new ServerDB("[2802:8010:8b42:a801::5555]");
            }
            else
            {
                // Set to offline data base
                //if (OfflineDataBaseEnabled()) {
                
                // If there is no offline database
                DB = new DisabledDB();
            }
            // si  esta online setea DB a la database, sino a la ofline DB
        }

        private static bool HasInternet()
        {
            return true;
        }
    }
}