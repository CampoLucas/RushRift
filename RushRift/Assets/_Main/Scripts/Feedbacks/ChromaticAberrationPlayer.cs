using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace _Main.Scripts.Feedbacks
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Volume))]
    public class ChromaticAberrationPlayer : VolumeEffectPlayerBase<ChromaticAberration>
    {
        private static ChromaticAberrationPlayer s_global;
        public static ChromaticAberrationPlayer GlobalInstance => s_global;

        protected override bool TryBindEffect(VolumeProfile profile, out ChromaticAberration effect)
        {
            if (!profile.TryGet(out effect) && addOverrideIfMissing)
            {
                effect = profile.Add<ChromaticAberration>(true);
                effect.active = true;
                effect.intensity.overrideState = true;
                effect.intensity.value = 0f;
                Log("Chromatic Aberration override added to profile");
            }
            return effect != null;
        }

        protected override FloatParameter GetIntensityParameter(ChromaticAberration effect) => effect.intensity;
        protected override float ClampValue(float v) => Mathf.Clamp01(v);

        protected override void RegisterGlobalInstance()
        {
            if (s_global && s_global != this && s_global.isActiveAndEnabled)
                Log("Replaced previous global instance");
            s_global = this;
        }

        protected override void UnregisterGlobalInstance()
        {
            if (s_global == this) s_global = null;
        }

        public static void PlayGlobal() { if (s_global) s_global.PlayOnce(); }
        public static void PlayGlobalStrong(float amplitude) { if (s_global) s_global.PlayStrong(amplitude); }

        [Header("Direct Tween")]
        [SerializeField, Tooltip("If true, TweenIntensity calls will use unscaled time by default.")]
        private bool tweenUseUnscaledByDefault = true;

        public void ChromaticTween(float fromValue, float toValue, float durationSeconds, bool useUnscaled = true) =>
            TweenIntensity(fromValue, toValue, durationSeconds, useUnscaled);

        public static void ChromaticTweenGlobal(float fromValue, float toValue, float durationSeconds, bool useUnscaled = true)
        {
            if (s_global) s_global.TweenIntensity(fromValue, toValue, durationSeconds, useUnscaled);
        }
    }
}
