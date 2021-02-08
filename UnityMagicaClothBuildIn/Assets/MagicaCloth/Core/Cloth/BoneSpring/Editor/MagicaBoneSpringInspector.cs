// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// ボーンスプリングのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaBoneSpring))]
    public class MagicaBoneSpringInspector : ClothEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            MagicaBoneSpring scr = target as MagicaBoneSpring;

            // データ状態
            EditorInspectorUtility.DispVersionStatus(scr);
            EditorInspectorUtility.DispDataStatus(scr);

            serializedObject.Update();
            Undo.RecordObject(scr, "CreateBoneSpring");

            // データ検証
            if (EditorApplication.isPlaying == false)
                VerifyData();

            // モニターボタン
            EditorInspectorUtility.MonitorButtonInspector();

            // メイン
            MainInspector();

            // パラメータ
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorPresetUtility.DrawPresetButton(scr, scr.Params);
            {
                var cparam = serializedObject.FindProperty("clothParams");
                if (EditorInspectorUtility.RadiusInspector(cparam))
                    scr.Params.SetChangeParam(ClothParams.ParamType.Radius);
                //if (EditorInspectorUtility.MassInspector(cparam))
                //    scr.Params.SetChangeParam(ClothParams.ParamType.Mass);
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
                if (EditorInspectorUtility.ClampPositionInspector(cparam, true, scr.HasChangedParam(ClothParams.ParamType.ClampPosition)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.ClampPosition);
                if (EditorInspectorUtility.SimpleSpringInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.Spring)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.Spring);
                if (EditorInspectorUtility.AdjustRotationInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.AdjustRotation)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.AdjustRotation);
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
                    Undo.RecordObject(scr, "CreateBoneSpring");
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
            if (PointSelector.EditEnable)
                return false;

            return true;
        }

        /// <summary>
        /// データ検証
        /// </summary>
        private void VerifyData()
        {
            MagicaBoneSpring scr = target as MagicaBoneSpring;
            if (scr.VerifyData() != Define.Error.None)
            {
                // 検証エラー
                serializedObject.ApplyModifiedProperties();
            }
        }

        //=========================================================================================
        void MainInspector()
        {
            MagicaBoneSpring scr = target as MagicaBoneSpring;

            EditorGUILayout.LabelField("Main Setup", EditorStyles.boldLabel);

            // ルートリスト
            EditorInspectorUtility.DrawObjectList<Transform>(
                serializedObject.FindProperty("clothTarget.rootList"),
                scr.gameObject,
                false, true
                );

            // ブレンド率
            UserBlendInspector();

            // アニメーション連動
            //scr.ClothTarget.IsAnimationBone = EditorGUILayout.Toggle("Is Animation Bones", scr.ClothTarget.IsAnimationBone);
            //scr.ClothTarget.IsAnimationPosition = EditorGUILayout.Toggle("Is Animation Position", scr.ClothTarget.IsAnimationPosition);
            //scr.ClothTarget.IsAnimationRotation = EditorGUILayout.Toggle("Is Animation Rotation", scr.ClothTarget.IsAnimationRotation);

            // ポイント選択
            //DrawInspectorGUI(scr);

            EditorGUILayout.Space();
        }

        //=========================================================================================
#if false
        /// <summary>
        /// 選択データの初期化
        /// 配列はすでに頂点数分が確保されゼロクリアされています。
        /// </summary>
        /// <param name="selectorData"></param>
        protected override void OnResetSelector(List<int> selectorData)
        {
            // ルートトランスフォームのみ固定で初期化する
            BoneSpring scr = target as BoneSpring;

            // 選択クラスが無い場合は作成する
            CreateSelection(scr, "clothSelection");

            // 現在の頂点選択データをコピー
            // また新規データの場合はボーン階層のルートを固定化する
            bool init = scr.ClothSelection.DeformerCount == 0;
            var sel = scr.ClothSelection.GetSelectionData(scr.MeshData);
            if (init)
            {
                var tlist = scr.GetTransformList();
                if (tlist.Count != sel.Count)
                {
                    sel.Clear();
                    for (int i = 0; i < tlist.Count; i++)
                        sel.Add(SelectionData.Invalid);
                }

                for (int i = 0; i < tlist.Count; i++)
                {
                    var t = tlist[i];
                    int data = 0;
                    if (scr.ClothTarget.GetRootIndex(t) >= 0)
                    {
                        // 固定
                        data = SelectionData.Fixed;
                    }
                    else
                    {
                        // 移動
                        data = SelectionData.Move;
                    }
                    sel[i] = data;
                }
            }
            for (int i = 0; i < selectorData.Count; i++)
            {
                if (i < sel.Count)
                    selectorData[i] = sel[i];
            }
        }

        /// <summary>
        /// 選択データの決定
        /// </summary>
        /// <param name="selectorData"></param>
        protected override void OnFinishSelector(List<int> selectorData)
        {
            BoneCloth scr = target as BoneCloth;

            serializedObject.Update();
            Undo.RecordObject(scr.ClothSelection, "Set Selector");

            // 選択データコピー
            scr.ClothSelection.SetSelectionData(scr.MeshData, selectorData);

            serializedObject.ApplyModifiedProperties();
        }
