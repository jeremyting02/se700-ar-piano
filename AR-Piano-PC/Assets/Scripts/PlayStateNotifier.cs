using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class PlayStateNotifier
{

    static PlayStateNotifier()
    {
        EditorApplication.playModeStateChanged += ModeChanged;
    }

    static void ModeChanged(PlayModeStateChange playModeState)
    {
        if (playModeState == PlayModeStateChange.EnteredEditMode)
        {
            SaveAndLoad.Save();
        }
    }
}