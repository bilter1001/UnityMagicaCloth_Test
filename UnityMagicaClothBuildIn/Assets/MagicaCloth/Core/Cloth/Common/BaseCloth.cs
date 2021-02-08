// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicaCloth
{
    /// <summary>
    /// クロス基本クラス
    /// </summary>
    public abstract partial class BaseCloth : PhysicsTeam
    {
        /// <summary>
        /// パラメータ設定
        /// </summary>
        [SerializeField]
        protected ClothParams clothParams = new ClothParams();

        [SerializeField]
        protected List<int> clothParamDataHashList = new List<int>();

        /// <summary>
        /// クロスデータ
        /// </summary>
        [SerializeField]
        private ClothData clothData = null;

        [SerializeField]
        protected int clothDataHash;
        [SerializeField]
        protected int clothDataVersion;

        /// <summary>
        /// 頂点選択データ
        /// </summary>
        [SerializeField]
        private SelectionData clothSelection = null;

        [SerializeField]
        private int clothSelectionHash;
        [SerializeField]
        private int clothSelectionVersion;

        /// <summary>
        /// ランタイムクロス設定
        /// </summary>
        protected ClothSetup setup = new ClothSetup();


        //=========================================================================================
        private float oldBlendRatio = -1.0f;


        //=========================================================================================
        /// <summary>
        /// データハッシュを求める
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = base.GetDataHash();
            if (ClothData != null)
                hash += ClothData.GetDataHash();
            if (ClothSelection != null)
                hash += ClothSelection.GetDataHash();

            return hash;
        }

        //=========================================================================================
        public ClothParams Params
        {
            get
            {
                return clothParams;
            }
        }

        public ClothData ClothData
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    return clothData;
                else
                {
                    // unity2019.3で参照がnullとなる不具合の対処（臨時）
                    var so = new SerializedObject(this);
                    return so.FindProperty("clothData").objectReferenceValue as ClothData;
                }
#else
                return clothData;
#endif
            }
            set
            {
                clothData = value;
            }
        }

        public SelectionData ClothSelection
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    return clothSelection;
                else
                {
                    // unity2019.3で参照がnullとなる不具合の対処（臨時）
                    var so = new SerializedObject(this);
                    return so.FindProperty("clothSelection").objectReferenceValue as SelectionData;
                }
#else
                return clothSelection;
