using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Developer-only log helper. In release (non-DEVELOPMENT_BUILD) player builds, <see cref="Dev"/>
/// call sites are omitted so the console stays quiet.
/// </summary>
public static class GsiLog
{
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Dev(string message)
    {
        UnityEngine.Debug.Log(string.IsNullOrEmpty(message) ? string.Empty : "[GSI] " + message);
    }
}
