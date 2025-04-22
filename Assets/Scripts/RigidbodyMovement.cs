using System;
using UnityEngine;

public static class RigidbodyMovement
{
    [Serializable]
    public struct MovementSettings
    {
        public float MaxSpeed;
        public float Acceleration;
        public float Deceleration;
        public AnimationCurve AccelerationFactorFromDot;
        //public float MaxAccelForce;
        //public AnimationCurve MaxAccelerationForceFactorFromDot;
        public Vector3 ForceScale;
    }
    
    public static void MoveRigidbody(Rigidbody rb, Vector3 moveInput, MovementSettings settings)
    {
        if(moveInput.magnitude > 1f)
            moveInput.Normalize();
        
        // 1. Determine Target Velocity
        // Calculate the velocity we *want* to achieve based on input and max speed.
        // We primarily control movement on the XZ plane.
        Vector3 targetVelocity = moveInput * settings.MaxSpeed;

        // Optional: Preserve existing vertical velocity (e.g., for jumping/gravity)
        // If you are handling jumping/gravity elsewhere, you might want to do this:
        // targetVelocity.y = rb.velocity.y;
        // Or, if you want this script to ONLY affect XZ movement:
        // targetVelocity.y = 0; // Explicitly set target Y velocity to 0

        // 2. Calculate Current Velocity (on the controlled plane)
        Vector3 currentVelocity = rb.linearVelocity;
        // If only controlling XZ, ignore the current Y velocity in our calculation
        // currentVelocity.y = 0; // Uncomment if targetVelocity.y is also 0

        // 3. Calculate Velocity Difference
        // Find the gap between where we are and where we want to be.
        Vector3 velocityDifference = targetVelocity - currentVelocity;

        // 4. Determine Acceleration Rate
        // Use deceleration value if specified and there's no input, otherwise use acceleration.
        bool isDecelerating = moveInput == Vector3.zero && settings.Deceleration > 0f;
        var actualAcceleration = isDecelerating ? settings.Deceleration : settings.Acceleration;
        
        // Multiply the acceleration by the factor from the animation curve based on the dot product of the current and target velocity.
        var dot = Vector3.Dot(currentVelocity.normalized, targetVelocity.normalized);
        actualAcceleration *= settings.AccelerationFactorFromDot.Evaluate(dot);

        // 5. Calculate Acceleration Needed (and Clamp)
        // Calculate the acceleration required *this frame* to bridge the velocity difference.
        Vector3 accelerationRequired = velocityDifference / Time.fixedDeltaTime;

        // Clamp the magnitude of the required acceleration to our defined max acceleration rate.
        // This ensures we don't exceed the desired acceleration/deceleration.
        Vector3 actualAccelerationVector = Vector3.ClampMagnitude(accelerationRequired, actualAcceleration);

        // 6. Apply Force
        // Use ForceMode.Acceleration: applies the acceleration directly, ignoring the Rigidbody's mass.
        // This makes our 'acceleration' and 'deceleration' values directly control the rate of speed change.
        // *** SELF NOTE ***
        // I multiply with force scale to allow for different force scales on different axes.
        rb.AddForce(Vector3.Scale(actualAccelerationVector, settings.ForceScale), ForceMode.Acceleration);

        // --- Optional Debugging ---
        // Draw rays in the Scene view to visualize vectors
        // Debug.DrawRay(transform.position, targetVelocity, Color.green);    // Target velocity
        // Debug.DrawRay(transform.position, rb.velocity, Color.blue);      // Current velocity
        // Debug.DrawRay(transform.position, actualAccelerationVector, Color.red); // Applied acceleration
    }

    public static void MoveToRigidbody(Rigidbody rb, Vector3 position, MovementSettings settings)
    {
        Vector3 currentPosition = rb.position;
        Vector3 targetPosition = position;

        Vector3 directionToTarget = targetPosition - currentPosition;
        float distanceToTarget = directionToTarget.magnitude;
    
        // Calculate current speed in the direction of the target
        float currentSpeed = Vector3.Dot(rb.linearVelocity, directionToTarget.normalized);
    
        // Calculate the stopping distance needed for current speed
        // Using the formula: stopping_distance = v²/(2*a)
        float deceleration = settings.Deceleration > 0 ? settings.Deceleration : settings.Acceleration;
        
        // Calculate the dot of the current velocity and the direction to the target
        float dot = Vector3.Dot(rb.linearVelocity.normalized, directionToTarget.normalized);
        // Use this to apm our deceleration based on the accel dot curve
        deceleration *= settings.AccelerationFactorFromDot.Evaluate(dot);
        
        float stoppingDistance = (currentSpeed * currentSpeed) / (2 * deceleration);

        // --- Stop Condition ---
        // Stop if we're close enough AND can stop safely within the remaining distance
        if (distanceToTarget <= 0.01f || (distanceToTarget <= stoppingDistance && currentSpeed > 0))
        {
            // If very close, perform hard stop
            if (distanceToTarget <= 0.01f)
            {
                rb.linearVelocity = Vector3.zero;
                return;
            }
        
            // Otherwise, apply appropriate braking force to stop naturally
            Vector3 brakeDirection = -rb.linearVelocity.normalized;
            Vector3 brakeAcceleration = brakeDirection * deceleration;
            var force = brakeAcceleration * rb.mass;
            rb.AddForce(Vector3.Scale(force, settings.ForceScale), ForceMode.Force);
            return;
        }

        // --- Calculate Speed Limits ---

        // 1. Calculate the maximum speed we can have *now* to be able to stop in time
        // Use the kinematic equation: v_f^2 = v_i^2 + 2ad  => 0 = v_i^2 - 2 * maxAcceleration * distance
        // So, v_i = sqrt(2 * maxAcceleration * distance)
        // If using separate deceleration: float deceleration = maxDeceleration;
        float maxSpeedToStop = Mathf.Sqrt(2 * deceleration * distanceToTarget);

        // 2. Determine the actual target speed for this frame
        // We want to go as fast as possible (maxSpeed), but no faster than needed to stop (maxSpeedToStop)
        float targetSpeed = Mathf.Min(settings.MaxSpeed, maxSpeedToStop);

        // --- Calculate Desired Velocity ---
        Vector3 desiredVelocity = directionToTarget.normalized * targetSpeed;

        // --- Calculate Acceleration and Apply Force ---
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 velocityChange = desiredVelocity - currentVelocity;

        // Calculate the acceleration required to reach the desired velocity in one physics step
        Vector3 requiredAcceleration = velocityChange / Time.fixedDeltaTime;

        // Clamp the acceleration magnitude so it doesn't exceed maxAcceleration
        Vector3 actualAcceleration = Vector3.ClampMagnitude(requiredAcceleration, settings.Acceleration);

        // Apply the force: F = m * a
        var force2 = actualAcceleration * rb.mass;
        rb.AddForce(Vector3.Scale(Vector3.Scale(force2, settings.ForceScale), settings.ForceScale), ForceMode.Force);
    }
    
    // Rotates the rigidbody towards direction based on a rotation speed
    public static void RotateRigidbody(Rigidbody rb, Vector3 dir, float rotationSpeed)
    {
        dir.Normalize();
        if(dir == Vector3.zero)
            return;
        
        Quaternion toRotation = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion rotation = Quaternion.RotateTowards(rb.rotation, toRotation, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(rotation);
    }
}