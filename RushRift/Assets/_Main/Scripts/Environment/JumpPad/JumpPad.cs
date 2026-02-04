using System;
using System.Collections.Generic;
using Game.Entities;
using Game.Entities.Components.MotionController;
using Game.Utils;
using UnityEngine;

namespace Game.LevelElements
{
    [DisallowMultipleComponent]
    public class JumpPad : ObserverComponent
    {
        private enum DirectionRelative { World, Transform, Custom }

        [Header("Impulse Settings")]
        [SerializeField] private bool isStatic = true;
        [SerializeField] private float force;
        
        [Header("Direction")]
        [SerializeField] private Vector3 direction = Vector3.up;
        [SerializeField] private DirectionRelative relativeTo = DirectionRelative.Transform;
        [SerializeField] private Transform customRelative;

        [Space(10)]
        [Header("Observer / Control")]
        [SerializeField] private bool startOn = true;
        [SerializeField, Tooltip("Argument that triggers activation.")]
        private string onArgument = "on";
        [SerializeField, Tooltip("Argument that triggers deactivation.")]
        private string offArgument = "off";

        [Header("Visuals")]
        [SerializeField, Tooltip("Child objects (VFX, Particles, Lights) to toggle on/off.")]
        private GameObject[] visualsToToggle;

        private bool _isOn;
        private Vector3 _staticDirection;

        private void Awake()
        {
            if (isStatic)
            {
                _staticDirection = GetDir();
            }
        }

        private void Start()
        {
            // Initialize state and visuals
            _isOn = startOn;
            UpdateVisuals(_isOn);
        }

        public override void OnNotify(string arg)
        {
            // Normalize string for comparison like in LightBridge.cs
            string a = (arg ?? "").Trim().ToLowerInvariant();
            
            // Compare against Terminal arguments or standard defaults
            if (a == onArgument.ToLowerInvariant() || a == "on" || a == "enable") 
                SetState(true);
            else if (a == offArgument.ToLowerInvariant() || a == "off" || a == "disable") 
                SetState(false);
        }

        private void SetState(bool state)
        {
            _isOn = state;
            UpdateVisuals(_isOn);
        }

        private void UpdateVisuals(bool state)
        {
            if (visualsToToggle == null) return;

            foreach (var visual in visualsToToggle)
            {
                if (visual != null)
                {
                    visual.SetActive(state);
                }
            }
        }

        private Vector3 GetDir()
        {
            var dir = direction.normalized;

            switch (relativeTo)
            {
                case DirectionRelative.World:
                    return dir;
                case DirectionRelative.Transform:
                    return transform.TransformDirection(dir);
                case DirectionRelative.Custom:
                    return (customRelative ? customRelative : transform).TransformDirection(dir);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Vector3 FinalDir()
        {
#if UNITY_EDITOR
            return GetDir();
#else
            return isStatic ? _staticDirection : GetDir();
#endif
        }

        private void OnTriggerEnter(Collider other)
        {
            // Ensure pad is active before applying force
            if (!_isOn) return;

            if (!other.gameObject.TryGetComponent<IController>(out var controller)) return;

            var model = controller.GetModel();
            if (model == null) return;

            if (model.TryGetComponent<MotionController>(out var motion))
            {
                if (other.gameObject.TryGetComponent(out Rigidbody rb))
                {
                    rb.velocity = rb.velocity.XOZ();
                }
                motion.ExternalImpulse(FinalDir() * force);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _isOn ? Color.cyan : Color.gray;

            var origin = transform.position;
            var dir = Application.isPlaying ? GetDir() : 
                relativeTo switch
                {
                    DirectionRelative.World => direction.normalized,
                    DirectionRelative.Transform => transform.TransformDirection(direction.normalized),
                    DirectionRelative.Custom => (customRelative ? customRelative : transform).TransformDirection(direction.normalized),
                    _ => transform.up
                };

            var arrowLength = Mathf.Max(1f, force * 0.1f);
            Gizmos.DrawRay(origin, dir * arrowLength);

            var right = Vector3.Cross(dir, Vector3.up).normalized * 0.2f;
            var up = Vector3.Cross(dir, right).normalized * 0.2f;
            var tip = origin + dir * arrowLength;
            Gizmos.DrawLine(tip, tip - dir * 0.3f + right);
            Gizmos.DrawLine(tip, tip - dir * 0.3f - right);
            Gizmos.DrawLine(tip, tip - dir * 0.3f + up);
            Gizmos.DrawLine(tip, tip - dir * 0.3f - up);
        }
    }
}