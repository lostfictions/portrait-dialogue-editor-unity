using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using UnityEngine.UI;

public class InvertText : MonoBehaviour
{
    Text text;
    CanvasRenderer ren;

    void Start()
    {
        text = GetComponentInChildren<Text>();
        Assert.IsNotNull(text);

        ren = GetComponent<CanvasRenderer>();
        Assert.IsNotNull(ren);
    }
    
    void Update()
    {
        text.color = ren.GetColor().Invert();
    }
}
