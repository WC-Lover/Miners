//using System;
//using UnityEngine;

//public class ObstacleEvasion : MonoBehaviour
//{

//    public EventHandler OnArrivedToDestination;
//    public Vector3[] destinations;
//    public int destinationCounter;
//    [SerializeField] public Vector3 finalDestination;
//    [SerializeField] public Vector3 currentDestination;
//    [SerializeField] public float speed;
//    [SerializeField] public float obstacleDetectionRange;
//    [SerializeField] public float obstacleRayDetectionRange;
//    [SerializeField] public float findObstacleTimer;
//    [SerializeField] public float findObstacleTimerMax;
//    [SerializeField] public float numberOfRays;
//    Rigidbody rb;

//    /*
//     * 1) Detect on CollisionStay then optimize movement
//     * 2) Detect with Physics.OverlapSphereNonAlloc
//     * 3) Detect with RayCast
//     */

//    private void Awake()
//    {
//        rb = GetComponent<Rigidbody>();
//        currentDestination = finalDestination;
//        destinationCounter = 0;
//    }

//    private void OnCollisionStay(Collision collision)
//    {
//        // 1
//    }

//    private void FixedUpdate()
//    {
//        // 2
//        var deltaPosition = currentDestination;

//        if (findObstacleTimer <= 0)
//        {
//            findObstacleTimer = findObstacleTimerMax;
//            for (int i = 0; i < numberOfRays; i++)
//            {
//                Quaternion rotation = this.transform.rotation;
//                Quaternion rotationModified = Quaternion.AngleAxis((i / (float)numberOfRays) * 60 - 30, this.transform.up);
//                Vector3 direction = rotation * rotationModified * Vector3.forward * 3;
//                Ray ray = new Ray(transform.position, direction);
//                RaycastHit raycastHit;
//                if (Physics.Raycast(ray, out raycastHit, obstacleRayDetectionRange))
//                {
//                    deltaPosition -= (1.0f / numberOfRays) * speed * direction;
//                }
//                else
//                {
//                    deltaPosition += (1.0f / numberOfRays) * speed * direction;
//                }
//            }
//        }
//        else
//        {
//            findObstacleTimer -= Time.deltaTime;
//        }
//        rb.rotation = Quaternion.LookRotation(currentDestination - transform.position);
//        rb.MovePosition(transform.position + deltaPosition * Time.fixedDeltaTime);

//        if (currentDestination != finalDestination)
//        {
//            if (Vector3.Distance(transform.position, deltaPosition) < 0.1f)
//            {
//                currentDestination = finalDestination;
//            }
//        }

//        if (Vector3.Distance(transform.position, finalDestination) < 0.2f)
//        {
//            finalDestination = destinations[destinationCounter];
//            destinationCounter++;
//            OnArrivedToDestination?.Invoke(this, EventArgs.Empty);
//        }
//        //var moveDirection = (currentDestination - transform.position) + new Vector3(0, 1, 0);
//        //Debug.Log(moveDirection);
//        //rb.MovePosition(transform.position + speed * Time.fixedDeltaTime * moveDirection.normalized);

//        // 3
//    }

//    private void OnDrawGizmos()
//    {
//        for (int i = 0; i < numberOfRays; i++)
//        {
//            var rotation = this.transform.rotation;
//            var rotationModified = Quaternion.AngleAxis((i / (float)numberOfRays) * 60 - 30, this.transform.up);
//            var direction = rotation * rotationModified * Vector3.forward * obstacleRayDetectionRange;
//            Gizmos.DrawRay(this.transform.position, direction);
//        }
//    }
//}
