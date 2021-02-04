// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MagicaCloth
{
    /// <summary>
    /// コライダーによるパーティクル押し出し拘束
    /// </summary>
    public class ColliderExtrusionConstraint : PhysicsManagerConstraint
    {
        public override void Create()
        {
        }

        public override void RemoveTeam(int teamId)
        {
        }

        // public void ChangeParam(int teamId, bool useCollision)
        // {
        //     Manager.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Collision, useCollision);
        // }

        public override void Release()
        {
        }

        //=========================================================================================
        public override JobHandle SolverConstraint(float dtime, float updatePower, int iteration, JobHandle jobHandle)
        {
            if (Manager.Particle.ColliderCount <= 0)
                return jobHandle;

            // コリジョン押し出し拘束
            var job1 = new CollisionExtrusionJob()
            {
                flagList = Manager.Particle.flagList.ToJobArray(),
                teamIdList = Manager.Particle.teamIdList.ToJobArray(),
                nextPosList = Manager.Particle.InNextPosList.ToJobArray(),
                nextRotList = Manager.Particle.InNextRotList.ToJobArray(),
                oldPosList = Manager.Particle.oldPosList.ToJobArray(),
                oldRotList = Manager.Particle.oldRotList.ToJobArray(),

                collisionLinkIdList = Manager.Particle.collisionLinkIdList.ToJobArray(),
                collisionDistList = Manager.Particle.collisionDistList.ToJobArray(),
                posList = Manager.Particle.posList.ToJobArray(),

                outNextPosList = Manager.Particle.OutNextPosList.ToJobArray(),

                teamDataList = Manager.Team.teamDataList.ToJobArray(),
            };
            jobHandle = job1.Schedule(Manager.Particle.Length, 64, jobHandle);
            Manager.Particle.SwitchingNextPosList();

            return jobHandle;
        }

        //=========================================================================================
        /// <summary>
        /// コリジョン押し出し拘束ジョブ
        /// 移動パーティクルごとに計算
        /// </summary>
        [BurstCompile]
        struct CollisionExtrusionJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> nextRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> oldRotList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> outNextPosList;
            public NativeArray<int> collisionLinkIdList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> collisionDistList;
            public NativeArray<float3> posList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;

            // パーティクルごと
            public void Execute(int index)
            {
                // 初期化コピー
                float3 nextpos = nextPosList[index];
                outNextPosList[index] = nextpos;

                // 接続コライダー
                int cindex = collisionLinkIdList[index];
                float cdist = collisionDistList[index];
                collisionLinkIdList[index] = 0; // リセット
                //collisionDistList[index] = 0;
                if (cindex <= 0)
                    return;

                var flag = flagList[index];
                if (flag.IsValid() == false || flag.IsFixed() || flag.IsCollider())
                    return;

                // チーム
                var team = teamIdList[index];
                var teamData = teamDataList[team];
                if (teamData.IsActive() == false)
                    return;
                if (teamData.IsFlag(PhysicsManagerTeamData.Flag_Collision) == false)
                    return;
                // 更新確認
                if (teamData.IsUpdate() == false)
                    return;

                //int vindex = index - teamData.particleChunk.startIndex;
                //Debug.Log($"vindex:{vindex}");

                // 移動前コライダー姿勢
                var oldcpos = oldPosList[cindex];
                var oldcrot = oldRotList[cindex];
                var v = nextpos - oldcpos; // nextposでないとダメ(oldPosList[index]ではまずい)
                var ioldcrot = math.inverse(oldcrot);
                var lpos = math.mul(ioldcrot, v);

                // 移動後コライダー姿勢
                var cpos = nextPosList[cindex];
                var crot = nextRotList[cindex];
                var fpos = math.mul(crot, lpos) + cpos;

                // 押し出しベクトル
                var ev = fpos - nextpos;
                var elen = math.length(ev);
                if (elen < 1e-06f)
                {
                    // コライダーが動いていない
                    return;
                }

                // 押し出しベクトルに対する移動前接触方向の角度
                var d = math.dot(math.normalize(ev), math.normalize(v));
                if (d <= 0.0f)
                    return;

                // 押し出し方向による補正
                d = math.pow(d, Define.Compute.ColliderExtrusionDirectionPower);

                // コライダーとの接触の深さにより強さを変える
                var power = math.saturate((Define.Compute.ColliderExtrusionDist - cdist) / Define.Compute.ColliderExtrusionDist);
                power = math.pow(power, Define.Compute.ColliderExtrusionDistPower);
                d *= power;

                // 押し出し
                var opos = nextpos;
                nextpos = math.lerp(nextpos, fpos, d);
                outNextPosList[index] = nextpos;

                // 速度影響
                var av = (nextpos - opos) * (1.0f - 0.5f); // 跳ねを抑えるため50%ほど入れる（※抑えすぎると突き抜けやすくなるので注意）
                posList[index] = posList[index] + av;
            }
        }
    }
}
