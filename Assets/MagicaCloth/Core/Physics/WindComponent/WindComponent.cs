// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 風コンポーネントの基底クラス
    /// </summary>
    public abstract class WindComponent : MonoBehaviour
    {
        /// <summary>
        /// 風データID
        /// </summary>
        protected int windId = -1;

        /// <summary>
        /// 実行状態
        /// </summary>
        protected RuntimeStatus status = new RuntimeStatus();

        public RuntimeStatus Status
        {
            get
            {
                return status;
            }
        }

        //=========================================================================================
        protected virtual void Start()
        {
            Init();
        }

        public virtual void OnEnable()
        {
            status.SetEnable(true);
            status.UpdateStatus();
        }

        public virtual void OnDisable()
        {
            status.SetEnable(false);
            status.UpdateStatus();
        }

        protected virtual void OnDestroy()
        {
            OnDispose();
            status.SetDispose();
        }

        protected virtual void Update()
        {
            if (status.IsInitSuccess)
            {
                var error = !VerifyData();
                status.SetRuntimeError(error);
                status.UpdateStatus();

                if (status.IsActive)
                    OnUpdate();
            }
        }

        //=========================================================================================
        /// <summary>
        /// 初期化
        /// 通常はStart()で呼ぶ
        /// </summary>
        /// <param name="vcnt"></param>
        void Init()
        {
            status.updateStatusAction = OnUpdateStatus;
            if (status.IsInitComplete || status.IsInitStart)
                return;
            status.SetInitStart();

            if (VerifyData() == false)
            {
                status.SetInitError();
                return;
            }

            OnInit();
            if (status.IsInitError)
                return;

            status.SetInitComplete();

            status.UpdateStatus();
        }

        // 実行状態の更新
        protected void OnUpdateStatus()
        {
            if (status.IsActive)
            {
                // 実行状態に入った
                OnActive();
            }
            else
            {
                // 実行状態から抜けた
                OnInactive();
            }
        }

        /// <summary>
        /// 現在のデータが正常（実行できる状態）か返す
        /// </summary>
        /// <returns></returns>
        public virtual bool VerifyData()
        {
            return true;
        }

        //=========================================================================================
        /// <summary>
        /// 初期化
        /// </summary>
        protected virtual void OnInit()
        {
            // 風作成
            CreateWind();

            // すでにアクティブならば有効化
            if (Status.IsActive)
                EnableWind();
        }

        /// <summary>
        /// 破棄
        /// </summary>
        protected virtual void OnDispose()
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            // 風を破棄する
            RemoveWind();
        }

        /// <summary>
        /// 更新
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// 実行状態に入った場合に呼ばれます
        /// </summary>
        protected virtual void OnActive()
        {
            // 風有効化
            EnableWind();
        }

        /// <summary>
        /// 実行状態から抜けた場合に呼ばれます
        /// </summary>
        protected virtual void OnInactive()
        {
            // 風無効化
            DisableWind();
        }

        //=========================================================================================
        /// <summary>
        /// 風有効化
        /// </summary>
        protected void EnableWind()
        {
            if (windId >= 0)
                MagicaPhysicsManager.Instance.Wind.SetEnable(windId, true, transform);
        }

        /// <summary>
        /// 風無効化
        /// </summary>
        protected void DisableWind()
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            if (windId >= 0)
                MagicaPhysicsManager.Instance.Wind.SetEnable(windId, false, transform);
        }

        //=========================================================================================
        /// <summary>
        /// 風削除
        /// </summary>
        protected void RemoveWind()
        {
            if (MagicaPhysicsManager.IsInstance())
            {
                if (windId >= 0)
                {
                    MagicaPhysicsManager.Instance.Wind.RemoveWind(windId);
                }
            }
            windId = -1;
        }

        /// <summary>
        /// 風作成
        /// </summary>
        protected abstract void CreateWind();
    }
}