#endif
            }
        }

        public ClothSetup Setup
        {
            get
            {
                return setup;
            }
        }

        //=========================================================================================
        protected virtual void Reset()
        {
        }

        protected virtual void OnValidate()
        {
            if (Application.isPlaying == false)
                return;

            // クロスパラメータのラインタイム変更
            setup.ChangeData(this, clothParams);
        }

        //=========================================================================================
        protected override void OnInit()
        {
            base.OnInit();
            BaseClothInit();
        }

        protected override void OnActive()
        {
            base.OnActive();
            // パーティクル有効化
            EnableParticle(UserTransform, UserTransformLocalPosition, UserTransformLocalRotation);
            SetUseMesh(true);
            ClothActive();
        }

        protected override void OnInactive()
        {
            base.OnInactive();
            // パーティクル無効化
            DisableParticle(UserTransform, UserTransformLocalPosition, UserTransformLocalRotation);
            SetUseMesh(false);
            ClothInactive();
        }

        protected override void OnDispose()
        {
            BaseClothDispose();
            base.OnDispose();
        }

        //=========================================================================================
        void BaseClothInit()
        {
            // デフォーマー初期化
            int dcount = GetDeformerCount();
            for (int i = 0; i < dcount; i++)
            {
                var deformer = GetDeformer(i);
                if (deformer == null)
                {
                    Status.SetInitError();
                    return;
                }

                // デフォーマーと状態を連動
                LinkDeformerStatus(deformer, true);

                deformer.Init();
                if (deformer.Status.IsInitError)
                {
                    Status.SetInitError();
                    return;
                }
            }

            if (VerifyData() != Define.Error.None)
            {
                Status.SetInitError();
                return;
            }

            // クロス初期化
            ClothInit();

            // クロス初期化後の主にワーカーへの登録
            WorkerInit();

            // 頂点有効化
            SetUseVertex(true);
        }

        void BaseClothDispose()
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            // デフォーマとの状態の連動を解除
            int dcount = GetDeformerCount();
            for (int i = 0; i < dcount; i++)
            {
                var deformer = GetDeformer(i);
                if (deformer != null)
                {
                    LinkDeformerStatus(deformer, false);
                }
            }

            if (Status.IsInitSuccess)
            {
                // 頂点無効化
                SetUseVertex(false);

                // クロス破棄
                // この中ですべてのコンストレイントとワーカーからチームのデータが自動削除される
                setup.ClothDispose(this);

                ClothDispose();
            }
        }

        /// <summary>
        /// デフォーマとの状態を連動
        /// ※デフォーマが接続するCoreComponentのStatusとリンクすることに注意！
        /// </summary>
        /// <param name="deformer"></param>
        /// <param name="sw"></param>
        private void LinkDeformerStatus(BaseMeshDeformer deformer, bool sw)
        {
            var core = deformer.Parent.GetComponent<CoreComponent>();
            if (core)
            {
                // デフォーマが親、クロスコンポーネントが子として接続するので注意！(v1.5.1)
                if (sw)
                {
                    Status.AddParentStatus(core.Status);
                    core.Status.AddChildStatus(Status);
                    //Status.AddChildStatus(core.Status);
                    //core.Status.AddParentStatus(Status);
                }
                else
                {
                    Status.RemoveParentStatus(core.Status);
                    core.Status.RemoveChildStatus(Status);
                    //Status.RemoveChildStatus(core.Status);
                    //core.Status.RemoveParentStatus(Status);
                }
            }
        }


        /// <summary>
        /// クロス初期化
        /// </summary>
        protected virtual void ClothInit()
        {
            setup.ClothInit(this, GetMeshData(), ClothData, clothParams, UserFlag);
        }

        protected virtual void ClothActive()
        {
            setup.ClothActive(this, clothParams, ClothData);

            // デフォーマの未来予測をリセットする
            // 遅延実行＋再アクティブ時のみ
            if (MagicaPhysicsManager.Instance.IsDelay && ActiveCount > 1)
            {
                int dcount = GetDeformerCount();
                for (int i = 0; i < dcount; i++)
                {
                    var deformer = GetDeformer(i);
                    if (deformer != null)
                    {
                        deformer.ResetFuturePrediction();
                    }
                }
            }
        }

        protected virtual void ClothInactive()
        {
            setup.ClothInactive(this);
        }

        protected virtual void ClothDispose()
        {
        }

        /// <summary>
        /// 頂点ごとのパーティクルフラグ設定（不要な場合は０）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected abstract uint UserFlag(int vindex);

        /// <summary>
        /// 頂点ごとの連動トランスフォーム設定（不要な場合はnull）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected abstract Transform UserTransform(int vindex);

        /// <summary>
        /// 頂点ごとの連動トランスフォームのLocalPositionを返す（不要な場合は0）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected abstract float3 UserTransformLocalPosition(int vindex);

        /// <summary>
        /// 頂点ごとの連動トランスフォームのLocalRotationを返す（不要な場合はquaternion.identity)
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected abstract quaternion UserTransformLocalRotation(int vindex);

        /// <summary>
        /// デフォーマーの数を返す
        /// </summary>
        /// <returns></returns>
        public abstract int GetDeformerCount();

        /// <summary>
        /// デフォーマーを返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public abstract BaseMeshDeformer GetDeformer(int index);

        /// <summary>
        /// クロス初期化時に必要なMeshDataを返す（不要ならnull）
        /// </summary>
        /// <returns></returns>
        protected abstract MeshData GetMeshData();

        /// <summary>
        /// クロス初期化後の主にワーカーへの登録
        /// </summary>
        protected abstract void WorkerInit();


        //=========================================================================================
        /// <summary>
        /// 使用デフォーマー設定
        /// </summary>
        /// <param name="sw"></param>
        void SetUseMesh(bool sw)
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            if (Status.IsInitSuccess == false)
                return;

            int dcount = GetDeformerCount();
            for (int i = 0; i < dcount; i++)
            {
                var deformer = GetDeformer(i);
                if (deformer != null)
                {
                    if (sw)
                        deformer.AddUseMesh(this);
                    else
                        deformer.RemoveUseMesh(this);
                }
            }
        }

        /// <summary>
        /// 使用頂点設定
        /// </summary>
        /// <param name="sw"></param>
        void SetUseVertex(bool sw)
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            int dcount = GetDeformerCount();
            for (int i = 0; i < dcount; i++)
            {
                var deformer = GetDeformer(i);
                if (deformer != null)
                {
                    SetDeformerUseVertex(sw, deformer, i);
                }
            }
        }

        /// <summary>
        /// デフォーマーごとの使用頂点設定
        /// 使用頂点に対して AddUseVertex() / RemoveUseVertex() を実行する
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="deformer"></param>
        /// <param name="deformerIndex"></param>
        protected abstract void SetDeformerUseVertex(bool sw, BaseMeshDeformer deformer, int deformerIndex);

        //=========================================================================================
        /// <summary>
        /// ブレンド率更新
        /// </summary>
        public void UpdateBlend()
        {
            if (teamId <= 0)
                return;

            // ユーザーブレンド率
            float blend = UserBlendWeight;

            // 距離ブレンド率
            blend *= setup.DistanceBlendRatio;

            // 変更チェック
            blend = Mathf.Clamp01(blend);
            if (blend != oldBlendRatio)
            {
                // チームデータへ反映
                MagicaPhysicsManager.Instance.Team.SetBlendRatio(teamId, blend);

                // コンポーネント有効化判定
                SetUserEnable(blend > 0.01f);

                oldBlendRatio = blend;
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーンを置換する
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        public override void ReplaceBone(Dictionary<Transform, Transform> boneReplaceDict)
        {
            base.ReplaceBone(boneReplaceDict);

            // セットアップデータのボーン置換
            setup.ReplaceBone(this, clothParams, boneReplaceDict);
        }

        //=========================================================================================
        /// <summary>
        /// データを検証して結果を格納する
        /// </summary>
        /// <returns></returns>
        public override void CreateVerifyData()
        {
            base.CreateVerifyData();
            clothDataHash = ClothData != null ? ClothData.SaveDataHash : 0;
            clothDataVersion = ClothData != null ? ClothData.SaveDataVersion : 0;
            clothSelectionHash = ClothSelection != null ? ClothSelection.SaveDataHash : 0;
            clothSelectionVersion = ClothSelection != null ? ClothSelection.SaveDataVersion : 0;

            // パラメータハッシュ
            clothParamDataHashList.Clear();
            for (int i = 0; i < (int)ClothParams.ParamType.Max; i++)
            {
                int paramHash = clothParams.GetParamHash(this, (ClothParams.ParamType)i);
                clothParamDataHashList.Add(paramHash);
            }
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

            // clothDataはオプション
            if (ClothData != null)
            {
                var clothDataError = ClothData.VerifyData();
                if (clothDataError != Define.Error.None)
                    return clothDataError;
                if (clothDataHash != ClothData.SaveDataHash)
                    return Define.Error.ClothDataHashMismatch;
                if (clothDataVersion != ClothData.SaveDataVersion)
                    return Define.Error.ClothDataVersionMismatch;
            }

            // clothSelectionはオプション
            if (ClothSelection != null)
            {
                var clothSelectionError = ClothSelection.VerifyData();
                if (clothSelectionError != Define.Error.None)
                    return clothSelectionError;
                if (clothSelectionHash != ClothSelection.SaveDataHash)
                    return Define.Error.ClothSelectionHashMismatch;
                if (clothSelectionVersion != ClothSelection.SaveDataVersion)
                    return Define.Error.ClothSelectionVersionMismatch;
            }

            return Define.Error.None;
        }

        /// <summary>
        /// パラメータに重要な変更が発生したか調べる
        /// 重要な変更はデータを作り直す必要を指している
        /// </summary>
        /// <param name="ptype"></param>
        /// <returns></returns>
        public bool HasChangedParam(ClothParams.ParamType ptype)
        {
            int index = (int)ptype;
            if (index >= clothParamDataHashList.Count)
            {
                return false;
            }
            int hash = clothParams.GetParamHash(this, ptype);
            if (hash == 0)
                return false;

            return clothParamDataHashList[index] != hash;
        }

        //=========================================================================================
        /// <summary>
        /// 共有データオブジェクト収集
        /// </summary>
        /// <returns></returns>
        public override List<ShareDataObject> GetAllShareDataObject()
        {
            var sdata = base.GetAllShareDataObject();
            sdata.Add(ClothData);
            sdata.Add(ClothSelection);
            return sdata;
        }

        /// <summary>
        /// sourceの共有データを複製して再セットする
        /// 再セットした共有データを返す
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public override ShareDataObject DuplicateShareDataObject(ShareDataObject source)
        {
            if (ClothData == source)
            {
                //clothData = Instantiate(ClothData);
                clothData = ShareDataObject.Clone(ClothData);
                return clothData;
            }

            if (ClothSelection == source)
            {
                //clothSelection = Instantiate(ClothSelection);
                clothSelection = ShareDataObject.Clone(ClothSelection);
                return clothSelection;
            }

            return null;
        }

    }
}
