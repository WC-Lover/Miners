//using System;
//using UnityEngine;

//public class ObstacleSpawner : MonoBehaviour
//{
//    [SerializeField] ObstacleEvasion obstacleEvasion;
//    GameObject[] obstacles;
//    GameObject obstaclePrefab;
//    Vector3[] spawnPosModifiers;
//    [SerializeField] float obstacleSpeed;
//    [SerializeField] float horizontalDistanceBetweenObstacles;
//    [SerializeField] float verticalDistanceBetweenObstacles;
//    [SerializeField] float obstacleSpawnTime;
//    [SerializeField] float obstacleSpawnTimeMax;
//    private void Start()
//    {
//        obstacles = new GameObject[4];
//        for (int i = 0; i < obstacles.Length; i++)
//        {
//            obstacles[i] = Instantiate(obstaclePrefab, new Vector3(i * 2, 0, i * 2), Quaternion.identity);
//        }
//        obstacleSpawnTime = obstacleSpawnTimeMax;
//        obstacleEvasion.OnArrivedToDestination += ObstacleEvasion_OnArrivedToDestination;
//        spawnPosModifiers = new Vector3[4]{
//            new Vector3(-1, 0, 1),
//            new Vector3(-1, 0, ),
//            new Vector3(0, 0, 0),
//            new Vector3(-1, 0, -1)};
//    }

//    private void ObstacleEvasion_OnArrivedToDestination(object sender, EventArgs e)
//    {
//        for (int i = 0; i < obstacles.Length; i++)
//        {
//            GameObject currentObstacle = obstacles[i];
//            currentObstacle.transform.position = obstacleEvasion.finalDestination + new Vector3(i % 2 > 0 ? 0.5f : -0.5f, 0, i % 2 == 0 ? 0.5f : 0.5f);

//            Rigidbody obstacleRB = currentObstacle.GetComponent<Rigidbody>();
//        }
//    }

//    void Update()
//    {
//        for (int i = 0; i < obstacles.Length; i++)
//        {
//            GameObject obstacleGO = obstacles[i];
//        }
//    }
//}
