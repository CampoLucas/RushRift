using System;
using Game.Utils;
using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    public class MovementHandler : MotionHandler<MovementConfig>
    {
        private float Slippery { get => Config.SlidingEnabled ? _slippery : 0; set => _slippery = value; }
        private float _slippery;
        
        public MovementHandler(MovementConfig config) : base(config)
        {
        }

        public override void OnFixedUpdate(in MotionContext context, in float delta)
        {
            base.OnFixedUpdate(context, delta);

            if (Config.SlidingEnabled) 
                CheckSlope(context, delta);
            
            ApplyMovement(context, delta);
        }

        private void CheckSlope(in MotionContext context, in float delta)
        {
            var velocity = context.Velocity;
            
            if (!context.Grounded)
            {
                if (Config.Sliding.KeepSlipperyValueOnAir) return;
                
                if (Config.Sliding.RecoverOnAir)
                {
                    DecreaseSlippery(Config.Sliding.AirRecoverySpeed, velocity, delta);
                    return;
                }
                
                Slippery = 0;
                return;
            }

            var angle = context.GroundAngle;
            var maxAngle = Config.Sliding.MaxSlopeAng;
            
            if (angle > Config.Sliding.MaxSlopeAng)
            {
                // Too steep → increase slipperiness
                var slopeRatio = Mathf.Clamp01((angle - maxAngle) / (90f - maxAngle));

                // Player speed influence (horizontal + vertical fall)
                var horizontalSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;
                var verticalSpeed = Mathf.Max(0, -velocity.y); // only falling, not jumping

                var speedFactor = (horizontalSpeed + verticalSpeed) / Config.Sliding.SpeedScaling;
                speedFactor = Mathf.Min(speedFactor, 5f); // allow strong boost
                speedFactor = Mathf.Pow(speedFactor, 1.1f); // optional: amplify

                // Combined slippery force
                var targetSlippery = slopeRatio * Config.Sliding.Mult * (1f + speedFactor);
                Slippery = Mathf.MoveTowards(Slippery, targetSlippery, Time.fixedDeltaTime * 2f);
            }
            else
            {
                DecreaseSlippery(Config.Sliding.RecoverSpeed, velocity, delta);
            }
        }

        private void DecreaseSlippery(in float recoverSpeed, in Vector3 velocity, in float delta)
        {
            var decaySpeed = recoverSpeed;
            
            // If you're still moving fast, reduce decay so you don't stop instantly
            var speed = velocity.magnitude;
            if (speed > 5f) // ToDo: serialized variables for this
                decaySpeed *= .3f; // slower decay when moving fast

            Slippery = Mathf.MoveTowards(Slippery, 0f, decaySpeed * delta);
        }

        private void ApplyMovement(in MotionContext context, in float delta)
        {
#if false
            // Find the velocity relative to where the player is looking.
            var localVel = GetLookVelocity(context.Origin, context.Velocity);
            
            // Apply drag to prevent the player from sliding.
            ApplyDrag(context, localVel, Slippery, delta);
            
            // Clamp the max speed.
            ClampHorizontal(context);
            
            // If speed is over max speed, ignores the input so it doesn't exceed it.
            DirCorrection(context, localVel);

            var mult = 1f;
            var forwardMult = 1f;
            var grounded = context.Grounded;
            
            if (!grounded)
            {
                var fallSpeed = Mathf.Max(0, -context.Velocity.y); // Only care about downward speed;
                
                // Scale air control as a function of fall speed (e.g., linear or exponential)
                var controlBoost = 1f + fallSpeed * Config.AirFallMultiplier;


                mult = Config.AirMultiplier * controlBoost;
                forwardMult = Config.AirForwardMultiplier * controlBoost;
            }

            var forward = context.Origin.forward;
            var right = context.Origin.right;
            var angle = context.GroundAngle;
            var normal = context.Normal;
            var maxAngle = Config.Sliding.MaxSlopeAng;
            var moveDir = context.MoveDirection;
            
            Vector3 moveDirForward;
            Vector3 moveDirRight;
            
            if (grounded && (Slippery >= .1f || angle > maxAngle))
            {
#if true
                // This keeps the player from sticking to the slopes
                var slopeDir = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;

                
                var intoSlope = Vector3.Dot(context.Velocity, normal) < 0f;
                if (intoSlope)
                {
                    context.Velocity = SlideVector(context.Velocity, normal);
                    //slopeDir = Vector3.ProjectOnPlane(context.Velocity, normal).normalized;

                }
#else
                // Only project once when landing or when slope changes drastically
                if (!context.PrevGrounded || Vector3.Dot(context.Normal, normal) < 0.98f)
                {
                    context.Velocity = SlideVector(context.Velocity, normal);
                }
#endif
                
                // controlled sliding force
                //var slopeDir = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;

                var slopeFactor = Mathf.Clamp01((angle - maxAngle) / (90f - maxAngle));
                var speed = context.Velocity.magnitude;
                var speedFactor = Mathf.Clamp01((speed + Mathf.Max(0, -context.Velocity.y) * 1.5f) / 20f);

                var finalSlideForce = Config.Sliding.SlideForce * slopeFactor * (1f + speedFactor) * Slippery;

                //context.Velocity = velocity;
                context.AddForce(slopeDir * finalSlideForce, ForceMode.Acceleration);

                // During sliding, apply force based on velocity direction or world space to avoid killing momentum
                moveDirForward = new Vector3(forward.x, 0, forward.z).normalized;
                moveDirRight = new Vector3(right.x, 0, right.z).normalized;
            }
            else
            {
                if (grounded)
                {
                    //context.Velocity = SlideVector(context.Velocity, normal);
                }
                
                //context.Velocity = velocity;
                // Regular slope-aligned movement
                moveDirForward = Vector3.ProjectOnPlane(forward, normal).normalized;
                moveDirRight = Vector3.ProjectOnPlane(right, normal).normalized;
            }
            
            context.AddForce(moveDirForward * (moveDir.z * Config.Speed * delta * mult * forwardMult));
            context.AddForce(moveDirRight * (moveDir.x * Config.Speed * delta * mult));
            
            if (context.Grounded && !context.Jump && context.MoveDirection.sqrMagnitude < 0.01f)
            {
                // Prevent upward drift on slopes
                var vel = context.Velocity;
                if (vel.y > 0f)
                    context.Velocity = vel.XOZ(Mathf.Min(0f, vel.y)); // clamp Y to zero or downward
            }
            
#else
            var grounded = context.Grounded;
            var normal = context.Normal;
            var moveDir = context.MoveDirection;

            var forward = context.Origin.forward;
            var right = context.Origin.right;

            // --- Build slope-aligned move vectors ---
            var slopeForward = Vector3.ProjectOnPlane(forward, normal).normalized;
            var slopeRight   = Vector3.ProjectOnPlane(right,   normal).normalized;

            // --- Compose desired move direction ---
            var slopeMove = (slopeForward * moveDir.z + slopeRight * moveDir.x);
            if (slopeMove.sqrMagnitude > 0.001f)
                slopeMove.Normalize();

            // --- Maintain some vertical component for realistic up/down slope behavior ---
            if (grounded)
            {
                float angleFactor = Mathf.Clamp01(context.GroundAngle / 45f);
                slopeMove = Vector3.Lerp(slopeMove, Vector3.down, 0.15f * angleFactor).normalized;
            }

            // --- Air control multipliers ---
            var mult = grounded ? 1f : Config.AirMultiplier;
            var forwardMult = grounded ? 1f : Config.AirForwardMultiplier;
            var speed = Config.Speed * delta * mult;

            // --- Apply acceleration ---
            context.AddForce(slopeMove * speed * forwardMult, ForceMode.Acceleration);

            // --- Drag (grounded only) ---
            ApplyDrag(context, GetLookVelocity(context.Origin, context.Velocity), Slippery, delta);

            // --- Clamp max speed ---
            ClampHorizontal(context);

            // --- Direction correction (prevent exceeding max) ---
            DirCorrection(context, GetLookVelocity(context.Origin, context.Velocity));

            // --- Prevent idle upward drift, safe for jump ---
            if (grounded && !context.Jump && !context.IsJumping && moveDir.sqrMagnitude < 0.01f)
            {
                var vel = context.Velocity;
                if (vel.y > 0f)
                    context.Velocity = vel.XOZ(Mathf.Min(0f, vel.y));
            }
#endif
        }

        /// <summary>
        /// Gets the relative velocity from where the player is looking.
        /// </summary>
        /// <returns></returns>
        private Vector3 GetLookVelocity(in Transform origin, in Vector3 velocity) =>
            origin.InverseTransformDirection(velocity);
        
        private void ApplyDrag(in MotionContext context, in Vector3 localVel, in float t, in float delta)
        {
#if false
            if (!context.Grounded) return;
            
            // Calculate drag reduction based on slipperiness
            var finalDrag = Mathf.Lerp(Config.Drag, 0, t);

            // Instead of using only horizontal XOZ space, project drag along ground plane
            var normal = context.Normal;
            var right = Vector3.ProjectOnPlane(context.Origin.right, normal).normalized;
            var forward = Vector3.ProjectOnPlane(context.Origin.forward, normal).normalized;
            
            var dir = context.MoveDirection;
            
            // // Drag
            // if (Mathf.Abs(localVel.x) > Config.MinThreshold && Mathf.Abs(dir.x) < Config.MaxThreshold ||
            //     (localVel.x < -Config.MinThreshold && dir.x > 0) || (localVel.x > Config.MinThreshold && dir.x < 0))
            // {
            //     context.AddForce(context.Origin.right * (Config.Speed * delta * -localVel.x * finalDrag));
            // }
            //
            // if (Mathf.Abs(localVel.z) > Config.MinThreshold && Mathf.Abs(dir.z) < Config.MaxThreshold ||
            //     (localVel.z < -Config.MinThreshold && dir.z > 0) || (localVel.z > Config.MinThreshold && dir.z < 0))
            // {
            //     context.AddForce(context.Origin.forward * (Config.Speed * delta * -localVel.z * finalDrag));
            // }
            
            // DRAG on X axis
            if ((Mathf.Abs(localVel.x) > Config.MinThreshold && Mathf.Abs(dir.x) < Config.MaxThreshold) ||
                (localVel.x < -Config.MinThreshold && dir.x > 0) ||
                (localVel.x > Config.MinThreshold && dir.x < 0))
            {
                context.AddForce(right * (Config.Speed * delta * -localVel.x * finalDrag));
            }

            // DRAG on Z axis
            if ((Mathf.Abs(localVel.z) > Config.MinThreshold && Mathf.Abs(dir.z) < Config.MaxThreshold) ||
                (localVel.z < -Config.MinThreshold && dir.z > 0) ||
                (localVel.z > Config.MinThreshold && dir.z < 0))
            {
                context.AddForce(forward * (Config.Speed * delta * -localVel.z * finalDrag));
            }
#elif false
            if (!context.Grounded) return;

            // Interpret Config.Drag as a per-second damping coefficient.
            // Slippery reduces drag via lerp (your original behavior).
            float baseDrag = Mathf.Lerp(Config.Drag, 0f, t);
            if (baseDrag <= 0f) return;

            // Exponential decay factor: how much of the component to remove this frame.
            float k = 1f - Mathf.Exp(-baseDrag * delta);

            // Build slope-aligned basis so drag acts along the ground plane
            var n = context.Normal;
            var rightOnPlane   = Vector3.ProjectOnPlane(context.Origin.right,   n).normalized;
            var forwardOnPlane = Vector3.ProjectOnPlane(context.Origin.forward, n).normalized;

            var v = context.Velocity;
            float vx = Vector3.Dot(v, rightOnPlane);
            float vz = Vector3.Dot(v, forwardOnPlane);

            var dir = context.MoveDirection;

            // X (strafe) damping: only when not actively accelerating that way, or pushing opposite
            bool dampX =
                (Mathf.Abs(vx) > Config.MinThreshold && Mathf.Abs(dir.x) < Config.MaxThreshold) ||
                (vx < -Config.MinThreshold && dir.x > 0f) ||
                (vx >  Config.MinThreshold && dir.x < 0f);

            if (dampX)
            {
                // Remove a k-fraction of the component along rightOnPlane
                v -= rightOnPlane * (vx * k);
            }

            // Z (forward) damping: same rule
            bool dampZ =
                (Mathf.Abs(vz) > Config.MinThreshold && Mathf.Abs(dir.z) < Config.MaxThreshold) ||
                (vz < -Config.MinThreshold && dir.z > 0f) ||
                (vz >  Config.MinThreshold && dir.z < 0f);

            if (dampZ)
            {
                v -= forwardOnPlane * (vz * k);
            }

            // Keep vertical velocity untouched here (gravity/jump handle Y)
            context.Velocity = v;
#else
            if (!context.Grounded) return;

            var finalDrag = Mathf.Lerp(Config.Drag, 0, t);

            // slope-aligned axes
            var normal = context.Normal;
            var right = Vector3.ProjectOnPlane(context.Origin.right, normal).normalized;
            var forward = Vector3.ProjectOnPlane(context.Origin.forward, normal).normalized;
            var dir = context.MoveDirection;

            // X drag
            if ((Mathf.Abs(localVel.x) > Config.MinThreshold && Mathf.Abs(dir.x) < Config.MaxThreshold) ||
                (localVel.x < -Config.MinThreshold && dir.x > 0) || (localVel.x > Config.MinThreshold && dir.x < 0))
            {
                context.AddForce(right * (Config.Speed * delta * -localVel.x * finalDrag));
            }

            // Z drag
            if ((Mathf.Abs(localVel.z) > Config.MinThreshold && Mathf.Abs(dir.z) < Config.MaxThreshold) ||
                (localVel.z < -Config.MinThreshold && dir.z > 0) || (localVel.z > Config.MinThreshold && dir.z < 0))
            {
                context.AddForce(forward * (Config.Speed * delta * -localVel.z * finalDrag));
            }
#endif
        }
        
        private void ClampHorizontal(in MotionContext context)
        {
            var max = GetMaxSpeed(Config.MaxSpeed);
            var horizontalSpeed = context.Velocity.XOZ();

            //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
            if (horizontalSpeed.magnitude <= max) return;
            
            var clampedVel = horizontalSpeed.normalized * max;
            clampedVel.y = context.Velocity.y;
            
            context.Velocity = clampedVel;
        }
        
        private void DirCorrection(in MotionContext context, in Vector3 localVelocity)
        {
            var max = GetMaxSpeed(Config.MaxSpeed);
            var dir = context.MoveDirection;

            if ((dir.x > 0 && localVelocity.x > max) || (dir.x < 0 && localVelocity.x < -max))
                context.MoveDirection.x = 0;
            if ((dir.z > 0 && localVelocity.z > max) || (dir.z < 0 && localVelocity.z < -max))
                context.MoveDirection.z = 0;
        }
        
        private Vector3 SlideVector(Vector3 velocity, Vector3 normal)
        {
            return Vector3.ProjectOnPlane(velocity, normal);
        }
        
        private float GetMaxSpeed(float maxSpeed) => maxSpeed * (1f + Slippery * Config.Sliding.MaxSpeedModifier);
    }
}