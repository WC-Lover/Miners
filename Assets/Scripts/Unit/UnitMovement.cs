using UnityEngine;

namespace Assets.Scripts.Unit
{
    [RequireComponent(typeof(Rigidbody))]
    public class UnitMovement : MonoBehaviour
    {
        private Unit unit;
        private Rigidbody rb;

        private void Awake()
        {
            unit = GetComponent<Unit>();
            rb = GetComponent<Rigidbody>();
        }

        public bool MoveTo(InteractionTarget interactionTarget)
        {
            Vector3 direction = (interactionTarget.Position - transform.position).normalized;
            Vector3 avoidanceDirection = HandleObstacleAvoidance(interactionTarget);
            if (avoidanceDirection != Vector3.one) direction = avoidanceDirection;

            RotateTowards(direction);

            rb.MovePosition(transform.position + direction * (unit.Config.Speed * Time.fixedDeltaTime)); // change direction to forward?

            return HasArrived(interactionTarget.Position, interactionTarget.InteractionDistance);
        }

        #region Helper Methods

        private Vector3 HandleObstacleAvoidance(InteractionTarget interactionTarget)
        {
            Vector3 direction = transform.forward;
            bool obstacleDetected = false;
            float leftWeight = 0f;
            float rightWeight = 0f;

            float[] rayAngles = { -30f, -15f, 0f, 15f, 30f }; // Wider detection
            float[] rayDistances = { 1f, 0.8f, 0.6f, 0.8f, 1f }; // Different distances per angle

            for (int i = 0; i < rayAngles.Length; i++)
            {
                float angle = rayAngles[i];
                float distance = rayDistances[i] * unit.Config.ObstacleDetectionDistance;
                Vector3 rayDir = Quaternion.Euler(0, angle, 0) * transform.forward;
                Vector3 rayStart = transform.position + transform.up * 0.1f;

                Debug.DrawRay(rayStart, rayDir, Color.red);

                if (Physics.Raycast(rayStart, rayDir, out RaycastHit hit, distance))
                {
                    if (IsCurrentTarget(hit.collider, interactionTarget)) continue;

                    obstacleDetected = true;
                    float weight = 1 - (hit.distance / distance);

                    // Determine steering direction based on ray angle
                    if (angle < 0)
                    {
                        // Left side obstacle - accumulate right steering
                        rightWeight += weight;
                    }
                    else if (angle > 0)
                    {
                        // Right side obstacle - accumulate left steering
                        leftWeight += weight;
                    }
                    else
                    {
                        // Center obstacle - check both sides
                        rightWeight += weight;
                        leftWeight += weight;
                    }
                }
            }

            if (!obstacleDetected) return Vector3.one;

            // Calculate final avoidance direction
            if (leftWeight > rightWeight)
            {
                // Steer left with intensity based on weight difference
                direction = Vector3.Lerp(transform.forward, -transform.right,
                    Mathf.Clamp01(leftWeight - rightWeight)).normalized;
            }
            else if (rightWeight >= leftWeight)
            {
                // Steer right with intensity based on weight difference
                direction = Vector3.Lerp(transform.forward, transform.right,
                    Mathf.Clamp01(rightWeight - leftWeight)).normalized;
            }

            Debug.DrawRay(transform.position, direction * 2f, Color.magenta);

            return direction;
        }

        private bool IsCurrentTarget(Collider detectedCollider, InteractionTarget interactionTarget)
        {
            if (interactionTarget == null) return false;
            // ReturningToBase specific case, as it doesn't have currentTarget.Building to check
            if (interactionTarget.Position + (Vector3.up * 0.25f) == detectedCollider.transform.position) return true;
            // Check if detected collider belongs to current target
            return (interactionTarget.Unit != null && detectedCollider.transform.IsChildOf(interactionTarget.Unit.transform)) ||
                   (interactionTarget.Building != null && detectedCollider.transform.IsChildOf(interactionTarget.Building.transform)) ||
                   (interactionTarget.Resource != null && detectedCollider.transform.IsChildOf(interactionTarget.Resource.transform));
        }

        private void RotateTowards(Vector3 direction)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(targetRotation);
        }

        private bool HasArrived(Vector3 target, float interactionDistance)
        {
            return Vector3.Distance(transform.position, target) < interactionDistance;
        }

        #endregion
    }
}
