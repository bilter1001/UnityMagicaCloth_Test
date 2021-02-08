// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// レンダーデフォーマーのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaRenderDeformer))]
    public class MagicaRenderDeformerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            MagicaRenderDeformer scr = target as MagicaRenderDeformer;

            serializedObject.Update();

            // データ検証
            if (EditorApplication.isPlaying == false)
                VerifyData();

            // データ状態
            EditorInspectorUtility.DispVersionStatus(scr);
            EditorInspectorUtility.DispDataStatus(scr);

            Undo.RecordObject(scr, "CreateRenderMesh");

            // モニターボタン
            EditorInspectorUtility.MonitorButtonInspector();

            DrawRenderDeformerInspector();

            // データ作成
            if (EditorApplication.isPlaying == false)
            {
                EditorGUILayout.Space();
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Create"))
                {
                    Undo.RecordObject(scr, "CreateRenderMeshData");
                    scr.CreateData();
                }
                GUI.backgroundColor = Color.white;
                serializedObject.ApplyModifiedProperties();
            }
        }

        void DrawRenderDeformerInspector()
        {
            MagicaRenderDeformer scr = target as MagicaRenderDeformer;

            serializedObject.Update();

            EditorGUILayout.LabelField("Update Mode", EditorStyles.boldLabel);

            var property1 = serializedObject.FindProperty("deformer.normalAndTangentUpdateMode");
            var value1 = property1.boolValue;

            EditorGUILayout.PropertyField(property1);

            serializedObject.ApplyModifiedProperties();

            if (property1.boolValue != value1)
                scr.Deformer.IsChangeNormalTangent = true; // 再計算
        }

        //=========================================================================================
        /// <summary>
        /// データ検証
        /// </summary>
        private void VerifyData()
        {
            MagicaRenderDeformer scr = target as MagicaRenderDeformer;
            if (scr.VerifyData() != Define.Error.None)
            {
                // 検証エラー
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}