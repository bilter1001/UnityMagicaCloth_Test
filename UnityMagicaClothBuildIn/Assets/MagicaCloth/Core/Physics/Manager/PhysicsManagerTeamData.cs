// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// チームデータ
    /// チーム０はグローバルとして扱う
    /// </summary>
    public class PhysicsManagerTeamData : PhysicsManagerAccess
    {
        /// <summary>
        /// チームフラグビット
        /// </summary>
        public const uint Flag_Enable = 0x00000001; // 有効フラグ
        public const uint Flag_Interpolate = 0x00000002; // 補間処理適用
        public const uint Flag_FixedNonRotation = 0x00000004; // 固定パーティクルは回転させない
        //public const uint Flag_DirectionalDamping = 0x00000008; // 重力方向減衰あり
        public const uint Flag_IgnoreClampPositionVelocity = 0x00000010; // ClampPositionの最大移動速度制限を無視する(Spring系)
        public const uint Flag_Collision = 0x00000020;
        public const uint Flag_AfterCollision = 0x00000040;
        //public const uint Flag_Update = 0x00000004; // 更新フラグ
        public const uint Flag_Reset_WorldInfluence = 0x00010000; // ワールド影響をリセットする
        public const uint Flag_Reset_Position = 0x00020000; // クロスパーティクル姿勢をリセット
        public const uint Flag_Collision_KeepShape = 0x00040000; // 当たり判定時の初期姿勢をキープ

        /// <summary>
        /// 速度変更モード
        /// </summary>
        public enum ForceMode
        {
            None,

            VelocityAdd,                    // 速度に加算（質量の影響を受ける）
            VelocityChange,                 // 速度を変更（質量の影響を受ける）

            VelocityAddWithoutMass = 10,    // 速度に加算（質量無視）
            VelocityChangeWithoutMass,      // 速度を変更（質量無視）
        }

        /// <summary>
        /// チーム状態
        /// </summary>
        public struct TeamData
        {
            /// <summary>
            /// チームが生成したパーティクル（コライダーパーティクルは除くので注意）
            /// </summary>
            public ChunkData particleChunk;

            /// <summary>
            /// チームが生成したコライダーパーティクルリスト
            /// </summary>
            public ChunkData colliderChunk;

            //public ChunkData skinningBoneChunk;

            /// <summary>
            /// フラグビットデータ
            /// </summary>
            public uint flag;

            /// <summary>
            /// 摩擦係数(0.0-1.0)
            /// </summary>
            public float friction;

            /// <summary>
            /// セルフコリジョンの影響範囲
            /// </summary>
            public float selfCollisionRange;

            /// <summary>
            /// 自身のボーンインデックス
            /// </summary>
            public int boneIndex;

            /// <summary>
            /// 現在のチームスケール倍率
            /// </summary>
            public float3 initScale;            // 自身のボーンのクロスデータ作成時のスケール長
            public float scaleRatio;            // スケール倍率
            public float3 scaleDirection;       // スケール値方向(xyz)：(1/-1)のみ
            public float4 quaternionScale;      // 回転フリップ用スケール

            /// <summary>
            /// 重力の方向減衰ターゲットボーンインデックス
            /// </summary>
            //public int directionalDampingBoneIndex;

            /// <summary>
            /// 重力の方向減衰ターゲットボーンの基準上方向（ローカル）
            /// </summary>
            //public float3 directionalDampingLocalDir;

            /// <summary>
            /// チーム固有の現在時間
            /// </summary>
            public float time;

            /// <summary>
            /// チーム固有の更新時間(deltaTime)
            /// </summary>
            public float addTime;

            /// <summary>
            /// チーム固有のタイムスケール(0.0-1.0)
            /// </summary>
            public float timeScale;

            /// <summary>
            /// チーム内更新時間
            /// </summary>
            public float nowTime;

            /// <summary>
            /// チームでの最初の物理更新が実行される時間
            /// </summary>
            public float startTime;

            /// <summary>
            /// チーム更新回数
            /// </summary>
            public int updateCount;

            /// <summary>
            /// 現在の更新カウント
            /// </summary>
            public int runCount;

            /// <summary>
            /// ブレンド率(0.0-1.0)
            /// </summary>
            public float blendRatio;

            /// <summary>
            /// 外力（計算結果）
            /// </summary>
            public float3 externalForce;

            /// <summary>
            /// 外力の重力影響率
            /// </summary>
            public float forceMassInfluence;

            /// <summary>
            /// 風の影響率
            /// </summary>
            public float forceWindInfluence;

            /// <summary>
            /// 風のランダム率
            /// </summary>
            public float forceWindRandomScale;

            /// <summary>
            /// 現在の速度ウエイト
            /// </summary>
            public float velocityWeight;

            /// <summary>
            /// 速度ウエイトの回復速度(s)
            /// </summary>
            public float velocityRecoverySpeed;


            public ForceMode forceMode;
            public float3 impactForce;


            /// <summary>
            /// 距離拘束データへのインデックス
            /// </summary>
            public short restoreDistanceGroupIndex;
            public short triangleBendGroupIndex;
            public short clampDistanceGroupIndex;
            public short clampDistance2GroupIndex;
            public short clampPositionGroupIndex;
            public short clampRotationGroupIndex;
            public short restoreRotationGroupIndex;
            public short adjustRotationGroupIndex;
            public short springGroupIndex;
            public short volumeGroupIndex;
            public short airLineGroupIndex;
            public short lineWorkerGroupIndex;
            public short triangleWorkerGroupIndex;
            public short selfCollisionGroupIndex;
            public short edgeCollisionGroupIndex;
            public short penetrationGroupIndex;
            public short baseSkinningGroupIndex;

            /// <summary>
            /// データが有効か判定する
            /// </summary>
            /// <returns></returns>
            public bool IsActive()
            {
                return (flag & Flag_Enable) != 0;
            }

            /// <summary>
            /// 更新すべきか判定する
            /// </summary>
            /// <returns></returns>
            public bool IsUpdate()
            {
                return runCount < updateCount;
            }

            public bool IsRunning()
            {
                return updateCount > 0;
            }

            /// <summary>
            /// 補間を行うか判定する
            /// </summary>
            /// <returns></returns>
            public bool IsInterpolate()
            {
                return (flag & Flag_Interpolate) != 0;
            }

            /// <summary>
            /// フラグ判定
            /// </summary>
            /// <param name="flag"></param>
            /// <returns></returns>
            public bool IsFlag(uint flag)
            {
                return (this.flag & flag) != 0;
            }

            /// <summary>
            /// フラグ設定
            /// </summary>
            /// <param name="flag"></param>
            /// <param name="sw"></param>
            public void SetFlag(uint flag, bool sw)
            {
                if (sw)
                    this.flag |= flag;
                else
                    this.flag &= ~flag;
            }

            /// <summary>
            /// パーティクル座標リセットが必要か判定する
            /// </summary>
            /// <returns></returns>
            public bool IsReset()
            {
                return (flag & (Flag_Reset_WorldInfluence | Flag_Reset_Position)) != 0;
            }
        }

        /// <summary>
        /// チームデータリスト
        /// </summary>
        public FixedNativeList<TeamData> teamDataList;

        public FixedNativeList<CurveParam> teamMassList;
        public FixedNativeList<CurveParam> teamGravityList;
        public FixedNativeList<CurveParam> teamDragList;
        public FixedNativeList<CurveParam> teamMaxVelocityList;
        //public FixedNativeList<CurveParam> teamDirectionalDampingList;

        /// <summary>
        /// チームのワールド移動回転影響
        /// </summary>
        public struct WorldInfluence
        {
            /// <summary>
            /// 影響力(0.0-1.0)
            /// </summary>
            public CurveParam moveInfluence;
            public CurveParam rotInfluence;
            public float maxMoveSpeed;      // (m/s)

            /// <summary>
            /// ワールド移動量
            /// </summary>
            public float3 nowPosition;
            public float3 oldPosition;
            public float3 moveIgnoreOffset; // 移動影響を無視する移動ベクトル
            public float3 moveOffset;       // 移動影響を考慮する移動ベクトル（実際の移動量はmoveIgnoreOffset + moveOffset）

            /// <summary>
            /// ワールド回転量
            /// </summary>
            public quaternion nowRotation;
            public quaternion oldRotation;
            public quaternion rotationOffset;

            /// <summary>
            /// テレポート
            /// </summary>
            public int resetTeleport;
            public float teleportDistance;
            public float teleportRotation;

            /// <summary>
            /// リセット
            /// </summary>
            public float stabilizationTime; // 安定化時間(s)
        }
        public FixedNativeList<WorldInfluence> teamWorldInfluenceList;

        /// <summary>
        /// チームごとの判定コライダー
        /// </summary>
        public FixedMultiNativeList<int> colliderList;

        //public FixedMultiNativeList<int> skinningBoneList;

        /// <summary>
        /// チームごとのチームコンポーネント参照への辞書（キー：チームID）
        /// nullはグローバルチーム
        /// </summary>
        private Dictionary<int, PhysicsTeam> teamComponentDict = new Dictionary<int, PhysicsTeam>();

        /// <summary>
        /// 稼働中のチーム数
        /// </summary>
        int activeTeamCount;

        //=========================================================================================
        /// <summary>
        /// 初期設定
        /// </summary>
        public override void Create()
        {
            teamDataList = new FixedNativeList<TeamData>();
            teamMassList = new FixedNativeList<CurveParam>();
            teamGravityList = new FixedNativeList<CurveParam>();
            teamDragList = new FixedNativeList<CurveParam>();
            teamMaxVelocityList = new FixedNativeList<CurveParam>();
            teamWorldInfluenceList = new FixedNativeList<WorldInfluence>();
            //teamDirectionalDampingList = new FixedNativeList<CurveParam>();
            colliderList = new FixedMultiNativeList<int>();
            //skinningBoneList = new FixedMultiNativeList<int>();

            // グローバルチーム[0]を作成し常に有効にしておく
            CreateTeam(null, 0);
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            if (teamDataList == null)
                return;

            //skinningBoneList.Dispose();
            colliderList.Dispose();
            teamMassList.Dispose();
            teamGravityList.Dispose();
            teamDragList.Dispose();
            teamMaxVelocityList.Dispose();
            teamWorldInfluenceList.Dispose();
            //teamDirectionalDampingList.Dispose();
            teamDataList.Dispose();
            teamComponentDict.Clear();
        }

        //=========================================================================================
        /// <summary>
        /// 登録チーム数を返す
        /// [0]はグローバルチームなので-1する
        /// </summary>
        public int TeamCount
        {
            get
            {
                return teamDataList.Count - 1;
            }
        }

        /// <summary>
        /// チーム配列数を返す
        /// </summary>
        public int TeamLength
        {
            get
            {
                return teamDataList.Length;
            }
        }

        /// <summary>
        /// 現在活動中のチーム数を返す
        /// これが0の場合はチームが無いか、すべて停止中となっている
        /// </summary>
        public int ActiveTeamCount
        {
            get
            {
                return activeTeamCount;
            }
        }

        /// <summary>
        /// コライダーの数を返す
        /// </summary>
        public int ColliderCount
        {
            get
            {
                if (colliderList == null)
                    return 0;

                return colliderList.Count;
            }
        }

        //=========================================================================================
        /// <summary>
        /// チームを作成する
        /// </summary>
        /// <returns></returns>
        public int CreateTeam(PhysicsTeam team, uint flag)
        {
            var data = new TeamData();
            flag |= Flag_Enable;
            flag |= Flag_Reset_WorldInfluence; // 移動影響リセット
            data.flag = flag;

            data.friction = 0;
            data.boneIndex = team != null ? 0 : -1; // グローバルチームはボーン無し
            data.initScale = 0;
            data.scaleDirection = 1;
            data.scaleRatio = 1;
            data.quaternionScale = 1;
            //data.directionalDampingBoneIndex = team != null ? 0 : -1; // グローバルチームはボーン無し
            //data.directionalDampingLocalDir = new float3(0, 1, 0);
            data.timeScale = 1.0f;
            data.blendRatio = 1.0f;
            data.forceMassInfluence = 1.0f;
            data.forceWindInfluence = 1.0f;
            data.forceWindRandomScale = 0.0f;

            // 拘束チームインデックス
            data.restoreDistanceGroupIndex = -1;
            data.triangleBendGroupIndex = -1;
            data.clampDistanceGroupIndex = -1;
            data.clampDistance2GroupIndex = -1;
            data.clampPositionGroupIndex = -1;
            data.clampRotationGroupIndex = -1;
            data.restoreRotationGroupIndex = -1;
            data.adjustRotationGroupIndex = -1;
            data.springGroupIndex = -1;
            data.volumeGroupIndex = -1;
            data.airLineGroupIndex = -1;
            data.lineWorkerGroupIndex = -1;
            data.triangleWorkerGroupIndex = -1;
            data.selfCollisionGroupIndex = -1;
            data.edgeCollisionGroupIndex = -1;
            data.penetrationGroupIndex = -1;
            data.baseSkinningGroupIndex = -1;

            int teamId = teamDataList.Add(data);
            teamMassList.Add(new CurveParam(1.0f));
            teamGravityList.Add(new CurveParam());
            teamDragList.Add(new CurveParam());
            teamMaxVelocityList.Add(new CurveParam());
            //teamDirectionalDampingList.Add(new CurveParam());

            teamWorldInfluenceList.Add(new WorldInfluence());

            teamComponentDict.Add(teamId, team);

            if (team != null)
                activeTeamCount++;

            return teamId;
        }

        /// <summary>
        /// チームを削除する
        /// </summary>
        /// <param name="teamId"></param>
        public void RemoveTeam(int teamId)
        {
            if (teamId >= 0)
            {
                teamDataList.Remove(teamId);
                teamMassList.Remove(teamId);
                teamGravityList.Remove(teamId);
                teamDragList.Remove(teamId);
                teamMaxVelocityList.Remove(teamId);
                teamWorldInfluenceList.Remove(teamId);
                //teamDirectionalDampingList.Remove(teamId);
                teamComponentDict.Remove(teamId);
            }
        }

        /// <summary>
        /// チームの有効フラグ切り替え
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="sw"></param>
        public void SetEnable(int teamId, bool sw)
        {
            if (teamId >= 0)
            {
                SetFlag(teamId, Flag_Enable, sw);
                SetFlag(teamId, Flag_Reset_Position, sw); // 位置リセット
                SetFlag(teamId, Flag_Reset_WorldInfluence, sw); // 移動影響リセット
            }
        }

        /// <summary>
        /// チームが存在するか判定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public bool IsValid(int teamId)
        {
            return teamId >= 0;
        }

        /// <summary>
        /// チームデータが存在するか判定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public bool IsValidData(int teamId)
        {
            return teamId >= 0 && teamComponentDict.ContainsKey(teamId);
        }

        /// <summary>
        /// チームが有効状態か判定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public bool IsActive(int teamId)
        {
            if (teamId >= 0)
                return teamDataList[teamId].IsActive();
            else
                return false;
        }

        /// <summary>
        /// チームの状態フラグ設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="flag"></param>
        /// <param name="sw"></param>
        public void SetFlag(int teamId, uint flag, bool sw)
        {
            if (teamId < 0)
                return;
            TeamData data = teamDataList[teamId];
            bool oldvalid = data.IsActive();
            data.SetFlag(flag, sw);
            bool newvalid = data.IsActive();
            if (oldvalid != newvalid)
            {
                // アクティブチーム数カウント
                activeTeamCount += newvalid ? 1 : -1;
            }
            teamDataList[teamId] = data;
        }

        public void SetParticleChunk(int teamId, ChunkData chunk)
        {
            TeamData data = teamDataList[teamId];
            data.particleChunk = chunk;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームの摩擦係数設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="friction"></param>
        public void SetFriction(int teamId, float friction)
        {
            TeamData data = teamDataList[teamId];
            data.friction = friction;
            teamDataList[teamId] = data;
        }

        public void SetMass(int teamId, BezierParam mass)
        {
            teamMassList[teamId] = new CurveParam(mass);
        }

        public void SetGravity(int teamId, BezierParam gravity)
        {
            teamGravityList[teamId] = new CurveParam(gravity);
        }

        //public void SetDirectionalDamping(int teamId, BezierParam directionalDamping)
        //{
        //    teamDirectionalDampingList[teamId] = new CurveParam(directionalDamping);
        //}

        public void SetDrag(int teamId, BezierParam drag)
        {
            teamDragList[teamId] = new CurveParam(drag);
        }

        public void SetMaxVelocity(int teamId, BezierParam maxVelocity)
        {
            teamMaxVelocityList[teamId] = new CurveParam(maxVelocity);
        }

        public void SetExternalForce(int teamId, float massInfluence, float windInfluence, float windRandomScale)
        {
            TeamData data = teamDataList[teamId];
            data.forceMassInfluence = massInfluence;
            data.forceWindInfluence = windInfluence;
            data.forceWindRandomScale = windRandomScale;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// ワールド移動影響設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="worldMoveInfluence"></param>
        public void SetWorldInfluence(int teamId, float maxSpeed, BezierParam moveInfluence, BezierParam rotInfluence, bool resetTeleport, float teleportDistance, float teleportRotation, float resetStabilizationTime)
        {
            var data = teamWorldInfluenceList[teamId];
            data.maxMoveSpeed = maxSpeed;
            data.moveInfluence = new CurveParam(moveInfluence);
            data.rotInfluence = new CurveParam(rotInfluence);
            data.resetTeleport = resetTeleport ? 1 : 0;
            data.teleportDistance = teleportDistance;
            data.teleportRotation = teleportRotation;
            data.stabilizationTime = resetStabilizationTime;
            teamWorldInfluenceList[teamId] = data;
        }

        public void SetWorldInfluence(int teamId, float maxSpeed, BezierParam moveInfluence, BezierParam rotInfluence)
        {
            var data = teamWorldInfluenceList[teamId];
            data.maxMoveSpeed = maxSpeed;
            data.moveInfluence = new CurveParam(moveInfluence);
            data.rotInfluence = new CurveParam(rotInfluence);
            teamWorldInfluenceList[teamId] = data;
        }

        public void SetAfterTeleport(int teamId, bool resetTeleport, float teleportDistance, float teleportRotation)
        {
            var data = teamWorldInfluenceList[teamId];
            data.resetTeleport = resetTeleport ? 1 : 0;
            data.teleportDistance = teleportDistance;
            data.teleportRotation = teleportRotation;
            teamWorldInfluenceList[teamId] = data;
        }

        public void SetStabilizationTime(int teamId, float resetStabilizationTime)
        {
            var data = teamWorldInfluenceList[teamId];
            data.stabilizationTime = resetStabilizationTime;
            teamWorldInfluenceList[teamId] = data;
        }

        /// <summary>
        /// セルフコリジョンの影響範囲設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="range"></param>
        public void SetSelfCollisionRange(int teamId, float range)
        {
            TeamData data = teamDataList[teamId];
            data.selfCollisionRange = range;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームのボーンインデックスを設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="boneIndex"></param>
        public void SetBoneIndex(int teamId, int boneIndex, Vector3 initScale)
        {
            TeamData data = teamDataList[teamId];
            data.boneIndex = boneIndex;
            data.initScale = initScale;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームの重力方向影響ボーンインデックスを設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="boneIndex"></param>
        //public void SetDirectionalDampingBoneIndex(int teamId, bool sw, int boneIndex, float3 upDir)
        //{
        //    TeamData data = teamDataList[teamId];
        //    data.directionalDampingBoneIndex = boneIndex;
        //    data.directionalDampingLocalDir = upDir;
        //    data.SetFlag(Flag_DirectionalDamping, sw);
        //    teamDataList[teamId] = data;
        //}

        /// <summary>
        /// チームにコライダーを追加
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="particleIndex"></param>
        public void AddCollider(int teamId, int particleIndex)
        {
            //Develop.Log($"AddCollider team:{teamId} pindex:{particleIndex}");
            TeamData data = teamDataList[teamId];
            var c = data.colliderChunk;
            if (c.IsValid() == false)
            {
                // 新規
                c = colliderList.AddChunk(4);
            }
            // 追加
            c = colliderList.AddData(c, particleIndex);

            data.colliderChunk = c;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームからコライダーを削除
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="particleIndex"></param>
        public void RemoveCollider(int teamId, int particleIndex)
        {
            //Develop.Log($"RemoveCollider team:{teamId} pindex:{particleIndex}");
            TeamData data = teamDataList[teamId];
            var c = data.colliderChunk;
            if (c.IsValid())
            {
                c = colliderList.RemoveData(c, particleIndex);
                data.colliderChunk = c;
                teamDataList[teamId] = data;
            }
        }

        /// <summary>
        /// チームのコライダーをすべて削除
        /// </summary>
        /// <param name="teamId"></param>
        public void RemoveCollider(int teamId)
        {
            //Develop.Log($"RemoveAllCollider team:{teamId}");
            TeamData data = teamDataList[teamId];
            var c = data.colliderChunk;
            if (c.IsValid())
            {
                colliderList.RemoveChunk(c);
                data.colliderChunk = new ChunkData();
                teamDataList[teamId] = data;
            }
        }

        /// <summary>
        /// チームのコライダートランスフォームの未来予測をリセットする
        /// </summary>
        /// <param name="teamId"></param>
        public void ResetFuturePredictionCollidere(int teamId)
        {
            TeamData data = teamDataList[teamId];
            var c = data.colliderChunk;
            if (c.IsValid())
            {
                colliderList.Process(c, (pindex) =>
                {
                    MagicaPhysicsManager.Instance.Particle.ResetFuturePredictionTransform(pindex);
                });
            }
        }

#if false
        /// <summary>
        /// チームにスキニングボーンインデックスを追加
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="particleIndex"></param>
        public void AddSkinningBoneIndex(int teamId, int boneIndex)
        {
            TeamData data = teamDataList[teamId];
            var c = data.skinningBoneChunk;
            if (c.IsValid() == false)
            {
                // 新規
                c = skinningBoneList.AddChunk(4);
            }
            // 追加
            c = skinningBoneList.AddData(c, boneIndex);

            data.skinningBoneChunk = c;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームのスキニングボーンインデックスをすべて削除
        /// </summary>
        /// <param name="teamId"></param>
        public void RemoveSkinningBoneIndex(int teamId)
        {
            TeamData data = teamDataList[teamId];
            var c = data.skinningBoneChunk;
            if (c.IsValid())
            {
                skinningBoneList.RemoveChunk(c);
                data.skinningBoneChunk = new ChunkData();
                teamDataList[teamId] = data;
            }
        }
#endif


        /// <summary>
        /// チームのタイムスケールを設定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="timeScale">0.0-1.0</param>
        public void SetTimeScale(int teamId, float timeScale)
        {
            TeamData data = teamDataList[teamId];
            data.timeScale = Mathf.Clamp01(timeScale);
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームのタイムスケールを取得する
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public float GetTimeScale(int teamId)
        {
            return teamDataList[teamId].timeScale;
        }

        /// <summary>
        /// チームのブレンド率を設定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="blendRatio"></param>
        public void SetBlendRatio(int teamId, float blendRatio)
        {
            TeamData data = teamDataList[teamId];
            data.blendRatio = Mathf.Clamp01(blendRatio);
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームのブレンド率を取得する
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public float GetBlendRatio(int teamId)
        {
            return teamDataList[teamId].blendRatio;
        }

        /// <summary>
        /// 外力を与える
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="force">１秒あたりの外力</param>
        public void SetImpactForce(int teamId, float3 force, ForceMode mode)
        {
            TeamData data = teamDataList[teamId];
            data.impactForce = force;
            data.forceMode = mode;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームのリセット後の安定化時間を設定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="resetStabilizationTime"></param>
        public void ResetStabilizationTime(int teamId, float resetStabilizationTime = -1.0f)
        {
            TeamData data = teamDataList[teamId];
            data.velocityWeight = 0;
            if (resetStabilizationTime >= 0.0f)
            {
                data.velocityRecoverySpeed = resetStabilizationTime;
            }
            else
            {
                // インスペクタで設定された値
                var wdata = teamWorldInfluenceList[teamId];
                data.velocityRecoverySpeed = wdata.stabilizationTime;
            }
            teamDataList[teamId] = data;
        }

        //=========================================================================================
        /// <summary>
        /// アクティブ状態に限らず行うチーム更新（メインスレッド）
        /// </summary>
        public void PreUpdateTeamAlways()
        {
            var mainCamera = Camera.main != null ? Camera.main.transform : manager.transform;

            foreach (var team in Team.teamComponentDict.Values)
            {
                var baseCloth = team as BaseCloth;
                if (baseCloth == null)
                    continue;

                // 距離ブレンド率更新
                float blend = 1.0f;
                if (baseCloth.Params.UseDistanceDisable)
                {
                    var refObject = baseCloth.Params.DisableReferenceObject;
                    if (refObject == null)
                        refObject = mainCamera;

                    float dist = Vector3.Distance(team.transform.position, refObject.position);
                    float disableDist = baseCloth.Params.DisableDistance;
                    float fadeDist = Mathf.Max(disableDist - baseCloth.Params.DisableFadeDistance, 0.0f);

                    blend = Mathf.InverseLerp(disableDist, fadeDist, dist);
                }
                baseCloth.Setup.DistanceBlendRatio = blend;

                // ブレンド率更新
                baseCloth.UpdateBlend();
            }
        }

        /// <summary>
        /// 今回フレームの最大実効回数を求める
        /// </summary>
        /// <param name="ups"></param>
        /// <returns></returns>
        public int CalcMaxUpdateCount(int ups, float dtime, float updateDeltaTime)
        {
            // 固定更新では更新回数は１回
            if (manager.UpdateTime.GetUpdateMode() == UpdateTimeManager.UpdateMode.OncePerFrame)
                return 1;

            // 全チームの最大実行回数を求める
            float globalTimeScale = manager.GetGlobalTimeScale();
            int maxcnt = 0;
            foreach (var kv in teamComponentDict)
            {
                if (kv.Value == null)
                    continue;

                int tid = kv.Key;
                if (tid <= 0)
                    continue;

                var tdata = teamDataList[tid];

                int cnt = 0;
                float timeScale = tdata.timeScale * globalTimeScale;
                float addTime = dtime * timeScale;
                float nowTime = tdata.nowTime + addTime;
                cnt = (int)(nowTime / updateDeltaTime);

                // リセットフラグがONならば最低１回更新とする
                if (tdata.IsReset())
                    cnt = Mathf.Max(cnt, 1);

                maxcnt = Mathf.Max(maxcnt, cnt);
            }

            // upsに関係なく１フレームの最大回数は4回
            maxcnt = Mathf.Min(maxcnt, 4);

            return maxcnt;
        }

        //=========================================================================================
        public void PreUpdateTeamData(float dtime, float updateDeltaTime, int ups, int maxUpdateCount)
        {
            bool unscaledUpdate = manager.UpdateTime.IsUnscaledUpdate;
            float globalTimeScale = manager.GetGlobalTimeScale();

            // 固定更新では１回の更新時間をupdateDeltaTimeに設定する
            if (unscaledUpdate == false)
                dtime = updateDeltaTime;

            // チームデータ前処理
            var job = new PreProcessTeamDataJob()
            {
                //time = Time.time,
                dtime = dtime,
                updateDeltaTime = updateDeltaTime,
                globalTimeScale = globalTimeScale,
                //unscaledUpdate = unscaledUpdate,
                //ups = ups,
                maxUpdateCount = maxUpdateCount,
                unityTimeScale = Time.timeScale,
                elapsedTime = Time.time,

                teamData = Team.teamDataList.ToJobArray(),
                teamWorldInfluenceList = Team.teamWorldInfluenceList.ToJobArray(),

                bonePosList = Bone.bonePosList.ToJobArray(),
                boneRotList = Bone.boneRotList.ToJobArray(),
                boneSclList = Bone.boneSclList.ToJobArray(),

                windData = Wind.windDataList.ToJobArray(),
                directionalWindId = Wind.DirectionalWindId,
            };
            Compute.MasterJob = job.Schedule(Team.teamDataList.Length, 8, Compute.MasterJob);
        }

        [BurstCompile]
        struct PreProcessTeamDataJob : IJobParallelFor
        {
            //public float time;
            public float dtime;
            public float updateDeltaTime;
            public float globalTimeScale;
            //public bool unscaledUpdate;
            //public int ups;
            public int maxUpdateCount;
            public float unityTimeScale;
            public float elapsedTime;

            public NativeArray<TeamData> teamData;
            public NativeArray<WorldInfluence> teamWorldInfluenceList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> boneRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> boneSclList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerWindData.WindData> windData;
            [Unity.Collections.ReadOnly]
            public int directionalWindId;

            // チームデータごと
            public void Execute(int teamId)
            {
                var tdata = teamData[teamId];

                // グローバルチーム判定
                bool isGlobal = teamId == 0;

                if (tdata.IsActive() == false || (isGlobal == false && tdata.boneIndex < 0))
                {
                    tdata.updateCount = 0;
                    tdata.runCount = 0;
                    teamData[teamId] = tdata;
                    return;
                }

                if (isGlobal)
                {
                    // グローバルチーム
                    // 時間更新（タイムスケール対応）
                    UpdateTime(ref tdata, false);
                }
                else
                {
                    // チームボーン情報
                    var bpos = bonePosList[tdata.boneIndex];
                    var brot = boneRotList[tdata.boneIndex];
                    var bscl = boneSclList[tdata.boneIndex];

                    // チームスケール倍率算出
                    if (tdata.initScale.x > 0.0f)
                    {
                        tdata.scaleRatio = math.length(bscl) / math.length(tdata.initScale);
                    }

                    // マイナススケール対応
                    tdata.scaleDirection = math.sign(bscl);
                    if (bscl.x < 0 || bscl.y < 0 || bscl.z < 0)
                        tdata.quaternionScale = new float4(-math.sign(bscl), 1);
                    else
                        tdata.quaternionScale = 1;

                    // ワールド移動影響
                    WorldInfluence wdata = teamWorldInfluenceList[teamId];

                    // 移動量算出
                    float3 moveVector = bpos - wdata.oldPosition;
                    quaternion moveRot = MathUtility.FromToRotation(wdata.oldRotation, brot);

                    // 移動影響（無視する移動量＋考慮する移動量）
                    var moveLen = math.length(moveVector);
                    if (moveLen > 1e-06f)
                    {
                        float speed = moveLen / dtime;

                        //const float maxspeed = 10.0f; // todo:最大速度テスト m/s
                        float ratio = math.max(speed - wdata.maxMoveSpeed, 0.0f) / speed;
                        wdata.moveIgnoreOffset = moveVector * ratio;
                        wdata.moveOffset = moveVector - wdata.moveIgnoreOffset;
                    }
                    else
                    {
                        wdata.moveIgnoreOffset = 0;
                        wdata.moveOffset = 0;
                    }

                    // 回転影響
                    wdata.rotationOffset = moveRot;

                    // 速度ウエイト更新
                    if (tdata.velocityWeight < 1.0f)
                    {
                        float addw = tdata.velocityRecoverySpeed > 1e-6f ? dtime / tdata.velocityRecoverySpeed : 1.0f;
                        tdata.velocityWeight = math.saturate(tdata.velocityWeight + addw);
                    }

                    // テレポート判定
                    if (wdata.resetTeleport == 1)
                    {
                        // テレポート距離はチームスケールを乗算する
                        if (moveLen >= wdata.teleportDistance * tdata.scaleRatio || math.degrees(MathUtility.Angle(moveRot)) >= wdata.teleportRotation)
                        {
                            tdata.SetFlag(Flag_Reset_WorldInfluence, true);
                            tdata.SetFlag(Flag_Reset_Position, true);
                        }
                    }

                    bool reset = false;
                    if (tdata.IsFlag(Flag_Reset_WorldInfluence) || tdata.IsFlag(Flag_Reset_Position))
                    {
                        // リセット
                        wdata.moveOffset = 0;
                        wdata.moveIgnoreOffset = 0;
                        wdata.rotationOffset = quaternion.identity;
                        wdata.oldPosition = bpos;
                        wdata.oldRotation = brot;

                        // チームタイムリセット（強制更新）
                        tdata.nowTime = updateDeltaTime;

                        // 速度ウエイト
                        tdata.velocityWeight = wdata.stabilizationTime > 1e-6f ? 0.0f : 1.0f;
                        tdata.velocityRecoverySpeed = wdata.stabilizationTime;
                        reset = true;
                    }
                    wdata.nowPosition = bpos;
                    wdata.nowRotation = brot;

                    // 書き戻し
                    teamWorldInfluenceList[teamId] = wdata;

                    // 時間更新（タイムスケール対応）
                    UpdateTime(ref tdata, reset);

                    // リセットフラグOFF
                    tdata.SetFlag(Flag_Reset_WorldInfluence, false);

                    // 風
                    Wind(ref tdata, bpos);
                }

                // 書き戻し
                teamData[teamId] = tdata;
            }

            /// <summary>
            /// チーム時間更新
            /// </summary>
            /// <param name="tdata"></param>
            /// <param name="reset"></param>
            void UpdateTime(ref TeamData tdata, bool reset)
            {
                // 時間更新（タイムスケール対応）
                // チームごとに固有の時間で動作する
                tdata.updateCount = 0;
                tdata.runCount = 0;
                float timeScale = tdata.timeScale * globalTimeScale;
                float addTime = dtime * timeScale;

                tdata.time += addTime;
                tdata.addTime = addTime;

                // 時間ステップ
                float nowTime = tdata.nowTime + addTime;
                while (nowTime >= updateDeltaTime)
                {
                    nowTime -= updateDeltaTime;
                    tdata.updateCount++;
                }

                // リセットフラグがONならば最低１回更新とする
                if (reset)
                    tdata.updateCount = Mathf.Max(tdata.updateCount, 1);

                // 最大実行回数
                //tdata.updateCount = math.min(tdata.updateCount, ups / 30);
                tdata.updateCount = math.min(tdata.updateCount, maxUpdateCount);

                tdata.nowTime = nowTime;

                // スタート時間（最初の物理更新が実行される時間）
                tdata.startTime = tdata.time - nowTime - updateDeltaTime * (tdata.updateCount - 1);

                // 補間再生判定
                if (timeScale < 0.99f || unityTimeScale < 0.99f)
                {
                    tdata.SetFlag(Flag_Interpolate, true);
                }
                else
                {
                    tdata.SetFlag(Flag_Interpolate, false);
                }
            }

            /// <summary>
            /// 風の計算
            /// </summary>
            /// <param name="tdata"></param>
            void Wind(ref TeamData tdata, float3 pos)
            {
                float3 externalForce = 0;

                if (tdata.forceWindInfluence >= 0.01f)
                {
                    // ノイズ
                    var noisePos = new float2(pos.x, pos.z) * 0.1f;
                    noisePos.x += elapsedTime * 1.0f; // 周期（数値を高くするとランダム性が増す）2.0f?
                    var nv = noise.snoise(noisePos); // -1.0f～1.0f

                    // 方向風（ワールドに１つ）
                    if (directionalWindId >= 0)
                    {
                        var wdata = windData[directionalWindId];
                        if (wdata.IsActive())
                        {
                            var wdir = wdata.direction;
                            wdir *= wdata.main; // main wind
                            float scl = math.max(nv * tdata.forceWindRandomScale, -1.0f); // scale
                            externalForce += (wdir + wdir * scl);
                        }
                    }

                    // ゾーン風
                    // ※未実装

                    // 風の影響率
                    externalForce *= tdata.forceWindInfluence;
                }

                // 外力設定
                tdata.externalForce = externalForce;
            }
        }

        //=========================================================================================
        public void PostUpdateTeamData()
        {
            // チームデータ後処理
            var job = new PostProcessTeamDataJob()
            {
                teamData = Team.teamDataList.ToJobArray(),
                teamWorldInfluenceList = Team.teamWorldInfluenceList.ToJobArray(),
            };
            Compute.MasterJob = job.Schedule(Team.teamDataList.Length, 8, Compute.MasterJob);
        }

        [BurstCompile]
        struct PostProcessTeamDataJob : IJobParallelFor
        {
            public NativeArray<TeamData> teamData;
            public NativeArray<WorldInfluence> teamWorldInfluenceList;

            // チームデータごと
            public void Execute(int index)
            {
                var tdata = teamData[index];
                if (tdata.IsActive() == false)
                    return;

                var wdata = teamWorldInfluenceList[index];

                wdata.oldPosition = wdata.nowPosition;
                wdata.oldRotation = wdata.nowRotation;

                if (tdata.IsRunning())
                {
                    // 外部フォースをリセット
                    tdata.impactForce = 0;
                    tdata.forceMode = ForceMode.None;
                }

                // 姿勢リセットフラグリセット
                tdata.SetFlag(Flag_Reset_Position, false);

                // 書き戻し
                teamData[index] = tdata;
                teamWorldInfluenceList[index] = wdata;
            }
        }

        //=========================================================================================
        public void UpdateTeamUpdateCount()
        {
            // チームデータ後処理
            var job = new UpdateTeamUpdateCountJob()
            {
                teamData = Team.teamDataList.ToJobArray(),
            };
            Compute.MasterJob = job.Schedule(Team.teamDataList.Length, 8, Compute.MasterJob);
        }

        [BurstCompile]
        struct UpdateTeamUpdateCountJob : IJobParallelFor
        {
            public NativeArray<TeamData> teamData;

            // チームデータごと
            public void Execute(int index)
            {
                var tdata = teamData[index];
                if (tdata.IsActive() == false)
                    return;

                tdata.runCount++;

                // 書き戻し
                teamData[index] = tdata;
            }
        }
    }
}
