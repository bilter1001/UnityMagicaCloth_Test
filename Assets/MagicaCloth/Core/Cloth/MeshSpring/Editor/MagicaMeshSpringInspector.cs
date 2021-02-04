// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// メッシュスプリングのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaMeshSpring))]
    public class MagicaMeshSpringInspector : ClothEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            MagicaMeshSpring scr = target as MagicaMeshSpring;

            // データ状態
            EditorInspectorUtility.DispVersionStatus(scr);
            EditorInspectorUtility.DispDataStatus(scr);

            serializedObject.Update();
            Undo.RecordObject(scr, "CreateMeshSpring");

            // データ検証
            if (EditorApplication.isPlaying == false)
                VerifyData();

            // モニターボタン
            EditorInspectorUtility.MonitorButtonInspector();

            EditorGUI.BeginChangeCheck();

            // メイン
            MainInspector();

            // パラメータ
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorPresetUtility.DrawPresetButton(scr, scr.Params);
            {
                var cparam = serializedObject.FindProperty("clothParams");
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
                if (EditorInspectorUtility.FullSpringInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.Spring)))
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
                    Undo.RecordObject(scr, "CreateMeshSpringData");
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

            if (EditorGUI.EndChangeCheck())
            {
                // Sceneビュー更新
                SceneView.RepaintAll();
            }
        }

        //=========================================================================================
        /// <summary>
        /// 作成を実行できるか判定する
        /// </summary>
        /// <returns></returns>
        protected override bool CheckCreate()
        {
            MagicaMeshSpring scr = target as MagicaMeshSpring;

            if (scr.Deformer == null)
                return false;

            if (scr.Deformer.VerifyData() != Define.Error.None)
                return false;

            return true;
        }

        /// <summary>
        /// データ検証
        /// </summary>
        private void VerifyData()
        {
            MagicaMeshSpring scr = target as MagicaMeshSpring;
            if (scr.VerifyData() != Define.Error.None)
            {
                // 検証エラー
                serializedObject.ApplyModifiedProperties();
            }
        }

        //=========================================================================================
        void MainInspector()
        {
            MagicaMeshSpring scr = target as MagicaMeshSpring;

            EditorGUILayout.LabelField("Main Setup", EditorStyles.boldLabel);

            // マージメッシュデフォーマー
            EditorGUILayout.PropertyField(serializedObject.FindProperty("virtualDeformer"));

            EditorGUILayout.Space();

            // センタートランスフォーム
            scr.CenterTransform = EditorGUILayout.ObjectField(
                "Center Transform", scr.CenterTransform, typeof(Transform), true
                ) as Transform;
            scr.DirectionAxis = (MagicaMeshSpring.Axis)EditorGUILayout.EnumPopup("Direction Axis", scr.DirectionAxis);

            EditorGUILayout.Space();

            // ブレンド率
            UserBlendInspector();
        }

        //=========================================================================================
        /// <summary>
        /// データ作成
        /// </summary>
        void CreateData()
        {
            MagicaMeshSpring scr = target as MagicaMeshSpring;

            Debug.Log("Started creating. [" + scr.name + "]");

            // センタートランスフォーム
            if (scr.CenterTransform == null)
                serializedObject.FindProperty("centerTransform").objectReferenceValue = scr.transform;

            // デフォーマーリスト整理
            //scr.VerifyDeformer();

            // 共有データオブジェクト作成
            SpringData sdata = ShareDataObject.CreateShareData<SpringData>("SpringData_" + scr.name);
            serializedObject.ApplyModifiedProperties();
            CreateClothData(scr, sdata, scr.GetDeformer(0));

            // データ検証
            sdata.CreateVerifyData();

            // 新しいデータを設定
            serializedObject.FindProperty("springData").objectReferenceValue = sdata;
            serializedObject.ApplyModifiedProperties();

            // 仮想デフォーマーのハッシュを設定
            //var property = serializedObject.FindProperty("virtualDeformerHash");
            //property.intValue = scr.VirtualDeformerHash;
            //serializedObject.ApplyModifiedProperties();

            // データ検証
            scr.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(sdata);

            if (scr.VerifyData() == Define.Error.None)
                Debug.Log("Creation completed. [" + scr.name + "]");
            else
                Debug.LogError("Creation failed.");
        }

        void CreateClothData(MagicaMeshSpring scr, SpringData sdata, BaseMeshDeformer deformer)
        {
            SpringData.DeformerData data = new SpringData.DeformerData();

            // 中心位置と方向
            var spos = scr.CenterTransform.position;
            var sdir = scr.CenterTransformDirection;
            var srot = scr.CenterTransform.rotation;
            var sscl = scr.Params.SpringRadiusScale;

            // 半径
            float sradius = scr.Params.SpringRadius;

            // マトリックス
            var mat = Matrix4x4.TRS(spos, srot, sscl);
            var imat = mat.inverse;

            // メッシュデータ
            List<Vector3> wposList;
            List<Vector3> wnorList;
            List<Vector3> wtanList;
            int vcnt = deformer.GetEditorPositionNormalTangent(out wposList, out wnorList, out wtanList);

            // 使用頂点とウエイト
            List<int> selectionList = Enumerable.Repeat(SelectionData.Invalid, vcnt).ToList(); // 仮のセレクションデータ
            List<int> useVertexIndexList = new List<int>();
            List<float> weightList = new List<float>();

            for (int i = 0; i < vcnt; i++)
            {
                // 範囲チェック
                var lpos = imat.MultiplyPoint(wposList[i]);
                var dist = lpos.magnitude;
                if (dist <= sradius)
                {
                    // 距離割合
                    var dratio = Mathf.InverseLerp(0.0f, sradius, dist);
                    var dpower = scr.Params.GetSpringDistanceAtten(dratio);

                    // 方向割合
                    var dir = wposList[i] - spos;
                    var ang = Vector3.Angle(sdir, dir);
                    var aratio = Mathf.InverseLerp(0.0f, 180.0f, ang);
                    var apower = scr.Params.GetSpringDirectionAtten(aratio);

                    // ウエイト算出
                    float weight = Mathf.Clamp01(dpower * apower * scr.Params.SpringIntensity);

                    // 登録
                    useVertexIndexList.Add(i);
                    weightList.Add(weight);

                    selectionList[i] = SelectionData.Move;
                }
            }

            // 利用頂点とトライアングル接続する頂点をウエイト０でマークする
            // クロスデータ用にセレクションデータを拡張する
            // （１）無効頂点の隣接が移動／固定頂点なら拡張に変更する
            selectionList = deformer.MeshData.ExtendSelection(selectionList, true, false);
            // 拡張となった頂点を固定としてウエイト０でマークする
            for (int i = 0; i < vcnt; i++)
            {
                if (selectionList[i] == SelectionData.Extend)
                {
                    useVertexIndexList.Add(i);
                    weightList.Add(0.0f);
                }
            }

            // デフォーマーデータ登録
            data.deformerDataHash = deformer.GetDataHash();
            data.vertexCount = deformer.MeshData.VertexCount;
            data.useVertexIndexList = useVertexIndexList.ToArray();
            data.weightList = weightList.ToArray();

            sdata.deformerData = data;

            // 設計時スケール
            Transform influenceTarget = scr.Params.GetInfluenceTarget() ? scr.Params.GetInfluenceTarget() : scr.transform;
            sdata.initScale = influenceTarget.lossyScale;
        }
    }
}