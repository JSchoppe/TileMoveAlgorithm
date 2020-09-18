using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws a series sprites along a grid path.
/// </summary>
public sealed class PathRenderer : MonoBehaviour
{
    [Tooltip("Provides the sprites to render the path.")]
    [SerializeField] private PathPalette palette = null;

    private List<SpriteRenderer> spriteTiles;

    private void Start()
    {
        spriteTiles = new List<SpriteRenderer>();
    }
    private void OnDestroy()
    {
        // Clear out all gameobjects used for this effect.
        foreach (SpriteRenderer renderer in spriteTiles)
            Destroy(renderer.gameObject);
    }

    /// <summary>
    /// Draws a series of sprites along the given path (previous path is removed).
    /// </summary>
    /// <param name="path">The path of coordinates to draw along.</param>
    public void DrawPath(Vector2Int[] path)
    {
        // Ensure there are enough gameobjects to draw the entire path.
        while(path.Length > spriteTiles.Count)
        {
            GameObject newObj = new GameObject();
            newObj.transform.parent = transform;
            SpriteRenderer renderer = newObj.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Effects";
            renderer.sortingOrder = 1;
            spriteTiles.Add(renderer);
            
        }
        // Hide any previous path rendering.
        ClearPath();

        // Draw the arrow along the path.
        for (int i = 0; i < path.Length; i++)
        {
            spriteTiles[i].gameObject.SetActive(true);
            spriteTiles[i].transform.localPosition = (Vector2)path[i];
            // TODO this is pretty ugly, maybe somehow rethink how the palette maps to directions.
            if (i == 0)
            {
                if (path.Length == 1)
                {
                    spriteTiles[i].sprite = palette.dotCap;
                }
                else
                {
                    if (path[0].x > path[1].x)
                        spriteTiles[i].sprite = palette.endCapRight;
                    else if (path[0].x < path[1].x)
                        spriteTiles[i].sprite = palette.endCapLeft;
                    else if (path[0].y > path[1].y)
                        spriteTiles[i].sprite = palette.endCapUp;
                    else
                        spriteTiles[i].sprite = palette.endCapDown;
                }
            }
            else if (i == path.Length - 1)
            {
                if (path[i - 1].x > path[i].x)
                    spriteTiles[i].sprite = palette.startCapRight;
                else if (path[i - 1].x < path[i].x)
                    spriteTiles[i].sprite = palette.startCapLeft;
                else if (path[i - 1].y > path[i].y)
                    spriteTiles[i].sprite = palette.startCapUp;
                else
                    spriteTiles[i].sprite = palette.startCapDown;
            }
            else
            {
                if (path[i - 1].x > path[i].x)
                {
                    if (path[i].x > path[i + 1].x)
                        spriteTiles[i].sprite = palette.horizontalSegment;
                    else if (path[i].y > path[i + 1].y)
                        spriteTiles[i].sprite = palette.rightToDownSegment;
                    else
                        spriteTiles[i].sprite = palette.rightToUpSegment;
                }
                else if (path[i - 1].x < path[i].x)
                {
                    if (path[i].x < path[i + 1].x)
                        spriteTiles[i].sprite = palette.horizontalSegment;
                    else if (path[i].y > path[i + 1].y)
                        spriteTiles[i].sprite = palette.leftToDownSegment;
                    else
                        spriteTiles[i].sprite = palette.leftToUpSegment;
                }
                else if (path[i - 1].y > path[i].y)
                {
                    if (path[i].y > path[i + 1].y)
                        spriteTiles[i].sprite = palette.verticalSegment;
                    else if (path[i].x > path[i + 1].x)
                        spriteTiles[i].sprite = palette.leftToUpSegment;
                    else
                        spriteTiles[i].sprite = palette.rightToUpSegment;
                }
                else
                {
                    if (path[i].y < path[i + 1].y)
                        spriteTiles[i].sprite = palette.verticalSegment;
                    else if (path[i].x > path[i + 1].x)
                        spriteTiles[i].sprite = palette.leftToDownSegment;
                    else
                        spriteTiles[i].sprite = palette.rightToDownSegment;
                }
            }
        }
    }
    /// <summary>
    /// Removes the current rendered path.
    /// </summary>
    public void ClearPath()
    {
        foreach (SpriteRenderer renderer in spriteTiles)
            renderer.gameObject.SetActive(false);
    }
}
