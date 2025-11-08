namespace Game.DataBase
{
    public static class DataBaseHandler
    {
        public static IDataBase DB { get; private set; }

        public static void Init()
        {
            // si  esta online setea DB a la database, sino a la ofline DB
        }
    }
}