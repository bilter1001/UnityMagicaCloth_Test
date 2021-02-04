// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// レンダラーメッシュデフォーマー
    /// </summary>
    [System.Serializable]
    public class RenderMeshDeformer : BaseMeshDeformer, IBoneReplace
    {
        /// <summary>
        /// データバージョン
        /// </summary>
        private const int DATA_VERSION = 2;

        /// <summary>
        /// 再計算モード
        /// </summary>
        public enum RecalculateMode
        {
            // なし
            None = 0,

            // 法線再計算あり
            UpdateNormalPerFrame = 1,

            // 法線・接線再計算あり
            UpdateNormalAndTangentPerFrame = 2,
        }

        // 法線/接線更新モード
        [SerializeField]
        private RecalculateMode normalAndTangentUpdateMode = RecalculateMode.UpdateNormalPerFrame;

        [SerializeField]
        private Mesh sharedMesh = null;

        /// <summary>
        /// メッシュの最適化情報
        /// </summary>
        [SerializeField]
        private int meshOptimize = 0;

        // ランタイムデータ //////////////////////////////////////////
        // 書き込み用
        MeshFilter meshFilter;
        SkinnedMeshRenderer skinMeshRenderer;
        Transform[] originalBones;
        Transform[] boneList;
        Mesh mesh;

        // メッシュ状態変更フラグ
        public bool IsChangePosition { get; set; }
        public bool IsChangeNormalTangent { get; set; }
        public bool IsChangeBoneWeights { get; set; }
        bool oldUse;

        //=========================================================================================
        /// <summary>
        /// データを識別するハッシュコードを作成して返す
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = base.GetDataHash();
            hash += sharedMesh.GetDataHash();
            if (meshOptimize != 0) // 下位互換のため
                hash += meshOptimize.GetDataHash();
            return hash;
        }

        //=========================================================================================
        public Mesh SharedMesh
        {
            get
            {
                return sharedMesh;
            }
        }

        //=========================================================================================
        public void OnValidate()
        {
            if (Application.isPlaying == false)
                return;

            if (status.IsActive)
            {
                // 法線／接線再計算モード設定
                SetRecalculateNormalAndTangentMode();
            }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        protected override void OnInit()
        {
            base.OnInit();
            if (status.IsInitError)
                return;

            // レンダラーチェック
            if (TargetObject == null)
            {
                status.SetInitError();
                return;
            }
            var ren = TargetObject.GetComponent<Renderer>();
            if (ren == null)
            {
                status.SetInitError();
                return;
            }

            if (MeshData.VerifyData() != Define.Error.None)
            {
                status.SetInitError();
                return;
            }

            VertexCount = MeshData.VertexCount;
            TriangleCount = MeshData.TriangleCount;

            // クローンメッシュ作成
            // ここではメッシュは切り替えない
            mesh = null;
            if (ren is SkinnedMeshRenderer)
            {
                var sren = ren as SkinnedMeshRenderer;
                skinMeshRenderer = sren;

                // メッシュクローン
                mesh = GameObject.Instantiate(sharedMesh);
#if !UNITY_EDITOR_OSX
                // MacではMetal関連でエラーが発生するので対応（エディタ環境のみ）
                mesh.MarkDynamic();
#endif
                originalBones = sren.bones;

                // クローンメッシュ初期化
                // srenのボーンリストはここで配列を作成し最後にレンダラーのトランスフォームを追加する
                var blist = new List<Transform>(originalBones);
                blist.Add(ren.transform); // レンダラートランスフォームを最後に追加
                boneList = blist.ToArray();

                var bindlist = new List<Matrix4x4>(sharedMesh.bindposes);
                bindlist.Add(Matrix4x4.identity); // レンダラーのバインドポーズを最後に追加
                mesh.bindposes = bindlist.ToArray();
            }
            else
            {
                // メッシュクローン
                mesh = GameObject.Instantiate(sharedMesh);
#if !UNITY_EDITOR_OSX
                // MacではMetal関連でエラーが発生するので対応（エディタ環境のみ）
                mesh.MarkDynamic();
#endif

                meshFilter = TargetObject.GetComponent<MeshFilter>();
                Debug.Assert(meshFilter);
            }
            oldUse = false;

            // 共有メッシュのuid
            int uid = sharedMesh.GetInstanceID(); // 共有メッシュのIDを使う
            bool first = MagicaPhysicsManager.Instance.Mesh.IsEmptySharedRenderMesh(uid);

            // メッシュ登録
            MeshIndex = MagicaPhysicsManager.Instance.Mesh.AddRenderMesh(
                uid,
                MeshData.isSkinning,
                MeshData.baseScale,
                MeshData.VertexCount,
                IsSkinning ? boneList.Length - 1 : 0, // レンダラーのボーンインデックス
#if UNITY_2018
                IsSkinning ? MeshData.VertexCount : 0 // ボーンウエイト数＝頂点数
#else
                IsSkinning ? sharedMesh.GetAllBoneWeights().Length : 0
#endif
                );

            // レンダーメッシュの共有データを一次元配列にコピーする
            if (first)
            {
                MagicaPhysicsManager.Instance.Mesh.SetRenderSharedMeshData(
                    MeshIndex,
                    IsSkinning,
                    mesh.vertices,
                    mesh.normals,
                    mesh.tangents,
#if UNITY_2018
                    IsSkinning ? mesh.boneWeights : null
#else
                    sharedMesh.GetBonesPerVertex(),
                    sharedMesh.GetAllBoneWeights()
#endif
                    );
            }

            // レンダーメッシュ情報確定
            // すべてのデータが確定してから実行しないと駄目なもの
            MagicaPhysicsManager.Instance.Mesh.UpdateMeshState(MeshIndex);

            // 法線／接線再計算モード設定
            SetRecalculateNormalAndTangentMode();
        }

        /// <summary>
        /// 実行状態に入った場合に呼ばれます
        /// </summary>
        protected override void OnActive()
        {
            base.OnActive();
            if (status.IsInitSuccess)
            {
                MagicaPhysicsManager.Instance.Mesh.SetRenderMeshActive(MeshIndex, true);

                // レンダラートランスフォーム登録
                MagicaPhysicsManager.Instance.Mesh.AddRenderMeshBone(MeshIndex, TargetObject.transform);
            }
        }

        /// <summary>
        /// 実行状態から抜けた場合に呼ばれます
        /// </summary>
        protected override void OnInactive()
        {
            base.OnInactive();
            if (status.IsInitSuccess)
            {
                if (MagicaPhysicsManager.IsInstance())
                {
                    // レンダラートランスフォーム解除
                    MagicaPhysicsManager.Instance.Mesh.RemoveRenderMeshBone(MeshIndex);

                    MagicaPhysicsManager.Instance.Mesh.SetRenderMeshActive(MeshIndex, false);
                }
            }
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            if (MagicaPhysicsManager.IsInstance())
            {
                // メッシュ解除
                MagicaPhysicsManager.Instance.Mesh.RemoveRenderMesh(MeshIndex);
            }

            base.Dispose();
        }

        /// <summary>
        /// 法線／接線再計算モード設定
        /// </summary>
        void SetRecalculateNormalAndTangentMode()
        {
            // ジョブシステムを利用した法線／接線再計算設定
            bool normal = false;
            bool tangent = false;
            if (normalAndTangentUpdateMode == RecalculateMode.UpdateNormalPerFrame)
            {
                normal = true;
            }
            else if (normalAndTangentUpdateMode == RecalculateMode.UpdateNormalAndTangentPerFrame)
            {
                normal = tangent = true;
            }
            MagicaPhysicsManager.Instance.Mesh.SetRenderMeshFlag(MeshIndex, PhysicsManagerMeshData.Meshflag_CalcNormal, normal);
            MagicaPhysicsManager.Instance.Mesh.SetRenderMeshFlag(MeshIndex, PhysicsManagerMeshData.Meshflag_CalcTangent, tangent);
        }

        //=========================================================================================
        public override bool IsMeshUse()
        {
            if (status.IsInitSuccess)
            {
                return MagicaPhysicsManager.Instance.Mesh.IsUseRenderMesh(MeshIndex);
            }

            return false;
        }

        public override bool IsActiveMesh()
        {
            if (status.IsInitSuccess)
            {
                return MagicaPhysicsManager.Instance.Mesh.IsActiveRenderMesh(MeshIndex);
            }

            return false;
        }

        public override void AddUseMesh(System.Object parent)
        {
            var virtualMeshDeformer = parent as VirtualMeshDeformer;
            Debug.Assert(virtualMeshDeformer != null);

            if (status.IsInitSuccess)
            {
                //Develop.Log($"★AddUseMesh:{this.Parent.name} meshIndex:{MeshIndex}");

                MagicaPhysicsManager.Instance.Mesh.AddUseRenderMesh(MeshIndex);
                IsChangePosition = true;
                IsChangeNormalTangent = true;
                IsChangeBoneWeights = true;

                // 親仮想メッシュと連動させる
                int virtualMeshIndex = virtualMeshDeformer.MeshIndex;
                var virtualMeshInfo = MagicaPhysicsManager.Instance.Mesh.virtualMeshInfoList[virtualMeshIndex];
                var sharedVirtualMeshInfo = MagicaPhysicsManager.Instance.Mesh.sharedVirtualMeshInfoList[virtualMeshInfo.sharedVirtualMeshIndex];
                int index = virtualMeshDeformer.GetRenderMeshDeformerIndex(this);
                long cuid = (long)sharedVirtualMeshInfo.uid << 16 + index;
                int sharedChildMeshIndex = MagicaPhysicsManager.Instance.Mesh.sharedChildMeshIdToSharedVirtualMeshIndexDict[cuid];
                var sharedChildMeshInfo = MagicaPhysicsManager.Instance.Mesh.sharedChildMeshInfoList[sharedChildMeshIndex];

                MagicaPhysicsManager.Instance.Mesh.LinkRenderMesh(
                    MeshIndex,
                    sharedChildMeshInfo.vertexChunk.startIndex,
                    sharedChildMeshInfo.weightChunk.startIndex,
                    virtualMeshInfo.vertexChunk.startIndex,
                    sharedVirtualMeshInfo.vertexChunk.startIndex
                    );

                // 利用頂点更新
                //MagicaPhysicsManager.Instance.Compute.RenderMeshWorker.SetUpdateUseFlag();
            }
        }

        public override void RemoveUseMesh(System.Object parent)
        {
            //base.RemoveUseMesh();

            var virtualMeshDeformer = parent as VirtualMeshDeformer;
            Debug.Assert(virtualMeshDeformer != null);

            if (status.IsInitSuccess)
            {
                // 親仮想メッシュとの連動を解除する
                int virtualMeshIndex = virtualMeshDeformer.MeshIndex;
                var virtualMeshInfo = MagicaPhysicsManager.Instance.Mesh.virtualMeshInfoList[virtualMeshIndex];
                var sharedVirtualMeshInfo = MagicaPhysicsManager.Instance.Mesh.sharedVirtualMeshInfoList[virtualMeshInfo.sharedVirtualMeshIndex];
                int index = virtualMeshDeformer.GetRenderMeshDeformerIndex(this);
                long cuid = (long)sharedVirtualMeshInfo.uid << 16 + index;
                int sharedChildMeshIndex = MagicaPhysicsManager.Instance.Mesh.sharedChildMeshIdToSharedVirtualMeshIndexDict[cuid];
                var sharedChildMeshInfo = MagicaPhysicsManager.Instance.Mesh.sharedChildMeshInfoList[sharedChildMeshIndex];

                MagicaPhysicsManager.Instance.Mesh.UnlinkRenderMesh(
                    MeshIndex,
                    sharedChildMeshInfo.vertexChunk.startIndex,
                    sharedChildMeshInfo.weightChunk.startIndex,
                    virtualMeshInfo.vertexChunk.startIndex,
                    sharedVirtualMeshInfo.vertexChunk.startIndex
                    );


                MagicaPhysicsManager.Instance.Mesh.RemoveUseRenderMesh(MeshIndex);
                IsChangePosition = true;
                IsChangeNormalTangent = true;
                IsChangeBoneWeights = true;

                // 利用頂点更新
                //MagicaPhysicsManager.Instance.Compute.RenderMeshWorker.SetUpdateUseFlag();
            }
        }

        //=========================================================================================
        /// <summary>
        /// メッシュ座標書き込み
        /// </summary>
        public override void Finish(int bufferIndex)
        {
            bool use = IsMeshUse();

            // 頂点の姿勢／ウエイトの計算状態
            bool vertexCalc = true;
            if (use && bufferIndex == 1)
            {
                var state = MagicaPhysicsManager.Instance.Mesh.renderMeshStateDict[MeshIndex];
                vertexCalc = state.IsFlag(PhysicsManagerMeshData.RenderStateFlag_DelayedCalculated);

                if (vertexCalc == false)
                    return;
            }

#if true
            // メッシュ切替
            // 頂点変形が不要な場合は元の共有メッシュに戻す
            if (use != oldUse)
            {
                if (meshFilter)
                {
                    meshFilter.mesh = use ? mesh : sharedMesh;
                }
                else if (skinMeshRenderer)
                {
                    skinMeshRenderer.sharedMesh = use ? mesh : sharedMesh;
                    skinMeshRenderer.bones = use ? boneList : originalBones;
                }
                oldUse = use;

                if (use)
                {
                    IsChangePosition = true;
                    IsChangeNormalTangent = true;
                    IsChangeBoneWeights = true;
                }
            }

            //if ((use || IsChangePosition || IsChangeNormalTangent) && mesh.isReadable && vertexCalc)
            if ((use || IsChangePosition || IsChangeNormalTangent) && vertexCalc)
            {
                // メッシュ書き戻し
                // meshバッファをmeshに設定する（重い！）
                // ★現状これ以外に方法がない！考えられる回避策は２つ：
                // ★（１）Unityの将来のバージョンでmeshのネイティブ配列がサポートされるのを待つ
                // ★（２）コンピュートバッファを使いシェーダーで頂点をマージする（かなり高速、しかしカスタムシェーダーが必須となり汎用性が無くなる）
                MagicaPhysicsManager.Instance.Mesh.CopyToRenderMeshLocalPositionData(MeshIndex, mesh, bufferIndex);
                bool normal = normalAndTangentUpdateMode == RecalculateMode.UpdateNormalPerFrame || normalAndTangentUpdateMode == RecalculateMode.UpdateNormalAndTangentPerFrame;
                bool tangent = normalAndTangentUpdateMode == RecalculateMode.UpdateNormalAndTangentPerFrame;
                if (normal || tangent)
                {
                    MagicaPhysicsManager.Instance.Mesh.CopyToRenderMeshLocalNormalTangentData(MeshIndex, mesh, bufferIndex, normal, tangent);
                }
                else if (IsChangeNormalTangent)
                {
                    // 元に戻す
                    mesh.normals = sharedMesh.normals;
                    mesh.tangents = sharedMesh.tangents;
                }
                IsChangePosition = false;
                IsChangeNormalTangent = false;
            }

            //if (use && IsSkinning && IsChangeBoneWeights && mesh.isReadable && weightCalc)
            if (use && IsSkinning && IsChangeBoneWeights && vertexCalc)
            {
                // 頂点ウエイト変更
                //Debug.Log("Change Mesh Weights:" + mesh.name + " buff:" + bufferIndex + " frame:" + Time.frameCount);
                MagicaPhysicsManager.Instance.Mesh.CopyToRenderMeshBoneWeightData(MeshIndex, mesh, sharedMesh, bufferIndex);
                IsChangeBoneWeights = false;
            }
#endif
        }


        //=========================================================================================
        /// <summary>
        /// ボーンを置換する
        /// </summary>
        public void ReplaceBone(Dictionary<Transform, Transform> boneReplaceDict)
        {
            if (originalBones != null)
            {
                for (int i = 0; i < originalBones.Length; i++)
                {
                    originalBones[i] = MeshUtility.GetReplaceBone(originalBones[i], boneReplaceDict);
                }
            }

            if (boneList != null)
            {
                for (int i = 0; i < boneList.Length; i++)
                {
                    boneList[i] = MeshUtility.GetReplaceBone(boneList[i], boneReplaceDict);
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// メッシュのワールド座標/法線/接線を返す（エディタ設定用）
        /// </summary>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        /// <returns>頂点数</returns>
        public override int GetEditorPositionNormalTangent(
            out List<Vector3> wposList,
            out List<Vector3> wnorList,
            out List<Vector3> wtanList
            )
        {
            wposList = new List<Vector3>();
            wnorList = new List<Vector3>();
            wtanList = new List<Vector3>();

            if (Application.isPlaying)
            {
                if (Status.IsDispose)
                    return 0;

                if (IsMeshUse() == false || TargetObject == null)
                    return 0;

                Vector3[] posArray = new Vector3[VertexCount];
                Vector3[] norArray = new Vector3[VertexCount];
                Vector3[] tanArray = new Vector3[VertexCount];
                MagicaPhysicsManager.Instance.Mesh.CopyToRenderMeshWorldData(MeshIndex, TargetObject.transform, posArray, norArray, tanArray);

                wposList = new List<Vector3>(posArray);
                wnorList = new List<Vector3>(norArray);
                wtanList = new List<Vector3>(tanArray);

                return VertexCount;
            }
            else
            {
                if (TargetObject == null)
                {
                    return 0;
                }
                var ren = TargetObject.GetComponent<Renderer>();
                MeshUtility.CalcMeshWorldPositionNormalTangent(ren, sharedMesh, out wposList, out wnorList, out wtanList);

                return wposList.Count;
            }
        }

        /// <summary>
        /// メッシュのトライアングルリストを返す（エディタ設定用）
        /// </summary>
        /// <returns></returns>
        public override List<int> GetEditorTriangleList()
        {
            if (sharedMesh)
            {
                return new List<int>(sharedMesh.triangles);
            }

            return null;
        }

        /// <summary>
        /// メッシュのラインリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public override List<int> GetEditorLineList()
        {
            // レンダーデフォーマーでは存在しない
            return null;
        }

        //=========================================================================================
        public override int GetVersion()
        {
            return DATA_VERSION;
        }

        /// <summary>
        /// 現在のデータが正常（実行できる状態）か返す
        /// </summary>
        /// <returns></returns>
        public override Define.Error VerifyData()
        {
            var baseError = base.VerifyData();
            if (baseError != Define.Error.None)
                return baseError;

            if (sharedMesh == null)
                return Define.Error.SharedMeshNull;
            if (sharedMesh.isReadable == false)
                return Define.Error.SharedMeshCannotRead;

            // 最大頂点数は65535（要望が多いようなら拡張する）
            if (sharedMesh.vertexCount > 65535)
                return Define.Error.MeshVertexCount65535Over;

#if UNITY_EDITOR
            // メッシュ最適化タイプが異なる場合は頂点順序が変更されているのでNG
            // またモデルインポート設定を参照するので実行時は判定しない
            if (!Application.isPlaying && meshOptimize != 0 && meshOptimize != EditUtility.GetOptimizeMesh(sharedMesh))
                return Define.Error.MeshOptimizeMismatch;
#endif

            return Define.Error.None;
        }

        /// <summary>
        /// データ情報
        /// </summary>
        /// <returns></returns>
        public override string GetInformation()
        {
            StaticStringBuilder.Clear();

            var err = VerifyData();
            if (err == Define.Error.None)
            {
                // OK
                StaticStringBuilder.AppendLine("Active: ", Status.IsActive);
                StaticStringBuilder.AppendLine("Skinning: ", MeshData.isSkinning);
                StaticStringBuilder.AppendLine("Vertex: ", MeshData.VertexCount);
                StaticStringBuilder.AppendLine("Triangle: ", MeshData.TriangleCount);
                StaticStringBuilder.Append("Bone: ", MeshData.BoneCount);
            }
            else if (err == Define.Error.EmptyData)
            {
                StaticStringBuilder.Append(Define.GetErrorMessage(err));
            }
            else
            {
                // エラー
                StaticStringBuilder.AppendLine("This mesh data is Invalid!");

                if (Application.isPlaying)
                {
                    StaticStringBuilder.AppendLine("Execution stopped.");
                }
                else
                {
                    StaticStringBuilder.AppendLine("Please create the mesh data.");
                }
                StaticStringBuilder.Append(Define.GetErrorMessage(err));
            }

            return StaticStringBuilder.ToString();
        }
    }
}
