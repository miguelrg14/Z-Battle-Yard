using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player flashlight, toggled with the Lantern action.
/// The GameObject starts active so Awake can subscribe to the input, then hides itself.
/// </summary>
public class Lantern : MonoBehaviour
{
    GameControls playerInput;
    bool lanternOn = false;

    void Awake()
    {
        playerInput = GameInput.CreateControls(this);
        playerInput.OnFoot.Lantern.performed += OnLanternPressed;

        gameObject.SetActive(false);    // Start the game with the lantern off
    }

    // The input stays enabled while the lantern is hidden. Disabling it in OnDisable (as before)
    // meant the lantern could never be switched on again.
    void OnDestroy()
    {
        if (playerInput == null)
            return;

        playerInput.OnFoot.Lantern.performed -= OnLanternPressed;
        playerInput.Disable();
        playerInput.Dispose();
    }

    void OnLanternPressed(InputAction.CallbackContext context) => ToggleLantern();

    void ToggleLantern()
    {
        lanternOn = !lanternOn;
        gameObject.SetActive(lanternOn);
    }
}
