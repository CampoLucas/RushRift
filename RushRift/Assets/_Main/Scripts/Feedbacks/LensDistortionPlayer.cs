using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace _Main.Scripts.Feedbacks
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Volume))]
    public class LensDistortionPlayer : VolumeEffectPlayerBase<LensDistortion>
    {
        [Header("Lens Settings")]
        [SerializeField, Tooltip("If true, adds the animated value to the initial intensity. If false, uses absolute values.")]
        private bool useRelativeIntensity = false;

        private static LensDistortionPlayer s_global;
        public static LensDistortionPlayer GlobalInstance => s_global;

        protected override bool TryBindEffect(VolumeProfile profile, out LensDistortion effect)
        {
            if (!profile.TryGet(out effect) && addOverrideIfMissing)
            {
                effect = profile.Add<LensDistortion>(true);
                effect.active = true;
                effect.intensity.overrideState = true;
                effect.intensity.value = 0f;
                Log("Lens Distortion override added to profile");
            }
            return effect != null;
        }

        protected override FloatParameter GetIntensityParameter(LensDistortion effect) => effect.intensity;
        protected override float ClampValue(float v) => Mathf.Clamp(v, -1f, 1f);

        protected override float MapFinalValue(bool isFinalPhase, float baseValue, float remapMaxTimesAmplitude)
        {
            if (isFinalPhase && resetToInitialOnStop) return InitialIntensity;
            if (useRelativeIntensity) return ClampValue(InitialIntensity + remapMaxTimesAmplitude);
            return baseValue;
        }

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

        public void LensDistortionTween(float fromValue, float toValue, float durationSeconds, bool useUnscaled = true) =>
            TweenIntensity(fromValue, toValue, durationSeconds, useUnscaled);

        public static void LensDistortionTweenGlobal(float fromValue, float toValue, float durationSeconds, bool useUnscaled = true)
        {
            if (s_global) s_global.TweenIntensity(fromValue, toValue, durationSeconds, useUnscaled);
        }
    }
}
