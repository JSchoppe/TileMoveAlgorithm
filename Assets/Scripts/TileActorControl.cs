using System;
using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the movement of an actor along the grid system.
/// </summary>
public sealed class TileActorControl : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("The tile arena that this player is bound to.")]
    [SerializeField] private TileArena arena = null;
    [Tooltip("A prefab to render at the location of highlighted tiles.")]
    [SerializeField] private GameObject tileHighlight = null;
    [Header("Gameplay Attributes")]
    [Tooltip("Controls the initial location of the actor.")]
    [SerializeField] private Vector2Int tileLocation = Vector2Int.zero;
    [Tooltip("Controls how many tiles the actor can move in a turn.")]
    [SerializeField] private uint moveRange;
    [Header("Aesthetic Attributes")]
    [Tooltip("Controls the rate at which the actor moves.")]
    [SerializeField] private float secondsPerTile = 1;

    // Store the entities used for effects.
    private Transform[] highlightedTiles;
    // Store the current available movement options.
    private Vector2Int[][] availablePaths;

    private void Start()
    {
        // Instantiate entities and position the actor.
        PropagateObjects();
        transform.parent = arena.transform;
        transform.localPosition = (Vector2)tileLocation;
        // Listen for when the arena changes form.
        arena.OnLayoutChange += OnArenaLayoutChange;
    }

    /// <summary>
    /// Controls how many tiles this player can move in one turn.
    /// </summary>
    public uint MoveRange
    {
        get { return moveRange; }
        set
        {
            moveRange = value;
            // Changing the move range changes the number of entities needed.
            PropagateObjects();
        }
    }
    /// <summary>
    /// The location of this actor in their tile arena.
    /// </summary>
    public Vector2Int TileLocation
    {
        get { return tileLocation; }
        set
        {
            tileLocation = value;
            transform.localPosition = (Vector2)tileLocation;
        }
    }

    private void PropagateObjects()
    {
        // Clear previous objects.
        if(highlightedTiles != null)
            foreach(Transform transform in highlightedTiles)
                Destroy(transform.gameObject);

        // Calculate the maximum number required for each entity.
        int maxTileCount = 1;
        for (int ring = 1; ring <= moveRange; ring++)
            maxTileCount += ring * 4;
        int maxArrowCount = (int)moveRange + 1;

        // Create the scene instances used to render the effects.
        highlightedTiles = new Transform[maxTileCount];
        for(int i = 0; i < maxTileCount; i++)
        {
            highlightedTiles[i] = Instantiate(tileHighlight).transform;
            highlightedTiles[i].parent = arena.transform;
            highlightedTiles[i].gameObject.SetActive(false);
            // Bind to the mouse events of the highlighted tile.
            OnMouseBroadcaster broadcaster =
                highlightedTiles[i].gameObject.AddComponent<OnMouseBroadcaster>();
            broadcaster.OnEnter += OnHighlightTileEnter;
            broadcaster.OnDown += OnHighlightTileDown;
        }
    }

    // Handle what happens when the arena layout changes.
    private void OnArenaLayoutChange()
    {
        // Check to see if the new layout has any room for the actor.
        Vector2Int? freeTile = arena.GetRandomFreeTile();
        if(freeTile != null)
            TileLocation = (Vector2Int)freeTile;
        
        // Clear up any previous state that may have been remaining.
        if(isAnimating)
        {
            isAnimating = false;
            StopCoroutine(animationRoutine);
        }
        arena.ClearPath();
        foreach (Transform tile in highlightedTiles)
            tile.gameObject.SetActive(false);
    }

    // Interaction state; TODO this state management feels sloppy.
    private bool isAnimating = false;
    private IEnumerator animationRoutine;
    // Implement mouse interaction logic.
    private void OnMouseDown()
    {
        if(!isAnimating)
        {
            // Retieve the possible movement paths.
            availablePaths = arena.GetAllMovementPaths(tileLocation, (int)moveRange);
            // Highlight the possible destination tiles.
            for(int i = 0; i < highlightedTiles.Length; i++)
            {
                if(i < availablePaths.Length)
                {
                    highlightedTiles[i].localPosition = (Vector2)availablePaths[i][0];
                    highlightedTiles[i].gameObject.SetActive(true);
                }
                else
                    highlightedTiles[i].gameObject.SetActive(false);
            }
        }
    }
    // Mouse events for the highlight tiles.
    private void OnHighlightTileEnter(GameObject tile)
    {
        if(!isAnimating)
        {
            Vector2Int[] path = GetPath(tile.transform.localPosition);
            arena.DrawPath(path);
        }
    }
    private void OnHighlightTileDown(GameObject tile)
    {
        if(!isAnimating)
        {
            // Hide all of the highlighted tiles.
            foreach (Transform transform in highlightedTiles)
                transform.gameObject.SetActive(false);

            Vector2Int[] path = GetPath(tile.transform.localPosition);
            // Reverse the path so the start is at the actor location.
            animationRoutine = TravelPath(path.Reverse().ToArray());
            StartCoroutine(animationRoutine);
        }
    }
    private Vector2Int[] GetPath(Vector2 pathEnd)
    {
        // Search and retrieve the path associated with a location.
        int x = (int)pathEnd.x;
        int y = (int)pathEnd.y;
        foreach(Vector2Int[] path in availablePaths)
            if (path[0].x == x && path[0].y == y)
                return path;
        throw new ArgumentException("Requested a path that has not been determined.");
    }
    // Implement the animation for traveling along the path.
    private IEnumerator TravelPath(Vector2Int[] path)
    {
        isAnimating = true;

        int ranges = path.Length - 1;
        float rangeLength = 1f / ranges;
        // Initialize the timing for the animation.
        float duration = ranges * secondsPerTile;
        float startTime = Time.time;

        // Begin the animation loop, only if there is a distance to animate over.
        if(ranges > 0)
        {
            while(true)
            {
                // Get our position in the animation in this call.
                float interpolant = (Time.time - startTime) / duration;
                // If the animation has completed, finish the coroutine.
                if(interpolant >= 1) { break; }

                // Localize the range and interpolant to our target segment.
                int subRange = Mathf.FloorToInt(interpolant * ranges);
                float subInterpolant = (interpolant % rangeLength) / rangeLength;
                // Apply the animation.
                transform.localPosition = Vector2.Lerp(path[subRange], path[subRange + 1], subInterpolant);
                // Wait for next Update call.
                yield return null;
            }
        }

        // Finalize the animation.
        tileLocation = path[path.Length - 1];
        transform.localPosition = (Vector2)tileLocation;
        arena.ClearPath();
        isAnimating = false;
    }
}
