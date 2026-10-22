using System.IO;
using _Main.Scripts.Ghost;
using UnityEngine;

namespace Game.GhostDebugger
{
    /// <summary>
    /// Readable JSON export and import of ghost runs, shared by the debugger window and the tests.
    /// </summary>
    public static class GhostRunJson
    {
        public static void Export(GhostRecorder.GhostRunData run, string path)
        {
            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(path, JsonUtility.ToJson(run, true));
        }

        public static GhostRecorder.GhostRunData Import(string path)
        {
            return JsonUtility.FromJson<GhostRecorder.GhostRunData>(File.ReadAllText(path));
        }
    }
}
