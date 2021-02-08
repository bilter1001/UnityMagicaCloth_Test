// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 仮想メッシュデフォーマーのコンポーネント
    /// </summary>
    [HelpURL("https://magicasoft.jp/magica-cloth-virtual-deformer/")]
    [AddComponentMenu("MagicaCloth/MagicaVirtualDeformer")]
    public class MagicaVirtualDeformer : CoreComponent
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
        /// 仮想メッシュのデフォーマー
        /// </summary>
        [SerializeField]
        private VirtualMeshDeformer deformer = new VirtualMeshDeformer();

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
        public VirtualMeshDeformer Deformer
        {
            get
            {
                deformer.Parent = this;
                return deformer;
            }
        }

        //=========================================================================================
        void OnValidate()
        {
            //Deformer.OnValidate();
        }

        protected override void OnInit()
        {
            LinkRenderDeformerStatus(true);
            Deformer.Init();
        }

        protected override void OnDispose()
        {
            Deformer.Dispose();
            LinkRenderDeformerStatus(false);
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

        /// <summary>
        /// 子のレンダーデフォーマーと状態をリンク
        /// </summary>
        /// <param name="sw"></param>
        private void LinkRenderDeformerStatus(bool sw)
        {
            int cnt = Deformer.RenderDeformerCount;
            for (int i = 0; i < cnt; i++)
            {
                var rd = Deformer.GetRenderDeformer(i);
                if (rd != null)
                {
                    // 連動はMagicaVirtualDeformer <-> MagicaRenderDeformerなので注意
                    if (sw)
                    {
                        status.AddChildStatus(rd.Status);
                        rd.Status.AddParentStatus(status);
                    }
                    else
                    {
                        status.RemoveChildStatus(rd.Status);
                        rd.Status.RemoveParentStatus(status);
                    }
                }
            }
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

            var d = Deformer;
            if (d == null)
                return Define.Error.DeformerNull;

            var deformerError = d.VerifyData();
            if (deformerError != Define.Error.None)
                return deformerError;

            if (deformerHash != d.SaveDataHash)
                return Define.Error.DeformerHashMismatch;
            if (deformerVersion != d.SaveDataVersion)
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
            if (Application.isPlaying)
            {
                var minfo = MagicaPhysicsManager.Instance.Mesh.GetVirtualMeshInfo(Deformer.MeshIndex);
                //var infoList = MagicaPhysicsManager.Instance.Mesh.virtualVertexInfoList;
                var vertexUseList = MagicaPhysicsManager.Instance.Mesh.virtualVertexUseList;

                var useList = new List<int>();
                for (int i = 0; i < minfo.vertexChunk.dataLength; i++)
                {
                    //uint data = infoList[minfo.vertexChunk.startIndex + i];
                    //useList.Add((int)(data & 0xffff));

                    useList.Add(vertexUseList[minfo.vertexChunk.startIndex + i]);
                }
                return useList;
            }
            else
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
    }
}
