using UnityEngine;

/// <summary>
/// Marks the player so enemies can find it when they spawn.
/// </summary>
public class PlayerTarget : MonoBehaviour
{
    public static Transform instance;   // Transform the zombies chase

    void Awake() => instance = transform;

    void OnDestroy()
    {
        if (instance == transform)
            instance = null;
    }
}
