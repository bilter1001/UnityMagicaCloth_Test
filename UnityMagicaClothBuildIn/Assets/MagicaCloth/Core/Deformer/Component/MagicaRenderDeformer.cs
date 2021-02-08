// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicaCloth
{
    /// <summary>
    /// レンダーメッシュデフォーマーのコンポーネント
    /// </summary>
    [HelpURL("https://magicasoft.jp/magica-cloth-render-deformer/")]
    [AddComponentMenu("MagicaCloth/MagicaRenderDeformer")]
    public class MagicaRenderDeformer : CoreComponent
    {
        /// <summary>
        /// データバージョン
        /// </summary>
        private const int DATA_VERSION = 2;

        /// <summary>
        /// エラーデータバージョン
        /// </summary>
        private const int ERR_DATA_VERSION = 0;

        /// <summary>
        /// レンダーメッシュのデフォーマー
        /// </summary>
        [SerializeField]
        private RenderMeshDeformer deformer = new RenderMeshDeformer();

        [SerializeField]
        private int deformerHash;
        [SerializeField]
        private int deformerVersion;

        //=========================================================================================
        /// <summary>
        /// データを識別するハッシュコードを作成して返す
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = 0;
            hash += Deformer.GetDataHash();
            return hash;
        }

        //=========================================================================================
        public RenderMeshDeformer Deformer
        {
            get
            {
                deformer.Parent = this;
                return deformer;
            }
        }

        //=========================================================================================
        void Reset()
        {
#if UNITY_EDITOR
            // 自動データ作成
            CreateData();
#endif
        }

        void OnValidate()
        {
            Deformer.OnValidate();
        }

        protected override void OnInit()
        {
            Deformer.Init();
        }

        protected override void OnDispose()
        {
            Deformer.Dispose();
        }

        protected override void OnUpdate()
        {
            Deformer.Update();
        }

        protected override void OnActive()
        {
            Deformer.OnEnable();
        }

        protected override void OnInactive()
        {
            Deformer.OnDisable();
        }

        //=========================================================================================
        public override int GetVersion()
        {
            return DATA_VERSION;
        }

        /// <summary>
        /// エラーとするデータバージョンを取得する
        /// </summary>
        /// <returns></returns>
        public override int GetErrorVersion()
        {
            return ERR_DATA_VERSION;
        }

        /// <summary>
        /// データを検証して結果を格納する
        /// </summary>
        /// <returns></returns>
        public override void CreateVerifyData()
        {
            base.CreateVerifyData();
            deformerHash = Deformer.SaveDataHash;
            deformerVersion = Deformer.SaveDataVersion;
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

            if (Deformer == null)
                return Define.Error.DeformerNull;

            var deformerError = Deformer.VerifyData();
            if (deformerError != Define.Error.None)
                return deformerError;

            if (deformerHash != Deformer.SaveDataHash)
                return Define.Error.DeformerHashMismatch;
            if (deformerVersion != Deformer.SaveDataVersion)
                return Define.Error.DeformerVersionMismatch;

            return Define.Error.None;
        }

        public override string GetInformation()
        {
            if (Deformer != null)
                return Deformer.GetInformation();
            else
                return base.GetInformation();
        }

        //=========================================================================================
        /// <summary>
        /// ボーンを置換する
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        public override void ReplaceBone(Dictionary<Transform, Transform> boneReplaceDict)
        {
            base.ReplaceBone(boneReplaceDict);

            Deformer.ReplaceBone(boneReplaceDict);
        }

        //=========================================================================================
        /// <summary>
        /// メッシュのワールド座標/法線/接線を返す（エディタ用）
        /// </summary>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        /// <returns>頂点数</returns>
        public override int GetEditorPositionNormalTangent(out List<Vector3> wposList, out List<Vector3> wnorList, out List<Vector3> wtanList)
        {
            return Deformer.GetEditorPositionNormalTangent(out wposList, out wnorList, out wtanList);
        }

        /// <summary>
        /// メッシュのトライアングルリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public override List<int> GetEditorTriangleList()
        {
            return Deformer.GetEditorTriangleList();
        }

        /// <summary>
        /// メッシュのラインリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public override List<int> GetEditorLineList()
        {
            return Deformer.GetEditorLineList();
        }

        //=========================================================================================
        /// <summary>
        /// 頂点の使用状態をリストにして返す（エディタ用）
        /// 数値が１以上ならば使用中とみなす
        /// すべて使用状態ならばnullを返す
        /// </summary>
        /// <returns></returns>
        public override List<int> GetUseList()
        {
            return null;
        }

        //=========================================================================================
        /// <summary>
        /// 共有データオブジェクト収集
        /// </summary>
        /// <returns></returns>
        public override List<ShareDataObject> GetAllShareDataObject()
        {
            var slist = base.GetAllShareDataObject();
            slist.Add(Deformer.MeshData);
            return slist;
        }

        /// <summary>
        /// sourceの共有データを複製して再セットする
        /// 再セットした共有データを返す
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public override ShareDataObject DuplicateShareDataObject(ShareDataObject source)
        {
            if (Deformer.MeshData == source)
            {
                //Deformer.MeshData = Instantiate(Deformer.MeshData);
                Deformer.MeshData = ShareDataObject.Clone(Deformer.MeshData);
                return Deformer.MeshData;
            }

            return null;
        }

#if UNITY_EDITOR
        //=========================================================================================
        /// <summary>
        /// 事前データ作成（エディット時のみ）
        /// ※RenderDeformerはマルチ選択＋コンポーネントアタッチで自動生成する必要があるのでこちらに配置する
        /// </summary>
        public void CreateData()
        {
            Debug.Log("Started creating. [" + this.name + "]");

            var serializedObject = new SerializedObject(this);

            // ターゲットオブジェクト
            serializedObject.FindProperty("deformer.targetObject").objectReferenceValue = gameObject;
            serializedObject.FindProperty("deformer.dataHash").intValue = 0;

            // 共有データ作成
            var meshData = ShareDataObject.CreateShareData<MeshData>("RenderMeshData_" + this.name);

            // renderer
            var ren = GetComponent<Renderer>();
            if (ren == null)
            {
                Debug.LogError("Creation failed. Renderer not found.");
                return;
            }

            Mesh sharedMesh = null;
            if (ren is SkinnedMeshRenderer)
            {
                meshData.isSkinning = true;
                var sren = ren as SkinnedMeshRenderer;
                sharedMesh = sren.sharedMesh;
            }
            else
            {
                meshData.isSkinning = false;
                var meshFilter = ren.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    Debug.LogError("Creation failed. MeshFilter not found.");
                    return;
                }
                sharedMesh = meshFilter.sharedMesh;
            }

            // 設計時スケール
            meshData.baseScale = transform.lossyScale;

            // 頂点
            meshData.vertexCount = sharedMesh.vertexCount;

            // 頂点ハッシュ
            var vlist = sharedMesh.vertices;
            List<ulong> vertexHashList = new List<ulong>();
            for (int i = 0; i < vlist.Length; i++)
            {
                var vhash = DataHashExtensions.GetVectorDataHash(vlist[i]);
                //Debug.Log("[" + i + "] (" + (vlist[i] * 1000) + ") :" + vhash);
                vertexHashList.Add(vhash);
            }
            meshData.vertexHashList = vertexHashList.ToArray();

            // トライアングル
            meshData.triangleCount = sharedMesh.triangles.Length / 3;

            // レンダーデフォーマーのメッシュデータにはローカル座標、法線、接線、UV、トライアングルリストは保存しない
            // 不要なため

            // ボーン
            int boneCount = meshData.isSkinning ? sharedMesh.bindposes.Length : 1;
            meshData.boneCount = boneCount;

            // メッシュデータの検証とハッシュ
            meshData.CreateVerifyData();

            serializedObject.FindProperty("deformer.sharedMesh").objectReferenceValue = sharedMesh;
            serializedObject.FindProperty("deformer.meshData").objectReferenceValue = meshData;
            serializedObject.FindProperty("deformer.meshOptimize").intValue = EditUtility.GetOptimizeMesh(sharedMesh);
            serializedObject.ApplyModifiedProperties();

            // デフォーマーデータの検証とハッシュ
            Deformer.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();

            // コアコンポーネントの検証とハッシュ
            CreateVerifyData();
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(meshData);

            // 変更後数
            Debug.Log("Creation completed. [" + this.name + "]");
        }
#endif
    }
}
