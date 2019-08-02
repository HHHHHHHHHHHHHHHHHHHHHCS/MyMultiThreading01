using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyTest01 : MonoBehaviour
{
    public int count = 10000;
    public float delay = 0.01f;

    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            int index = i;
            float start = Time.realtimeSinceStartup;
            MyLooper.Call(i * delay, () => { Debug.Log(index * delay + ":::" + (Time.realtimeSinceStartup - start)); });
        }
    }
}