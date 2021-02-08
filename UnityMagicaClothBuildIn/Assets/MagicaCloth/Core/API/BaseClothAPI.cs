// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp

using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// BaseCloth API
    /// </summary>
    public abstract partial class BaseCloth : PhysicsTeam
    {
        /// <summary>
        /// クロスの物理シミュレーションをリセットします
        /// Reset cloth physics simulation.
        /// </summary>
        public void ResetCloth()
        {
            if (IsValid())
            {
                MagicaPhysicsManager.Instance.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Reset_WorldInfluence, true);
                MagicaPhysicsManager.Instance.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Reset_Position, true);
                MagicaPhysicsManager.Instance.Team.ResetStabilizationTime(teamId);
            }
        }

        /// <summary>
        /// クロスの物理シミュレーションをリセットします
        /// Reset cloth physics simulation.
        /// </summary>
        /// <param name="resetStabilizationTime">Time to stabilize simulation (s)</param>
        public void ResetCloth(float resetStabilizationTime)
        {
            if (IsValid())
            {
                MagicaPhysicsManager.Instance.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Reset_WorldInfluence, true);
                MagicaPhysicsManager.Instance.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Reset_Position, true);
                MagicaPhysicsManager.Instance.Team.ResetStabilizationTime(teamId, Mathf.Max(resetStabilizationTime, 0.0f));
            }
        }

        /// <summary>
        /// タイムスケールを変更します
        /// Change the time scale.
        /// </summary>
        /// <param name="timeScale">0.0-1.0</param>
        public void SetTimeScale(float timeScale)
        {
            if (IsValid())
                MagicaPhysicsManager.Instance.Team.SetTimeScale(teamId, Mathf.Clamp01(timeScale));
        }

        /// <summary>
        /// タイムスケールを取得します
        /// Get the time scale.
        /// </summary>
        /// <returns></returns>
        public float GetTimeScale()
        {
            if (IsValid())
                return MagicaPhysicsManager.Instance.Team.GetTimeScale(teamId);
            else
                return 1.0f;
        }

        /// <summary>
        /// 外力を与えます
        /// Add external force.
        /// </summary>
        /// <param name="force"></param>
        public void AddForce(Vector3 force, PhysicsManagerTeamData.ForceMode mode)
        {
            if (IsValid() && IsActive())
                MagicaPhysicsManager.Instance.Team.SetImpactForce(teamId, force, mode);
        }

        /// <summary>
        /// 元の姿勢とシミュレーション結果とのブレンド率
        /// Blend ratio between original posture and simulation result.
        /// (0.0 = 0%, 1.0 = 100%)
        /// </summary>
        public float BlendWeight
        {
            get
            {
                return UserBlendWeight;
            }
            set
            {
                UserBlendWeight = value;
            }
        }

        /// <summary>
        /// コライダーをチームに追加します
        /// Add collider to the team.
        /// </summary>
        /// <param name="collider"></param>
        public void AddCollider(ColliderComponent collider)
        {
            if (collider)
                collider.CreateColliderParticle(teamId);
        }

        /// <summary>
        /// コライダーをチームから削除します
        /// Remove collider from the team.
        /// </summary>
        /// <param name="collider"></param>
        public void RemoveCollider(ColliderComponent collider)
        {
            if (collider)
                collider.RemoveColliderParticle(teamId);
        }

        //=========================================================================================
        // [Radius] Parameters access.
        //=========================================================================================
        /// <summary>
        /// パーティクル半径の設定
        /// Setting up a particle radius.
        /// </summary>
        /// <param name="startVal">0.001 ~ </param>
        /// <param name="endVal">0.001 ~ </param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void Radius_SetRadius(float startVal, float endVal, float curveVal = 0)
        {
            var b = clothParams.GetRadius().AutoSetup(Mathf.Max(startVal, 0.001f), Mathf.Max(endVal, 0.001f), curveVal);

            // update team particles.
            var manager = MagicaPhysicsManager.Instance;
            for (int i = 0; i < ParticleChunk.dataLength; i++)
            {
                int pindex = ParticleChunk.startIndex + i;
                float depth = manager.Particle.depthList[pindex];
                float radius = b.Evaluate(depth);
                manager.Particle.SetRadius(pindex, radius);
            }
        }

        //=========================================================================================
        // [Mass] Parameters access.
        //=========================================================================================
        /// <summary>
        /// 重量の設定
        /// Setting up a mass.
        /// </summary>
        /// <param name="startVal">1.0 ~ </param>
        /// <param name="endVal">1.0 ~ </param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void Mass_SetMass(float startVal, float endVal, float curveVal = 0)
        {
            var b = clothParams.GetMass().AutoSetup(Mathf.Max(startVal, 1.0f), Mathf.Max(endVal, 1.0f), curveVal);
            MagicaPhysicsManager.Instance.Team.SetMass(TeamId, b);

            // Parameters related to mass
            MagicaPhysicsManager.Instance.Compute.RestoreDistance.ChangeParam(
                TeamId,
                clothParams.GetMass(),
                clothParams.RestoreDistanceVelocityInfluence,
                clothParams.GetStructDistanceStiffness(),
                clothParams.UseBendDistance,
                clothParams.GetBendDistanceStiffness(),
                clothParams.UseNearDistance,
                clothParams.GetNearDistanceStiffness()
                );
        }

        //=========================================================================================
        // [Gravity] Parameters access.
        //=========================================================================================
        /// <summary>
        /// 重力加速度の設定
        /// Setting up a gravity.
        /// </summary>
        /// <param name="startVal"></param>
        /// <param name="endVal"></param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void Gravity_SetGravity(float startVal, float endVal, float curveVal = 0)
        {
            var b = clothParams.GetGravity().AutoSetup(startVal, endVal, curveVal);
            MagicaPhysicsManager.Instance.Team.SetGravity(TeamId, b);
        }

        //=========================================================================================
        // [Drag] Parameters access.
        //=========================================================================================
        /// <summary>
        /// 空気抵抗の設定
        /// Setting up a drag.
        /// </summary>
        /// <param name="startVal">0.0 ~ 1.0</param>
        /// <param name="endVal">0.0 ~ 1.0</param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void Drag_SetDrag(float startVal, float endVal, float curveVal = 0)
        {
            var b = clothParams.GetDrag().AutoSetup(startVal, endVal, curveVal);
            MagicaPhysicsManager.Instance.Team.SetDrag(TeamId, b);
        }

        //=========================================================================================
        // [Distance Disable] Parameters access.
        //=========================================================================================
        /// <summary>
        /// アクティブ設定
        /// Active settings.
        /// </summary>
        public bool DistanceDisable_Active
        {
            get
            {
                return clothParams.UseDistanceDisable;
            }
            set
            {
                clothParams.UseDistanceDisable = value;
            }
        }

        /// <summary>
        /// 距離計測の対象設定
        /// nullを指定するとメインカメラが参照されます。
        /// Target setting for distance measurement.
        /// If null is specified, the main camera is referred.
        /// </summary>
        public Transform DistanceDisable_ReferenceObject
        {
            get
            {
                return clothParams.DisableReferenceObject;
            }
            set
            {
                clothParams.DisableReferenceObject = value;
            }
        }

        /// <summary>
        /// シミュレーションを無効化する距離
        /// Distance to disable simulation.
        /// </summary>
        public float DistanceDisable_Distance
        {
            get
            {
                return clothParams.DisableDistance;
            }
            set
            {
                clothParams.DisableDistance = Mathf.Max(value, 0.0f);
            }
        }

        /// <summary>
        /// シミュレーションを無効化するフェード距離
        /// DistanceDisable_DistanceからDistanceDisable_FadeDistanceの距離を引いた位置からフェードが開始します。
        /// Fade distance to disable simulation.
        /// Fade from DistanceDisable_Distance minus DistanceDisable_FadeDistance distance.
        /// </summary>
        public float DistanceDisable_FadeDistance
        {
            get
            {
                return clothParams.DisableFadeDistance;
            }
            set
            {
                clothParams.DisableFadeDistance = Mathf.Max(value, 0.0f);
            }
        }

        //=========================================================================================
        // [External Force] Parameter access.
        //=========================================================================================
        /// <summary>
        /// パーティクル重量の影響率(0.0-1.0)
        /// Particle weight effect rate (0.0-1.0).
        /// </summary>
        public float ExternalForce_MassInfluence
        {
            get
            {
                return clothParams.MassInfluence;
            }
            set
            {
                clothParams.MassInfluence = value;
                MagicaPhysicsManager.Instance.Team.SetExternalForce(TeamId, clothParams.MassInfluence, clothParams.WindInfluence, clothParams.WindRandomScale);
            }
        }

        /// <summary>
        /// 風の影響率(1.0 = 100%)
        /// Wind influence rate (1.0 = 100%).
        /// </summary>
        public float ExternalForce_WindInfluence
        {
            get
            {
                return clothParams.WindInfluence;
            }
            set
            {
                clothParams.WindInfluence = value;
                MagicaPhysicsManager.Instance.Team.SetExternalForce(TeamId, clothParams.MassInfluence, clothParams.WindInfluence, clothParams.WindRandomScale);
            }
        }

        /// <summary>
        /// 風のランダム率(1.0 = 100%)
        /// Wind random rate (1.0 = 100%).
        /// </summary>
        public float ExternalForce_WindRandomScale
        {
            get
            {
                return clothParams.WindRandomScale;
            }
            set
            {
                clothParams.WindRandomScale = value;
                MagicaPhysicsManager.Instance.Team.SetExternalForce(TeamId, clothParams.MassInfluence, clothParams.WindInfluence, clothParams.WindRandomScale);
            }
        }

        //=========================================================================================
        // [World Influence] Parameter access.
        //=========================================================================================
        /// <summary>
        /// 移動影響の設定
        /// Setting up a moving influence.
        /// </summary>
        /// <param name="startVal">0.0 ~ 1.0</param>
        /// <param name="endVal">0.0 ~ 1.0</param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void WorldInfluence_SetMovementInfluence(float startVal, float endVal, float curveVal = 0)
        {
            var b = clothParams.GetWorldMoveInfluence().AutoSetup(startVal, endVal, curveVal);
            MagicaPhysicsManager.Instance.Team.SetWorldInfluence(TeamId, clothParams.MaxMoveSpeed, b, clothParams.GetWorldRotationInfluence());
        }

        /// <summary>
        /// 回転影響の設定
        /// Setting up a rotation influence.
        /// </summary>
        /// <param name="startVal">0.0 ~ 1.0</param>
        /// <param name="endVal">0.0 ~ 1.0</param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void WorldInfluence_SetRotationInfluence(float startVal, float endVal, float curveVal = 0)
        {
            var b = clothParams.GetWorldRotationInfluence().AutoSetup(startVal, endVal, curveVal);
            MagicaPhysicsManager.Instance.Team.SetWorldInfluence(TeamId, clothParams.MaxMoveSpeed, clothParams.GetWorldMoveInfluence(), b);
        }

        /// <summary>
        /// 最大速度の設定
        /// Setting up a max move speed.(m/s)
        /// </summary>
        public float WorldInfluence_MaxMoveSpeed
        {
            get
            {
                return clothParams.MaxMoveSpeed;
            }
            set
            {
                clothParams.MaxMoveSpeed = Mathf.Max(value, 0.0f);
                MagicaPhysicsManager.Instance.Team.SetWorldInfluence(TeamId, clothParams.MaxMoveSpeed, clothParams.GetWorldMoveInfluence(), clothParams.GetWorldRotationInfluence());
            }
        }

        /// <summary>
        /// 自動テレポートの有効設定
        /// Enable automatic teleportation.
        /// </summary>
        public bool WorldInfluence_ResetAfterTeleport
        {
            get
            {
                return clothParams.UseResetTeleport;
            }
            set
            {
                clothParams.UseResetTeleport = value;
                MagicaPhysicsManager.Instance.Team.SetAfterTeleport(TeamId, clothParams.UseResetTeleport, clothParams.TeleportDistance, clothParams.TeleportRotation);
            }
        }

        /// <summary>
        /// 自動テレポートと検出する１フレームの移動距離
        /// Travel distance in one frame to be judged as automatic teleport.
        /// </summary>
        public float WorldInfluence_TeleportDistance
        {
            get
            {
                return clothParams.TeleportDistance;
            }
            set
            {
                clothParams.TeleportDistance = value;
                MagicaPhysicsManager.Instance.Team.SetAfterTeleport(TeamId, clothParams.UseResetTeleport, clothParams.TeleportDistance, clothParams.TeleportRotation);
            }
        }

        /// <summary>
        /// 自動テレポートと検出する１フレームの回転角度(0.0 ~ 360.0)
        /// Rotation angle of one frame to be judged as automatic teleport.(0.0 ~ 360.0)
        /// </summary>
        public float WorldInfluence_TeleportRotation
        {
            get
            {
                return clothParams.TeleportRotation;
            }
            set
            {
                clothParams.TeleportRotation = value;
                MagicaPhysicsManager.Instance.Team.SetAfterTeleport(TeamId, clothParams.UseResetTeleport, clothParams.TeleportDistance, clothParams.TeleportRotation);
            }
        }

        /// <summary>
        /// リセット後の安定時間を設定(s)
        /// Set stabilization time after reset.
        /// </summary>
        public float WorldInfluence_StabilizationTime
        {
            get
            {
                return clothParams.ResetStabilizationTime;
            }
            set
            {
                clothParams.ResetStabilizationTime = Mathf.Max(value, 0.0f);
                MagicaPhysicsManager.Instance.Team.SetStabilizationTime(TeamId, clothParams.ResetStabilizationTime);

            }
        }

        //=========================================================================================
        // [Collider Collision] Parameter access.
        //=========================================================================================
        /// <summary>
        /// アクティブ設定
        /// Active settings.
        /// </summary>
        public bool ColliderCollision_Active
        {
            get
            {
                return clothParams.UseCollision;
            }
            set
            {
                clothParams.SetCollision(value, clothParams.Friction);
                MagicaPhysicsManager.Instance.Team.SetFlag(TeamId, PhysicsManagerTeamData.Flag_Collision_KeepShape, clothParams.KeepInitialShape);
                MagicaPhysicsManager.Instance.Compute.Collision.ChangeParam(TeamId, clothParams.UseCollision);
            }
        }

        //=========================================================================================
        // [Penetration] Parameter access.
        //=========================================================================================
        /// <summary>
        /// アクティブ設定
        /// Active settings.
        /// </summary>
        public bool Penetration_Active
        {
            get
            {
                return clothParams.UsePenetration;
            }
            set
            {
                clothParams.UsePenetration = value;
                MagicaPhysicsManager.Instance.Compute.Penetration.ChangeParam(TeamId, clothParams.UsePenetration, clothParams.GetPenetrationDistance(), clothParams.GetPenetrationRadius());
            }
        }
    }
}
