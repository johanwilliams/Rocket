using System;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MyLogger
{
    private static string Color(this string myStr, string color)
    {
        return $"<color={color}>{myStr}</color>";
    }
    
    private static void DoLog(Action<string, Object> LogFunction, string prefix, Object myObj, params object[] msg)
    {
#if UNITY_EDITOR
        LogFunction($"{prefix}[{myObj.name.Color("lightblue")}]: {String.Join(separator:"; ", msg)}\n", myObj);
#endif
    }

    public static void Log(this Object myObj, params object[] msg)
    {
        DoLog(Debug.Log, prefix: "", myObj, msg);
    }

    public static void LogWarning(this Object myObj, params object[] msg)
    {
        DoLog(Debug.LogWarning, prefix: "⚠".Color("yellow"), myObj, msg);
    }

    public static void LogError(this Object myObj, params object[] msg)
    {
        DoLog(Debug.LogError, prefix: "<!>".Color("red"), myObj, msg);
    }
}
