// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 物理チーム
    /// 特定のパーティクルと機能を１つのチームとして管理します。
    /// 当たり判定はチーム内部のパーティクルに対してのみ有効です。
    /// これによりチームごとに独立した環境を構築することが可能です。
    /// 例：キャラクタAとBは当たり判定が干渉しないようにする
    /// </summary>
    public abstract class PhysicsTeam : CoreComponent
    {
        [SerializeField]
        protected PhysicsTeamData teamData = new PhysicsTeamData();

        /// <summary>
        /// ユーザー設定ブレンド率
        /// </summary>
        [SerializeField]
        private float userBlendWeight = 1.0f;

        //=========================================================================================
        /// <summary>
        /// この物理チームのID
        /// </summary>
        protected int teamId = -1;

        /// <summary>
        /// この物理チームで管理するパーティクル
        /// </summary>
        protected ChunkData particleChunk = new ChunkData();

        /// <summary>
        /// 速度影響／ワープ判定トランスフォーム
        /// </summary>
        protected Transform influenceTarget;


        //=========================================================================================
        /// <summary>
        /// データハッシュを求める
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = teamData.GetDataHash();
            return hash;
        }

        //=========================================================================================
        public int TeamId
        {
            get
            {
                return teamId;
            }
        }

        public PhysicsTeamData TeamData
        {
            get
            {
                return teamData;
            }
        }

        public ChunkData ParticleChunk
        {
            get
            {
                return particleChunk;
            }
        }

        public Transform InfluenceTarget
        {
            set
            {
                influenceTarget = value;
            }
            get
            {
                return influenceTarget;
            }
        }

        public float UserBlendWeight
        {
            get
            {
                return userBlendWeight;
            }
            set
            {
                userBlendWeight = value;
            }
        }

        //=========================================================================================
        protected override void OnInit()
        {
            // チーム作成
            teamId = MagicaPhysicsManager.Instance.Team.CreateTeam(this, 0);
            TeamData.Init(TeamId);
        }

        /// <summary>
        /// 破棄
        /// 通常はOnDestroy()で呼ぶ
        /// </summary>
        protected override void OnDispose()
        {
            // 破棄
            if (TeamId >= 0)
            {
                TeamData.Dispose(TeamId);

                // チーム削除
                if (MagicaPhysicsManager.IsInstance())
                    MagicaPhysicsManager.Instance.Team.RemoveTeam(teamId);
            }
        }

        /// <summary>
        /// 更新
        /// </summary>
        protected override void OnUpdate()
        {
        }

        /// <summary>
        /// 実行状態に入った場合に呼ばれます
        /// </summary>
        protected override void OnActive()
        {
            // チーム有効化
            MagicaPhysicsManager.Instance.Team.SetEnable(teamId, true);

            // コライダーボーンの未来予測をリセットする
            // 遅延実行＋再アクティブ時のみ
            if (MagicaPhysicsManager.Instance.IsDelay && ActiveCount > 1)
            {
                MagicaPhysicsManager.Instance.Team.ResetFuturePredictionCollidere(TeamId);
            }
        }

        /// <summary>
        /// 実行状態から抜けた場合に呼ばれます
        /// </summary>
        protected override void OnInactive()
        {
            if (MagicaPhysicsManager.IsInstance())
            {
                // チーム無効化
                MagicaPhysicsManager.Instance.Team.SetEnable(teamId, false);
            }
        }

        //=========================================================================================
        /// <summary>
        /// 実行状態を取得
        /// </summary>
        /// <returns></returns>
        public bool IsActive()
        {
            if (MagicaPhysicsManager.IsInstance())
                return MagicaPhysicsManager.Instance.Team.IsActive(teamId);
            else
                return false;
        }

        /// <summary>
        /// チームデータが存在するか判定
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            if (MagicaPhysicsManager.IsInstance())
                return MagicaPhysicsManager.Instance.Team.IsValid(teamId);
            else
                return false;
        }

        //=========================================================================================
        /// <summary>
        /// パーティクルを１つ追加
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="team"></param>
        /// <param name="wpos"></param>
        /// <param name="wrot"></param>
        /// <param name="lpos"></param>
        /// <param name="lrot"></param>
        /// <param name="radius"></param>
        /// <param name="mass"></param>
        /// <param name="gravity"></param>
        /// <param name="drag"></param>
        /// <param name="depth"></param>
        /// <param name="maxVelocity"></param>
        /// <param name="target"></param>
        /// <param name="targetLocalPos"></param>
        /// <returns></returns>
        //public ChunkData CreateParticle(
        //    uint flag,
        //    int team,
        //    float3 wpos, quaternion wrot,
        //    float3 lpos, quaternion lrot,
        //    float depth,
        //    float radius,
        //    Transform target, float3 targetLocalPos
        //    )
        //{
        //    var c = MagicaPhysicsManager.Instance.Particle.CreateParticle(
        //        flag,
        //        team,
        //        wpos, wrot,
        //        lpos, lrot,
        //        depth,
        //        radius,
        //        target, targetLocalPos
        //        );

        //    // チームパーティクルとして管理する
        //    particleChunk = c;

        //    return c;
        //}

        /// <summary>
        /// パーティクルをグループで追加
        /// </summary>
        /// <param name="team"></param>
        /// <param name="count"></param>
        /// <param name="funcFlag"></param>
        /// <param name="funcWpos"></param>
        /// <param name="funcWrot"></param>
        /// <param name="funcLpos"></param>
        /// <param name="funcLrot"></param>
        /// <param name="funcRadius"></param>
        /// <param name="funcMass"></param>
        /// <param name="funcGravity"></param>
        /// <param name="funcDrag"></param>
        /// <param name="funcDepth"></param>
        /// <param name="funcMaxVelocity"></param>
        /// <param name="funcTarget"></param>
        /// <param name="funcTargetLocalPos"></param>
        /// <returns></returns>
        public ChunkData CreateParticle(
            int team,
            int count,
            System.Func<int, uint> funcFlag,
            System.Func<int, float3> funcWpos,
            System.Func<int, quaternion> funcWrot,
            //System.Func<int, float3> funcLpos = null,
            //System.Func<int, quaternion> funcLrot = null,
            System.Func<int, float> funcDepth,
            System.Func<int, float3> funcRadius,
            //System.Func<int, Transform> funcTarget = null,
            System.Func<int, float3> funcTargetLocalPos
            )
        {
            var c = MagicaPhysicsManager.Instance.Particle.CreateParticle(
                team,
                count,
                funcFlag,
                funcWpos,
                funcWrot,
                //funcLpos,
                //funcLrot,
                funcDepth,
                funcRadius,
                //funcTarget,
                funcTargetLocalPos
                );

            // チームパーティクルとして管理する
            particleChunk = c;

            return c;
        }

        /// <summary>
        /// パーティクル削除（全体のみ）
        /// </summary>
        public void RemoveAllParticle()
        {
            MagicaPhysicsManager.Instance.Particle.RemoveParticle(particleChunk);
            particleChunk.Clear();
        }

        /// <summary>
        /// 管理するパーティクルを有効化する
        /// </summary>
        public void EnableParticle(
            System.Func<int, Transform> funcTarget,
            System.Func<int, float3> funcLpos,
            System.Func<int, quaternion> funcLrot
            )
        {
            MagicaPhysicsManager.Instance.Particle.SetEnable(particleChunk, true, funcTarget, funcLpos, funcLrot);
        }

        /// <summary>
        /// 管理するパーティクルを無効化する
        /// </summary>
        public void DisableParticle(
            System.Func<int, Transform> funcTarget,
            System.Func<int, float3> funcLpos,
            System.Func<int, quaternion> funcLrot
            )
        {
            if (MagicaPhysicsManager.IsInstance())
            {
                MagicaPhysicsManager.Instance.Particle.SetEnable(particleChunk, false, funcTarget, funcLpos, funcLrot);
            }
        }

        //=========================================================================================
        /// <summary>
        /// 現在のデータが正常（実行できる状態）か返す
        /// </summary>
        /// <returns></returns>
        public override Define.Error VerifyData()
        {
            return base.VerifyData();
        }
    }
}
