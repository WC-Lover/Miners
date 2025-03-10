using UnityEngine;

namespace Assets.Scripts.Unit
{
    [RequireComponent(typeof(Rigidbody))]
    public class UnitMovement : MonoBehaviour
    {
        private Unit unit;
        private Rigidbody rb;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private float delay;
        [SerializeField] private float delayMax;
        private Vector3 lastDirection;

        private void Awake()
        {
            unit = GetComponent<Unit>();
            rb = GetComponent<Rigidbody>();
            delay = delayMax;
        }

        public bool MoveTo(InteractionTarget interactionTarget)
        {
            if (delay <= 0)
            {
                delay = delayMax;
                Vector3 direction = (interactionTarget.Position - transform.position).normalized;
                Vector3 avoidanceDirection = HandleObstacleAvoidance(interactionTarget);
                //if (avoidanceDirection != Vector3.one) direction = avoidanceDirection;

                Vector3 adaptedDirection = (direction + avoidanceDirection).normalized;
                lastDirection = adaptedDirection;
            }
            delay -= Time.deltaTime;

            RotateTowards(lastDirection);

            //rb.MovePosition(transform.position + direction * (unit.Config.Speed * Time.fixedDeltaTime)); // change direction to forward?
            rb.MovePosition(transform.position + lastDirection * (unit.Config.Speed * Time.fixedDeltaTime)); // change direction to forward?

            return HasArrived(interactionTarget.Position, interactionTarget.InteractionDistance);
        }

        #region Helper Methods

        private Vector3 HandleObstacleAvoidance(InteractionTarget interactionTarget)
        {
            Vector3 avoidance = Vector3.zero;

            for (int i = 0; i < unit.Config.ObstacleDetectionAngleSegmentsAmount; i++)
            {
                // Angle from most right segment to most left
                float angle = -unit.Config.ObstacleDetectionAngle / 2
                    + (unit.Config.ObstacleDetectionAngle / (unit.Config.ObstacleDetectionAngleSegmentsAmount - 1)) * i;
                Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * transform.forward;

                Debug.DrawRay(transform.position, rayDirection, Color.blue);
                if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, unit.Config.ObstacleDetectionDistance, obstacleMask))
                {
                    bool isTarget = IsCurrentTarget(hit.collider, interactionTarget);

                    if (isTarget) continue;

                    float forceMultiplier = 1f - (hit.distance / unit.Config.ObstacleDetectionDistance);
                    avoidance += hit.normal * (unit.Config.ObstacleEvasionForce * forceMultiplier);
                }
            }

            avoidance.y += -avoidance.y;
            return avoidance.normalized;
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
            if (direction == Vector3.zero) return;
            
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(targetRotation);
        }

        private bool HasArrived(Vector3 target, float interactionDistance)
        {
            //Debug.Log($"Distance / interactionDistance -> {Vector3.Distance(transform.position, target)} / {interactionDistance}");
            return Vector3.Distance(transform.position, target) < interactionDistance;
        }

        #endregion
    }
}
