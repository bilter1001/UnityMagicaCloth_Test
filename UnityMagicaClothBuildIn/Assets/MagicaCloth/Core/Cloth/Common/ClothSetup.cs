// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// クロスの実行時設定
    /// </summary>
    public class ClothSetup
    {
        // チームのボーンインデックス
        int teamBoneIndex;

        // 重力方向減衰ボーンインデックス
        //int teamDirectionalDampingBoneIndex;

        /// <summary>
        /// 距離によるブレンド率
        /// </summary>
        float distanceBlendRatio = 1.0f;

        //=========================================================================================
        /// <summary>
        /// クロス初期化
        /// </summary>
        /// <param name="team"></param>
        /// <param name="meshData">メッシュデータ(不要ならnull)</param>
        /// <param name="clothData"></param>
        /// <param name="param"></param>
        /// <param name="funcUserFlag">各頂点の追加フラグ設定アクション</param>
        /// <param name="funcUserTransform">各頂点の連動トランスフォーム設定アクション</param>
        public void ClothInit(
            PhysicsTeam team,
            MeshData meshData,
            ClothData clothData,
            ClothParams param,
            System.Func<int, uint> funcUserFlag
            )
        {
            var manager = MagicaPhysicsManager.Instance;
            var compute = manager.Compute;

            // チームデータ設定
            manager.Team.SetMass(team.TeamId, param.GetMass());
            manager.Team.SetGravity(team.TeamId, param.GetGravity());
            manager.Team.SetDrag(team.TeamId, param.GetDrag());
            manager.Team.SetMaxVelocity(team.TeamId, param.GetMaxVelocity());
            manager.Team.SetFriction(team.TeamId, param.Friction);
            manager.Team.SetExternalForce(team.TeamId, param.MassInfluence, param.WindInfluence, param.WindRandomScale);
            //manager.Team.SetDirectionalDamping(team.TeamId, param.GetDirectionalDamping());

            // ワールド移動影響
            manager.Team.SetWorldInfluence(
                team.TeamId,
                param.MaxMoveSpeed,
                param.GetWorldMoveInfluence(),
                param.GetWorldRotationInfluence(),
                param.UseResetTeleport,
                param.TeleportDistance,
                param.TeleportRotation,
                param.ResetStabilizationTime
                );

            int vcnt = clothData.VertexUseCount;
            Debug.Assert(vcnt > 0);
            Debug.Assert(clothData.useVertexList.Count > 0);

            // パーティクル追加（使用頂点のみ）
            var c = team.CreateParticle(team.TeamId, clothData.useVertexList.Count,
                // flag
                (i) =>
                {
                    bool isFix = clothData.IsFixedVertex(i) || clothData.IsExtendVertex(i); // 固定もしくは拡張
                    uint flag = 0;
                    if (funcUserFlag != null)
                        flag = funcUserFlag(i); // ユーザーフラグ
                    if (isFix)
                        flag |= (PhysicsManagerParticleData.Flag_Kinematic | PhysicsManagerParticleData.Flag_Step_Update);
                    if (clothData.IsFlag(i, ClothData.VertexFlag_TriangleRotation))
                        flag |= PhysicsManagerParticleData.Flag_TriangleRotation; // TriangleWorkerによる回転補間
                    //flag |= (param.UseCollision && !isFix) ? PhysicsManagerParticleData.Flag_Collision : 0;
                    flag |= PhysicsManagerParticleData.Flag_Reset_Position;
                    return flag;
                },
                // wpos
                null,
                // wrot
                null,
                // depth
                (i) =>
                {
                    return clothData.vertexDepthList[i];
                },
                // radius
                (i) =>
                {
                    float depth = clothData.vertexDepthList[i];
                    return param.GetRadius(depth);
                },
                // target local pos
                null
                );
            manager.Team.SetParticleChunk(team.TeamId, c);

            // 原点スプリング拘束
            if (param.UseSpring)
            {
                // 拘束データ
                int group = compute.Spring.AddGroup(
                    team.TeamId,
                    param.UseSpring,
                    param.GetSpringPower()
                    );
                var teamData = manager.Team.teamDataList[team.TeamId];
                teamData.springGroupIndex = (short)group;
                manager.Team.teamDataList[team.TeamId] = teamData;
            }

            // 原点移動制限
            if (param.UseClampPositionLength)
            {
                // 拘束データ
                int group = compute.ClampPosition.AddGroup(
                    team.TeamId,
                    param.UseClampPositionLength,
                    param.GetClampPositionLength(),
                    param.ClampPositionAxisRatio,
                    param.ClampPositionVelocityInfluence
                    );
                var teamData = manager.Team.teamDataList[team.TeamId];
                teamData.clampPositionGroupIndex = (short)group;
                manager.Team.teamDataList[team.TeamId] = teamData;
            }

            // ルートからの最大最小距離拘束
            if (param.UseClampDistanceRatio && clothData.ClampDistanceConstraintCount > 0)
            {
                // 拘束データ
                int group = compute.ClampDistance.AddGroup(
                    team.TeamId,
                    param.UseClampDistanceRatio,
                    param.ClampDistanceMinRatio,
                    param.ClampDistanceMaxRatio,
                    param.ClampDistanceVelocityInfluence,
                    clothData.rootDistanceDataList,
                    clothData.rootDistanceReferenceList
                    );
                var teamData = manager.Team.teamDataList[team.TeamId];
                teamData.clampDistanceGroupIndex = (short)group;
                manager.Team.teamDataList[team.TeamId] = teamData;
            }

#if false
            // パーティクル最大最小距離拘束
            if(param.UseClampDistanceRatio && clothData.ClampDistance2ConstraintCount > 0)
            {
                // 拘束データ
                int group = compute.ClampDistance2.AddGroup(
                    team.TeamId,
                    param.UseClampDistanceRatio,
                    param.ClampDistanceMinRatio,
                    param.ClampDistanceMaxRatio,
                    param.ClampDistanceVelocityInfluence,
                    clothData.clampDistance2DataList,
                    clothData.clampDistance2RootInfoList
                    );
                var teamData = manager.Team.teamDataList[team.TeamId];
                teamData.clampDistance2GroupIndex = (short)group;
                manager.Team.teamDataList[team.TeamId] = teamData;
            }
#endif

            // 距離復元拘束
            if (clothData.StructDistanceConstraintCount > 0 || clothData.BendDistanceConstraintCount > 0 || clothData.NearDistanceConstraintCount > 0)
            {
                // 拘束データ
                int group = compute.RestoreDistance.AddGroup(
                    team.TeamId,
                    param.GetMass(),
                    param.RestoreDistanceVelocityInfluence,
                    param.GetStructDistanceStiffness(),
                    clothData.structDistanceDataList,
                    clothData.structDistanceReferenceList,
                    param.UseBendDistance,
                    param.GetBendDistanceStiffness(),
                    clothData.bendDistanceDataList,
                    clothData.bendDistanceReferenceList,
                    param.UseNearDistance,
                    param.GetNearDistanceStiffness(),
                    clothData.nearDistanceDataList,
                    clothData.nearDistanceReferenceList
                    );
                var teamData = manager.Team.teamDataList[team.TeamId];
                teamData.restoreDistanceGroupIndex = (short)group;
                manager.Team.teamDataList[team.TeamId] = teamData;
            }

            // 回転復元拘束
            if (param.UseRestoreRotation && clothData.RestoreRotationConstraintCount > 0)
            {
                // 拘束データ
                int group = compute.RestoreRotation.AddGroup(
                    team.TeamId,
                    param.UseRestoreRotation,
                    param.GetRotationPower(),
                    param.RestoreRotationVelocityInfluence,
                    clothData.restoreRotationDataList,
                    clothData.restoreRotationReferenceList
                    );
                var teamData = manager.Team.teamDataList[team.TeamId];
                teamData.restoreRotationGroupIndex = (short)group;
                manager.Team.teamDataList[team.TeamId] = teamData;
            }

            // 最大回転復元拘束
            if (param.UseClampRotation && clothData.ClampRotationConstraintDataCount > 0)
            {
                // 拘束データ
                int group = compute.ClampRotation.AddGroup(
                    team.TeamId,
                    param.UseClampRotation,
                    param.GetClampRotationAngle(),
                    param.ClampRotationVelocityInfluence,
                    clothData.clampRotationDataList,
                    clothData.clampRotationRootInfoList
                    );
                var teamData = manager.Team.teamDataList[team.TeamId];
                teamData.clampRotationGroupIndex = (short)group;
                manager.Team.teamDataList[team.TeamId] = teamData;
            }

            // トライアングルベンド拘束
            if (param.UseTriangleBend && clothData.TriangleBendConstraintCount > 0)
            {
                int group = compute.TriangleBend.AddGroup(
                    team.TeamId,
                    param.UseTriangleBend,
                    param.GetTriangleBendStiffness(),
                    clothData.triangleBendDataList,
                    clothData.triangleBendReferenceList,
                    clothData.triangleBendWriteBufferCount
                    );
                var teamData = manager.Team.teamDataList[team.TeamId];
                teamData.triangleBendGroupIndex = (short)group;
                manager.Team.teamDataList[team.TeamId] = teamData;
            }

            // コライダーコリジョン
            if (param.UseCollision)
            {
                var teamData = manager.Team.teamDataList[team.TeamId];

                // 形状維持フラグ
                teamData.SetFlag(PhysicsManagerTeamData.Flag_Collision_KeepShape, param.KeepInitialShape);
                teamData.SetFlag(PhysicsManagerTeamData.Flag_Collision, param.UseCollision);

#if false
                // エッジコリジョン拘束
                if (param.UseEdgeCollision && clothData.EdgeCollisionConstraintCount > 0)
                {
                    int group = compute.EdgeCollision.AddGroup(
                        team.TeamId,
                        param.UseEdgeCollision,
                        param.EdgeCollisionRadius,
                        clothData.edgeCollisionDataList,
                        clothData.edgeCollisionReferenceList,
                        clothData.edgeCollisionWriteBufferCount
                        );
                    teamData.edgeCollisionGroupIndex = (short)group;
                }
#endif

                manager.Team.teamDataList[team.TeamId] = teamData;
            }

            // 浸透制限
            if (param.UsePenetration && clothData.PenetrationCount > 0)
            {
                int group = compute.Penetration.AddGroup(
                    team.TeamId,
                    param.UsePenetration,
                    //param.GetPenetrationMode(),
                    clothData.penetrationMode, // データ作成時のモード
                    param.GetPenetrationDistance(),
                    param.GetPenetrationRadius(),
                    clothData.penetrationDataList,
                    clothData.penetrationReferenceList
                    );
                var teamData = manager.Team.teamDataList[team.TeamId];
                teamData.penetrationGroupIndex = (short)group;
                manager.Team.teamDataList[team.TeamId] = teamData;
            }

#if false
            // ベーススキニング（ワーカー）
            if (param.UseBaseSkinning && clothData.BaseSkinningCount > 0)
            {
                int group = compute.BaseSkinningWorker.AddGroup(
                    team.TeamId,
                    param.UseBaseSkinning,
                    clothData.baseSkinningDataList
                    );
                var teamData = manager.Team.teamDataList[team.TeamId];
                teamData.baseSkinningGroupIndex = (short)group;
                manager.Team.teamDataList[team.TeamId] = teamData;
            }
#endif

#if false
            // ボリューム拘束
            if (param.UseVolume && clothData.VolumeConstraintCount > 0)
            {
                //var sw = new StopWatch().Start();

                int group = compute.Volume.AddGroup(
                    team.TeamId,
                    param.UseVolume,
                    param.GetVolumeStretchStiffness(),
                    param.GetVolumeShearStiffness(),
                    clothData.volumeDataList,
                    clothData.volumeReferenceList,
                    clothData.volumeWriteBufferCount
                    );
                var teamData = manager.Team.teamDataList[team.TeamId];
                teamData.volumeGroupIndex = group;
                manager.Team.teamDataList[team.TeamId] = teamData;

                //sw.Stop();
                //Debug.Log("Volume.AddGroup():" + sw.ElapsedMilliseconds);
            }
#endif

            // 回転調整（これはワーカー）：BoneSpring / MeshSpringのみ
            if (team is MagicaBoneSpring || team is MagicaMeshSpring)
            {
                // 拘束データ
                int group = compute.AdjustRotationWorker.AddGroup(
                    team.TeamId,
                    true,
                    (int)param.AdjustRotationMode,
                    param.AdjustRotationVector,
                    clothData.adjustRotationDataList
                    );
                var teamData = manager.Team.teamDataList[team.TeamId];
                teamData.adjustRotationGroupIndex = (short)group;
                manager.Team.teamDataList[team.TeamId] = teamData;
            }

            // ライン回転調整（ワーカー）
            if (clothData.lineRotationDataList != null && clothData.lineRotationDataList.Length > 0)
            {
                // 拘束データ
                int group = compute.LineWorker.AddGroup(
                    team.TeamId,
                    param.UseLineAvarageRotation,
                    clothData.lineRotationDataList,
                    clothData.lineRotationRootInfoList
                    );
                var teamData = manager.Team.teamDataList[team.TeamId];
                teamData.lineWorkerGroupIndex = (short)group;
                manager.Team.teamDataList[team.TeamId] = teamData;
            }

            // トライアングル回転調整（ワーカー）
            if (clothData.triangleRotationDataList != null && clothData.triangleRotationDataList.Length > 0)
            {
                // 拘束データ
                int group = compute.TriangleWorker.AddGroup(
                    team.TeamId,
                    clothData.triangleRotationDataList,
                    clothData.triangleRotationIndexList
                    );
                var teamData = manager.Team.teamDataList[team.TeamId];
                teamData.triangleWorkerGroupIndex = (short)group;
                manager.Team.teamDataList[team.TeamId] = teamData;
            }

            // 回転補間
            manager.Team.SetFlag(team.TeamId, PhysicsManagerTeamData.Flag_FixedNonRotation, param.UseFixedNonRotation);
        }

        //=========================================================================================
        /// <summary>
        /// クロス破棄
        /// </summary>
        public void ClothDispose(PhysicsTeam team)
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            // コンストレイント解放
            MagicaPhysicsManager.Instance.Compute.RemoveTeam(team.TeamId);

            // パーティクル解放
            team.RemoveAllParticle();

            // 自身の登録ボーン開放
            //MagicaPhysicsManager.Instance.Bone.RemoveBone(teamBoneIndex);
        }

        //=========================================================================================
        public void ClothActive(PhysicsTeam team, ClothParams param, ClothData clothData)
        {
            var manager = MagicaPhysicsManager.Instance;

            // ワールド移動影響ボーンを登録
            Transform influenceTarget = param.GetInfluenceTarget() ? param.GetInfluenceTarget() : team.transform;
            teamBoneIndex = manager.Bone.AddBone(influenceTarget);
            manager.Team.SetBoneIndex(team.TeamId, teamBoneIndex, clothData.initScale);
            team.InfluenceTarget = influenceTarget;

            // 重力方向減衰ボーンを登録
            //Debug.Log("Damp dir:" + clothData.directionalDampingUpDir);
            //influenceTarget = param.DirectionalDampingObject ? param.DirectionalDampingObject : team.transform;
            //teamDirectionalDampingBoneIndex = manager.Bone.AddBone(influenceTarget);
            //manager.Team.SetDirectionalDampingBoneIndex(team.TeamId, param.UseDirectionalDamping, teamDirectionalDampingBoneIndex, clothData.directionalDampingUpDir);

            // ベーススキニング用ボーンを登録
            //foreach (var bone in team.TeamData.SkinningBoneList)
            //{
            //    var boneIndex = manager.Bone.AddBone(bone);
            //    manager.Team.AddSkinningBoneIndex(team.TeamId, boneIndex);
            //}
        }

        public void ClothInactive(PhysicsTeam team)
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            var manager = MagicaPhysicsManager.Instance;

            // 自身の登録ボーン開放
            manager.Bone.RemoveBone(teamBoneIndex);
            manager.Team.SetBoneIndex(team.TeamId, -1, Vector3.zero);

            //manager.Bone.RemoveBone(teamDirectionalDampingBoneIndex);
            //manager.Team.SetDirectionalDampingBoneIndex(team.TeamId, false, -1, 0);

            // ベーススキニング用ボーンを解除
            //manager.Team.RemoveSkinningBoneIndex(team.TeamId);
        }

        /// <summary>
        /// アバター着せ替えによるボーン置換
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        public void ReplaceBone(PhysicsTeam team, ClothParams param, Dictionary<Transform, Transform> boneReplaceDict)
        {
            // この呼び出しは ClothActive() の前なので注意！

            // ワールド移動影響ボーン切り替え
            Transform influenceTarget = param.GetInfluenceTarget();
            if (influenceTarget && boneReplaceDict.ContainsKey(influenceTarget))
            {
                param.SetInfluenceTarget(boneReplaceDict[influenceTarget]);
            }
        }

        //=========================================================================================
        /// <summary>
        /// 距離によるブレンド率
        /// </summary>
        public float DistanceBlendRatio
        {
            get
            {
                return distanceBlendRatio;
            }
            set
            {
                distanceBlendRatio = value;
            }
        }

        //=========================================================================================
        /// <summary>
        /// ランタイムデータ変更
        /// </summary>
        public void ChangeData(PhysicsTeam team, ClothParams param)
        {
            if (Application.isPlaying == false)
                return;

            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            if (team == null)
                return;

            var manager = MagicaPhysicsManager.Instance;
            var compute = manager.Compute;

            bool changeMass = false;

            // 半径
            if (param.ChangedParam(ClothParams.ParamType.Radius))
            {
                // これはパーティクルごと
                for (int i = 0; i < team.ParticleChunk.dataLength; i++)
                {
                    int pindex = team.ParticleChunk.startIndex + i;
                    float depth = manager.Particle.depthList[pindex];
                    float radius = param.GetRadius(depth);
                    manager.Particle.SetRadius(pindex, radius);
                }
            }

            // 重量
            if (param.ChangedParam(ClothParams.ParamType.Mass))
            {
                manager.Team.SetMass(team.TeamId, param.GetMass());
                changeMass = true;
            }

            // 重力係数
            if (param.ChangedParam(ClothParams.ParamType.Gravity))
            {
                manager.Team.SetGravity(team.TeamId, param.GetGravity());
                //manager.Team.SetDirectionalDamping(team.TeamId, param.GetDirectionalDamping());
                //manager.Team.SetFlag(team.TeamId, PhysicsManagerTeamData.Flag_DirectionalDamping, param.UseDirectionalDamping);
            }

            // 空気抵抗
            if (param.ChangedParam(ClothParams.ParamType.Drag))
            {
                manager.Team.SetDrag(team.TeamId, param.GetDrag());
            }

            // 最大速度
            if (param.ChangedParam(ClothParams.ParamType.MaxVelocity))
            {
                manager.Team.SetMaxVelocity(team.TeamId, param.GetMaxVelocity());
            }

            // 外力
            if (param.ChangedParam(ClothParams.ParamType.ExternalForce))
            {
                manager.Team.SetExternalForce(team.TeamId, param.MassInfluence, param.WindInfluence, param.WindRandomScale);
            }

            // チームの摩擦係数変更
            if (param.ChangedParam(ClothParams.ParamType.ColliderCollision))
                manager.Team.SetFriction(team.TeamId, param.Friction);

            // チームワールド移動影響変更
            if (param.ChangedParam(ClothParams.ParamType.WorldInfluence))
            {
                manager.Team.SetWorldInfluence(
                    team.TeamId,
                    param.MaxMoveSpeed,
                    param.GetWorldMoveInfluence(),
                    param.GetWorldRotationInfluence(),
                    param.UseResetTeleport,
                    param.TeleportDistance,
                    param.TeleportRotation,
                    param.ResetStabilizationTime
                    );
            }

            // 距離復元拘束パラメータ再設定
            if (param.ChangedParam(ClothParams.ParamType.RestoreDistance) || changeMass)
            {
                compute.RestoreDistance.ChangeParam(
                    team.TeamId,
                    param.GetMass(),
                    param.RestoreDistanceVelocityInfluence,
                    param.GetStructDistanceStiffness(),
                    param.UseBendDistance,
                    param.GetBendDistanceStiffness(),
                    param.UseNearDistance,
                    param.GetNearDistanceStiffness()
                    );
            }

            // トライアングルベンド拘束パラメータ再設定
            if (param.ChangedParam(ClothParams.ParamType.TriangleBend))
            {
                compute.TriangleBend.ChangeParam(team.TeamId, param.UseTriangleBend, param.GetTriangleBendStiffness());
            }

            // ボリューム拘束パラメータ再設定
            //if (param.ChangedParam(ClothParams.ParamType.Volume))
            //{
            //    compute.Volume.ChangeParam(team.TeamId, param.UseVolume, param.GetVolumeStretchStiffness(), param.GetVolumeShearStiffness());
            //}

            // ルートからの最小最大距離拘束パラメータ再設定
            if (param.ChangedParam(ClothParams.ParamType.ClampDistance))
            {
                compute.ClampDistance.ChangeParam(team.TeamId, param.UseClampDistanceRatio, param.ClampDistanceMinRatio, param.ClampDistanceMaxRatio, param.ClampDistanceVelocityInfluence);
            }

#if false
            // パーティクルからの最大最小距離拘束パラメータ再設定
            if (param.ChangedParam(ClothParams.ParamType.ClampDistance))
            {
                compute.ClampDistance2.ChangeParam(team.TeamId, param.UseClampDistanceRatio, param.ClampDistanceMinRatio, param.ClampDistanceMaxRatio, param.ClampDistanceVelocityInfluence);
            }
#endif

            // 移動範囲拘束パラメータ再設定
            if (param.ChangedParam(ClothParams.ParamType.ClampPosition))
            {
                compute.ClampPosition.ChangeParam(team.TeamId, param.UseClampPositionLength, param.GetClampPositionLength(), param.ClampPositionAxisRatio, param.ClampPositionVelocityInfluence);
            }

            // 回転復元拘束パラメータ再設定
            if (param.ChangedParam(ClothParams.ParamType.RestoreRotation))
            {
                compute.RestoreRotation.ChangeParam(team.TeamId, param.UseRestoreRotation, param.GetRotationPower(), param.RestoreRotationVelocityInfluence);
            }

            // 最大回転拘束パラメータ再設定
            if (param.ChangedParam(ClothParams.ParamType.ClampRotation))
            {
                compute.ClampRotation.ChangeParam(
                    team.TeamId,
                    param.UseClampRotation,
                    param.GetClampRotationAngle(),
                    //param.GetClampRotationStiffness(),
                    param.ClampRotationVelocityInfluence
                    );
            }

            // スプリング回転調整パラメータ再設定（これはワーカー）
            if (param.ChangedParam(ClothParams.ParamType.AdjustRotation))
            {
                compute.AdjustRotationWorker.ChangeParam(team.TeamId, true, (int)param.AdjustRotationMode, param.AdjustRotationVector);
            }

            // コリジョン有無
            if (param.ChangedParam(ClothParams.ParamType.ColliderCollision))
            {
                manager.Team.SetFlag(team.TeamId, PhysicsManagerTeamData.Flag_Collision_KeepShape, param.KeepInitialShape);
                compute.Collision.ChangeParam(team.TeamId, param.UseCollision);
                //compute.EdgeCollision.ChangeParam(team.TeamId, param.UseCollision && param.UseEdgeCollision, param.EdgeCollisionRadius);
            }

            // スプリング拘束パラメータ再設定
            if (param.ChangedParam(ClothParams.ParamType.Spring))
            {
                compute.Spring.ChangeParam(team.TeamId, param.UseSpring, param.GetSpringPower());
            }

            // 回転補間
            if (param.ChangedParam(ClothParams.ParamType.RotationInterpolation))
            {
                compute.LineWorker.ChangeParam(team.TeamId, param.UseLineAvarageRotation);
                manager.Team.SetFlag(team.TeamId, PhysicsManagerTeamData.Flag_FixedNonRotation, param.UseFixedNonRotation);
            }

            // 浸透制限
            if (param.ChangedParam(ClothParams.ParamType.Penetration))
            {
                compute.Penetration.ChangeParam(team.TeamId, param.UsePenetration, param.GetPenetrationDistance(), param.GetPenetrationRadius());
            }

#if false
            // ベーススキニング
            if (param.ChangedParam(ClothParams.ParamType.BaseSkinning))
            {
                compute.BaseSkinningWorker.ChangeParam(team.TeamId, param.UseBaseSkinning);
            }
#endif

            //変更フラグクリア
            param.ClearChangeParam();
        }
    }
}
