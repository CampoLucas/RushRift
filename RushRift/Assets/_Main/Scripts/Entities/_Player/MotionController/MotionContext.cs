using System;
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

        public Vector3 Velocity { get => _rb.velocity; set => _rb.velocity = value; }
        public Vector3 Position { get => _rb.position; set => _rb.position = value; }

        #endregion
        
        #region Public Variables
        
        public Vector3 MoveDirection;

        // Grounded
        public bool Grounded;
        public bool PrevGrounded;
        public Vector3 Normal;
        public Vector3 GroundPos;
        public float GroundAngle;
        
        // Slippery
        public float Slippery;
        
        // inputs
        public bool Jump;
        public bool Dash;

        #endregion

        #region Private Variables

        private Rigidbody _rb;

        #endregion

        public MotionContext(Rigidbody rigidBody, CapsuleCollider collider, Transform origin, Transform camera, Transform orientation)
        {
            _rb = rigidBody;
            Look = camera;
            Orientation = orientation;
            Origin = origin;
            Collider = collider;
        }

        public void AddForce(Vector3 force, ForceMode forceMode = ForceMode.Force) => _rb.AddForce(force, forceMode);
        public void MovePosition(Vector3 position) => _rb.MovePosition(position);
        
        public void Dispose()
        {
            _rb = null;
            Look = null;
            Orientation = null;
            Collider = null;
        }
    }
}