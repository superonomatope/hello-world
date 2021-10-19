using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BooleanLogicalOperators : MonoBehaviour
{
    [SerializeField] private int a;
    [SerializeField] private int b;
    [SerializeField] private int c;
    [SerializeField] private int d;
    void Start()
    {

        Debug.Log(String.Format("a = {0} (0x{1})", a, Convert.ToString(a, 2)));
        Debug.Log(String.Format("b = {0} (0x{1})", b, Convert.ToString(b, 2)));
        Debug.Log(String.Format("c = {0} (0x{1})", c, Convert.ToString(c, 2)));
        Debug.Log(String.Format("d = {0} (0x{1})", d, Convert.ToString(d, 2)));
        
        a |= b;
        ShowResult("a |= b");
        a ^= c;
        ShowResult("a ^= c");
        a &= d;
        ShowResult("a &= d");

    }

    void ShowResult(string input)=> Debug.Log(input + String.Format("的結果為： {0} (0x{1})", a, Convert.ToString(a, 2)));

}
