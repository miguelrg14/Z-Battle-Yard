using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    public GameControls playerInput;

    void Start()
    {
        playerInput = GameInput.CreateControls(this); // Initialize Player input to use new input system

        if (!playerInput.asset.enabled)     playerInput.Enable(); // Enable input system!
    }

    void OnDisable()
    {
        playerInput.Disable();
    }
    void OnDestroy()
    {
        playerInput.Disable();
    }
}
