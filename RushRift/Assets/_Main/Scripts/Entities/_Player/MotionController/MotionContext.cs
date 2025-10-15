using System;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    public class MotionContext : IDisposable
    {
        #region Public Properties
        public Transform Look { get; private set; }
        public Transform Orientation { get; private set; }
        public Transform Origin { get; private set; }
        public CapsuleCollider Collider { get; private set; }

        public Vector3 Velocity
        {
            get => _rb.velocity;
            set
            {
                var isKinematic = IsKinematic;

                if (isKinematic) IsKinematic = false;
                _rb.velocity = value;
                if (isKinematic) IsKinematic = true;
            }
        }
        public Vector3 Position { get => _rb.position; set => _rb.position = value; }
        public Subject<bool> OnGroundedChanged => _onGroundedChanged;

        public bool IsKinematic
        {
            get => _rb && _rb.isKinematic;
            set
            {
                if (_rb)
                {
                    _rb.isKinematic = value;
                }
            }
        }
        

        #endregion
        
        #region Public Variables
        
        public Vector3 MoveDirection;

        // Grounded
        public bool Grounded
        {
            get => _grounded;
            set
            {
                if (value != _grounded)
                {
                    _onGroundedChanged.NotifyAll(value);
                }

                _grounded = value;
            }
        }
        
        public bool PrevGrounded;
        public Vector3 Normal;
        public Vector3 GroundPos;
        public float GroundAngle;
        
        // Slippery
        public float Slippery;
        
        // Jump
        public bool Jump;
        public bool IsJumping;
        
        // Dash
        public bool Dash;
        public bool IsDashing;

        #endregion

        #region Private Variables

        private Rigidbody _rb;
        private bool _grounded;
        private Subject<bool> _onGroundedChanged;

        #endregion

        public MotionContext(Rigidbody rigidBody, CapsuleCollider collider, Transform origin, Transform camera, Transform orientation)
        {
            _rb = rigidBody;
            Look = camera;
            Orientation = orientation;
            Origin = origin;
            Collider = collider;
            _onGroundedChanged = new Subject<bool>();
        }

        public void AddForce(Vector3 force, ForceMode forceMode = ForceMode.Force) => _rb.AddForce(force, forceMode);
        public void MovePosition(Vector3 position) => _rb.MovePosition(position);
        
        public void Dispose()
        {
            _rb = null;
            Look = null;
            Orientation = null;
            Collider = null;
            OnGroundedChanged.DetachAll();
        }
    }
}