#endif

        //=========================================================================================
        void CreateData()
        {
            MagicaBoneSpring scr = target as MagicaBoneSpring;

            Debug.Log("Started creating. [" + scr.name + "]");

            // チームハッシュ設定
            scr.TeamData.ValidateColliderList();

            // メッシュデータ作成
            CreateMeshData(scr);

            // クロスデータ作成
            CreateClothdata(scr);

            // 検証
            scr.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();

            if (scr.VerifyData() == Define.Error.None)
                Debug.Log("Creation completed. [" + scr.name + "]");
            else
                Debug.LogError("Creation failed.");
        }

        /// <summary>
        /// メッシュデータ作成
        /// </summary>
        void CreateMeshData(MagicaBoneSpring scr)
        {
            // 共有データオブジェクト作成
            string dataname = "BoneSpringMeshData_" + scr.name;
            MeshData mdata = ShareDataObject.CreateShareData<MeshData>(dataname);

            // トランスフォームリスト作成
            var transformList = scr.GetTransformList();
            if (transformList.Count == 0)
                return;

            // 頂点作成
            List<Vector3> wposList = new List<Vector3>();
            List<Vector3> lposList = new List<Vector3>();
            List<Vector3> lnorList = new List<Vector3>();
            List<Vector3> ltanList = new List<Vector3>();
            Transform myt = scr.transform;
            for (int i = 0; i < transformList.Count; i++)
            {
                var t = transformList[i];

                // 頂点追加
                var pos = t.position;
                var lpos = myt.InverseTransformDirection(pos - myt.position);
                var lnor = myt.InverseTransformDirection(t.forward);
                var ltan = myt.InverseTransformDirection(t.up);
                wposList.Add(pos);
                lposList.Add(lpos);
                lnorList.Add(lnor);
                ltanList.Add(ltan);
            }
            var vertexInfoList = new List<uint>();
            var vertexWeightList = new List<MeshData.VertexWeight>();
            for (int i = 0; i < lposList.Count; i++)
            {
                // １ウエイトで追加
                uint vinfo = DataUtility.Pack4_28(1, i);
                vertexInfoList.Add(vinfo);
                var vw = new MeshData.VertexWeight();
                vw.parentIndex = i;
                vw.weight = 1.0f;
                vw.localPos = lposList[i];
                vw.localNor = lnorList[i];
                vw.localTan = ltanList[i];
            }
            mdata.vertexInfoList = vertexInfoList.ToArray();
            mdata.vertexWeightList = vertexWeightList.ToArray();
            mdata.vertexCount = lposList.Count;

            // ライン作成
            HashSet<uint> lineSet = new HashSet<uint>();

            // 構造ライン
            for (int i = 0; i < transformList.Count; i++)
            {
                var t = transformList[i];
                var pt = t.parent;
                if (pt != null && transformList.Contains(pt))
                {
                    int v0 = i;
                    int v1 = transformList.IndexOf(pt);
                    uint pair = DataUtility.PackPair(v0, v1);
                    lineSet.Add(pair);
                }
            }

            // ライン格納
            List<int> lineList = new List<int>();
            foreach (var pair in lineSet)
            {
                int v0, v1;
                DataUtility.UnpackPair(pair, out v0, out v1);
                lineList.Add(v0);
                lineList.Add(v1);
            }
            mdata.lineList = lineList.ToArray();
            mdata.lineCount = lineList.Count / 2;

            serializedObject.FindProperty("meshData").objectReferenceValue = mdata;
            serializedObject.ApplyModifiedProperties();

            // 使用トランスフォームシリアライズ
            var property = serializedObject.FindProperty("useTransformList");
            var propertyPos = serializedObject.FindProperty("useTransformPositionList");
            var propertyRot = serializedObject.FindProperty("useTransformRotationList");
            var propertyScl = serializedObject.FindProperty("useTransformScaleList");
            property.arraySize = transformList.Count;
            propertyPos.arraySize = transformList.Count;
            propertyRot.arraySize = transformList.Count;
            propertyScl.arraySize = transformList.Count;
            for (int i = 0; i < transformList.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = transformList[i];
                propertyPos.GetArrayElementAtIndex(i).vector3Value = transformList[i].localPosition;
                propertyRot.GetArrayElementAtIndex(i).quaternionValue = transformList[i].localRotation;
                propertyScl.GetArrayElementAtIndex(i).vector3Value = transformList[i].localScale;
            }
            serializedObject.ApplyModifiedProperties();

            // データ検証とハッシュ
            mdata.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(mdata);
        }

        /// <summary>
        /// クロスデータ作成
        /// </summary>
        void CreateClothdata(MagicaBoneSpring scr)
        {
            if (scr.MeshData == null)
                return;

            // クロスデータ共有データ作成（既存の場合は選択状態のみコピーする）
            string dataname = "BoneSpringData_" + scr.name;
            var cloth = ShareDataObject.CreateShareData<ClothData>(dataname);

            // セレクトデータはすべて「移動」で受け渡す
            List<int> selectList = new List<int>();
            for (int i = 0; i < scr.MeshData.VertexCount; i++)
                selectList.Add(SelectionData.Move);

            // クロスデータ作成
            cloth.CreateData(
                scr,
                scr.Params,
                scr.TeamData,
                scr.MeshData,
                scr,
                selectList
                );
            serializedObject.FindProperty("clothData").objectReferenceValue = cloth;

            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(cloth);
        }
    }
}