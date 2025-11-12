using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
[AddComponentMenu("FX/Freeze Frame On Disable")]
public sealed class FreezeFrameOnDisable : MonoBehaviour
{
    [SerializeField] private float freezeDuration = 0.08f;

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        if (freezeDuration <= 0f) return;
        FreezeFrame.Request(freezeDuration);
    }

    private static class FreezeFrame
    {
        private static float endTime;
        private static bool isFrozen;
        private static float previousScale = 1f;
        private static Runner runner;

        public static void Request(float duration)
        {
            if (runner == null)
            {
                var go = new GameObject("[FreezeFrameRunner]");
                go.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<Runner>();
            }

            float now = Time.realtimeSinceStartup;
            float newEnd = now + duration;
            if (!isFrozen)
            {
                previousScale = Time.timeScale;
                Time.timeScale = 0f;
                isFrozen = true;
                endTime = newEnd;
                runner.StartCoroutine(UnfreezeWhenReady());
            }
            else
            {
                if (newEnd > endTime) endTime = newEnd;
            }
        }

        private static IEnumerator UnfreezeWhenReady()
        {
            while (Time.realtimeSinceStartup < endTime) yield return null;
            Time.timeScale = previousScale <= 0f ? 1f : previousScale;
            isFrozen = false;
        }

        private sealed class Runner : MonoBehaviour { }
    }
}