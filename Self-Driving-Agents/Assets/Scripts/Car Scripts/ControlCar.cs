using UnityEngine;

public class ControlCar : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Rigidbody carRb;
    // Assign: Rigidbody on the root "Car" object (recommended)

    [SerializeField] private Transform baseCarCenter;
    // Assign: "Base Car" child transform at the center of the car body

    [Header("Wheel Points (Raycast Origins)")]
    [SerializeField] private Transform[] wheelPoints;
    // Recommended index mapping
    // [0] Front Right Tire
    // [1] Front Left Tire
    // [2] Back Right Tire
    // [3] Back Left Tire

    [Header("Starting Position")]
    [SerializeField] private Vector3 startingPosition = new Vector3(0f, 2f, 0f);
    // Assign/Tune: Starting world position for the car at scene start

    [Header("Ground / Raycast Settings")]
    [SerializeField] private LayerMask groundMask = ~0;
    // Assign: Ground/road layers or leave default = everything

    [SerializeField] private float rayLength = 1.5f;
    // Tune: How far downward each wheel ray checks for ground

    [SerializeField] private float restDistance = 0.8f;
    // Tune: Desired wheel-point-to-ground distance at rest

    [Header("Suspension (Y Force Only)")]
    [SerializeField] private float springStrength = 18000f;
    // Tune: Upward spring force strength

    [SerializeField] private float damperStrength = 2200f;
    // Tune: Damping force to reduce bouncing

    [Header("Debug")]
    [SerializeField] private bool drawDebug = true;
    // Toggle: Draws debug vectors in Scene view during Play mode

    [SerializeField] private float debugVectorScale = 0.00005f;
    // Tune: Scales the visualized force vector so it is visible

    private Vector3 currentYForceVector = Vector3.zero;

    // #==============================
    // # Function Name:
    // # Start
    // #Function Parameters
    // # None
    // # Function Purpose:
    // # Set the car to a known starting position when the scene begins.
    // #
    // # Input: Serialized starting position and car references
    // # Output: Car transform moved to starting position, rigidbody reset if assigned
    // #==============================
    // Function()
    private void Start()
    {
        //Set the car to its starting position at the beginning of the scene.
        //transform.position = startingPosition;

        if (carRb != null)
        {
            //Cleear Velocity at start of scene.
            carRb.linearVelocity = Vector3.zero;
            carRb.angularVelocity = Vector3.zero;
        }

        // Optional setup validation (helpful while building):
        //if (wheelPoints == null || wheelPoints.Length == 0)
        //{
        //    Debug.LogWarning("ControlCar: wheelPoints array is empty. Assign 4 wheel point transforms.");
        //}
        //else if (wheelPoints.Length != 4)
        //{
        //    Debug.LogWarning("ControlCar: Expected 4 wheel points. Current count = " + wheelPoints.Length);
        //}
    }

    // #==============================
    // # Function Name:
    // # FixedUpdate
    // #Function Parameters
    // # None
    // # Function Purpose:
    // # Runtime physics update loop. Calls Y_Vector_Calc, which now applies 4 separate
    // # suspension forces at the wheel positions, and returns the summed Y force for debug.
    // #
    // # Input: Current rigidbody state and scene physics state
    // # Output: Per-wheel suspension forces applied to the car each physics step
    // #==============================
    // Function()
    private void FixedUpdate()
    {
        // Goal:
        // - Recalculate the current Y suspension force every physics frame.
        // - Y_Vector_Calc now applies force at each wheel position (4 separate forces).
        // - Keep the returned total force only for debug visualization.

        if (carRb == null)
        {
            return;
        }

        currentYForceVector = Y_Vector_Calc(
            carRb,
            wheelPoints,
            groundMask,
            rayLength,
            restDistance,
            springStrength,
            damperStrength
        );

        // IMPORTANT:
        // We DO NOT call carRb.AddForce(currentYForceVector) anymore.
        // Per-wheel forces are now applied inside Y_Vector_Calc() using AddForceAtPosition().

        if (drawDebug)
        {
            Vector3 debugOrigin = (baseCarCenter != null) ? baseCarCenter.position : transform.position;

            // 3 = blue (per your color code request)
            VisualizeVector(currentYForceVector * debugVectorScale, 3, debugOrigin);
        }
    }

    // #==============================
    // # Function Name:
    // # Y_Vector_Calc
    // #Function Parameters
    // # bodyRb, wheels, groundLayerMask, maxRayDistance, desiredRestDistance, springK, damperC
    // # Function Purpose:
    // # Raycast downward from each wheel point, calculate spring + damper Y force,
    // # apply each wheel's force at that wheel position, and return the summed Y force vector.
    // # Suspension force is clamped so it only pushes upward (gravity handles downward motion).
    // #
    // # Input: Wheel point transforms, raycast settings, rigidbody point velocity
    // # Output: Combined suspension force vector (Y-axis only, mainly for debug)
    // #==============================
    // Function()
    private Vector3 Y_Vector_Calc(
        Rigidbody bodyRb,
        Transform[] wheels,
        LayerMask groundLayerMask,
        float maxRayDistance,
        float desiredRestDistance,
        float springK,
        float damperC
    )
    {
        // Goal:
        // - For each wheel point:
        //   1) Raycast downward to find the ground
        //   2) Compute compression using desiredRestDistance - hit.distance
        //   3) Clamp compression so spring only works when compressed (no downward pull)
        //   4) Compute spring force from compression
        //   5) Compute damping force from wheel-point vertical velocity
        //   6) Clamp final wheel force so suspension only pushes upward (>= 0)
        //   7) Apply that wheel's Y force at the wheel position (AddForceAtPosition)
        //   8) Add that wheel's Y force to the total (for debug return)
        // - Return total Y force as Vector3.up * totalYForce

        if (bodyRb == null || wheels == null || wheels.Length == 0)
        {
            return Vector3.zero;
        }

        float totalYForce = 0f;

        for (int i = 0; i < wheels.Length; i++)
        {
            Transform wheel = wheels[i];

            if (wheel == null)
            {
                continue;
            }

            if (Physics.Raycast(
                wheel.position,
                Vector3.down,
                out RaycastHit hit,
                maxRayDistance,
                groundLayerMask,
                QueryTriggerInteraction.Ignore))
            {
                // Compression only counts when the suspension is compressed.
                // If hit.distance > desiredRestDistance, compression would be negative,
                // but we clamp to 0 so the spring does NOT pull downward.
                float compression = Mathf.Max(0f, desiredRestDistance - hit.distance);

                // Vertical speed at this wheel point (used for damping to reduce bounce).
                float pointYVelocity = bodyRb.GetPointVelocity(wheel.position).y;

                // Spring force pushes up when compressed.
                float springForce = compression * springK;

                // Damper force opposes vertical motion.
                float damperForce = -pointYVelocity * damperC;

                // Combine spring + damper.
                float wheelYForce = springForce + damperForce;

                // Clamp so suspension only pushes upward.
                // Gravity should be the only thing pulling downward.
                wheelYForce = Mathf.Max(0f, wheelYForce);

                // Apply this wheel's upward force at the wheel position (creates pitch/roll naturally).
                Vector3 wheelForceVector = Vector3.up * wheelYForce;
                bodyRb.AddForceAtPosition(wheelForceVector, wheel.position, ForceMode.Force);

                // Add this wheel's Y contribution to the total (for debug visualization).
                totalYForce += wheelYForce;
            }
        }

        // Return Y-only force vector (mainly used for debug visualization)
        return Vector3.up * totalYForce;
    }

    // #==============================
    // # Function Name:
    // # VisualizeVector
    // #Function Parameters
    // # vectorToDraw, colorCode, origin
    // # Function Purpose:
    // # Draw a debug vector from a supplied origin using a simple color selector.
    // #
    // # Input: Any vector, a color code (1=green, 2=red, 3=blue), and a start position
    // # Output: Scene view debug line rendered during Play mode
    // #==============================
    // Function()
    private void VisualizeVector(Vector3 vectorToDraw, int colorCode, Vector3 origin)
    {
        // Goal:
        // - Choose a color using the provided number code.
        // - Draw the vector from the supplied origin.

        Color drawColor = Color.white;

        if (colorCode == 1)
        {
            drawColor = Color.green;
        }
        else if (colorCode == 2)
        {
            drawColor = Color.red;
        }
        else if (colorCode == 3)
        {
            drawColor = Color.blue;
        }

        Debug.DrawRay(origin, vectorToDraw, drawColor);
    }
}