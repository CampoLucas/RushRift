using System;
using System.Collections;
using Game.DesignPatterns.Observers;
using Game.Levels;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.LevelSelector.Portal
{
    [System.Serializable]
    public struct PortalState
    {
        [Min(0f)] public float transitionDur;
        public AnimationCurve transitionCurve;
            
        [Header("Shader floats")]
        public float distortion;
        public float speed;
        //public float isOn;
        
        [Header("Color")]
        public Color color;
        public bool isLevelColor;
        [Min(0f)] public float hdrIntensity;
        
        public PortalState(PortalState other)
        {
            transitionDur = other.transitionDur;
            transitionCurve = other.transitionCurve;

            distortion = other.distortion;
            speed = other.speed;

            color = other.color;
            isLevelColor = other.isLevelColor;
            hdrIntensity = other.hdrIntensity;
        }
        
        

        public Color GetColor(BaseLevelSO level)
        {
            var c = color;
            if (isLevelColor && level) c = level.LevelSelectData.Color;

            return ToHdr(c, hdrIntensity);
        }
        
        private Color ToHdr(Color ldr, float intensity)
        {
            return new Color(ldr.r * intensity, ldr.g * intensity, ldr.b * intensity, ldr.a);
        }
    }
    
    public class PortalView : MonoBehaviour
    {
        [FormerlySerializedAs("idleAnim")]
        [Header("Animations")]
        [SerializeField] private PortalState idleState;
        [SerializeField] private PortalState selectedState;

        [Header("References")]
        [SerializeField] private Renderer renderer;
        [SerializeField] private PortalPrototype controller;

        private NullCheck<ActionObserver<GameModeSO, BaseLevelSO>> _levelSelectedObserver;
        
        private MaterialPropertyBlock _mpb;
        private int _distortId;
        private int _colorId;
        private int _isOnId;
        private int _onSpeedId;
        
        private Coroutine _anim;
        
        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _distortId = Shader.PropertyToID("_RadialDistortion");
            _colorId = Shader.PropertyToID("_Color2");
            _isOnId = Shader.PropertyToID("_IsOn");
            _onSpeedId = Shader.PropertyToID("_OnSpeed");

            if (_levelSelectedObserver.TryGet(out var observer, GetLevelSelectObserver))
            {
                controller.OnLevelSelected.Attach(observer);
            }
        }

        private ActionObserver<GameModeSO, BaseLevelSO> GetLevelSelectObserver()
        {
            return new ActionObserver<GameModeSO, BaseLevelSO>(OnLevelSelected);
        }

        private void OnLevelSelected(GameModeSO gameMode, BaseLevelSO level)
        {
            PlaySelectedAnim(level);
        }
        
        public void PlaySelectedAnim(BaseLevelSO level)
        {
            if (_anim != null) StopCoroutine(_anim);

            _anim = StartCoroutine(PingPongOnce(level));
        }
        
        private void SetRadialDistortion(float value)
        {
            renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_distortId, value);
            renderer.SetPropertyBlock(_mpb);
        }
        
        private void SetPortalColor(Color hdrColor)
        {
            renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_colorId, hdrColor);
            renderer.SetPropertyBlock(_mpb);
        }

        private void SetIsOn(float value)
        {
            renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_isOnId, value);
            renderer.SetPropertyBlock(_mpb);
        }

        private void SetOnSpeed(float value)
        {
            renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_onSpeedId, value);
            renderer.SetPropertyBlock(_mpb);
        }
        
        private float GetPortalDistortion()
        {
            renderer.GetPropertyBlock(_mpb);
            var c = _mpb.GetFloat(_distortId);

            return c;
        }
        
        private Color GetPortalColor()
        {
            renderer.GetPropertyBlock(_mpb);
            var c = _mpb.GetColor(_colorId);

            return c;
        }
        
        private float GetPortalSpeed()
        {
            renderer.GetPropertyBlock(_mpb);
            var c = _mpb.GetFloat(_onSpeedId);

            return c;
        }
        
        private IEnumerator PingPongOnce(BaseLevelSO level)
        {
            var startState = new PortalState(idleState);
            startState.isLevelColor = false;
            startState.color = GetPortalColor();
            startState.distortion = GetPortalDistortion();
            startState.speed = GetPortalSpeed();
            startState.hdrIntensity = 1;
            
            yield return LerpAnimation(level, startState, selectedState);
            yield return LerpAnimation(level, selectedState, idleState);
            _anim = null;
        }

        private IEnumerator LerpAnimation(BaseLevelSO level, PortalState from, PortalState to)
        {
            var duration = to.transitionDur;
            var toColor = to.GetColor(level);
            
            
            if (duration <= 0f)
            {
                SetRadialDistortion(to.distortion);
                SetPortalColor(toColor);
                //SetIsOn(to.isOn);
                SetOnSpeed(to.speed);
                
                yield break;
            }

            var fromColor = from.GetColor(level);
            
            SetRadialDistortion(from.distortion);
            SetPortalColor(fromColor);
            //SetIsOn(from.isOn);
            SetOnSpeed(from.speed);
            var t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / duration);
                var e = to.transitionCurve?.Evaluate(u) ?? u;
                
                // Distortion goes the whole way up
                SetRadialDistortion(Mathf.Lerp(from.distortion, to.distortion, e));
                SetPortalColor(Color.Lerp(fromColor, toColor, e));
                //SetIsOn(Mathf.LerpUnclamped(from.isOn, to.isOn, e));
                SetOnSpeed(Mathf.Lerp(from.speed, to.speed, e));
                
                yield return null;
            }
            
            SetRadialDistortion(to.distortion);
            SetPortalColor(toColor);
            //SetIsOn(to.isOn);
            SetOnSpeed(to.speed);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            _anim = null;
            
            if (_levelSelectedObserver.TryGet(out var observer))
            {
                controller.OnLevelSelected.Detach(observer);
            }
            
            _levelSelectedObserver.Dispose();

            controller = null;
        }
    }
}