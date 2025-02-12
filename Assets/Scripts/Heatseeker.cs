using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Heatseeker : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 5f;  // Speed at which the missile moves
    public float turnSpeed = 2f;  // Speed at which the missile turns to follow the path
    public float detectionRange = 50f; // Distance at which the missile starts homing
    public float radiusOfSatisfaction = 0.5f; // Distance at which missile is considered to have hit the target

    private List<Vector3> path; // A* path list
    private int currentPathIndex;
    private bool pathSelected;

    private int[,] worldData; // Grid data from WorldDecomposer
    private float nodeSize; // Size of the nodes in the WorldDecomposer grid
    private int rows, cols;

    private enum State
    {
        Idle,
        Homing,
        Explode
    }

    private class NodeRecord
    {
        public Vector2Int Node;
        public NodeRecord Connection;
        public int CostSoFar;
        public int EstimatedTotalCost;

        public NodeRecord(int x, int y, NodeRecord connection, int costSoFar, int estimatedTotalCost)
        {
            Node = new Vector2Int(x, y);
            Connection = connection;
            CostSoFar = costSoFar;
            EstimatedTotalCost = estimatedTotalCost;
        }
    }

    void Start()
    {
        var decomposer = FindObjectOfType<WorldDecomposer>();

        if (decomposer != null)
        {
            worldData = decomposer.GetWorldData();
            nodeSize = decomposer.GetNodeSize();
            rows = decomposer.GetRows();
            cols = decomposer.GetCols();
            Debug.Log($"{gameObject.name} successfully initialized world data.");
        }
        else
        {
            Debug.LogError($"{gameObject.name} could not find WorldDecomposer!");
        }
    }

    void Update()
    {
        if (player != null){
            // If the path isn't selected yet, calculate it
            if (!pathSelected)
            {
                SetPathToPlayer();
            }
            else
            {
                MoveAlongPath();
            }
        }
    }

    // Set the path to the player using A*
    private void SetPathToPlayer()
    {
        Vector2Int startGrid = WorldToGrid(transform.position);
        Vector2Int targetGrid = WorldToGrid(player.position);
        Debug.Log($"{gameObject.name}: StartGrid = {startGrid}, TargetGrid = {targetGrid}");

        path = AStar(startGrid, targetGrid);

        if (path != null && path.Count > 0)
        {
            currentPathIndex = 0;
            pathSelected = true;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} failed to calculate path!");
        }
    }

    // Move the missile along the calculated path
    private void MoveAlongPath()
    {
        if (path == null || currentPathIndex >= path.Count)
        {
            pathSelected = false;
            return;
        }

        Vector3 currentTarget = path[currentPathIndex];
        Vector3 direction = currentTarget - transform.position;

        if (direction.magnitude <= radiusOfSatisfaction)
        {
            currentPathIndex++;
            return;
        }

        // Turn towards the target
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        targetRotation *= Quaternion.Euler(0, -90, 0); // 90 degrees offest, otherwise the shell goes forward sideways
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        transform.position += direction.normalized * moveSpeed * Time.deltaTime;

        // If we reach the target, consider the missile exploded or hit
        if (Vector3.Distance(transform.position, player.position) <= radiusOfSatisfaction)
        {
            Explode();
        }
    }

    // Explode when the missile hits the player or reaches the target
    private void Explode()
    {
        // Trigger explosion or damage (this can be customized)
        Debug.Log("Missile Exploded!");

        // Destroy the missile after explosion
        Destroy(gameObject);
    }

    // A* Pathfinding
    private List<Vector3> AStar(Vector2Int start, Vector2Int target)
    {
        var openList = new List<NodeRecord>();
        var closedList = new List<NodeRecord>();

        var startNode = new NodeRecord(start.x, start.y, null, 0, Heuristic(start, target));
        var targetNode = new NodeRecord(target.x, target.y, null, 0, 0);
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            openList.Sort((a, b) => a.EstimatedTotalCost.CompareTo(b.EstimatedTotalCost)); 

            var current = openList[0];

            if (current.Node.Equals(target))
            {
                return ReconstructPath(current);
            }

            openList.Remove(current);
            closedList.Add(current);

            foreach (var neighbor in GetNeighbors(current.Node))
            {
                if (IsInClosedList(closedList, neighbor) || !IsWalkable(neighbor))
                    continue;

                int tentativeCost = current.CostSoFar + 1;
                var neighborRecord = openList.Find(n => n.Node.Equals(neighbor));
                if (neighborRecord == null || tentativeCost < neighborRecord.CostSoFar)
                {
                    neighborRecord = new NodeRecord(neighbor.x, neighbor.y, current, tentativeCost, Heuristic(neighbor, target));

                    if (!openList.Contains(neighborRecord))
                        openList.Add(neighborRecord);
                }
            }
        }

        return new List<Vector3>(); // No path found
    }

    private List<Vector2Int> GetNeighbors(Vector2Int node)
    {
        var neighbors = new List<Vector2Int>();
        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { -1, 1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int nx = node.x + dx[i];
            int ny = node.y + dy[i];
            if (nx >= 0 && nx < cols && ny >= 0 && ny < rows)
            {
                neighbors.Add(new Vector2Int(nx, ny));
            }
        }

        return neighbors;
    }

    private bool IsInClosedList(List<NodeRecord> closedList, Vector2Int neighbor)
    {
        return closedList.Exists(nodeRecord => nodeRecord.Node.Equals(neighbor));
    }

    private bool IsWalkable(Vector2Int node)
    {
        // Return false if the node is not walkable
        if (worldData[node.y, node.x] != 0)
            return false;

        // Account for the enemy size, otherwise the center of the enemy follows a fine path, but the edges clip through objects
        int enemyRadius = 1; // Extra size
        for (int dy = -enemyRadius; dy <= enemyRadius; dy++)
        { // Loop + and - the enemy radius. ex, if the enemy is at (4, 4), bewteen (2, 2) and (6, 6)
            for (int dx = -enemyRadius; dx <= enemyRadius; dx++)
            {
                Vector2Int neighbor = new Vector2Int(node.x + dx, node.y + dy); // Create a new node 
                // Return 'false' (not walkable) if any neignor within the range of the enemy radius are unpathable
                if (worldData[neighbor.y, neighbor.x] != 0)
                    return false;
            }
        }

        return true;
    }

    private int Heuristic(Vector2Int start, Vector2Int target)
    {
        return Mathf.Abs(start.x - target.x) + Mathf.Abs(start.y - target.y); // Manhattan distance
    }

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int col = Mathf.FloorToInt((worldPos.x + 40) / nodeSize);
        int row = Mathf.FloorToInt((worldPos.z + 40) / nodeSize);
        return new Vector2Int(col, row);
    }

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        float x = (gridPos.x * nodeSize) - 40;
        float z = (gridPos.y * nodeSize) - 40;
        return new Vector3(x, 0, z);
    }

    private List<Vector3> ReconstructPath(NodeRecord endNode)
    {
        var path = new List<Vector3>();
        var current = endNode;

        while (current != null)
        {
            path.Add(GridToWorld(current.Node));
            current = current.Connection;
        }

        path.Reverse();
        return path;
    }
}
