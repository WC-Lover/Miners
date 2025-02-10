using System;
using System.Collections.Generic;
using UnityEngine;

public class AStarPathFinder : MonoBehaviour
{
    public class Node
    {
        public int x, z;
        public float gCost, hCost, fCost;
        public Node parent;

        public Node(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        public void CalculateFCost()
        {
            fCost = gCost + hCost;
        }
    }

    public float speed = 5f;
    List<Vector3> waypoints;
    bool[,] grid;

    private void Awake()
    {
        grid = new bool[10, 10]; // true = walkable, false = obstacle
    }

    void Update()
    {
        if (waypoints != null && waypoints.Count > 0)
        {
            Vector3 target = waypoints[0];
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) < 0.1f)
            {
                waypoints.RemoveAt(0);
            }
        }
    }

    public List<Node> FindPath(Node startNode, Node targetNode)
    {
        List<Node> openList = new List<Node> { startNode };
        HashSet<Node> closedList = new HashSet<Node>();

        while (openList.Count > 0)
        {
            Node currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < currentNode.fCost ||
                    (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            if (currentNode.x == targetNode.x && currentNode.z == targetNode.z)
            {
                return RetracePath(startNode, currentNode);
            }

            foreach (Node neighbor in GetNeighbors(currentNode))
            {
                if (!grid[neighbor.x, neighbor.z] || closedList.Contains(neighbor)) continue;

                float newGCost = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newGCost < neighbor.gCost || !openList.Contains(neighbor))
                {
                    neighbor.gCost = newGCost;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.CalculateFCost();
                    neighbor.parent = currentNode;

                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }
        }

        return null; // No path found
    }

    private List<Node> GetNeighbors(Node currentNode)
    {
        List<Node> neighbours = new List<Node>();
        for (int i = -1; i < 2; i += 2)
        {
            for (int j = -1; j < 2; j += 2)
            {
                if (grid[i, j]) neighbours.Add(new Node(currentNode.x + i, currentNode.z + j));
            }
        }

        return neighbours;
    }

    float GetDistance(Node a, Node b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dz = Mathf.Abs(a.z - b.z);
        return dx + dz; // Manhattan (4-direction movement)
                        // return Mathf.Sqrt(dx*dx + dz*dz); // Euclidean (8-direction)
    }

    List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }
}
