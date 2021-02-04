// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth
{
    /// <summary>
    /// 物理マネージャのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaPhysicsManager))]
    public class MagicaPhysicsManagerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            //DrawDefaultInspector();

            MagicaPhysicsManager scr = target as MagicaPhysicsManager;

            serializedObject.Update();
            Undo.RecordObject(scr, "PhysicsManager");

            MainInspector();

            serializedObject.ApplyModifiedProperties();
        }

        void MainInspector()
        {
            MagicaPhysicsManager scr = target as MagicaPhysicsManager;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Update", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                var prop = serializedObject.FindProperty("updateTime.updatePerSeccond");
                EditorGUILayout.PropertyField(prop);

                var prop2 = serializedObject.FindProperty("updateTime.updateMode");
                EditorGUILayout.PropertyField(prop2);

                // 以下は遅延実行時のみ
                if (scr.UpdateTime.IsDelay)
                {
                    var prop3 = serializedObject.FindProperty("updateTime.futurePredictionRate");
                    EditorGUILayout.PropertyField(prop3);
                }

                Help1();

#if (UNITY_2018 || UNITY_2019_1 || UNITY_2019_2_1 || UNITY_2019_2_2 || UNITY_2019_2_3 || UNITY_2019_2_4 || UNITY_2019_2_5 || UNITY_2019_2_6 || UNITY_2019_2_7 || UNITY_2019_2_8 || UNITY_2019_2_9 || UNITY_2019_2_10 || UNITY_2019_2_11 || UNITY_2019_2_12 || UNITY_2019_2_13)
                var prop4 = serializedObject.FindProperty("updateTime.updateBoneScale");
                //EditorGUILayout.PropertyField(prop4);
                //prop4.boolValue = EditorGUILayout.Toggle("Update Bone Scale (2019.2.13 or earlier)", prop4.boolValue);
                prop4.boolValue = EditorGUILayout.Toggle("Update Bone Scale", prop4.boolValue);

                Help2();
#endif
            }
        }

        void Help1()
        {
            MagicaPhysicsManager scr = target as MagicaPhysicsManager;

            if (scr.UpdateMode == UpdateTimeManager.UpdateMode.OncePerFrame)
            {
                EditorGUILayout.HelpBox("[OncePerFrame] must have stable FPS.", MessageType.Info);
            }
            else if (scr.UpdateTime.IsDelay)
            {
                EditorGUILayout.HelpBox(
                    "Delayed execution. [experimental]\n" +
                    "Improve performance by running simulations during rendering.\n" +
                    "Note, however, that the result is one frame late.\n" +
                    "This delay is covered by future predictions.",
                    MessageType.Info);
            }
        }

        void Help2()
        {
            MagicaPhysicsManager scr = target as MagicaPhysicsManager;

#if (UNITY_2018 || UNITY_2019_1 || UNITY_2019_2_1 || UNITY_2019_2_2 || UNITY_2019_2_3 || UNITY_2019_2_4 || UNITY_2019_2_5 || UNITY_2019_2_6 || UNITY_2019_2_7 || UNITY_2019_2_8 || UNITY_2019_2_9 || UNITY_2019_2_10 || UNITY_2019_2_11 || UNITY_2019_2_12 || UNITY_2019_2_13)
            //if (scr.UpdateTime.UpdateBoneScale)
            {
                EditorGUILayout.HelpBox(
                    "Required if you want to scale or flip the character at runtime.\n" +
                    "However, it consumes CPU (Main Thread).\n" +
                    "Be careful if you use many cloth simulations.\n" +
                    "This is a problem for Unity 2019.2.13 or earlier.",
                    MessageType.Info);
            }
#endif
        }
    }
}