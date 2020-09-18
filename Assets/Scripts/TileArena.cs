using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a tile based arena that actors can navigate.
/// </summary>
public sealed class TileArena : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Handles the drawing of path objects.")]
    [SerializeField] private PathRenderer pathRenderer = null;
    [Tooltip("Object to duplicate at the wall coordinates.")]
    [SerializeField] private GameObject wallPrefab = null;
    [Header("Arena Attributes")]
    [Tooltip("Number of x-axis tiles in the arena.")]
    [SerializeField] private uint width = 10;
    [Tooltip("Number of y-axis tiles in the arena.")]
    [SerializeField] private uint height = 10;

    // Store the terrain state and instantiated objects.
    private bool[,] isWallTile;
    private List<GameObject> wallObjects;

    // Initialize collections.
    private void Start()
    {
        isWallTile = new bool[width, height];
        wallObjects = new List<GameObject>();
    }

    /// <summary>
    /// Called every time the arena layout changes.
    /// </summary>
    public event Action OnLayoutChange;

    /// <summary>
    /// 2D array containing flags for whether walls exist at each coordinate.
    /// </summary>
    public bool[,] WallTileStates
    { 
        get { return (bool[,])isWallTile.Clone(); }
    }
    /// <summary>
    /// Number of x-axis tiles in the arena.
    /// </summary>
    public uint Width { get { return width; } }
    /// <summary>
    /// Number of y-axis tiles in the arena.
    /// </summary>
    public uint Height { get { return height; } }

    /// <summary>
    /// Finds all possible movement paths with the given range.
    /// </summary>
    /// <param name="start">The starting coordinate.</param>
    /// <param name="range">The maximum number of steps that can be taken.</param>
    /// <returns>
    /// An array of coordinate paths, where in each array the first
    /// coordinate is the path destination and the following coordinates
    /// lead back to the start position.
    /// </returns>
    public Vector2Int[][] GetAllMovementPaths(Vector2Int start, int range)
    {
        // Check for invalid arguments.
        if (range < 0)
            throw new ArgumentException("Range must be greater than or equal to zero.");
        if (start.x < 0 || start.x >= width)
            throw new ArgumentException("Starting point x coordinate was not within the tile arena bounds.");
        if (start.y < 0 || start.y >= height)
            throw new ArgumentException("Starting point y coordinate was not within the tile arena bounds.");

        // Create a dictionary to store found paths using their coordinate values.
        Dictionary<int, Dictionary<int, Vector2Int[]>> foundPaths =
            new Dictionary<int, Dictionary<int, Vector2Int[]>>();

        // Create another dictionary to store the minimum moves to reach each tile.
        // This helps prevent retreading of exhuasted branches.
        Dictionary<int, Dictionary<int, int>> tileBestMovesLeft =
            new Dictionary<int, Dictionary<int, int>>();

        // Handle initialization of dictionaries and move values
        // for all possible reachable tiles. This helps avoid
        // null checks in the following recursive function.
        for (int x = start.x - range; x <= start.x + range; x++)
        {
            tileBestMovesLeft.Add(x, new Dictionary<int, int>());
            foundPaths.Add(x, new Dictionary<int, Vector2Int[]>());
            for (int y = start.y - range; y <= start.y + range; y++)
                // We can check for -1 later to see if a tile wasn't reached.
                tileBestMovesLeft[x][y] = -1;
        }

        // Keep a record of the current path.
        Stack<Vector2Int> currentPath = new Stack<Vector2Int>();
        // Keep track of the current path efficiency.
        // We add one at the start because the first "move"
        // to our current position costs one move.
        int movesLeft = range + 1;

        // Start the recursive function.
        Traverse(start);
        void Traverse(Vector2Int tile)
        {
            // Update the current movement states.
            currentPath.Push(tile);
            movesLeft--;

            // Update the record for this tile.
            tileBestMovesLeft[tile.x][tile.y] = movesLeft;
            foundPaths[tile.x][tile.y] = currentPath.ToArray();

            // Look for further travel options if we still have moves left.
            if (movesLeft > 0)
            {
                // Get all possible movement directions.
                Vector2Int left = tile + Vector2Int.left;
                Vector2Int right = tile + Vector2Int.right;
                Vector2Int up = tile + Vector2Int.up;
                Vector2Int down = tile + Vector2Int.down;

                // Foreach direction we check if it is within the map
                // and if it is not a tile.
                if (left.x >= 0 && !isWallTile[left.x, left.y])
                    // Next we check if this tile has already been reached
                    // with a fewer number of moves.
                    if (tileBestMovesLeft[left.x][left.y] < movesLeft - 1)
                        // If not we explore this branch.
                        Traverse(left);

                if (right.x < width && !isWallTile[right.x, right.y])
                    if (tileBestMovesLeft[right.x][right.y] < movesLeft - 1)
                        Traverse(right);

                if (up.y < height && !isWallTile[up.x, up.y])
                    if (tileBestMovesLeft[up.x][up.y] < movesLeft - 1)
                        Traverse(up);

                if (down.y >= 0 && !isWallTile[down.x, down.y])
                    if (tileBestMovesLeft[down.x][down.y] < movesLeft - 1)
                        Traverse(down);
            }

            // Revert the movement state as we step back a tile
            // to exhaust the remaining branches.
            currentPath.Pop();
            movesLeft++;
        }

        // Once the recursion algorithm completes
        // retrieve and return all of the found paths.
        List<Vector2Int[]> output = new List<Vector2Int[]>();
        foreach(Dictionary<int, Vector2Int[]> dictionary in foundPaths.Values)
            foreach (Vector2Int[] path in dictionary.Values)
                output.Add(path);
        return output.ToArray();
    }

    /// <summary>
    /// Finds a random tile on the map that does not have a wall.
    /// </summary>
    /// <returns>The coordinate of the located tile. Returns null if there are no free tiles.</returns>
    public Vector2Int? GetRandomFreeTile()
    {
        // Create a pool from all tiles that are not walls.
        List<Vector2Int> pool = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (!isWallTile[x, y])
                    pool.Add(new Vector2Int(x, y));
        // If there are no free tiles return null.
        if(pool.Count == 0) { return null; }

        // Pull a random free tile from the pool.
        int index = UnityEngine.Random.Range(0, pool.Count);
        return pool[index];
    }

    /// <summary>
    /// Generates new random walls based on the given perlin noise threshold.
    /// </summary>
    /// <param name="threshold">Ratio between 0-1. A smaller threshold means it is easier for walls to spawn.</param>
    public void Randomize(float threshold)
    {
        // Start by removing any existing wall entities.
        foreach (GameObject wall in wallObjects)
            Destroy(wall);

        // Randomly assign wall state for each tile in the arena.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                isWallTile[x, y] = (Mathf.PerlinNoise(Time.time + x, Time.time + y) > threshold);
                if(isWallTile[x, y])
                {
                    // Instantiate the new wall and set its properties.
                    GameObject wall = Instantiate(wallPrefab);
                    wall.transform.parent = transform;
                    wall.transform.localPosition = new Vector2(x, y);
                    wallObjects.Add(wall);
                }
            }
        }
        // Notify listeners of the layout change.
        OnLayoutChange?.Invoke();
    }

    // TODO: Add the possibility to draw multiple paths.
    // Right now this just routes to the one existing
    // path.
    public void DrawPath(Vector2Int[] path)
    {
        pathRenderer.DrawPath(path);
    }
    public void ClearPath()
    {
        pathRenderer.ClearPath();
    }
}
