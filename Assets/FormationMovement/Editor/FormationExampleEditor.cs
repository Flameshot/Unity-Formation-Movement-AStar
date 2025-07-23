using FormationMovement;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(FormationExample))]
    public class FormationExampleNewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FormationExample controller = (FormationExample)target;

            GUILayout.Space(10);

            if (GUILayout.Button("Change Visual Leader"))
                controller.ChangeVisualLeader();
            
            if (GUILayout.Button("Change Formation"))
                controller.ChangeFormation();
            
            if (GUILayout.Button("Switch Next Formation"))
                controller.SwitchToNextFormation();
        }
    }
}