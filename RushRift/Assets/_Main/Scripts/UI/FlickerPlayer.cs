using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FlickerPlayer : MonoBehaviour
{
    public enum BlendMode { LerpToFlicker, AddToInitial }
    public enum TargetSet { Auto, BaseOnly, EmissionOnly, Both }

    [Header("Targets")]
    [SerializeField, Tooltip("Renderers to flicker. If empty and Auto Bind is enabled, renderers are fetched from this GameObject and its children.")]
    private Renderer[] targetRenderers;
    [SerializeField, Tooltip("If true and no targets are set, automatically binds all renderers in children on Awake.")]
    private bool autoBindRenderersIfEmpty = true;
    [SerializeField, Tooltip("Material indexes to affect on each Renderer. If empty, index 0 is used.")]
    private int[] materialIndexes = new[] { 0 };

    [Header("Explicit Materials")]
    [SerializeField, Tooltip("Optional explicit material list to drive directly. Drag the material from the object's Renderer slot here.")]
    private Material[] explicitTargetMaterials;
    [SerializeField, Tooltip("When using Explicit Materials, which channels to affect.")]
    private TargetSet explicitMaterialsTargetSet = TargetSet.Both;
    [SerializeField, Tooltip("If true, edits the shared Material assets. If false, nothing is auto-assigned; only enable this if you understand the implications.")]
    private bool editSharedMaterialAssets = true;

    [Header("URP / Shader Properties")]
    [SerializeField, Tooltip("Which properties to affect for Renderer targets. Auto = Emission if present else Base. Both = Base and Emission together.")]
    private TargetSet targetSet = TargetSet.Auto;
    [SerializeField, Tooltip("Base color property (URP Lit: _BaseColor; legacy: _Color).")]
    private string baseColorProperty = "_BaseColor";
    [SerializeField, Tooltip("Legacy base color fallback (usually _Color).")]
    private string baseColorFallbackProperty = "_Color";
    [SerializeField, Tooltip("Emission color property (URP Lit: _EmissionColor).")]
    private string emissionColorProperty = "_EmissionColor";
    [SerializeField, Tooltip("Use MaterialPropertyBlocks to avoid instantiating materials for Renderer targets.")]
    private bool useMaterialPropertyBlocks = true;
    [SerializeField, Tooltip("When editing materials directly (not MPB) and affecting emission, enable the _EMISSION keyword automatically.")]
    private bool enableEmissionKeywordWhenEditingMaterials = true;
    [SerializeField, Tooltip("If using PropertyBlocks on SpriteRenderers, set the sprite texture property name here to preserve sprites.")]
    private string spriteTexturePropertyName = "_MainTex";

    [Header("Playback Defaults")]
    [SerializeField, ColorUsage(true, true), Tooltip("Default flicker color (HDR enabled).")]
    private Color defaultFlickerColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField, Tooltip("Extra HDR intensity multiplier applied to the flicker color.")]
    private float flickerHdrIntensity = 1f;
    [SerializeField, Tooltip("Total flicker duration in seconds.")]
    private float defaultDurationSeconds = 0.08f;
    [SerializeField, Tooltip("Period between toggles or one curve cycle in seconds.")]
    private float defaultPeriodSeconds = 0.02f;
    [SerializeField, Tooltip("Use unscaled time for the flicker.")]
    private bool useUnscaledTime = false;
    [SerializeField, Tooltip("If true, starting a new flicker while one is running restarts it. If false, new calls are ignored until the current finishes.")]
    private bool restartIfAlreadyPlaying = true;
    [SerializeField, Tooltip("Restore initial colors when stopping or finishing.")]
    private bool resetToInitialOnStop = true;

    [Header("Smoothing & Blending")]
    [SerializeField, Tooltip("How to combine the flicker with the initial color. Lerp blends between initial and flicker; Add adds on top for bright HDR pops.")]
    private BlendMode blendMode = BlendMode.LerpToFlicker;
    [SerializeField, Tooltip("If enabled, blends between initial and flicker colors using the curve below, instead of hard toggles.")]
    private bool useInterpolationCurve = true;
    [SerializeField, Tooltip("Blend curve evaluated over each period. 0 = initial color, 1 = flicker color/intensity.")]
    private AnimationCurve interpolationCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 4f),
        new Keyframe(0.25f, 1f),
        new Keyframe(0.5f, 0f),
        new Keyframe(0.75f, 1f),
        new Keyframe(1f, 0f, -4f, 0f)
    );

    [Header("Global Access")]
    [SerializeField, Tooltip("If true, registers this as a global instance to call via FlickerPlayer.PlayGlobal(...).")]
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

    private struct RendererSlot
    {
        public Renderer renderer;
        public int matIndex;

        public bool baseFound;
        public int basePid;
        public string baseName;
        public Color baseInitial;

        public bool emissFound;
        public int emissPid;
        public string emissName;
        public Color emissInitial;

        public SpriteRenderer spriteRenderer;
        public Texture spriteTexture;
        public MaterialPropertyBlock block;
    }

    private struct MaterialSlot
    {
        public Material mat;
        public bool baseFound;
        public int basePid;
        public Color baseInitial;
        public bool emissFound;
        public int emissPid;
        public Color emissInitial;
    }

    private readonly List<RendererSlot> _rendererTargets = new List<RendererSlot>(32);
    private readonly List<MaterialSlot> _materialTargets = new List<MaterialSlot>(8);

    private Coroutine _playRoutine;
    private bool _isPlaying;
    private float _lastEvaluated;
    private Color _activeFlickerColorHDR;

    private void Awake()
    {
        if ((targetRenderers == null || targetRenderers.Length == 0) && autoBindRenderersIfEmpty)
            targetRenderers = GetComponentsInChildren<Renderer>(true);

        BuildRendererTargets();
        BuildMaterialTargets();
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
            RestoreToInitial();

        _isPlaying = false;
    }

    [ContextMenu("Flicker/Play Default Settings")]
    public void PlayDefaultSettings()
    {
        FlickerPlay(defaultFlickerColor, defaultDurationSeconds, defaultPeriodSeconds, flickerHdrIntensity);
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
            RestoreToInitial();

        _isPlaying = false;
        Log("Stopped");
    }

    public void FlickerPlay() => FlickerPlay(defaultFlickerColor, defaultDurationSeconds, defaultPeriodSeconds, flickerHdrIntensity);
    public void FlickerPlay(Color color) => FlickerPlay(color, defaultDurationSeconds, defaultPeriodSeconds, flickerHdrIntensity);
    public void FlickerPlay(Color color, float durationSeconds, float periodSeconds, float hdrIntensity = 1f)
    {
        if (_rendererTargets.Count == 0 && _materialTargets.Count == 0) { Log("No targets"); return; }
        if (_isPlaying && !restartIfAlreadyPlaying) { Log("Already playing"); return; }

        if (_playRoutine != null) StopCoroutine(_playRoutine);
        _playRoutine = StartCoroutine(FlickerRoutine(color, Mathf.Max(0f, durationSeconds), Mathf.Max(0.0001f, periodSeconds), Mathf.Max(0f, hdrIntensity)));
    }

    public static void PlayGlobal() { if (s_global) s_global.FlickerPlay(); }
    public static void PlayGlobal(Color color, float durationSeconds = 0.1f, float periodSeconds = 0.03f, float hdrIntensity = 1f)
    { if (s_global) s_global.FlickerPlay(color, durationSeconds, periodSeconds, hdrIntensity); }

    private void BuildRendererTargets()
    {
        _rendererTargets.Clear();
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
                var slot = new RendererSlot
                {
                    renderer = rend,
                    matIndex = idx,
                    baseFound = false,
                    emissFound = false,
                    basePid = 0,
                    emissPid = 0,
                    baseName = "",
                    emissName = "",
                    baseInitial = Color.white,
                    emissInitial = Color.black,
                    spriteRenderer = rend.GetComponent<SpriteRenderer>(),
                    spriteTexture = null,
                    block = useMaterialPropertyBlocks ? new MaterialPropertyBlock() : null
                };

                if (slot.spriteRenderer && slot.spriteRenderer.sprite)
                    slot.spriteTexture = slot.spriteRenderer.sprite.texture;

                var sm = sharedMats.Length > idx ? sharedMats[idx] : null;
                if (sm)
                {
                    if (sm.HasProperty(baseColorProperty)) { slot.basePid = Shader.PropertyToID(baseColorProperty); slot.baseName = baseColorProperty; slot.baseFound = true; slot.baseInitial = sm.GetColor(slot.basePid); }
                    else if (!string.IsNullOrEmpty(baseColorFallbackProperty) && sm.HasProperty(baseColorFallbackProperty)) { slot.basePid = Shader.PropertyToID(baseColorFallbackProperty); slot.baseName = baseColorFallbackProperty; slot.baseFound = true; slot.baseInitial = sm.GetColor(slot.basePid); }

                    if (!string.IsNullOrEmpty(emissionColorProperty) && sm.HasProperty(emissionColorProperty))
                    { slot.emissPid = Shader.PropertyToID(emissionColorProperty); slot.emissName = emissionColorProperty; slot.emissFound = true; slot.emissInitial = sm.GetColor(slot.emissPid); }
                }

                _rendererTargets.Add(slot);
            }
        }

        int fb = 0, fe = 0; for (int i = 0; i < _rendererTargets.Count; i++) { if (_rendererTargets[i].baseFound) fb++; if (_rendererTargets[i].emissFound) fe++; }
        Log($"Renderer targets: {_rendererTargets.Count} slots, base:{fb} emission:{fe}");
    }

    private void BuildMaterialTargets()
    {
        _materialTargets.Clear();
        if (explicitTargetMaterials == null || explicitTargetMaterials.Length == 0) return;

        for (int i = 0; i < explicitTargetMaterials.Length; i++)
        {
            var m = explicitTargetMaterials[i];
            if (!m) continue;

            var slot = new MaterialSlot
            {
                mat = m,
                baseFound = false,
                emissFound = false,
                basePid = 0,
                emissPid = 0,
                baseInitial = Color.white,
                emissInitial = Color.black
            };

            if (m.HasProperty(baseColorProperty)) { slot.basePid = Shader.PropertyToID(baseColorProperty); slot.baseFound = true; slot.baseInitial = m.GetColor(slot.basePid); }
            else if (!string.IsNullOrEmpty(baseColorFallbackProperty) && m.HasProperty(baseColorFallbackProperty)) { slot.basePid = Shader.PropertyToID(baseColorFallbackProperty); slot.baseFound = true; slot.baseInitial = m.GetColor(slot.basePid); }

            if (!string.IsNullOrEmpty(emissionColorProperty) && m.HasProperty(emissionColorProperty)) { slot.emissPid = Shader.PropertyToID(emissionColorProperty); slot.emissFound = true; slot.emissInitial = m.GetColor(slot.emissPid); }

            _materialTargets.Add(slot);
        }

        int fb = 0, fe = 0; for (int i = 0; i < _materialTargets.Count; i++) { if (_materialTargets[i].baseFound) fb++; if (_materialTargets[i].emissFound) fe++; }
        Log($"Explicit materials: {_materialTargets.Count}, base:{fb} emission:{fe}");
    }

    private IEnumerator FlickerRoutine(Color flickerColor, float duration, float period, float hdrIntensity)
    {
        _isPlaying = true;
        _activeFlickerColorHDR = MultiplyHdr(flickerColor, hdrIntensity);
        _lastEvaluated = 0f;
        Log($"Play color={flickerColor} x{hdrIntensity:0.###} duration={duration:0.###} period={period:0.###}");

        CacheInitialsForRendererTargets();
        CacheInitialsForMaterialTargets();

        float t0 = useUnscaledTime ? Time.unscaledTime : Time.time;
        float tEnd = t0 + duration;

        if (useInterpolationCurve)
        {
            while ((useUnscaledTime ? Time.unscaledTime : Time.time) < tEnd)
            {
                float now = useUnscaledTime ? Time.unscaledTime : Time.time;
                float cycle = Mathf.Repeat(now - t0, period) / Mathf.Max(1e-5f, period);
                float k = Mathf.Clamp01(interpolationCurve.Evaluate(cycle));
                _lastEvaluated = k;
                ApplyToRendererTargetsBlended(_activeFlickerColorHDR, k);
                ApplyToMaterialTargetsBlended(_activeFlickerColorHDR, k);
                yield return null;
            }
        }
        else
        {
            while ((useUnscaledTime ? Time.unscaledTime : Time.time) < tEnd)
            {
                ApplyToRendererTargets(_activeFlickerColorHDR);
                ApplyToMaterialTargets(_activeFlickerColorHDR);
                _lastEvaluated = 1f;
                yield return Wait(period);

                RestoreRendererTargets();
                RestoreMaterialTargets();
                _lastEvaluated = 0f;
                yield return Wait(period);
            }
        }

        if (resetToInitialOnStop)
        {
            RestoreRendererTargets();
            RestoreMaterialTargets();
        }

        _isPlaying = false;
        _playRoutine = null;
        Log("Finished");
    }

    private object Wait(float seconds)
    {
        if (seconds <= 0f) return null;
        return useUnscaledTime ? (object)new WaitForSecondsRealtime(seconds) : new WaitForSeconds(seconds);
    }

    private void CacheInitialsForRendererTargets()
    {
        if (useMaterialPropertyBlocks) return;
        for (int i = 0; i < _rendererTargets.Count; i++)
        {
            var s = _rendererTargets[i];
            if (!s.renderer) continue;
            var mats = s.renderer.materials;
            var m = (mats.Length > s.matIndex) ? mats[s.matIndex] : null;
            if (!m) continue;
            if (s.baseFound) s.baseInitial = m.GetColor(s.basePid);
            if (s.emissFound) s.emissInitial = m.GetColor(s.emissPid);
            _rendererTargets[i] = s;
        }
    }

    private void CacheInitialsForMaterialTargets()
    {
        for (int i = 0; i < _materialTargets.Count; i++)
        {
            var s = _materialTargets[i];
            var m = s.mat;
            if (!m) continue;
            if (s.baseFound) s.baseInitial = m.GetColor(s.basePid);
            if (s.emissFound) s.emissInitial = m.GetColor(s.emissPid);
            _materialTargets[i] = s;
        }
    }

    private bool AffectBase(bool baseFound, bool emissFound, TargetSet set)
    {
        return (set == TargetSet.Both) || (set == TargetSet.BaseOnly) ||
               (set == TargetSet.Auto && (!emissFound || !baseFound ? baseFound : false));
    }

    private bool AffectEmiss(bool baseFound, bool emissFound, TargetSet set)
    {
        return (set == TargetSet.Both) || (set == TargetSet.EmissionOnly) ||
               (set == TargetSet.Auto && (emissFound && (baseFound || !baseFound)));
    }

    private void ApplyToRendererTargets(Color flicker)
    {
        for (int i = 0; i < _rendererTargets.Count; i++)
        {
            var s = _rendererTargets[i];
            if (!s.renderer) continue;

            bool affB = s.baseFound && AffectBase(s.baseFound, s.emissFound, targetSet);
            bool affE = s.emissFound && AffectEmiss(s.baseFound, s.emissFound, targetSet);
            if (!affB && !affE) continue;

            if (useMaterialPropertyBlocks)
            {
                s.renderer.GetPropertyBlock(s.block, s.matIndex);
                if (affB) s.block.SetColor(s.basePid, flicker);
                if (affE) s.block.SetColor(s.emissPid, flicker);
                if (s.spriteRenderer && s.spriteTexture && !string.IsNullOrEmpty(spriteTexturePropertyName))
                    s.block.SetTexture(spriteTexturePropertyName, s.spriteTexture);
                s.renderer.SetPropertyBlock(s.block, s.matIndex);
            }
            else
            {
                var mats = s.renderer.materials;
                if (mats.Length <= s.matIndex || mats[s.matIndex] == null) continue;
                if (affB) mats[s.matIndex].SetColor(s.basePid, flicker);
                if (affE)
                {
                    if (enableEmissionKeywordWhenEditingMaterials)
                        mats[s.matIndex].EnableKeyword("_EMISSION");
                    mats[s.matIndex].SetColor(s.emissPid, flicker);
                }
            }
        }
    }

    private void ApplyToRendererTargetsBlended(Color flicker, float k)
    {
        for (int i = 0; i < _rendererTargets.Count; i++)
        {
            var s = _rendererTargets[i];
            if (!s.renderer) continue;

            bool affB = s.baseFound && AffectBase(s.baseFound, s.emissFound, targetSet);
            bool affE = s.emissFound && AffectEmiss(s.baseFound, s.emissFound, targetSet);
            if (!affB && !affE) continue;

            Color baseOut = s.baseInitial;
            Color emisOut = s.emissInitial;

            if (affB) baseOut = (blendMode == BlendMode.AddToInitial) ? s.baseInitial + flicker * k : Color.LerpUnclamped(s.baseInitial, flicker, k);
            if (affE) emisOut = (blendMode == BlendMode.AddToInitial) ? s.emissInitial + flicker * k : Color.LerpUnclamped(s.emissInitial, flicker, k);

            if (useMaterialPropertyBlocks)
            {
                s.renderer.GetPropertyBlock(s.block, s.matIndex);
                if (affB) s.block.SetColor(s.basePid, baseOut);
                if (affE) s.block.SetColor(s.emissPid, emisOut);
                if (s.spriteRenderer && s.spriteTexture && !string.IsNullOrEmpty(spriteTexturePropertyName))
                    s.block.SetTexture(spriteTexturePropertyName, s.spriteTexture);
                s.renderer.SetPropertyBlock(s.block, s.matIndex);
            }
            else
            {
                var mats = s.renderer.materials;
                if (mats.Length <= s.matIndex || mats[s.matIndex] == null) continue;
                if (affB) mats[s.matIndex].SetColor(s.basePid, baseOut);
                if (affE)
                {
                    if (enableEmissionKeywordWhenEditingMaterials)
                        mats[s.matIndex].EnableKeyword("_EMISSION");
                    mats[s.matIndex].SetColor(s.emissPid, emisOut);
                }
            }
        }
    }

    private void RestoreRendererTargets()
    {
        for (int i = 0; i < _rendererTargets.Count; i++)
        {
            var s = _rendererTargets[i];
            if (!s.renderer) continue;

            bool affB = s.baseFound && AffectBase(s.baseFound, s.emissFound, targetSet);
            bool affE = s.emissFound && AffectEmiss(s.baseFound, s.emissFound, targetSet);

            if (useMaterialPropertyBlocks)
            {
                s.renderer.GetPropertyBlock(s.block, s.matIndex);
                if (affB) s.block.SetColor(s.basePid, s.baseInitial);
                if (affE) s.block.SetColor(s.emissPid, s.emissInitial);
                if (s.spriteRenderer && s.spriteTexture && !string.IsNullOrEmpty(spriteTexturePropertyName))
                    s.block.SetTexture(spriteTexturePropertyName, s.spriteTexture);
                s.renderer.SetPropertyBlock(s.block, s.matIndex);
            }
            else
            {
                var mats = s.renderer.materials;
                if (mats.Length <= s.matIndex || mats[s.matIndex] == null) continue;
                if (affB) mats[s.matIndex].SetColor(s.basePid, s.baseInitial);
                if (affE) mats[s.matIndex].SetColor(s.emissPid, s.emissInitial);
            }
        }
    }

    private void ApplyToMaterialTargets(Color flicker)
    {
        if (!editSharedMaterialAssets) return;

        for (int i = 0; i < _materialTargets.Count; i++)
        {
            var s = _materialTargets[i];
            if (!s.mat) continue;

            bool affB = s.baseFound && AffectBase(s.baseFound, s.emissFound, explicitMaterialsTargetSet);
            bool affE = s.emissFound && AffectEmiss(s.baseFound, s.emissFound, explicitMaterialsTargetSet);
            if (!affB && !affE) continue;

            if (affB) s.mat.SetColor(s.basePid, flicker);
            if (affE)
            {
                if (enableEmissionKeywordWhenEditingMaterials)
                    s.mat.EnableKeyword("_EMISSION");
                s.mat.SetColor(s.emissPid, flicker);
            }
        }
    }

    private void ApplyToMaterialTargetsBlended(Color flicker, float k)
    {
        if (!editSharedMaterialAssets) return;

        for (int i = 0; i < _materialTargets.Count; i++)
        {
            var s = _materialTargets[i];
            if (!s.mat) continue;

            bool affB = s.baseFound && AffectBase(s.baseFound, s.emissFound, explicitMaterialsTargetSet);
            bool affE = s.emissFound && AffectEmiss(s.baseFound, s.emissFound, explicitMaterialsTargetSet);
            if (!affB && !affE) continue;

            if (affB)
            {
                var c = (blendMode == BlendMode.AddToInitial) ? s.baseInitial + flicker * k : Color.LerpUnclamped(s.baseInitial, flicker, k);
                s.mat.SetColor(s.basePid, c);
            }
            if (affE)
            {
                var c = (blendMode == BlendMode.AddToInitial) ? s.emissInitial + flicker * k : Color.LerpUnclamped(s.emissInitial, flicker, k);
                s.mat.EnableKeyword("_EMISSION");
                s.mat.SetColor(s.emissPid, c);
            }
        }
    }

    private void RestoreMaterialTargets()
    {
        if (!editSharedMaterialAssets) return;

        for (int i = 0; i < _materialTargets.Count; i++)
        {
            var s = _materialTargets[i];
            if (!s.mat) continue;

            bool affB = s.baseFound && AffectBase(s.baseFound, s.emissFound, explicitMaterialsTargetSet);
            bool affE = s.emissFound && AffectEmiss(s.baseFound, s.emissFound, explicitMaterialsTargetSet);

            if (affB) s.mat.SetColor(s.basePid, s.baseInitial);
            if (affE) s.mat.SetColor(s.emissPid, s.emissInitial);
        }
    }

    private static Color MultiplyHdr(Color c, float intensity)
    {
        float k = Mathf.Max(0f, intensity);
        return new Color(c.r * k, c.g * k, c.b * k, c.a);
    }

    private void RestoreToInitial()
    {
        RestoreRendererTargets();
        RestoreMaterialTargets();
    }

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[FlickerPlayer] {name}: {msg}", this);
    }

#if UNITY_EDITOR
    [ContextMenu("Flicker/Rebuild Targets Now")]
    private void RebuildTargetsNow()
    {
        BuildRendererTargets();
        BuildMaterialTargets();
    }

    private void OnValidate()
    {
        if (interpolationCurve == null || interpolationCurve.length == 0)
            interpolationCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

        flickerHdrIntensity = Mathf.Max(0f, flickerHdrIntensity);

        if (materialIndexes == null || materialIndexes.Length == 0) materialIndexes = new[] { 0 };
        if (targetRenderers != null && targetRenderers.Length > 0) BuildRendererTargets();
        if (explicitTargetMaterials != null && explicitTargetMaterials.Length > 0) BuildMaterialTargets();
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
    }
#endif
}
