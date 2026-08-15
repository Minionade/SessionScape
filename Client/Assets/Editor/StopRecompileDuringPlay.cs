using UnityEditor;

[InitializeOnLoad]
class StopRecompileDuringPlay
{
    static StopRecompileDuringPlay()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.LockReloadAssemblies();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.UnlockReloadAssemblies();
        }
    }
}