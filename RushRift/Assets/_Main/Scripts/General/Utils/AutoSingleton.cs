using UnityEngine;

namespace MyTools.Utils
{
    public class AutoSingleton<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        protected static T _instance;
        private static readonly object _lock;
        
        public static T GetInstance()
        {
            lock (_lock)
                return Get();
        }

        private static T Get()
        {
            if (!_instance)
            {
                var instance = FindObjectOfType<T>();
                if (!instance)
                {
                    instance = new GameObject("DefaultManagerName").AddComponent<T>();
                }

                _instance = instance;
            }

            return _instance;
        }

    }
}