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
        [Header("Impulse")]
        [SerializeField] private bool isStatic = true;
        [SerializeField] private float force;
        
        [Header("Direction")]
        [SerializeField] private Vector3 direction = Vector3.up;
        [SerializeField] private DirectionRelative relativeTo = DirectionRelative.Transform;
        [SerializeField] private Transform customRelative;

        [Space(10)]
        [Header("Observer Settings")]
        [SerializeField] private bool startOn;
        [SerializeField] private bool invertArgs;

        private bool _isOn;
        private Dictionary<string, Action> _notifyActions = new();
        private Vector3 _staticDirection;


        private void Awake()
        {
            var onArg = invertArgs ? Terminal.OFF_ARGUMENT : Terminal.ON_ARGUMENT;
            var offArg = invertArgs ? Terminal.ON_ARGUMENT : Terminal.OFF_ARGUMENT;
            
            _notifyActions.Add(onArg, On);
            _notifyActions.Add(offArg, Off);

            if (isStatic)
            {
                _staticDirection = GetDir();
            }
        }

        private void Start()
        {
            _isOn = startOn;
        }

        public override void OnNotify(string arg)
        {
            if (!_notifyActions.TryGetValue(arg, out var action) || action == null) return;
            
            action();
        }

        private void On()
        {
            _isOn = true;
        }

        private void Off()
        {
            _isOn = false;
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
            // Color based on state
            Gizmos.color = _isOn ? Color.cyan : Color.gray;

            var origin = transform.position;
            var dir = Vector3.zero;

            if (Application.isPlaying)
                dir = GetDir();
            else
            {
                // Approximate in edit mode (without runtime refs)
                dir = relativeTo switch
                {
                    DirectionRelative.World => direction.normalized,
                    DirectionRelative.Transform => transform.TransformDirection(direction.normalized),
                    DirectionRelative.Custom => (customRelative ? customRelative : transform).TransformDirection(direction.normalized),
                    _ => transform.up
                };
            }

            var arrowLength = Mathf.Max(1f, force * 0.1f);
            Gizmos.DrawRay(origin, dir * arrowLength);

            // Draw small arrowhead
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