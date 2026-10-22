using System;
using System.Linq;
using System.Reflection;

namespace Game.ConsoleCommands.Tests
{
    /// <summary>
    /// Finds and creates the IConsoleCommandsTestBackend implementation that lives in the editor assembly.
    /// </summary>
    public static class ConsoleCommandsTestBackendLocator
    {
        private static Type _backendType;

        public static IConsoleCommandsTestBackend Create()
        {
            return (IConsoleCommandsTestBackend)Activator.CreateInstance(FindBackendType());
        }

        private static Type FindBackendType()
        {
            if (_backendType != null) return _backendType;

            _backendType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeTypes)
                .FirstOrDefault(t => typeof(IConsoleCommandsTestBackend).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            if (_backendType == null)
            {
                throw new InvalidOperationException("No IConsoleCommandsTestBackend implementation found. Expected ConsoleCommandsTestBackend in Tools/Editor/ConsoleCommands.");
            }

            return _backendType;
        }

        private static Type[] SafeTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null).ToArray();
            }
        }
    }
}
