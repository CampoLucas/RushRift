using TMPro;
using UnityEngine;

namespace _Main.Scripts.Feedbacks
{
    [DisallowMultipleComponent]
    public class FloatingTextFeedback : MonoBehaviour
    {
        [Header("Spawner")]
        [SerializeField, Tooltip("If assigned, spawns via this spawner. If null and Use Global is enabled, will use FloatingTextSpawner.Instance.")]
        private FloatingTextSpawner targetSpawner;
        [SerializeField, Tooltip("If true and Target Spawner is null, use FloatingTextSpawner.Instance.")]
        private bool useGlobalSpawnerIfNull = true;

        [Header("Text")]
        [SerializeField, Tooltip("Default text used by Play() when no custom value is provided.")]
        private string defaultText = "";
        [SerializeField, Tooltip("If true, positive numbers include a '+' sign when formatting with PlayValue().")]
        private bool usePlusSignForPositive = true;
        [SerializeField, Tooltip("Numeric format for PlayValue(). Examples: 0, 0.#, 0.00")]
        private string numberFormat = "0";
        [SerializeField, Tooltip("Text prefix added before the generated value.")]
        private string prefix = "";
        [SerializeField, Tooltip("Text suffix added after the generated value.")]
        private string suffix = "";
        [SerializeField, Tooltip("If true, final text is converted to UPPERCASE.")]
        private bool uppercase = false;

        [Header("Appearance Overrides")]
        [SerializeField, Tooltip("If true, overrides the spawned TMP text's font size by a multiplier.")]
        private bool overrideFontSize = false;
        [SerializeField, Tooltip("Multiplier applied to the spawned TMP text's font size if Override Font Size is enabled.")]
        private float fontSizeMultiplier = 1f;

        [Header("Placement")]
        [SerializeField, Tooltip("Base world-space offset from this transform.")]
        private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField, Tooltip("If true, try to place the text at the top of this object's bounds (Renderer or Collider), plus the world offset.")]
        private bool useTopOfBounds = true;
        [SerializeField, Tooltip("If true, the floating text will follow this object while alive.")]
        private bool followThisObject = true;
        [SerializeField, Tooltip("Optional attachment target; used if Follow This Object is false.")]
        private Transform customAttachment;

        [Header("Motion")]
        [SerializeField, Tooltip("Base direction for the floating motion.")]
        private Vector3 baseDirection = Vector3.up;
        [SerializeField, Tooltip("If true, the direction is randomized inside a cone around the Base Direction.")]
        private bool randomizeDirection = false;
        [SerializeField, Tooltip("Apex angle of the randomization cone, in degrees.")]
        [Range(0f, 180f)] private float randomConeAngleDegrees = 15f;

        [Header("Timing")]
        [SerializeField, Tooltip("Lifetime in seconds for the spawned text.")]
        private float lifetimeSeconds = 0.9f;
        [SerializeField, Tooltip("Use unscaled time for the spawned text animation.")]
        private bool useUnscaledTime = true;

        [Header("Visuals")]
        [SerializeField, Tooltip("Optional color gradient for the spawned text over its lifetime.")]
        private Gradient colorGradient;
        [SerializeField, Tooltip("Intensity passed along to the floating text (if used by your effect).")]
        private float intensity = 1f;

        [Header("Debug")]
        [SerializeField, Tooltip("If enabled, prints detailed logs.")]
        private bool isDebugLoggingEnabled = false;
        [SerializeField, Tooltip("Draw gizmos for previewing spawn position and direction.")]
        private bool drawGizmos = true;
        [SerializeField, Tooltip("Gizmo color.")]
        private Color gizmoColor = new Color(0.2f, 0.9f, 1f, 0.9f);

        /// <summary>Triggers a floating text using Default Text at this object's position.</summary>
        public void Play()
        {
            string txt = PrepareFinalText(defaultText);
            Vector3 pos = ResolveSpawnPosition();
            Vector3 dir = ResolveDirection();
            Transform attach = ResolveAttachment();
            var spawner = ResolveSpawner();
            if (!spawner) { Log("Play ignored: no spawner available"); return; }

            var ft = spawner.Spawn(txt, pos, dir, intensity, lifetimeSeconds, colorGradient, attach, useUnscaledTime);
            ApplyAppearanceOverrides(ft);
            Log($"Play: \"{txt}\" at {pos} dir={dir}");
        }

        /// <summary>Triggers a floating text using the provided text at this object's position.</summary>
        public void PlayText(string text)
        {
            string txt = PrepareFinalText(text);
            Vector3 pos = ResolveSpawnPosition();
            Vector3 dir = ResolveDirection();
            Transform attach = ResolveAttachment();
            var spawner = ResolveSpawner();
            if (!spawner) { Log("PlayText ignored: no spawner available"); return; }

            var ft = spawner.Spawn(txt, pos, dir, intensity, lifetimeSeconds, colorGradient, attach, useUnscaledTime);
            ApplyAppearanceOverrides(ft);
            Log($"PlayText: \"{txt}\" at {pos} dir={dir}");
        }

        /// <summary>Formats and triggers a floating text from the provided numeric value at this object's position.</summary>
        public void PlayValue(float value)
        {
            string sign = value > 0f && usePlusSignForPositive ? "+" : "";
            string formatted = sign + value.ToString(numberFormat);
            string txt = PrepareFinalText(formatted);
            Vector3 pos = ResolveSpawnPosition();
            Vector3 dir = ResolveDirection();
            Transform attach = ResolveAttachment();
            var spawner = ResolveSpawner();
            if (!spawner) { Log("PlayValue ignored: no spawner available"); return; }

            var ft = spawner.Spawn(txt, pos, dir, intensity, lifetimeSeconds, colorGradient, attach, useUnscaledTime);
            ApplyAppearanceOverrides(ft);
            Log($"PlayValue: \"{txt}\" at {pos} dir={dir}");
        }

