// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// クロス用のエディタ拡張
    /// </summary>
    public abstract class ClothEditor : Editor
    {
        /// <summary>
        /// ポイントセレクタークラス
        /// </summary>
        PointSelector pointSelector = new PointSelector();

        /// <summary>
        /// 現在編集中の選択データ
        /// </summary>
        List<int> selectorData = new List<int>();

        /// <summary>
        /// アクティブなエディタメッシュインターフェース
        /// </summary>
        IEditorMesh editorMesh;

        //=========================================================================================
        protected virtual void OnEnable()
        {
            pointSelector.EnableEdit();
        }

        protected virtual void OnDisable()
        {
            pointSelector.DisableEdit(this);
        }

        /// <summary>
        /// 選択データの初期化
        /// 配列はすでに頂点数分が確保されゼロクリアされています。
        /// すでに選択データがある場合はここでselectorDataにデータをセットしてください。
        /// </summary>
        /// <param name="selectorData"></param>
        protected virtual void OnResetSelector(List<int> selectorData) { }

        /// <summary>
        /// 選択データの決定
        /// </summary>
        /// <param name="selectorData"></param>
        protected virtual void OnFinishSelector(List<int> selectorData) { }

        /// <summary>
        /// ポイント選択GUIの表示と制御
        /// </summary>
        /// <param name="clothData"></param>
        /// <param name="editorMesh"></param>
        protected void DrawInspectorGUI(IEditorMesh editorMesh)
        {
            this.editorMesh = editorMesh;

            if (editorMesh == null)
                return;

            pointSelector.DrawInspectorGUI(this, StartEdit, EndEdit);
        }

        /// <summary>
        /// 共有選択データを初期化する
        /// </summary>
        protected void InitSelectorData()
        {
            // メッシュデータ
            List<Vector3> wposList;
            List<Vector3> wnorList;
            List<Vector3> wtanList;
            int meshVertexCount = editorMesh.GetEditorPositionNormalTangent(out wposList, out wnorList, out wtanList);

            // 選択データ初期化
            selectorData.Clear();
            for (int i = 0; i < meshVertexCount; i++)
                selectorData.Add(0); // Invalid

            // 基本設定
            OnResetSelector(selectorData);

            // 共有データ作成
            OnFinishSelector(selectorData);
        }


        //=============================================================================================
        /// <summary>
        /// 作成を実行できるか判定する
        /// </summary>
        /// <returns></returns>
        protected abstract bool CheckCreate();

        //=============================================================================================
        /// <summary>
        /// ポイント選択開始
        /// </summary>
        /// <param name="pointSelector"></param>
        void StartEdit(PointSelector pointSelector)
        {
            // 毎回初期化する
            // 各ポイントのタイプ情報を設定
            pointSelector.AddPointType("Move Point", Color.green, SelectionData.Move);
            pointSelector.AddPointType("Fixed Point", Color.red, SelectionData.Fixed);
            pointSelector.AddPointType("Invalid Point", Color.gray, SelectionData.Invalid);

            // メッシュデータ
            List<Vector3> wposList;
            List<Vector3> wnorList;
            List<Vector3> wtanList;
            int meshVertexCount = editorMesh.GetEditorPositionNormalTangent(out wposList, out wnorList, out wtanList);

            // 選択データ初期化
            selectorData.Clear();
            for (int i = 0; i < meshVertexCount; i++)
                selectorData.Add(0); // Invalid
            OnResetSelector(selectorData);

            if (meshVertexCount == 0)
                return;

            // 各ポイントデータをセレクタークラスへ流し込む
            for (int i = 0; i < meshVertexCount; i++)
            {
                pointSelector.AddPoint(wposList[i], i, selectorData[i]);
            }
        }

        /// <summary>
        /// ポイント選択終了
        /// </summary>
        /// <param name="pointSelector"></param>
        void EndEdit(PointSelector pointSelector)
        {
            // 現在のポイント内容をデータに反映
            var pointList = pointSelector.GetPointList();
            foreach (var p in pointList)
            {
                selectorData[p.index] = p.value;
            }

            // 確定
            OnFinishSelector(selectorData);
        }

        /// <summary>
        /// 新規選択クラスを作成して返す
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        protected SelectionData CreateSelection(MonoBehaviour obj, string property)
        {
            string dataname = "SelectionData_" + obj.name;
            var selection = ShareDataObject.CreateShareData<SelectionData>(dataname);
            return selection;
        }

        /// <summary>
        /// 選択クラスをプロパティに保存する
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="property"></param>
        /// <param name="selectionData"></param>
        protected void ApplySelection(MonoBehaviour obj, string property, SelectionData selectionData)
        {
            var so = new SerializedObject(obj);
            var sel = so.FindProperty(property);
            sel.objectReferenceValue = selectionData;
            so.ApplyModifiedProperties();
        }

        //=========================================================================================
        /// <summary>
        /// ブレンド率設定インスペクタ
        /// </summary>
        protected void UserBlendInspector()
        {
            PhysicsTeam scr = target as PhysicsTeam;

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Slider(serializedObject.FindProperty("userBlendWeight"), 0.0f, 1.0f, "Blend Weight");
        }

        /// <summary>
        /// コライダー設定インスペクタ
        /// </summary>
        protected void ColliderInspector()
        {
            PhysicsTeam scr = target as PhysicsTeam;

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Collider", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("teamData.mergeAvatarCollider"));
            EditorInspectorUtility.DrawObjectList<ColliderComponent>(
                serializedObject.FindProperty("teamData.colliderList"),
                scr.gameObject,
                true, true
                );
        }
    }
}