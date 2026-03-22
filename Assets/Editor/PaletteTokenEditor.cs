using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PaletteToken))]
public class PaletteTokenEditor : Editor
{
	public override void OnInspectorGUI()
	{
		PaletteToken t = (PaletteToken)target;

		EditorGUI.BeginChangeCheck();

		t.token = (PaletteToken.Token)EditorGUILayout.EnumPopup("Token", t.token);

		if (EditorGUI.EndChangeCheck())
		{
			// Force apply immediately when token changes
			t.ApplyFromPalette();
			EditorUtility.SetDirty(t);
		}

		// Preview swatch
		if (DoAPalette.Instance != null)
		{
			EditorGUILayout.Space(4);
			Color resolved = DoAPalette.Instance.Resolve(t.token);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Resolved", GUILayout.Width(80));

			Rect swatchRect = EditorGUILayout.GetControlRect(
				GUILayout.Width(40), GUILayout.Height(16));
			EditorGUI.DrawRect(swatchRect, resolved);

			EditorGUILayout.LabelField(
				"#" + ColorUtility.ToHtmlStringRGB(resolved),
				EditorStyles.miniLabel);
			EditorGUILayout.EndHorizontal();
		}
		else
		{
			EditorGUILayout.HelpBox(
				"DoAPalette.asset not found in Resources folder.",
				MessageType.Warning);
		}
	}
}