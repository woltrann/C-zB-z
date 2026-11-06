using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CraftStation))]
public sealed class CraftStationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var station = (CraftStation)target;
        if (station.Recipes.Count == 0) EditorGUILayout.HelpBox("Bu tezgaha en az bir tarif ekle.", MessageType.Info);
        for (int i = 0; i < station.Recipes.Count; i++)
            if (!station.ValidateRecipe(i, out string error))
                EditorGUILayout.HelpBox("Tarif " + (i + 1) + ": " + error, MessageType.Error);
        EditorGUILayout.HelpBox("Rezerv malzemeler yalnızca bu çalışma oturumunda tutulur. Kayıt/sahne geçişi kalıcılığı yoktur.", MessageType.Info);
    }
}
