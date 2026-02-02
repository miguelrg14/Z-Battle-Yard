using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class DestroyTimer : MonoBehaviour
{
    [SerializeField] private float Timer = 3.0f;

    void Start()
    {
        Destroy(gameObject, Timer);
    }
}
