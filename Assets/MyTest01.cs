using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyTest01 : MonoBehaviour
{
    public int count = 100;
    public float delay = 0.05f;

    void Start()
    {
        StartCoroutine(DoThing());
    }

    private IEnumerator DoThing()
    {
        for (int i = 0; i < count; i++)
        {
            int index = i;
            float start = Time.realtimeSinceStartup;
            MyLooper.Call(i * delay,
                () =>
                {
                    //Debug.Log("A:" + index + "->" + index * delay + ":::" + (Time.realtimeSinceStartup - start));
                });
        }

        yield return new WaitForSeconds(0.5f);
        MyLooper.PrintLog();
        for (int i = 0; i < 20; i++)
        {
            int index = i;
            float start = Time.realtimeSinceStartup;
            MyLooper.Call(i * delay,
                () =>
                {
                    //Debug.Log("B:" + index + "->" + index * delay + ":::" + (Time.realtimeSinceStartup - start));
                });
        }

        yield return new WaitForSeconds(0.5f);
        MyLooper.PrintLog();
        for (int i = 0; i < 20; i++)
        {
            int index = i;
            float start = Time.realtimeSinceStartup;
            MyLooper.Call(i * delay,
                () =>
                {
                    //Debug.Log("C:" + index + "->" + index * delay + ":::" + (Time.realtimeSinceStartup - start));
                });
        }
        MyLooper.PrintLog();

    }
}