namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Login state of the debug console. Every command except login needs it to be logged in.
    /// </summary>
    public sealed class ConsoleSession
    {
        public static ConsoleSession Shared { get; } = new ConsoleSession(null);

        public bool LoggedIn { get; private set; }
        public bool HasPassword => !string.IsNullOrEmpty(_password);

        private string _password;

        public ConsoleSession(string password)
        {
            _password = password;
        }

        public void SetPassword(string password)
        {
            _password = password;
        }

        public bool TryLogin(string password)
        {
            if (!HasPassword || password != _password) return false;

            LoggedIn = true;
            return true;
        }

        public void Logout()
        {
            LoggedIn = false;
        }
    }
}
