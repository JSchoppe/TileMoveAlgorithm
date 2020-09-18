using UnityEngine;

/// <summary>
/// Delegate for GameObjects to communicate with scripts.
/// </summary>
/// <param name="sender">The GameObject whose event was triggered.</param>
public delegate void GameObjectListener(GameObject sender);

/// <summary>
/// Exposes the mouse events to other behaviours.
/// </summary>
public sealed class OnMouseBroadcaster : MonoBehaviour
{
    /// <summary>
    /// Called every time the mouse is clicked down on this object.
    /// </summary>
    public event GameObjectListener OnDown;
    /// <summary>
    /// Called every time the mouse is released on this object.
    /// </summary>
    public event GameObjectListener OnUp;
    /// <summary>
    /// Called every time the mouse begins hovering on this object.
    /// </summary>
    public event GameObjectListener OnEnter;
    /// <summary>
    /// Called every time the mouse stops hovering on this object.
    /// </summary>
    public event GameObjectListener OnExit;

    private void OnMouseDown() { OnDown?.Invoke(gameObject); }
    private void OnMouseUp() { OnUp?.Invoke(gameObject); }
    private void OnMouseEnter() { OnEnter?.Invoke(gameObject); }
    private void OnMouseExit() { OnExit?.Invoke(gameObject); }
}
