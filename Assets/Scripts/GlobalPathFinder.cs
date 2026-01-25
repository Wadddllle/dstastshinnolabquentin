using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class GlobalPathfinder : MonoBehaviour
{
    [Header("References")]
    public Transform playerHead;
    public Transform targetDestination;
    public GameObject arrowIndicator;

    [Header("Map Settings")]
    public LayerMask obstacleLayer;     // CRITICAL: Set to "Spatial Mesh" or "Default"
    public float nodeSize = 0.3f;       // 30cm grid resolution
    public float padding = 3.0f;        // Scan boundary padding

    [Header("Height Range (The 'Map Generator' Logic)")]
    public float floorBuffer = 0.2f;    // Start check 20cm above floor (ignore floor)
    public float ceilingClip = 1.6f;    // Stop check at 1.6m (head height)

    [Header("Arrow Settings")]
    public float arrowDist = 1.0f;
    public float lookAhead = 1.5f;

    // Internal
    private LineRenderer _line;
    private List<Vector3> _currentPath = new List<Vector3>();
    private bool _hasPath = false;

    // Simple Node
    private class Node
    {
        public int x, y;
        public bool isWalkable;
        public Vector3 worldPos;
        public Node parent;
        public float gCost, hCost;
        public float fCost { get { return gCost + hCost; } }

        public Node(int x, int y, Vector3 pos, bool walkable)
        {
            this.x = x; this.y = y; this.worldPos = pos; this.isWalkable = walkable;
        }
    }

    void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.positionCount = 0;

        // Safety Visuals Setup
        _line.startWidth = 0.05f;
        _line.endWidth = 0.05f;
        if (_line.sharedMaterial == null)
        {
            Material defaultMat = new Material(Shader.Find("Sprites/Default"));
            defaultMat.color = Color.cyan;
            _line.material = defaultMat;
        }

        if (arrowIndicator) arrowIndicator.SetActive(false);
    }

    void Update()
    {
        if (_hasPath && arrowIndicator != null && _currentPath.Count > 1)
        {
            UpdateArrow();
        }
    }

    public void GeneratePath()
    {
        if (playerHead == null || targetDestination == null) return;

        Vector3 startPos = playerHead.position;
        Vector3 endPos = targetDestination.position;

        // 1. Define Bounding Box
        float minX = Mathf.Min(startPos.x, endPos.x) - padding;
        float maxX = Mathf.Max(startPos.x, endPos.x) + padding;
        float minZ = Mathf.Min(startPos.z, endPos.z) - padding;
        float maxZ = Mathf.Max(startPos.z, endPos.z) + padding;

        int gridX = Mathf.CeilToInt((maxX - minX) / nodeSize);
        int gridY = Mathf.CeilToInt((maxZ - minZ) / nodeSize);

        // Safety Cap
        if (gridX * gridY > 25000)
        {
            DrawStraightLine(startPos, endPos);
            return;
        }

        Node[,] grid = new Node[gridX, gridY];
        Node startNode = null;
        Node targetNode = null;

        // 2. Build Grid with CAPSULE CHECK
        for (int x = 0; x < gridX; x++)
        {
            for (int y = 0; y < gridY; y++)
            {
                float wx = minX + (x * nodeSize) + (nodeSize / 2);
                float wz = minZ + (y * nodeSize) + (nodeSize / 2);

                // --- THE FIX: CAPSULE CHECK ---
                // We define the bottom and top of the cylinder
                Vector3 pointBottom = new Vector3(wx, floorBuffer, wz); // e.g. 0.2m
                Vector3 pointTop = new Vector3(wx, ceilingClip, wz); // e.g. 1.6m

                // Radius is slightly smaller than node to avoid overlap
                float checkRadius = nodeSize * 0.45f;

                // If ANYTHING is inside this cylinder, it's a wall.
                bool isBlocked = Physics.CheckCapsule(pointBottom, pointTop, checkRadius, obstacleLayer);

                // Create Node
                // We assume floor is at y=0 for the line drawing
                Vector3 nodePos = new Vector3(wx, 0.05f, wz);
                grid[x, y] = new Node(x, y, nodePos, !isBlocked);

                // Find closest Start/End
                if (startNode == null || Vector3.Distance(nodePos, startPos) < Vector3.Distance(startNode.worldPos, startPos))
                    startNode = grid[x, y];
                if (targetNode == null || Vector3.Distance(nodePos, endPos) < Vector3.Distance(targetNode.worldPos, endPos))
                    targetNode = grid[x, y];
            }
        }

        // 3. Run A*
        if (startNode != null && targetNode != null)
        {
            startNode.isWalkable = true;
            targetNode.isWalkable = true;
            FindPath(grid, startNode, targetNode, gridX, gridY);
        }
    }

    void FindPath(Node[,] grid, Node startNode, Node targetNode, int w, int h)
    {
        List<Node> openSet = new List<Node> { startNode };
        HashSet<Node> closedSet = new HashSet<Node>();

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                    currentNode = openSet[i];
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                RetracePath(startNode, targetNode);
                return;
            }

            foreach (Node neighbour in GetNeighbours(grid, currentNode, w, h))
            {
                if (!neighbour.isWalkable || closedSet.Contains(neighbour)) continue;

                float newMovementCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newMovementCost < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;
                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
                }
            }
        }

        DrawStraightLine(playerHead.position, targetDestination.position);
    }

    void RetracePath(Node start, Node end)
    {
        _currentPath.Clear();
        Node curr = end;
        while (curr != start)
        {
            _currentPath.Add(curr.worldPos);
            curr = curr.parent;
        }
        _currentPath.Add(start.worldPos);
        _currentPath.Reverse();

        // Optional: Simple Smoothing (remove every other point for cleaner lines)
        // You can skip this if you like the jagged "technical" look
        if (_currentPath.Count > 2)
        {
            List<Vector3> smooth = new List<Vector3>();
            smooth.Add(_currentPath[0]);
            for (int i = 1; i < _currentPath.Count - 1; i += 2) smooth.Add(_currentPath[i]);
            smooth.Add(_currentPath[_currentPath.Count - 1]);
            _currentPath = smooth;
        }

        _line.positionCount = _currentPath.Count;
        _line.SetPositions(_currentPath.ToArray());

        _hasPath = true;
        if (arrowIndicator) arrowIndicator.SetActive(true);
    }

    void DrawStraightLine(Vector3 start, Vector3 end)
    {
        _currentPath.Clear();
        _currentPath.Add(start);
        _currentPath.Add(end);
        _line.positionCount = 2;
        _line.SetPositions(_currentPath.ToArray());
        _hasPath = true;
        if (arrowIndicator) arrowIndicator.SetActive(true);
    }

    void UpdateArrow()
    {
        Vector3 flatForward = playerHead.forward;
        flatForward.y = 0;
        flatForward.Normalize();
        arrowIndicator.transform.position = playerHead.position + (flatForward * arrowDist) + (Vector3.down * 0.2f);

        float closestDist = float.MaxValue;
        int closestIndex = 0;
        for (int i = 0; i < _currentPath.Count; i++)
        {
            float d = Vector3.Distance(playerHead.position, _currentPath[i]);
            if (d < closestDist) { closestDist = d; closestIndex = i; }
        }

        int lookIndex = Mathf.Min(closestIndex + 3, _currentPath.Count - 1);
        Vector3 lookTarget = _currentPath[lookIndex];
        lookTarget.y = arrowIndicator.transform.position.y;

        arrowIndicator.transform.LookAt(lookTarget);
        arrowIndicator.transform.rotation *= Quaternion.Euler(90, 0, 0);
    }

    List<Node> GetNeighbours(Node[,] grid, Node node, int w, int h)
    {
        List<Node> list = new List<Node>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                int cx = node.x + x, cy = node.y + y;
                if (cx >= 0 && cx < w && cy >= 0 && cy < h) list.Add(grid[cx, cy]);
            }
        }
        return list;
    }
    float GetDistance(Node a, Node b)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return (dx > dy) ? 14 * dy + 10 * (dx - dy) : 14 * dx + 10 * (dy - dx);
    }
}