// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 各チームのデータ
    /// </summary>
    [System.Serializable]
    public class PhysicsTeamData : IDataHash
    {
        // チーム固有のコライダーリスト
        [SerializeField]
        private List<ColliderComponent> colliderList = new List<ColliderComponent>();

        /// <summary>
        /// 移動制限で無視するコライダーリスト
        /// </summary>
        [SerializeField]
        private List<ColliderComponent> penetrationIgnoreColliderList = new List<ColliderComponent>();

        /// <summary>
        /// スキニング用ボーンリスト
        /// </summary>
        //[SerializeField]
        //private List<Transform> skinningBoneList = new List<Transform>();

        /// <summary>
        /// スキニングで無視するコライダーリスト
        /// </summary>
        //[SerializeField]
        //private List<ColliderComponent> skinningIgnoreColliderList = new List<ColliderComponent>();

        /// <summary>
        /// 親アバターのコライダーを結合するかどうか
        /// </summary>
        [SerializeField]
        private bool mergeAvatarCollider = true;

        //=========================================================================================
        /// <summary>
        /// データハッシュを求める
        /// </summary>
        /// <returns></returns>
        public int GetDataHash()
        {
            return colliderList.GetDataHash();
        }

        //=========================================================================================
        public void Init(int teamId)
        {
            // コライダーをチームに参加させる
            foreach (var collider in colliderList)
            {
                if (collider)
                {
                    collider.CreateColliderParticle(teamId);
                }
            }
        }

        public void Dispose(int teamId)
        {
            if (MagicaPhysicsManager.IsInstance())
            {
                // コライダーをチームから除外する
                foreach (var collider in colliderList)
                {
                    if (collider)
                    {
                        collider.RemoveColliderParticle(teamId);
                    }
                }
            }
        }

        //=========================================================================================
        public int ColliderCount
        {
            get
            {
                return colliderList.Count;
            }
        }

        public List<ColliderComponent> ColliderList
        {
            get
            {
                return colliderList;
            }
        }

        public List<ColliderComponent> PenetrationIgnoreColliderList
        {
            get
            {
                return penetrationIgnoreColliderList;
            }
        }

        //public List<Transform> SkinningBoneList
        //{
        //    get
        //    {
        //        return skinningBoneList;
        //    }
        //}

        //public List<ColliderComponent> SkinningIgnoreColliderList
        //{
        //    get
        //    {
        //        return skinningIgnoreColliderList;
        //    }
        //}

        public bool MergeAvatarCollider
        {
            get
            {
                return mergeAvatarCollider;
            }
        }

        //=========================================================================================
        /// <summary>
        /// コライダーリスト検証
        /// </summary>
        public void ValidateColliderList()
        {
            // コライダーのnullや重複を削除する
            ShareDataObject.RemoveNullAndDuplication(colliderList);
        }
    }
}
