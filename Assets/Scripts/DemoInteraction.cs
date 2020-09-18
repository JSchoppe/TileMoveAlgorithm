using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the demo components in the scene.
/// </summary>
public sealed class DemoInteraction : MonoBehaviour
{
    [SerializeField] private Button showStepsButton = null;
    [SerializeField] private Button newLayoutButton = null;
    [SerializeField] private Text moveCountText = null;
    [SerializeField] private Slider moveCountSlider = null;

    [SerializeField] private TileArena arena = null;
    [SerializeField] private TileActorControl tileActor = null;

    private bool isShowingSteps = false;

    // Use start to bind all the events.
    private void Start()
    {
        moveCountSlider.onValueChanged.AddListener(OnSliderChanged);
        OnSliderChanged(moveCountSlider.value);
        newLayoutButton.onClick.AddListener(OnRandomizeLayout);
        showStepsButton.onClick.AddListener(OnShowSteps);
        StartCoroutine(LateInitialize());
    }
    // Avoid problems with script execution order.
    private IEnumerator LateInitialize()
    {
        yield return new WaitForEndOfFrame();
        OnRandomizeLayout();
    }

    private void OnSliderChanged(float newValue)
    {
        uint roundedValue = (uint)newValue;
        moveCountText.text = roundedValue + " Move Range";
        tileActor.MoveRange = roundedValue;
    }

    private void OnRandomizeLayout()
    {
        arena.Randomize(Random.Range(0.4f, 0.6f));
    }

    private void OnShowSteps()
    {
        if(!isShowingSteps)
            StartCoroutine(AnimateSteps(0.1f));
    }

    private IEnumerator AnimateSteps(float secondsPerStep)
    {
        Vector2Int[][] steps = GetAlgorithmSteps(tileActor.TileLocation, (int)tileActor.MoveRange);
        isShowingSteps = true;
        for(int i = 0; i < steps.Length; i++)
        {
            arena.DrawPath(steps[i]);
            yield return new WaitForSeconds(secondsPerStep);
        }
        isShowingSteps = false;
        arena.ClearPath();
    }

    // This is a modified version of the algorithm in TileArena.
    public Vector2Int[][] GetAlgorithmSteps(Vector2Int start, int range)
    {
        bool[,] isWallTile = arena.WallTileStates;
        uint width = arena.Width;
        uint height = arena.Height;

        List<Vector2Int[]> steps = new List<Vector2Int[]>();

        Dictionary<int, Dictionary<int, int>> tileBestMovesLeft =
            new Dictionary<int, Dictionary<int, int>>();

        for (int x = start.x - range; x <= start.x + range; x++)
        {
            tileBestMovesLeft.Add(x, new Dictionary<int, int>());
            for (int y = start.y - range; y <= start.y + range; y++)
                tileBestMovesLeft[x][y] = -1;
        }

        Stack<Vector2Int> currentPath = new Stack<Vector2Int>();
        int movesLeft = range + 1;

        Traverse(start);
        void Traverse(Vector2Int tile)
        {
            currentPath.Push(tile);
            movesLeft--;

            tileBestMovesLeft[tile.x][tile.y] = movesLeft;
            steps.Add(currentPath.ToArray());

            if (movesLeft > 0)
            {
                Vector2Int left = tile + Vector2Int.left;
                Vector2Int right = tile + Vector2Int.right;
                Vector2Int up = tile + Vector2Int.up;
                Vector2Int down = tile + Vector2Int.down;

                if (left.x >= 0 && !isWallTile[left.x, left.y])
                    if (tileBestMovesLeft[left.x][left.y] < movesLeft - 1)
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

            currentPath.Pop();
            movesLeft++;
        }

        return steps.ToArray();
    }
}
