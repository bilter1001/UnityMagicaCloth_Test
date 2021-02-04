// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// ボーンクロスのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaBoneCloth))]
    public class MagicaBoneClothInspector : ClothEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            MagicaBoneCloth scr = target as MagicaBoneCloth;

            // データ状態
            EditorInspectorUtility.DispVersionStatus(scr);
            EditorInspectorUtility.DispDataStatus(scr);

            serializedObject.Update();
            Undo.RecordObject(scr, "CreateBoneCloth");

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
                if (EditorInspectorUtility.ClampPositionInspector(cparam, true, scr.HasChangedParam(ClothParams.ParamType.ClampPosition)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.ClampPosition);
                if (EditorInspectorUtility.ClampRotationInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.ClampRotation)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.ClampRotation);

                if (EditorInspectorUtility.RestoreDistanceInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.RestoreDistance)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.RestoreDistance);
                if (EditorInspectorUtility.RestoreRotationInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.RestoreRotation)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.RestoreRotation);
                if (scr.ClothTarget.Connection == BoneClothTarget.ConnectionMode.Mesh)
                {
                    if (EditorInspectorUtility.TriangleBendInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.TriangleBend)))
                        scr.Params.SetChangeParam(ClothParams.ParamType.TriangleBend);
                }
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
                    Undo.RecordObject(scr, "CreateBoneCloth");
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
            MagicaBoneCloth scr = target as MagicaBoneCloth;
            if (scr.VerifyData() != Define.Error.None)
            {
                // 検証エラー
                serializedObject.ApplyModifiedProperties();
            }
        }

        //=========================================================================================
        void MainInspector()
        {
            MagicaBoneCloth scr = target as MagicaBoneCloth;

            EditorGUILayout.LabelField("Main Setup", EditorStyles.boldLabel);

            // ルートリスト
            EditorInspectorUtility.DrawObjectList<Transform>(
                serializedObject.FindProperty("clothTarget.rootList"),
                scr.gameObject,
                false, true
                );
            //EditorGUILayout.Space();

            // 接続モード
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clothTarget.connection"), new GUIContent("Connection Mode"));
            if (scr.ClothTarget.Connection == BoneClothTarget.ConnectionMode.Mesh)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("clothTarget.sameSurfaceAngle"), new GUIContent("Same Surface Angle"));

            // ブレンド率
            UserBlendInspector();

            // アニメーション連動
            //scr.ClothTarget.IsAnimationBone = EditorGUILayout.Toggle("Is Animation Bones", scr.ClothTarget.IsAnimationBone);
            //scr.ClothTarget.IsAnimationPosition = EditorGUILayout.Toggle("Is Animation Position", scr.ClothTarget.IsAnimationPosition);
            //scr.ClothTarget.IsAnimationRotation = EditorGUILayout.Toggle("Is Animation Rotation", scr.ClothTarget.IsAnimationRotation);

            // ポイント選択
            DrawInspectorGUI(scr);
            EditorGUILayout.Space();
        }

        //=========================================================================================
        /// <summary>
        /// 選択データの初期化
        /// 配列はすでに頂点数分が確保されゼロクリアされています。
        /// </summary>
        /// <param name="selectorData"></param>
        protected override void OnResetSelector(List<int> selectorData)
        {
            // ルートトランスフォームのみ固定で初期化する
            MagicaBoneCloth scr = target as MagicaBoneCloth;

            // 現在の頂点選択データをコピー
            // また新規データの場合はボーン階層のルートを固定化する
            if (scr.ClothSelection != null)
            {
                // 既存
                var sel = scr.ClothSelection.GetSelectionData(scr.MeshData, null);
                for (int i = 0; i < selectorData.Count; i++)
                {
                    if (i < sel.Count)
                        selectorData[i] = sel[i];
                }
            }
            else
            {
                // 新規
                var tlist = scr.GetTransformList();
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
                    selectorData[i] = data;
                }
            }
        }

        /// <summary>
        /// 選択データの決定
        /// </summary>
        /// <param name="selectorData"></param>
        protected override void OnFinishSelector(List<int> selectorData)
        {
            MagicaBoneCloth scr = target as MagicaBoneCloth;

            // 必ず新規データを作成する（ヒエラルキーでのコピー対策）
            var sel = CreateSelection(scr, "clothSelection");

            // 選択データコピー
            sel.SetSelectionData(scr.MeshData, selectorData, null);

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
        void CreateData()
        {
            MagicaBoneCloth scr = target as MagicaBoneCloth;

            Debug.Log("Started creating. [" + scr.name + "]");

            // 共有選択データが存在しない場合は作成する
            if (scr.ClothSelection == null)
                InitSelectorData();

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
        void CreateMeshData(MagicaBoneCloth scr)
        {
            // 共有データオブジェクト作成
            string dataname = "BoneClothMeshData_" + scr.name;
            MeshData mdata = ShareDataObject.CreateShareData<MeshData>(dataname);

            // トランスフォームリスト作成
            var transformList = scr.GetTransformList();
            if (transformList.Count == 0)
                return;

            // 頂点作成
            int vcnt = transformList.Count;
            List<Vector3> wposList = new List<Vector3>();
            List<Vector3> wnorList = new List<Vector3>();
            List<Vector4> wtanList = new List<Vector4>();
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
                wnorList.Add(t.forward);
                wtanList.Add(t.up);
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

            // デプスリスト作成
            var sel = scr.ClothSelection.GetSelectionData(null, null);
            List<int> depthList = new List<int>();
            for (int i = 0; i < transformList.Count; i++)
            {
                int depth = 0;
                var t = transformList[i];

                while (t && transformList.Contains(t))
                {
                    int index = transformList.IndexOf(t);
                    if (sel[index] != SelectionData.Move)
                        break;

                    depth++;
                    t = t.parent;
                }

                depthList.Add(depth);
                //Debug.Log($"[{transformList[i].name}] depth:{depth}");
            }


            // 構造ライン
            HashSet<uint> lineSet = new HashSet<uint>();
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

            // グリッドによるトライアングル
            List<int> triangleList = new List<int>();
            if (scr.ClothTarget.Connection == BoneClothTarget.ConnectionMode.Mesh)
            {
                HashSet<uint> triangleLineSet = new HashSet<uint>(lineSet);

                // 周りのボーンを調べ一定範囲内のボーンを接続する
                for (int i = 0; i < transformList.Count; i++)
                {
                    //if (sel[i] != SelectionData.Move)
                    //    continue;

                    var t = transformList[i];
                    int depth = depthList[i];
                    float mindist = 10000.0f;

                    List<int> linkList = new List<int>();
                    List<float> distList = new List<float>();

                    for (int j = 0; j < transformList.Count; j++)
                    {
                        if (i == j || depthList[j] != depth)
                            continue;

                        linkList.Add(j);
                        var dist = Vector3.Distance(t.position, transformList[j].position);
                        distList.Add(dist);
                        mindist = Mathf.Min(mindist, dist);
                    }

                    // 最短距離より少し長めの範囲の頂点以外は削除する
                    HashSet<int> removeSet = new HashSet<int>();
                    mindist *= 1.5f;
                    for (int j = 0; j < linkList.Count; j++)
                    {
                        if (distList[j] > mindist)
                            removeSet.Add(j);
                    }

#if true
                    // 方向が一定以内ならば最も近い接続以外を削除する
                    for (int j = 0; j < linkList.Count - 1; j++)
                    {
                        for (int k = j + 1; k < linkList.Count; k++)
                        {
                            if (removeSet.Contains(j))
                                continue;
                            if (removeSet.Contains(k))
                                continue;

                            int index0 = linkList[j];
                            int index1 = linkList[k];

                            var ang = Vector3.Angle(transformList[index0].position - t.position, transformList[index1].position - t.position);
                            if (ang <= 45.0f)
                            {
                                removeSet.Add(distList[j] < distList[k] ? k : j);
                            }
                        }
                    }
#endif
                    // 登録
                    for (int j = 0; j < linkList.Count; j++)
                    {
                        if (removeSet.Contains(j))
                            continue;
                        // 接続する
                        uint pair = DataUtility.PackPair(i, linkList[j]);
                        triangleLineSet.Add(pair);
                    }
                }

                // 一旦各頂点の接続頂点リストを取得
                var vlink = MeshUtility.GetVertexLinkList(mdata.vertexCount, triangleLineSet);

                // トライアングル情報作成
                HashSet<ulong> registTriangleSet = new HashSet<ulong>();
                for (int i = 0; i < vlink.Count; i++)
                {
                    var linkset = vlink[i];
                    var t = transformList[i];
                    var move = sel[i] == SelectionData.Move;

                    foreach (var j in linkset)
                    {
                        var t2 = transformList[j];
                        var v = (t2.position - t.position).normalized;
                        var move2 = sel[j] == SelectionData.Move;

                        foreach (var k in linkset)
                        {
                            if (j == k)
                                continue;

                            // j-kのエッジがtriangleLineSetに含まれていない場合は無効
                            //if (triangleLineSet.Contains(DataUtility.PackPair(j, k)) == false)
                            //    continue;

                            var t3 = transformList[k];
                            var v2 = (t3.position - t.position).normalized;
                            var move3 = sel[k] == SelectionData.Move;

                            // すべて固定頂点なら無効
                            if (move == false && move2 == false && move3 == false)
                                continue;

                            // 面積が０のトライアングルは除外する
                            var n = Vector3.Cross(t2.position - t.position, t3.position - t.position);
                            var clen = n.magnitude;
                            if (clen < 1e-06f)
                            {
                                //Debug.Log($"clen == 0 ({i},{j},{k})");
                                continue;
                            }

                            var ang = Vector3.Angle(v, v2); // deg
                            if (ang <= 100)
                            {
                                // i - j - k をトライアングルとして登録する
                                var thash = DataUtility.PackTriple(i, j, k);
                                if (registTriangleSet.Contains(thash) == false)
                                {
                                    triangleList.Add(i);
                                    triangleList.Add(j);
                                    triangleList.Add(k);
                                    registTriangleSet.Add(thash);
                                }
                            }
                        }
                    }
                }
            }

            // トライアングルの法線を揃える
            HashSet<ulong> triangleSet = new HashSet<ulong>();
            if (triangleList.Count > 0)
            {
                // リダクションメッシュを作成する
                // ただ現在は面法線を揃える用途にしか使用しない
                var reductionMesh = new MagicaReductionMesh.ReductionMesh();
                reductionMesh.WeightMode = MagicaReductionMesh.ReductionMesh.ReductionWeightMode.Distance;
                reductionMesh.MeshData.MaxWeightCount = 1;
                reductionMesh.MeshData.WeightPow = 1;
                reductionMesh.MeshData.SameSurfaceAngle = scr.ClothTarget.SameSurfaceAngle; // 80?
                reductionMesh.AddMesh(myt, wposList, wnorList, wtanList, null, triangleList);

                // リダクション（面法線を整えるだけでリダクションは行わない）
                reductionMesh.Reduction(0.0f, 0.0f, 0.0f, false);

                // 最終メッシュデータ取得
                var final = reductionMesh.GetFinalData(myt);
                Debug.Assert(vcnt == final.VertexCount);

                // トライアングルデータ取得
                triangleList = final.triangles;
            }

            // 近接ライン接続
            //if (scr.ClothTarget.LineConnection)
            //{
            //    CreateNearLine(scr, lineSet, wposList, mdata);
            //}

#if false
            // トライアングル接続されているエッジはラインセットから削除する
            for (int i = 0; i < triangleList.Count / 3; i++)
            {
                int v0, v1, v2;
                int index = i * 3;
                v0 = triangleList[index];
                v1 = triangleList[index + 1];
                v2 = triangleList[index + 2];

                var pair0 = DataUtility.PackPair(v0, v1);
                var pair1 = DataUtility.PackPair(v1, v2);
                var pair2 = DataUtility.PackPair(v2, v0);

                lineSet.Remove(pair0);
                lineSet.Remove(pair1);
                lineSet.Remove(pair2);
            }
#endif

            // todo:test
            //lineSet.Clear();

            // ライン格納
            if (lineSet.Count > 0)
            {
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
            }

            // トライアングル格納
            //if (triangleSet.Count > 0)
            //{
            //    List<int> triangleList = new List<int>();
            //    foreach (var tpack in triangleSet)
            //    {
            //        int v0, v1, v2;
            //        DataUtility.UnpackTriple(tpack, out v0, out v1, out v2);
            //        triangleList.Add(v0);
            //        triangleList.Add(v1);
            //        triangleList.Add(v2);
            //    }
            //    mdata.triangleCount = triangleSet.Count;
            //    mdata.triangleList = triangleList.ToArray();
            //}
            if (triangleList.Count > 0)
            {
                mdata.triangleCount = triangleList.Count / 3;
                mdata.triangleList = triangleList.ToArray();
            }

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
        /// 近接ラインの接続
        /// </summary>
        /// <param name="lineSet"></param>
        /// <param name="wposList"></param>
        /// <param name="mdata"></param>
        //void CreateNearLine(BoneCloth scr, HashSet<uint> lineSet, List<Vector3> wposList, MeshData mdata)
        //{
        //    for (int i = 0; i < (mdata.VertexCount - 1); i++)
        //    {
        //        for (int j = i + 1; j < mdata.VertexCount; j++)
        //        {
        //            float dist = Vector3.Distance(wposList[i], wposList[j]);
        //            if (dist <= scr.ClothTarget.ConnectionDistance)
        //            {
        //                // 接続
        //                uint pair = DataUtility.PackPair(i, j);
        //                lineSet.Add(pair);
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// クロスデータ作成
        /// </summary>
        void CreateClothdata(MagicaBoneCloth scr)
        {
            if (scr.MeshData == null)
                return;

            // クロスデータ共有データ作成（既存の場合は選択状態のみコピーする）
            string dataname = "BoneClothData_" + scr.name;
            var cloth = ShareDataObject.CreateShareData<ClothData>(dataname);

            // クロスデータ作成
            cloth.CreateData(
                scr,
                scr.Params,
                scr.TeamData,
                scr.MeshData,
                scr,
                scr.ClothSelection.GetSelectionData(scr.MeshData, null)
                );
            serializedObject.FindProperty("clothData").objectReferenceValue = cloth;

            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(cloth);
        }
    }
}