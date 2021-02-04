// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// ClothParamクラスのプリセットに関するユーティリティ
    /// </summary>
    public static class EditorPresetUtility
    {
        const string configName = "preset folder";

        public static void DrawPresetButton(MonoBehaviour owner, ClothParams clothParam)
        {
            using (var horizontalScope = new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);
                if (GUILayout.Button("Save", GUILayout.Width(40), GUILayout.Height(16)))
                {
                    SaveClothParam(clothParam);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Load", GUILayout.Width(40), GUILayout.Height(16)))
                {
                    LoadClothParam(owner, clothParam);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private static void SaveClothParam(ClothParams clothParam)
        {
            // フォルダを読み込み
            string folder = EditorUserSettings.GetConfigValue(configName);

            // 保存ダイアログ
            string path = UnityEditor.EditorUtility.SaveFilePanelInProject(
                "Save Preset",
                "preset",
                "json",
                "Enter a name for the preset json.",
                folder
                );
            if (string.IsNullOrEmpty(path))
                return;

            // フォルダを記録
            folder = Path.GetDirectoryName(path);
            EditorUserSettings.SetConfigValue(configName, folder);

            Debug.Log("Save preset file:" + path);

            // json
            string json = JsonUtility.ToJson(clothParam);

            // save
            File.WriteAllText(path, json);

            AssetDatabase.Refresh();

            Debug.Log("Complete.");
        }

        private static void LoadClothParam(MonoBehaviour owner, ClothParams clothParam)
        {
            // フォルダを読み込み
            string folder = EditorUserSettings.GetConfigValue(configName);

            // 読み込みダイアログ
            string path = UnityEditor.EditorUtility.OpenFilePanel("Load Preset", folder, "json");
            if (string.IsNullOrEmpty(path))
                return;

            // フォルダを記録
            folder = Path.GetDirectoryName(path);
            EditorUserSettings.SetConfigValue(configName, folder);

            // json
            Debug.Log("Load preset file:" + path);
            string json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json) == false)
            {
                // 上書きしないプロパティを保持
                Transform influenceTarget = clothParam.GetInfluenceTarget();
                Transform disableReferenceObject = clothParam.DisableReferenceObject;
                //Transform directionalDampingObject = clothParam.DirectionalDampingObject;

                // undo
                Undo.RecordObject(owner, "Load preset");

                JsonUtility.FromJsonOverwrite(json, clothParam);

                // 上書きしないプロパティを書き戻し
                clothParam.SetInfluenceTarget(influenceTarget);
                clothParam.DisableReferenceObject = disableReferenceObject;
                //clothParam.DirectionalDampingObject = directionalDampingObject;

                Debug.Log("Complete.");
            }
        }
    }
}