        /// <summary>Triggers a floating text at a custom world position.</summary>
        public void PlayAtPosition(string text, Vector3 worldPosition)
        {
            string txt = PrepareFinalText(text);
            Vector3 dir = ResolveDirection();
            var spawner = ResolveSpawner();
            if (!spawner) { Log("PlayAtPosition ignored: no spawner available"); return; }

            var ft = spawner.Spawn(txt, worldPosition, dir, intensity, lifetimeSeconds, colorGradient, null, useUnscaledTime);
            ApplyAppearanceOverrides(ft);
            Log($"PlayAtPosition: \"{txt}\" at {worldPosition} dir={dir}");
        }

        /// <summary>Sets the default text used by Play().</summary>
        public void SetDefaultText(string text) => defaultText = text;

        /// <summary>Sets the lifetime in seconds used for subsequent plays.</summary>
        public void SetLifetime(float seconds) => lifetimeSeconds = Mathf.Max(0.01f, seconds);

        /// <summary>Sets the intensity value passed to the spawner.</summary>
        public void SetIntensity(float value) => intensity = Mathf.Max(0f, value);

        /// <summary>Sets the gradient that will be passed to the FloatingText.</summary>
        public void SetGradient(Gradient gradient) => colorGradient = gradient;

        /// <summary>Sets the override font size multiplier and toggles the override.</summary>
        public void SetFontSizeOverride(bool enabled, float multiplier = 1f)
        {
            overrideFontSize = enabled;
            fontSizeMultiplier = multiplier;
        }

        /// <summary>Sets whether the spawned text follows this object during its lifetime.</summary>
        public void SetFollowThisObject(bool follow) => followThisObject = follow;

        /// <summary>Sets a custom attachment target; ignored if Follow This Object is enabled.</summary>
        public void SetCustomAttachment(Transform target) => customAttachment = target;

        /// <summary>Resolves which spawner to use based on fields and global availability.</summary>
        private FloatingTextSpawner ResolveSpawner()
        {
            if (targetSpawner) return targetSpawner;
            if (useGlobalSpawnerIfNull) return FloatingTextSpawner.Instance;
            return null;
        }

        /// <summary>Resolves the world-space position to spawn the text.</summary>
        private Vector3 ResolveSpawnPosition()
        {
            if (!useTopOfBounds) return transform.position + worldOffset;

            bool hadBounds = false;
            Bounds b = default;
            var r = GetComponentInChildren<Renderer>();
            if (r) { b = r.bounds; hadBounds = true; }
            else
            {
                var c = GetComponentInChildren<Collider>();
                if (c) { b = c.bounds; hadBounds = true; }
            }

            if (!hadBounds) return transform.position + worldOffset;

            var top = new Vector3(b.center.x, b.max.y, b.center.z);
            return top + worldOffset;
        }

        /// <summary>Resolves the optional follow attachment for the spawned text.</summary>
        private Transform ResolveAttachment()
        {
            if (followThisObject) return transform;
            return customAttachment;
        }

        /// <summary>Resolves the direction to be sent to the FloatingText.</summary>
        private Vector3 ResolveDirection()
        {
            if (!randomizeDirection) return baseDirection.normalized;
            return SampleDirectionInCone(baseDirection, randomConeAngleDegrees);
        }

        /// <summary>Samples a random unit vector inside a cone around a given axis.</summary>
        private static Vector3 SampleDirectionInCone(Vector3 axis, float coneAngleDeg)
        {
            var nAxis = (axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up);
            float angle = Random.Range(0f, Mathf.Clamp(coneAngleDeg, 0f, 180f));
            Vector3 randPerp = Vector3.Cross(nAxis, Random.onUnitSphere).normalized;
            if (randPerp.sqrMagnitude < 1e-5f) randPerp = Vector3.Cross(nAxis, Vector3.right).normalized;
            var q = Quaternion.AngleAxis(angle, randPerp);
            return (q * nAxis).normalized;
        }

        /// <summary>Builds the final display string with prefix/suffix and case options.</summary>
        private string PrepareFinalText(string input)
        {
            string s = $"{prefix}{input}{suffix}";
            return uppercase ? s.ToUpperInvariant() : s;
        }

        /// <summary>Applies optional appearance overrides to the spawned instance.</summary>
        private void ApplyAppearanceOverrides(FloatingText instance)
        {
            if (!instance) return;
            if (!overrideFontSize) return;
            var tmp = instance.GetComponentInChildren<TMP_Text>(true);
            if (!tmp) return;
            tmp.fontSize *= Mathf.Max(0.01f, fontSizeMultiplier);
        }

        /// <summary>Writes a debug line if debug logging is enabled.</summary>
        private void Log(string msg)
        {
            if (!isDebugLoggingEnabled) return;
            Debug.Log($"[FloatingTextFeedback] {name}: {msg}", this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;
            Gizmos.color = gizmoColor;

            Vector3 p = ResolveSpawnPosition();
            Gizmos.DrawWireSphere(p, 0.06f);

            Vector3 dir = ResolveDirection();
            Gizmos.DrawRay(p, dir * 0.25f);
        }
#endif
    }
}
