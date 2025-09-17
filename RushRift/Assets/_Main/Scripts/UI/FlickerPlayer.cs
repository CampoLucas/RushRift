using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FlickerPlayer : MonoBehaviour
{
    public enum TargetMode { AutoDetect, ColorProperty, NamedProperty }

    [Header("Targets")]
    [SerializeField, Tooltip("Renderers to flicker. If empty and Auto Bind is enabled, renderers are fetched from this GameObject and its children.")]
    private Renderer[] targetRenderers;
    [SerializeField, Tooltip("If true and no targets are set, automatically binds all renderers in children on Awake.")]
    private bool autoBindRenderersIfEmpty = true;
    [SerializeField, Tooltip("Material indexes to affect on each Renderer. If empty, index 0 is used.")]
    private int[] materialIndexes = new[] { 0 };

    [Header("Material Access")]
    [SerializeField, Tooltip("AutoDetect = try common color properties (_BaseColor, _Color, _Tint, _TintColor). ColorProperty = prefer _Color then _BaseColor. NamedProperty = use the custom property below.")]
    private TargetMode targetMode = TargetMode.AutoDetect;
    [SerializeField, Tooltip("Shader property to set when mode is NamedProperty.")]
    private string colorPropertyName = "_BaseColor";
    [SerializeField, Tooltip("If true, falls back to autodetect list when the chosen property isn't found on a material.")]
    private bool fallbackToAutoDetectIfMissing = true;
    [SerializeField, Tooltip("If true, uses MaterialPropertyBlocks instead of modifying material instances.")]
    private bool useMaterialPropertyBlocks = true;
    [SerializeField, Tooltip("If using PropertyBlocks on SpriteRenderers, set the sprite texture property name here to preserve sprites.")]
    private string spriteTexturePropertyName = "_MainTex";

    [Header("Playback Defaults")]
    [SerializeField, Tooltip("Default flicker color.")]
    private Color defaultFlickerColor = new Color32(255, 40, 40, 255);
    [SerializeField, Tooltip("Total flicker duration in seconds.")]
    private float defaultDurationSeconds = 0.08f;
    [SerializeField, Tooltip("Period between toggles or the length of one curve cycle in seconds.")]
    private float defaultPeriodSeconds = 0.02f;
    [SerializeField, Tooltip("Use unscaled time for the flicker.")]
    private bool useUnscaledTime = false;
    [SerializeField, Tooltip("If true, starting a new flicker while one is running restarts it. If false, new calls are ignored until the current finishes.")]
    private bool restartIfAlreadyPlaying = true;
    [SerializeField, Tooltip("Restore initial colors when stopping or finishing.")]
    private bool resetToInitialOnStop = true;

    [Header("Smoothing")]
    [SerializeField, Tooltip("If enabled, blends between initial and flicker colors using the curve below, instead of hard toggles.")]
    private bool useInterpolationCurve = true;
    [SerializeField, Tooltip("Blend curve evaluated over each period. 0 = initial color, 1 = flicker color.")]
    private AnimationCurve interpolationCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 4f),
        new Keyframe(0.25f, 1f),
        new Keyframe(0.5f, 0f),
        new Keyframe(0.75f, 1f),
        new Keyframe(1f, 0f, -4f, 0f)
    );

    [Header("Global Access")]
    [SerializeField, Tooltip("If true, registers this component as a global instance to call via FlickerPlayer.PlayGlobal(...).")]
    private bool registerAsGlobalInstance = true;

    [Header("Debug Controls")]
    [SerializeField, Tooltip("Enable a key to trigger the default flicker while in Play Mode.")]
    private bool enableDebugKey = true;
    [SerializeField, Tooltip("Key used to trigger the default flicker when Debug Key is enabled.")]
    private KeyCode debugTriggerKey = KeyCode.K;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled = false;
    [SerializeField, Tooltip("Draw gizmos for state and targets.")]
    private bool drawGizmos = true;
    [SerializeField, Tooltip("Gizmo color when idle.")]
    private Color gizmoIdleColor = new Color(0.2f, 1f, 0.6f, 0.9f);
    [SerializeField, Tooltip("Gizmo color when flickering.")]
    private Color gizmoPlayingColor = new Color(1f, 0.6f, 0.2f, 0.9f);

    private static FlickerPlayer s_global;

    private struct TargetSlot
    {
        public Renderer renderer;
        public int matIndex;
        public bool propertyFound;
        public int propertyId;
        public string propertyName;
        public Color initialColor;
        public SpriteRenderer spriteRenderer;
        public Texture spriteTexture;
        public MaterialPropertyBlock block;
    }

    private readonly List<TargetSlot> _targets = new List<TargetSlot>(32);
    private Coroutine _playRoutine;
    private bool _isPlaying;
    private float _lastEvaluated;
    private Color _activeFlickerColor;

    private static readonly string[] AutoDetectNames = { "_BaseColor", "_Color", "_Tint", "_TintColor" };

    private void Awake()
    {
        if ((targetRenderers == null || targetRenderers.Length == 0) && autoBindRenderersIfEmpty)
            targetRenderers = GetComponentsInChildren<Renderer>(true);

        BuildTargetList();
        if (registerAsGlobalInstance) s_global = this;
        Log("Awake");
    }

    private void Update()
    {
        if (enableDebugKey && Input.GetKeyDown(debugTriggerKey))
            PlayDefaultSettings();
    }

    private void OnDisable()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        if (_isPlaying && resetToInitialOnStop)
            RestoreInitialColors();

        _isPlaying = false;
    }

    [ContextMenu("Flicker/Play Default Settings")]
    public void PlayDefaultSettings()
    {
        FlickerPlay(defaultFlickerColor, defaultDurationSeconds, defaultPeriodSeconds);
    }

    [ContextMenu("Flicker/Stop And Reset")]
    public void StopAndReset()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        if (resetToInitialOnStop)
            RestoreInitialColors();

        _isPlaying = false;
        Log("Stopped");
    }

    public void FlickerPlay() => FlickerPlay(defaultFlickerColor, defaultDurationSeconds, defaultPeriodSeconds);
    public void FlickerPlay(Color color) => FlickerPlay(color, defaultDurationSeconds, defaultPeriodSeconds);

    public void FlickerPlay(Color color, float durationSeconds, float periodSeconds)
    {
        if (_targets.Count == 0) { Log("No targets"); return; }
        if (_isPlaying && !restartIfAlreadyPlaying) { Log("Already playing"); return; }

        if (_playRoutine != null) StopCoroutine(_playRoutine);
        _playRoutine = StartCoroutine(FlickerRoutine(color, Mathf.Max(0f, durationSeconds), Mathf.Max(0.0001f, periodSeconds)));
    }

    public static void PlayGlobal() { if (s_global) s_global.FlickerPlay(); }
    public static void PlayGlobal(Color color, float durationSeconds = 0.1f, float periodSeconds = 0.03f)
    { if (s_global) s_global.FlickerPlay(color, durationSeconds, periodSeconds); }

    private void BuildTargetList()
    {
        _targets.Clear();

        if (targetRenderers == null) return;
        if (materialIndexes == null || materialIndexes.Length == 0) materialIndexes = new[] { 0 };

        for (int r = 0; r < targetRenderers.Length; r++)
        {
            var rend = targetRenderers[r];
            if (!rend) continue;

            var sharedMats = rend.sharedMaterials;
            for (int mi = 0; mi < materialIndexes.Length; mi++)
            {
                int idx = Mathf.Clamp(materialIndexes[mi], 0, Mathf.Max(0, sharedMats.Length - 1));
                var slot = new TargetSlot
                {
                    renderer = rend,
                    matIndex = idx,
                    propertyFound = false,
                    propertyId = 0,
                    propertyName = "",
                    initialColor = Color.white,
                    spriteRenderer = rend.GetComponent<SpriteRenderer>(),
                    spriteTexture = null,
                    block = useMaterialPropertyBlocks ? new MaterialPropertyBlock() : null
                };

                if (slot.spriteRenderer && slot.spriteRenderer.sprite)
                    slot.spriteTexture = slot.spriteRenderer.sprite.texture;

                var sm = sharedMats.Length > idx ? sharedMats[idx] : null;
                if (sm)
                {
                    var namesToTry = new List<string>(4);
                    if (targetMode == TargetMode.NamedProperty && !string.IsNullOrEmpty(colorPropertyName))
                        namesToTry.Add(colorPropertyName);

                    if (targetMode == TargetMode.ColorProperty)
                    {
                        namesToTry.Add("_Color");
                        if (fallbackToAutoDetectIfMissing) namesToTry.Add("_BaseColor");
                    }
                    else if (targetMode == TargetMode.AutoDetect || (fallbackToAutoDetectIfMissing && namesToTry.Count == 1))
                    {
                        for (int n = 0; n < AutoDetectNames.Length; n++)
                            if (!namesToTry.Contains(AutoDetectNames[n]))
                                namesToTry.Add(AutoDetectNames[n]);
                    }

                    for (int n = 0; n < namesToTry.Count; n++)
                    {
                        int pid = Shader.PropertyToID(namesToTry[n]);
                        if (sm.HasProperty(pid))
                        {
                            slot.propertyFound = true;
                            slot.propertyId = pid;
                            slot.propertyName = namesToTry[n];
                            slot.initialColor = sm.GetColor(pid);
                            break;
                        }
                    }
                }

                _targets.Add(slot);
            }
        }

        int found = 0;
        for (int i = 0; i < _targets.Count; i++) if (_targets[i].propertyFound) found++;
        Log($"Bound targets: {_targets.Count} slots, {found} with color property");
    }

    private IEnumerator FlickerRoutine(Color flickerColor, float duration, float period)
    {
        _isPlaying = true;
        _activeFlickerColor = flickerColor;
        _lastEvaluated = 0f;
        Log($"Play color={flickerColor} duration={duration:0.###} period={period:0.###} mode={(useInterpolationCurve ? "Curve" : "Toggle")}");

        float t0 = useUnscaledTime ? Time.unscaledTime : Time.time;
        float tEnd = t0 + duration;

        CacheInitialColors();

        if (useInterpolationCurve)
        {
            while ((useUnscaledTime ? Time.unscaledTime : Time.time) < tEnd)
            {
                float now = useUnscaledTime ? Time.unscaledTime : Time.time;
                float cycle = Mathf.Repeat(now - t0, period) / Mathf.Max(1e-5f, period);
                float k = Mathf.Clamp01(interpolationCurve.Evaluate(cycle));
                _lastEvaluated = k;
                SetAllColorsBlended(_activeFlickerColor, k);
                yield return null;
            }
        }
        else
        {
            while ((useUnscaledTime ? Time.unscaledTime : Time.time) < tEnd)
            {
                SetAllColors(flickerColor);
                _lastEvaluated = 1f;
                yield return Wait(period);

                RestoreToInitialOnlyOnFound();
                _lastEvaluated = 0f;
                yield return Wait(period);
            }
        }

        if (resetToInitialOnStop) RestoreInitialColors();

        _isPlaying = false;
        _playRoutine = null;
        Log("Finished");
    }

    private object Wait(float seconds)
    {
        if (seconds <= 0f) return null;
        return useUnscaledTime ? (object)new WaitForSecondsRealtime(seconds) : new WaitForSeconds(seconds);
    }

    private void CacheInitialColors()
    {
        if (useMaterialPropertyBlocks) return;

        for (int i = 0; i < _targets.Count; i++)
        {
            var s = _targets[i];
            if (!s.renderer || !s.propertyFound) continue;

            var mats = s.renderer.materials;
            var m = (mats.Length > s.matIndex) ? mats[s.matIndex] : null;
            if (!m) continue;
            s.initialColor = m.GetColor(s.propertyId);
            _targets[i] = s;
        }
    }

    private void SetAllColors(Color c)
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            var s = _targets[i];
            if (!s.renderer || !s.propertyFound) continue;

            if (useMaterialPropertyBlocks)
            {
                s.renderer.GetPropertyBlock(s.block, s.matIndex);
                s.block.SetColor(s.propertyId, c);
                if (s.spriteRenderer && s.spriteTexture && !string.IsNullOrEmpty(spriteTexturePropertyName))
                    s.block.SetTexture(spriteTexturePropertyName, s.spriteTexture);
                s.renderer.SetPropertyBlock(s.block, s.matIndex);
            }
            else
            {
                var mats = s.renderer.materials;
                if (mats.Length <= s.matIndex || mats[s.matIndex] == null) continue;
                mats[s.matIndex].SetColor(s.propertyId, c);
            }
        }
    }

    private void SetAllColorsBlended(Color flicker, float blend01)
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            var s = _targets[i];
            if (!s.renderer || !s.propertyFound) continue;

            Color c = Color.Lerp(s.initialColor, flicker, blend01);

            if (useMaterialPropertyBlocks)
            {
                s.renderer.GetPropertyBlock(s.block, s.matIndex);
                s.block.SetColor(s.propertyId, c);
                if (s.spriteRenderer && s.spriteTexture && !string.IsNullOrEmpty(spriteTexturePropertyName))
                    s.block.SetTexture(spriteTexturePropertyName, s.spriteTexture);
                s.renderer.SetPropertyBlock(s.block, s.matIndex);
            }
            else
            {
                var mats = s.renderer.materials;
                if (mats.Length <= s.matIndex || mats[s.matIndex] == null) continue;
                mats[s.matIndex].SetColor(s.propertyId, c);
            }
        }
    }

    private void RestoreToInitialOnlyOnFound()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            var s = _targets[i];
            if (!s.renderer || !s.propertyFound) continue;

            if (useMaterialPropertyBlocks)
            {
                s.renderer.GetPropertyBlock(s.block, s.matIndex);
                s.block.SetColor(s.propertyId, s.initialColor);
                if (s.spriteRenderer && s.spriteTexture && !string.IsNullOrEmpty(spriteTexturePropertyName))
                    s.block.SetTexture(spriteTexturePropertyName, s.spriteTexture);
                s.renderer.SetPropertyBlock(s.block, s.matIndex);
            }
            else
            {
                var mats = s.renderer.materials;
                if (mats.Length <= s.matIndex || mats[s.matIndex] == null) continue;
                mats[s.matIndex].SetColor(s.propertyId, s.initialColor);
            }
        }
    }

    private void RestoreInitialColors() => RestoreToInitialOnlyOnFound();

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[FlickerPlayer] {name}: {msg}", this);
    }

#if UNITY_EDITOR
    [ContextMenu("Flicker/Rebuild Targets Now")]
    private void RebuildTargetsNow() => BuildTargetList();

    private void OnValidate()
    {
        if (interpolationCurve == null || interpolationCurve.length == 0)
            interpolationCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

        if (materialIndexes == null || materialIndexes.Length == 0) materialIndexes = new[] { 0 };
        if (targetRenderers != null && targetRenderers.Length > 0) BuildTargetList();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = _isPlaying ? gizmoPlayingColor : gizmoIdleColor;
        Gizmos.DrawWireSphere(transform.position, 0.18f);

        float w = 0.35f;
        Vector3 a = transform.position + Vector3.up * 0.25f + Vector3.left * (w * 0.5f);
        Vector3 b = a + Vector3.right * w;
        Gizmos.DrawLine(a, b);
        Vector3 f = Vector3.Lerp(a, b, Mathf.Clamp01(_lastEvaluated));
        Gizmos.DrawLine(a, f);

        if (targetRenderers != null)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            foreach (var r in targetRenderers)
            {
                if (!r) continue;
                var bb = r.bounds;
                Gizmos.DrawWireCube(bb.center, bb.size);
            }
        }
    }
#endif
}