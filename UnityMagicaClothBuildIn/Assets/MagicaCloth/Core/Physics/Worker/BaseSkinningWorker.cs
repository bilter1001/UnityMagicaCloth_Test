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
    /// コライダー移動制限拘束
    /// </summary>
    public class BaseSkinningWorker : PhysicsManagerWorker
    {
        /// <summary>
        /// 移動制限
        /// todo:共有可能
        /// </summary>
        [System.Serializable]
        public struct BaseSkinningData
        {
            /// <summary>
            /// スキニングボーン配列インデックス
            /// </summary>
            public int boneIndex;

            /// <summary>
            /// ローカル座標
            /// </summary>
            public float3 localPos;
            public float3 localNor;
            public float3 localTan;

            /// <summary>
            /// ウエイト
            /// </summary>
            public float weight;

            public bool IsValid()
            {
                return weight > 0;
            }
        }
        FixedChunkNativeArray<BaseSkinningData> dataList;

        /// <summary>
        /// グループごとの拘束データ
        /// </summary>
        public struct GroupData
        {
            public int teamId;
            public int active;

            public ChunkData dataChunk;
        }
        public FixedNativeList<GroupData> groupList;

        //=========================================================================================
        public override void Create()
        {
            groupList = new FixedNativeList<GroupData>();
            dataList = new FixedChunkNativeArray<BaseSkinningData>();
        }

        public override void Release()
        {
            groupList.Dispose();
            dataList.Dispose();
        }

        public int AddGroup(int teamId, bool active, BaseSkinningData[] skinningDataList)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];

            var gdata = new GroupData();
            gdata.teamId = teamId;
            gdata.active = active ? 1 : 0;
            gdata.dataChunk = dataList.AddChunk(skinningDataList.Length);

            // チャンクデータコピー
            dataList.ToJobArray().CopyFromFast(gdata.dataChunk.startIndex, skinningDataList);

            int group = groupList.Add(gdata);
            return group;
        }


        public override void RemoveGroup(int teamId)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.baseSkinningGroupIndex;
            if (group < 0)
                return;

            var gdata = groupList[group];

            // チャンクデータ削除
            dataList.RemoveChunk(gdata.dataChunk);

            // データ削除
            groupList.Remove(group);
        }

        public void ChangeParam(int teamId, bool active)
        {
            var teamData = Manager.Team.teamDataList[teamId];
            int group = teamData.baseSkinningGroupIndex;
            if (group < 0)
                return;

            var gdata = groupList[group];
            gdata.active = active ? 1 : 0;
            groupList[group] = gdata;
        }

        //=========================================================================================
        /// <summary>
        /// トランスフォームリード中に実行する処理
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public override void Warmup()
        {
        }

        //=========================================================================================
        /// <summary>
        /// 物理更新前処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public override JobHandle PreUpdate(JobHandle jobHandle)
        {
            if (groupList.Count == 0)
                return jobHandle;

            var job = new BaseSkinningJob()
            {
                groupList = groupList.ToJobArray(),
                dataList = dataList.ToJobArray(),

                flagList = Manager.Particle.flagList.ToJobArray(),
                teamIdList = Manager.Particle.teamIdList.ToJobArray(),
                transformIndexList = Manager.Particle.transformIndexList.ToJobArray(),

                basePosList = Manager.Particle.basePosList.ToJobArray(),
                baseRotList = Manager.Particle.baseRotList.ToJobArray(),

                //skinningBoneList = Manager.Team.skinningBoneList.ToJobArray(),
                colliderList = Manager.Team.colliderList.ToJobArray(),

                teamDataList = Manager.Team.teamDataList.ToJobArray(),

                bonePosList = Manager.Bone.bonePosList.ToJobArray(),
                boneRotList = Manager.Bone.boneRotList.ToJobArray(),
                boneSclList = Manager.Bone.boneSclList.ToJobArray(),
            };
            jobHandle = job.Schedule(Manager.Particle.Length, 64, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct BaseSkinningJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<GroupData> groupList;
            [Unity.Collections.ReadOnly]
            public NativeArray<BaseSkinningData> dataList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> transformIndexList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> basePosList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> baseRotList;

            //[Unity.Collections.ReadOnly]
            //public NativeArray<int> skinningBoneList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> colliderList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> boneRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> boneSclList;

            // パーティクルごと
            public void Execute(int index)
            {
                var flag = flagList[index];
                if (flag.IsValid() == false || flag.IsFixed() || flag.IsCollider())
                    return;

                // チーム
                var team = teamIdList[index];
                var teamData = teamDataList[team];
                if (teamData.IsActive() == false)
                    return;
                if (teamData.baseSkinningGroupIndex < 0)
                    return;
                // 更新確認
                if (teamData.IsUpdate() == false)
                    return;

                // グループデータ
                var gdata = groupList[teamData.baseSkinningGroupIndex];
                if (gdata.active == 0)
                    return;

                // スキニング
                float3 spos = 0;
                float3 snor = 0;
                float3 stan = 0;

                int vindex = index - teamData.particleChunk.startIndex;
                int dindex = vindex * Define.Compute.BaseSkinningWeightCount;
                for (int i = 0; i < Define.Compute.BaseSkinningWeightCount; i++, dindex++)
                {
                    var data = dataList[gdata.dataChunk.startIndex + dindex];

                    if (data.IsValid())
                    {
#if true
                        int cindex = colliderList[teamData.colliderChunk.startIndex + data.boneIndex];
                        int tindex = transformIndexList[cindex];
                        var tpos = bonePosList[tindex];
                        var trot = boneRotList[tindex];
                        var tscl = boneSclList[tindex];

#else
                        int bindex = skinningBoneList[teamData.skinningBoneChunk.startIndex + data.boneIndex];

                        var tpos = bonePosList[bindex];
                        var trot = boneRotList[bindex];
                        var tscl = boneSclList[bindex];
#endif

                        // ウエイト０はありえない
                        spos += (tpos + math.mul(trot, data.localPos * tscl)) * data.weight;
                        snor += math.mul(trot, data.localNor) * data.weight;
                        stan += math.mul(trot, data.localTan) * data.weight;
                    }
                }

                // 書き込み
                basePosList[index] = spos;
                baseRotList[index] = quaternion.LookRotation(snor, stan);
                //baseRotList[index] = quaternion.LookRotationSafe(snor, stan);
            }
        }

        //=========================================================================================
        /// <summary>
        /// 物理更新後処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public override JobHandle PostUpdate(JobHandle jobHandle)
        {
            return jobHandle;
        }

    }
}
