// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 仮想デフォーマーのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaVirtualDeformer))]
    public class MagicaVirtualDeformerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            MagicaVirtualDeformer scr = target as MagicaVirtualDeformer;

            //DrawDefaultInspector();

            serializedObject.Update();

            // データ検証
            if (EditorApplication.isPlaying == false)
                VerifyData();

            // データ状態
            EditorInspectorUtility.DispVersionStatus(scr);
            EditorInspectorUtility.DispDataStatus(scr);

            Undo.RecordObject(scr, "CreateVirtualDeformer");

            // モニターボタン
            EditorInspectorUtility.MonitorButtonInspector();

            DrawVirtualDeformerInspector();

            // データ作成
            if (EditorApplication.isPlaying == false)
            {
                EditorGUILayout.Space();
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Create"))
                {
                    Undo.RecordObject(scr, "CreateVirtualMeshData");
                    CreateData(scr);
                }
                GUI.backgroundColor = Color.white;
                serializedObject.ApplyModifiedProperties();
            }
        }

        void DrawVirtualDeformerInspector()
        {
            MagicaVirtualDeformer scr = target as MagicaVirtualDeformer;

            serializedObject.Update();

            //EditorGUILayout.PropertyField(serializedObject.FindProperty("deformer.renderDeformerList"), true);
            EditorInspectorUtility.DrawObjectList<MagicaRenderDeformer>(
                serializedObject.FindProperty("deformer.renderDeformerList"),
                scr.gameObject,
                true, true
                );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reduction Setting", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("deformer.mergeVertexDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deformer.mergeTriangleDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deformer.sameSurfaceAngle"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deformer.useSkinning"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deformer.maxWeightCount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deformer.weightPow"));

            serializedObject.ApplyModifiedProperties();
        }

        //=========================================================================================
        /// <summary>
        /// データ検証
        /// </summary>
        private void VerifyData()
        {
            MagicaVirtualDeformer scr = target as MagicaVirtualDeformer;
            if (scr.VerifyData() != Define.Error.None)
            {
                // 検証エラー
                //scr.SetVerifyError();
                serializedObject.ApplyModifiedProperties();
            }
        }

        //=========================================================================================
        /// <summary>
        /// 事前データ作成
        /// </summary>
        /// <param name="scr"></param>
        private void CreateData(MagicaVirtualDeformer scr)
        {
            Debug.Log("Started creating. [" + scr.name + "]");

            // 子メッシュの検証
            if (VerifyChildData(scr.Deformer) == false)
            {
                // error
                Debug.LogError("Setup failed. Invalid RenderDeformer data.");
                return;
            }

            serializedObject.FindProperty("deformer.targetObject").objectReferenceValue = scr.gameObject;

            // 新規メッシュデータ
            var meshData = ShareDataObject.CreateShareData<MeshData>("VirtualMeshData_" + scr.name);

            // 設計時スケール
            meshData.baseScale = scr.transform.lossyScale;

            // 仮想メッシュ作成
            var reductionMesh = new MagicaReductionMesh.ReductionMesh();
            reductionMesh.WeightMode = MagicaReductionMesh.ReductionMesh.ReductionWeightMode.Average; // 平均法(v1.5.2)
            reductionMesh.MeshData.MaxWeightCount = scr.Deformer.MaxWeightCount;
            reductionMesh.MeshData.WeightPow = scr.Deformer.WeightPow;
            reductionMesh.MeshData.SameSurfaceAngle = scr.Deformer.SameSurfaceAngle;
            for (int i = 0; i < scr.Deformer.RenderDeformerCount; i++)
            {
                var deformer = scr.Deformer.GetRenderDeformer(i).Deformer;
                if (deformer != null)
                {
                    var sren = deformer.TargetObject.GetComponent<SkinnedMeshRenderer>();
                    List<Transform> boneList = new List<Transform>();
                    if (sren)
                        boneList = new List<Transform>(sren.bones);
                    else
                        boneList.Add(deformer.TargetObject.transform);
                    reductionMesh.AddMesh(
                        deformer.MeshData.isSkinning,
                        deformer.SharedMesh,
                        boneList,
                        deformer.SharedMesh.bindposes,
                        deformer.SharedMesh.boneWeights
                        );
                }
            }

            //reductionMesh.DebugData.DispMeshInfo("リダクション前");

            // リダクション
            reductionMesh.Reduction(
                scr.Deformer.MergeVertexDistance > 0.0f ? 0.0001f : 0.0f,
                scr.Deformer.MergeVertexDistance,
                scr.Deformer.MergeTriangleDistance,
                false
                );

            // （１）ゼロ距離リダクション
            //if (scr.Deformer.MergeVertexDistance > 0.0f)
            //    reductionMesh.ReductionZeroDistance();

            //// （２）頂点距離マージ
            //if (scr.Deformer.MergeVertexDistance > 0.0001f)
            //    reductionMesh.ReductionRadius(scr.Deformer.MergeVertexDistance);

            //// （３）トライアングル接続マージ
            //if (scr.Deformer.MergeTriangleDistance > 0.0f)
            //    reductionMesh.ReductionPolygonLink(scr.Deformer.MergeTriangleDistance);

            //// （４）未使用ボーンの削除
            //reductionMesh.ReductionBone();

            // （５）頂点の最大接続トライアングル数制限
            //reductionMesh.ReductionTriangleConnect(6);

            //reductionMesh.DebugData.DispMeshInfo("リダクション後");

            // 最終メッシュデータ取得
            var final = reductionMesh.GetFinalData(scr.gameObject.transform);

            // メッシュデータシリアライズ
            meshData.isSkinning = final.IsSkinning;
            meshData.vertexCount = final.VertexCount;

            List<uint> vlist;
            List<MeshData.VertexWeight> wlist;
            CreateVertexWeightList(
                final.VertexCount, final.vertices, final.normals, final.tangents, final.boneWeights, final.bindPoses,
                out vlist, out wlist
                );
            meshData.vertexInfoList = vlist.ToArray();
            meshData.vertexWeightList = wlist.ToArray();
            meshData.boneCount = final.BoneCount;

            meshData.uvList = final.uvs.ToArray();
            meshData.lineCount = final.LineCount;
            meshData.lineList = final.lines.ToArray();
            meshData.triangleCount = final.TriangleCount;
            meshData.triangleList = final.triangles.ToArray();

            List<uint> vertexToTriangleInfoList = new List<uint>();
            for (int i = 0; i < final.VertexCount; i++)
            {
                int tcnt = final.vertexToTriangleCountList[i];
                int tstart = final.vertexToTriangleStartList[i];
                vertexToTriangleInfoList.Add(DataUtility.Pack8_24(tcnt, tstart));
            }
            meshData.vertexToTriangleInfoList = vertexToTriangleInfoList.ToArray();
            meshData.vertexToTriangleIndexList = final.vertexToTriangleIndexList.ToArray();

            // 子メッシュ情報
            for (int i = 0; i < final.MeshCount; i++)
            {
                var minfo = final.meshList[i];
                var rdeformer = scr.Deformer.GetRenderDeformer(i).Deformer;
                var mdata = new MeshData.ChildData();

                mdata.childDataHash = rdeformer.GetDataHash();
                mdata.vertexCount = minfo.VertexCount;

                // 頂点ウエイト情報作成
                CreateVertexWeightList(
                    minfo.VertexCount, minfo.vertices, minfo.normals, minfo.tangents, minfo.boneWeights, final.vertexBindPoses,
                    out vlist, out wlist
                    );

                mdata.vertexInfoList = vlist.ToArray();
                mdata.vertexWeightList = wlist.ToArray();

                mdata.parentIndexList = minfo.parents.ToArray();

                meshData.childDataList.Add(mdata);
            }

            // レイヤー情報
            //for (int i = 0; i < final.LayerCount; i++)
            //{
            //    var linfo = new MeshData.LayerInfo();
            //    linfo.triangleList = new List<int>(final.layerList[i].triangleList);
            //    meshData.layerInfoList.Add(linfo);
            //}

            // 検証
            meshData.CreateVerifyData();
            serializedObject.FindProperty("deformer.meshData").objectReferenceValue = meshData;

            // ボーン
            var property = serializedObject.FindProperty("deformer.boneList");
            property.arraySize = final.bones.Count;
            for (int i = 0; i < final.bones.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = final.bones[i];

            serializedObject.ApplyModifiedProperties();

            // デフォーマーデータの検証とハッシュ
            scr.Deformer.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();

            // コアコンポーネントの検証とハッシュ
            scr.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(meshData);

            Debug.Log("Setup completed. [" + scr.name + "]");
        }

        /// <summary>
        /// 頂点ウエイト情報の作成
        /// </summary>
        void CreateVertexWeightList(
            int vcnt,
            List<Vector3> vertices, List<Vector3> normals, List<Vector4> tangents,
            List<BoneWeight> boneWeights, List<Matrix4x4> bindPoses,
            out List<uint> vlist, out List<MeshData.VertexWeight> wlist
            )
        {
            vlist = new List<uint>();
            wlist = new List<MeshData.VertexWeight>();
            for (int j = 0; j < vcnt; j++)
            {
                var bw = boneWeights[j];

                int wcnt = 0;
                int wstart = wlist.Count;

                // ローカル座標を事前計算する（バインドポーズ方式よりメモリは食うが実行が速い）
                if (bw.weight0 > 0.0f)
                {
                    wcnt++;
                    var vw = new MeshData.VertexWeight();
                    vw.weight = bw.weight0;
                    vw.parentIndex = bw.boneIndex0;
                    vw.localPos = bindPoses[bw.boneIndex0].MultiplyPoint(vertices[j]);
                    vw.localNor = bindPoses[bw.boneIndex0].MultiplyVector(normals[j]).normalized;
                    vw.localTan = bindPoses[bw.boneIndex0].MultiplyVector(tangents[j]).normalized;
                    wlist.Add(vw);
                }
                if (bw.weight1 > 0.0f)
                {
                    wcnt++;
                    var vw = new MeshData.VertexWeight();
                    vw.weight = bw.weight1;
                    vw.parentIndex = bw.boneIndex1;
                    vw.localPos = bindPoses[bw.boneIndex1].MultiplyPoint(vertices[j]);
                    vw.localNor = bindPoses[bw.boneIndex1].MultiplyVector(normals[j]).normalized;
                    vw.localTan = bindPoses[bw.boneIndex1].MultiplyVector(tangents[j]).normalized;
                    wlist.Add(vw);
                }
                if (bw.weight2 > 0.0f)
                {
                    wcnt++;
                    var vw = new MeshData.VertexWeight();
                    vw.weight = bw.weight2;
                    vw.parentIndex = bw.boneIndex2;
                    vw.localPos = bindPoses[bw.boneIndex2].MultiplyPoint(vertices[j]);
                    vw.localNor = bindPoses[bw.boneIndex2].MultiplyVector(normals[j]).normalized;
                    vw.localTan = bindPoses[bw.boneIndex2].MultiplyVector(tangents[j]).normalized;
                    wlist.Add(vw);
                }
                if (bw.weight3 > 0.0f)
                {
                    wcnt++;
                    var vw = new MeshData.VertexWeight();
                    vw.weight = bw.weight3;
                    vw.parentIndex = bw.boneIndex3;
                    vw.localPos = bindPoses[bw.boneIndex3].MultiplyPoint(vertices[j]);
                    vw.localNor = bindPoses[bw.boneIndex3].MultiplyVector(normals[j]).normalized;
                    vw.localTan = bindPoses[bw.boneIndex3].MultiplyVector(tangents[j]).normalized;
                    wlist.Add(vw);
                }

                // 頂点のウエイト情報
                uint pack = DataUtility.Pack4_28(wcnt, wstart);
                vlist.Add(pack);
            }
        }

        /// <summary>
        /// 子メッシュに問題がないか検証する
        /// </summary>
        /// <param name="scr"></param>
        /// <returns></returns>
        bool VerifyChildData(VirtualMeshDeformer scr)
        {
            if (scr.RenderDeformerCount == 0)
                return false;

            for (int i = 0; i < scr.RenderDeformerCount; i++)
            {
                //var deformer = scr.GetRenderDeformer(i).Deformer;
                var deformer = scr.GetRenderDeformer(i);
                if (deformer == null)
                    return false;

                if (deformer.VerifyData() != Define.Error.None)
                    return false;
            }

            return true;
        }
    }
}