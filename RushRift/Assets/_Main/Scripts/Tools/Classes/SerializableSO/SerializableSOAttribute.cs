using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Draws a ScriptableObject inline in the inspector as a foldout.
    /// Use it on fields that reference ScriptableObjects.
    /// </summary>
    public class SerializableSOAttribute : PropertyAttribute
    {
        public SerializableSOAttribute()
        {
        }
    }
}