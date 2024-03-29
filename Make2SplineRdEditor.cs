using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Make2SplineRd))]
public class Make2SplineRdEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //draw the default Inspector GUI
        base.OnInspectorGUI();

        //button style
        GUIStyle buttonStyle;
        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 14;
        buttonStyle.normal.textColor = Color.yellow;
        buttonStyle.fixedHeight = 36;
        buttonStyle.fixedWidth = 140;

        //display a button (when user presses in inspector window, do something)
        if (GUILayout.Button("Refresh Gizmos", buttonStyle))
        {
            Make2SplineRd scr = (Make2SplineRd)target;
            scr.CalcArraysOffSplines();
            SceneView.RepaintAll(); //this will force a gizmo update by repainting the sceneview
        }        
    }
}
