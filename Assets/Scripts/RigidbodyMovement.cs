using System;
using UnityEngine;

public static class RigidbodyMovement
{
    [Serializable]
    public struct MovementSettings
    {
        public float MaxSpeed;
        public float Acceleration;
        public AnimationCurve AccelerationFactorFromDot;
        public float MaxAccelForce;
        public AnimationCurve MaxAccelerationForceFactorFromDot;
        public Vector3 ForceScale;

        public MovementSettings(
            float maxSpeed,
            float acceleration,
            AnimationCurve accelerationFactorFromDot,
            float maxAccelForce,
            AnimationCurve maxAccelerationForceFactorFromDot,
            Vector3 forceScale)
        {
            MaxSpeed = maxSpeed;
            Acceleration = acceleration;
            AccelerationFactorFromDot = accelerationFactorFromDot;
            MaxAccelForce = maxAccelForce;
            MaxAccelerationForceFactorFromDot = maxAccelerationForceFactorFromDot;
            ForceScale = forceScale;
        }
    }
    
    
    // Credit to https://youtu.be/qdskE8PJy6Q?si=hSfY9B58DNkoP-Yl
    // Modified
    public static Vector3 MoveRigidbody(Rigidbody rb, Vector3 input, Vector3 sGoalVel, MovementSettings settings)
    {
        Vector3 move = input;

        if (move.magnitude > 1f)
        {
            move.Normalize();
        }

        // do dotproduct of input to current goal velocity to apply acceleration based on dot to direction (makes sharp turns better).
        Vector3 unitVel = sGoalVel.normalized;

        float velDot = Vector3.Dot(move, unitVel);
        float accel = settings.Acceleration * settings.AccelerationFactorFromDot.Evaluate(velDot);
        Vector3 goalVel = move * settings.MaxSpeed;

        // lerp goal velocity towards new calculated goal velocity.
        sGoalVel = Vector3.MoveTowards(sGoalVel, goalVel, accel * Time.fixedDeltaTime);

        // calculate needed acceleration to reach goal velocity in a single fixed update.
        Vector3 neededAccel = (sGoalVel - rb.linearVelocity) / Time.fixedDeltaTime;

        // clamp the needed acceleration to max possible acceleration.
        float maxAccel = settings.MaxAccelForce * settings.MaxAccelerationForceFactorFromDot.Evaluate(velDot);
        neededAccel = Vector3.ClampMagnitude(neededAccel, maxAccel);

        rb.AddForce(Vector3.Scale(neededAccel * rb.mass, settings.ForceScale));

        // return the stored goal velocity
        return sGoalVel;
    }
    
    // Rotates the rigidbody towards direction based on a rotation speed
    public static void RotateRigidbody(Rigidbody rb, Vector3 dir, float rotationSpeed)
    {
        Quaternion toRotation = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion rotation = Quaternion.RotateTowards(rb.rotation, toRotation, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(rotation);
    }
}