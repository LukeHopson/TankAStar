using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAStar : MonoBehaviour
{
    public Transform player;
    public GameObject shellPrefab;
    public Transform firePoint; // Position from where shells are fired
    public Transform turret; // Reference to the turret transform
    [SerializeField] private Transform myTransform;
    public float moveSpeed = 4f;
    public float shellSpeed = 5f;
    public float rotationSpeed = 8f; // Speed for rotating turret
    public float bodyRotationSpeed = 5f; // Speed for rotating the tank body
    public float detectionRange = 70f; // Range for seeing player, won't go towards player unless in this range
    public float fireRange = 10f; // Range for firing at player
    public float radiusOfSatisfaction = 0.5f;
    public float fireCooldown = 1.5f; // Time between shots

    private List<Vector3> path; // Store the A* path as a list of world positions
    private int currentPathIndex;
    private float lastFireTime;
    private bool pathSelected;

    private int[,] worldData; // Grid data from WorldDecomposer
    private float nodeSize; // Size of the nodes in the WorldDecomposer grid, higher = adjust for more accurate A*
    private int rows, cols;

    private State currentState = State.Idle; // Enemies start in the idle state

    private enum State
    {
        Idle,
        Move,
        Fire
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

        if (player == null)
        {
            currentState = State.Idle; // If player dies, go back to idle
            return;
        }
        Debug.Log($"{gameObject.name} current state: {currentState}");

        AimTurret(); // Always aim the turret at the player

        if (IsPlayerInDetectionRange())
            {
                if (IsPlayerInFireRange() && !IsBulletPathBlocked())
                {
                    currentState = State.Fire;
                }
                else
                {
                    currentState = State.Move; // Move to reposition if firing is not possible
                }
            }
            else
            {
                currentState = State.Idle;
            }

            switch (currentState)
            {
                case State.Fire:
                    Fire();
                    break;
                case State.Move:
                    if (!pathSelected)
                        SetPathToPlayer();
                    MoveToPosition();
                    break;
                case State.Idle:
                    break;
            }
    }

    // Raycast to check if the player is blocked by a wall or something, then dont shoot
    private bool IsBulletPathBlocked()
    {
        Vector3 directionToPlayer = (player.position - firePoint.position).normalized;
        float distanceToPlayer = Vector3.Distance(firePoint.position, player.position);

        Vector3 adjustedStartPoint = firePoint.position - directionToPlayer * 0.2f;
        // Move the firepoint slightly inwards otherwise if the turret is inside of an object it will put the tank into fire mode.

        if (Physics.Raycast(firePoint.position, directionToPlayer, out RaycastHit hit, distanceToPlayer))
        {
            if (hit.collider.transform != player)
            {
                Debug.Log($"{gameObject.name} bullet path blocked by {hit.collider.gameObject.name}");
                return true;
            }
        }

        return false; // No blockage detected
    }

    void AimTurret()
    {
        // Aim turret towards player
        if (player != null)
        {
            Vector3 direction = player.position - turret.position; // Use turret position to aim

            // Look at player
            Quaternion targetRotation = Quaternion.LookRotation(direction);


            targetRotation *= Quaternion.Euler(-90, -90, 0); // Offset otherwise the tank turret is rotated 90 degrees on the X and Y axis

            turret.rotation = Quaternion.Slerp(turret.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void Fire()
    {
        if (Time.time > lastFireTime + fireCooldown)
        {
            GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
            Rigidbody shellRigidbody = shell.GetComponent<Rigidbody>();
            if (shellRigidbody != null && player != null)
            {
                Vector3 directionToPlayer = (player.position - firePoint.position).normalized;
                shellRigidbody.velocity = directionToPlayer * shellSpeed;
            }

            lastFireTime = Time.time;
        }
    }

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

    private void MoveToPosition()
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

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        targetRotation *= Quaternion.Euler(0, -90, 0); // 90 degrees offest, otherwise the tank goes forward sideways
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, bodyRotationSpeed * Time.deltaTime); 

        transform.position += direction * moveSpeed * Time.deltaTime; // Move in the direction of the player

        if (IsPlayerInDetectionRange() && !IsBulletPathBlocked()){
            Fire();
        }
    }

    private bool IsPlayerInDetectionRange()
    {
        return Vector3.Distance(transform.position, player.position) <= detectionRange;
    }

    private bool IsPlayerInFireRange()
    {
        return Vector3.Distance(transform.position, player.position) <= fireRange;
    }

    // A* method
    private List<Vector3> AStar(Vector2Int start, Vector2Int target)
    {
        // Initalize open and clsoed lists
        var openList = new List<NodeRecord>();
        var closedList = new List<NodeRecord>();
        // Starting node
        var startNode = new NodeRecord(start.x, start.y, null, 0, Heuristic(start, target));
        // Goal
        var targetNode = new NodeRecord(target.x, target.y, null, 0, 0);
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            openList.Sort((a, b) => a.EstimatedTotalCost.CompareTo(b.EstimatedTotalCost)); // Sort by EstimatedTotalCost

            var current = openList[0];

            // Get the final path if we reach the target
            if (current.Node.Equals(target))
            {
                return ReconstructPath(current);
            }
            // Move explored node from the open to closed list
            openList.Remove(current);
            closedList.Add(current);

            foreach (var neighbor in GetNeighbors(current.Node))
            {
                // Check if neighbor in closed list already, we dont want to use it if so
                bool isInClosedList = false;
                foreach (var nodeRecord in closedList)
                {
                    if (nodeRecord.Node.Equals(neighbor))
                    {
                        isInClosedList = true;
                        break;
                    }
                }

                if (isInClosedList || !IsWalkable(neighbor))
                    continue;

                int tentativeCost = current.CostSoFar + 1;

                // Check if neighbor is not in open list or has a better path
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

    private bool IsWalkable(Vector2Int node)
    {
        // Return false if the node is not walkable
        if (worldData[node.y, node.x] != 0)
            return false;

        // Account for the enemy size, otherwise the center of the enemy follows a fine path, but the edges clip through objects
        int enemyRadius = 2; // Extra size
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
        return Mathf.Abs(start.x - target.x) + Mathf.Abs(start.y - target.y);
    }

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        if (worldPos == null)
        {
            Debug.LogError("WorldData is null in WorldToGrid!");
        }
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
