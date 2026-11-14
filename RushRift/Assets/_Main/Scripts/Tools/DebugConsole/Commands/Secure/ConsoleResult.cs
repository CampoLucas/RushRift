namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Outcome of a console command and the message the console prints for it.
    /// </summary>
    public readonly struct ConsoleResult
    {
        public bool Success { get; }
        public string Message { get; }

        public ConsoleResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static ConsoleResult Ok(string message)
        {
            return new ConsoleResult(true, message);
        }

        public static ConsoleResult Error(string message)
        {
            return new ConsoleResult(false, message);
        }
    }
}
