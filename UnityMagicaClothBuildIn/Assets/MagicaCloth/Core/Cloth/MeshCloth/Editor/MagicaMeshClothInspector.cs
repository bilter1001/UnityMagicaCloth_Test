// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// メッシュクロスのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaMeshCloth))]
    public class MagicaMeshClothInspector : ClothEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            MagicaMeshCloth scr = target as MagicaMeshCloth;

            // データ状態
            EditorInspectorUtility.DispVersionStatus(scr);
            EditorInspectorUtility.DispDataStatus(scr);

            serializedObject.Update();

            // データ検証
            if (EditorApplication.isPlaying == false)
                VerifyData();

            // モニターボタン
            EditorInspectorUtility.MonitorButtonInspector();

            // メイン
            MainInspector();

            // コライダー
            ColliderInspector();

            // パラメータ
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorPresetUtility.DrawPresetButton(scr, scr.Params);
            {
                var cparam = serializedObject.FindProperty("clothParams");
                if (EditorInspectorUtility.RadiusInspector(cparam))
                    scr.Params.SetChangeParam(ClothParams.ParamType.Radius);
                if (EditorInspectorUtility.MassInspector(cparam))
                    scr.Params.SetChangeParam(ClothParams.ParamType.Mass);
                if (EditorInspectorUtility.GravityInspector(cparam))
                    scr.Params.SetChangeParam(ClothParams.ParamType.Gravity);
                if (EditorInspectorUtility.ExternalForceInspector(cparam))
                    scr.Params.SetChangeParam(ClothParams.ParamType.ExternalForce);
                if (EditorInspectorUtility.DragInspector(cparam))
                    scr.Params.SetChangeParam(ClothParams.ParamType.Drag);
                if (EditorInspectorUtility.MaxVelocityInspector(cparam))
                    scr.Params.SetChangeParam(ClothParams.ParamType.MaxVelocity);
                if (EditorInspectorUtility.WorldInfluenceInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.WorldInfluence)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.WorldInfluence);
                if (EditorInspectorUtility.DistanceDisableInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.DistanceDisable)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.DistanceDisable);
                if (EditorInspectorUtility.ClampDistanceInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.ClampDistance)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.ClampDistance);
                if (EditorInspectorUtility.ClampPositionInspector(cparam, false, scr.HasChangedParam(ClothParams.ParamType.ClampPosition)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.ClampPosition);
                if (EditorInspectorUtility.ClampRotationInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.ClampRotation)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.ClampRotation);
                if (EditorInspectorUtility.RestoreDistanceInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.RestoreDistance)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.RestoreDistance);
                if (EditorInspectorUtility.RestoreRotationInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.RestoreRotation)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.RestoreRotation);
                if (EditorInspectorUtility.TriangleBendInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.TriangleBend)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.TriangleBend);
                //if (EditorInspectorUtility.VolumeInspector(cparam))
                //    scr.Params.SetChangeParam(ClothParams.ParamType.Volume);
                if (EditorInspectorUtility.CollisionInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.ColliderCollision)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.ColliderCollision);
                if (EditorInspectorUtility.PenetrationInspector(serializedObject, cparam, scr.HasChangedParam(ClothParams.ParamType.Penetration)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.Penetration);
                //if (EditorInspectorUtility.BaseSkinningInspector(serializedObject, cparam))
                //    scr.Params.SetChangeParam(ClothParams.ParamType.BaseSkinning);
                if (EditorInspectorUtility.RotationInterpolationInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.RotationInterpolation)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.RotationInterpolation);
            }
            serializedObject.ApplyModifiedProperties();

            // データ作成
            if (EditorApplication.isPlaying == false)
            {
                EditorGUI.BeginDisabledGroup(CheckCreate() == false);

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Create"))
                {
                    Undo.RecordObject(scr, "CreateMeshCloth");
                    CreateData();
                }
                GUI.backgroundColor = Color.white;

                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                GUI.backgroundColor = Color.blue;
                if (GUILayout.Button("Reset Position"))
                {
                    scr.ResetCloth();
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.Space();
        }

        //=========================================================================================
        /// <summary>
        /// 作成を実行できるか判定する
        /// </summary>
        /// <returns></returns>
        protected override bool CheckCreate()
        {
            MagicaMeshCloth scr = target as MagicaMeshCloth;

            if (PointSelector.EditEnable)
                return false;

            if (scr.Deformer == null)
                return false;

            if (scr.Deformer.VerifyData() != Define.Error.None)
                return false;

            if (scr.IsValidPointSelect() == false)
                return false;

            return true;
        }

        /// <summary>
        /// データ検証
        /// </summary>
        private void VerifyData()
        {
            MagicaMeshCloth scr = target as MagicaMeshCloth;
            if (scr.VerifyData() != Define.Error.None)
            {
                // 検証エラー
                serializedObject.ApplyModifiedProperties();
            }
        }

        //=========================================================================================
        void MainInspector()
        {
            MagicaMeshCloth scr = target as MagicaMeshCloth;

            EditorGUILayout.LabelField("Main Setup", EditorStyles.boldLabel);

            // 仮想メッシュ
            EditorGUILayout.PropertyField(serializedObject.FindProperty("virtualDeformer"));

            EditorGUILayout.Space();

            // ブレンド率
            UserBlendInspector();

            // ポイント選択
            if (scr.Deformer != null)
            {
                EditorGUI.BeginDisabledGroup(scr.Deformer.VerifyData() != Define.Error.None);
                DrawInspectorGUI(scr.Deformer);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.Space();
        }

        //=============================================================================================
        /// <summary>
        /// 選択データの初期化
        /// 配列はすでに頂点数分が確保されゼロクリアされています。
        /// </summary>
        /// <param name="selectorData"></param>
        protected override void OnResetSelector(List<int> selectorData)
        {
            MagicaMeshCloth scr = target as MagicaMeshCloth;

            // すでに選択クラスがある場合は内容をコピーする
            if (scr.ClothSelection != null)
            {
                var sel = scr.ClothSelection.GetSelectionData(scr.Deformer.MeshData, scr.Deformer.GetRenderDeformerMeshList());
                for (int i = 0; i < selectorData.Count; i++)
                    selectorData[i] = sel[i];
            }
        }

        /// <summary>
        /// 選択データの決定
        /// </summary>
        /// <param name="selectorData"></param>
        protected override void OnFinishSelector(List<int> selectorData)
        {
            MagicaMeshCloth scr = target as MagicaMeshCloth;

            // 必ず新規データを作成する（ヒエラルキーでのコピー対策）
            var sel = CreateSelection(scr, "clothSelection");

            // 選択データコピー
            sel.SetSelectionData(scr.Deformer.MeshData, selectorData, scr.Deformer.GetRenderDeformerMeshList());

            // 現在のデータと比較し差異がない場合は抜ける
            if (scr.ClothSelection != null && scr.ClothSelection.Compare(sel))
                return;

            //if (scr.ClothSelection != null)
            //    Undo.RecordObject(scr.ClothSelection, "Set Selector");

            // 保存
            var cdata = serializedObject.FindProperty("clothSelection");
            cdata.objectReferenceValue = sel;
            serializedObject.ApplyModifiedProperties();
        }

        //=========================================================================================
        /// <summary>
        /// データ作成
        /// </summary>
        void CreateData()
        {
            MagicaMeshCloth scr = target as MagicaMeshCloth;

            Debug.Log("Started creating. [" + scr.name + "]");

            // 共有選択データが存在しない場合は作成する
            if (scr.ClothSelection == null)
                InitSelectorData();

            // チームハッシュを設定
            scr.TeamData.ValidateColliderList();

            // クロスデータ共有データ作成
            string dataname = "MeshClothData_" + scr.name;
            var cloth = ShareDataObject.CreateShareData<ClothData>(dataname);

            // クロスデータ用にセレクションデータを拡張する
            // （１）無効頂点の隣接が移動／固定頂点なら拡張に変更する
            // （２）移動／固定頂点に影響する子頂点に接続する無効頂点は拡張に変更する
            var selection = scr.Deformer.MeshData.ExtendSelection(
                scr.ClothSelection.GetSelectionData(scr.Deformer.MeshData, scr.Deformer.GetRenderDeformerMeshList()),
                true,
                true
                );

            // クロスデータ作成
            cloth.CreateData(
                scr,
                scr.Params,
                scr.TeamData,
                scr.Deformer.MeshData,
                scr.Deformer,
                selection
                );

            // クロスデータを設定
            var cdata = serializedObject.FindProperty("clothData");
            cdata.objectReferenceValue = cloth;
            serializedObject.ApplyModifiedProperties();

            // 検証
            scr.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(cloth);

            if (scr.VerifyData() == Define.Error.None)
                Debug.Log("Creation completed. [" + scr.name + "]");
            else
                Debug.LogError("Creation failed.");
        }
    }
}