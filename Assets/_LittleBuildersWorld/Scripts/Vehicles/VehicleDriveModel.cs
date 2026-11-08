using System;
using UnityEngine;

namespace LittleBuildersWorld.Vehicles
{
    /// <summary>Player intent for one frame. Values are already clamped: steer -1..1, throttle/brake 0..1.</summary>
    public struct DriveInput
    {
        public float Steer;
        public float Throttle;
        public float Brake;

        public static readonly DriveInput None = default;

        public DriveInput(float steer, float throttle, float brake)
        {
            Steer = Mathf.Clamp(steer, -1f, 1f);
            Throttle = Mathf.Clamp01(throttle);
            Brake = Mathf.Clamp01(brake);
        }

        public bool IsIdle => Mathf.Abs(Steer) < 0.01f && Throttle < 0.01f && Brake < 0.01f;
    }

    /// <summary>Plain handling numbers copied out of a <see cref="VehicleProfile"/> so the model can be unit tested.</summary>
    [Serializable]
    public struct DriveTuning
    {
        [Tooltip("Top forward speed, m/s. Keep low: this is a toy playground.")] public float MaxForwardSpeed;
        [Tooltip("Top reverse speed, m/s.")] public float MaxReverseSpeed;
        [Tooltip("Forward acceleration, m/s².")] public float Acceleration;
        [Tooltip("Reverse acceleration, m/s².")] public float ReverseAcceleration;
        [Tooltip("Deceleration while braking, m/s².")] public float BrakeDeceleration;
        [Tooltip("Deceleration when no pedal is pressed (automatic gentle braking), m/s².")] public float CoastDeceleration;
        [Tooltip("Yaw rate at full steer, degrees/s.")] public float TurnRate;
        [Tooltip("Speed at which full turn rate is reached, m/s. Below this wheeled vehicles turn less.")] public float FullTurnSpeed;
        [Tooltip("Tracked vehicles can pivot on the spot. Fraction of TurnRate available at standstill.")] public float TurnInPlace;
        [Tooltip("How quickly steering follows the input, 1/s. Lower is smoother.")] public float SteerResponse;

        public static DriveTuning Default => new DriveTuning
        {
            MaxForwardSpeed = 7f,
            MaxReverseSpeed = 3f,
            Acceleration = 3.5f,
            ReverseAcceleration = 2.5f,
            BrakeDeceleration = 9f,
            CoastDeceleration = 4f,
            TurnRate = 70f,
            FullTurnSpeed = 2.5f,
            TurnInPlace = 0f,
            SteerResponse = 5f
        };
    }

    /// <summary>
    /// Deterministic arcade drive model: speed along the vehicle's forward axis plus a yaw rate.
    /// No wheel physics. The Rigidbody controller applies the result; tests drive this class directly.
    /// </summary>
    public sealed class VehicleDriveModel
    {
        private const float StopEpsilon = 0.05f;

        /// <summary>Signed speed along forward, m/s (negative = reversing).</summary>
        public float Speed { get; private set; }

        /// <summary>Smoothed steering, -1..1.</summary>
        public float Steer { get; private set; }

        /// <summary>Yaw rate this step, degrees/s.</summary>
        public float YawRate { get; private set; }

        public DriveTuning Tuning;

        public VehicleDriveModel(DriveTuning tuning) { Tuning = tuning; }

        public void Reset()
        {
            Speed = 0f;
            Steer = 0f;
            YawRate = 0f;
        }

        /// <summary>
        /// When the body is blocked (wall, another vehicle) the measured speed is lower than ours.
        /// Pull the model toward reality so pushing into a wall does not build up hidden speed.
        /// </summary>
        public void ReconcileMeasuredSpeed(float measuredForwardSpeed)
        {
            if (Mathf.Abs(measuredForwardSpeed) < Mathf.Abs(Speed) - 0.5f) Speed = Mathf.Lerp(Speed, measuredForwardSpeed, 0.5f);
        }

        public void Step(DriveInput input, float dt)
        {
            if (dt <= 0f) return;
            DriveTuning t = Tuning;

            float target;
            float rate;
            if (input.Throttle > 0.01f)
            {
                target = t.MaxForwardSpeed * input.Throttle;
                rate = Speed < -StopEpsilon ? t.BrakeDeceleration : t.Acceleration;
            }
            else if (input.Brake > 0.01f)
            {
                if (Speed > StopEpsilon)
                {
                    target = 0f;
                    rate = t.BrakeDeceleration;
                }
                else
                {
                    target = -t.MaxReverseSpeed * input.Brake;
                    rate = t.ReverseAcceleration;
                }
            }
            else
            {
                target = 0f;
                rate = t.CoastDeceleration;
            }

            // Slowing down toward a lower target is always at least as quick as coasting.
            if (Mathf.Abs(target) < Mathf.Abs(Speed) || Mathf.Sign(target) != Mathf.Sign(Speed))
                rate = Mathf.Max(rate, t.CoastDeceleration);

            Speed = Mathf.MoveTowards(Speed, target, rate * dt);
            if (target == 0f && Mathf.Abs(Speed) < StopEpsilon) Speed = 0f;

            Steer = Mathf.MoveTowards(Steer, input.Steer, Mathf.Max(0.01f, t.SteerResponse) * dt);
            if (Mathf.Abs(input.Steer) < 0.01f && Mathf.Abs(Steer) < 0.02f) Steer = 0f;

            float speedFactor = t.FullTurnSpeed > 0.01f ? Mathf.Clamp01(Mathf.Abs(Speed) / t.FullTurnSpeed) : 1f;
            float turnFactor = Mathf.Max(speedFactor, Mathf.Clamp01(t.TurnInPlace));
            // Reversing turns the nose the opposite way, like a real vehicle (the tail follows the arrow).
            float direction = Speed < -StopEpsilon ? -1f : 1f;
            YawRate = Steer * t.TurnRate * turnFactor * direction;
        }
    }
}
