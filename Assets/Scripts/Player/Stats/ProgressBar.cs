using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    int baseValue;
    int maxValue;

    Slider fill;
    Image fillImage;

    #region - Awake -

    void Awake()
    {
        GetReferences();
    }

    #endregion

    public void SetValues(int _baseValue, int _maxValue)
    {
        baseValue = _baseValue;
        maxValue = _maxValue;

        CalculateFillAmount();
    }

    void CalculateFillAmount()
    {
        float fillAmount = (float)baseValue / (float)maxValue;
        fill.value = fillAmount;
    }

    #region - Reference && Initialize -

    void GetReferences()
    {
        fill = GetComponentInChildren<Slider>();
        fillImage = GetComponentInChildren<Image>();
    }
    void InitVariables()
    {
    }

    #endregion
}
