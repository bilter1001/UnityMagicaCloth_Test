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
    /// 風データ
    /// </summary>
    public class PhysicsManagerWindData : PhysicsManagerAccess
    {
        /// <summary>
        /// 風タイプ
        /// </summary>
        public enum WindType
        {
            None = 0,
            Direction,
        }

        /// <summary>
        /// 風フラグビット
        /// </summary>
        public const uint Flag_Enable = 0x00000001; // 有効フラグ

        /// <summary>
        /// 風データ
        /// </summary>
        public struct WindData
        {
            /// <summary>
            /// 風タイプ
            /// </summary>
            public WindType windType;

            /// <summary>
            /// フラグビットデータ
            /// </summary>
            public uint flag;

            /// <summary>
            /// 連動トランスフォームインデックス
            /// </summary>
            public int transformIndex;

            /// <summary>
            /// 風量
            /// </summary>
            public float main;

            /// <summary>
            /// 乱流率(0.0-1.0)
            /// </summary>
            public float turbulence;

            /// <summary>
            /// 現在の風の方向（ここが計算で使用される）
            /// </summary>
            public float3 direction;

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
            /// 有効フラグの設定
            /// </summary>
            /// <param name="sw"></param>
            public void SetEnable(bool sw)
            {
                if (sw)
                    flag |= Flag_Enable;
                else
                    flag &= ~Flag_Enable;
            }

            /// <summary>
            /// データが有効か判定する
            /// </summary>
            /// <returns></returns>
            public bool IsActive()
            {
                return (flag & Flag_Enable) != 0;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 風データリスト
        /// </summary>
        public FixedNativeList<WindData> windDataList;

        /// <summary>
        /// 方向風のリスト
        /// </summary>
        private List<int> directionalWindList = new List<int>();

        //=========================================================================================
        /// <summary>
        /// 初期設定
        /// </summary>
        public override void Create()
        {
            windDataList = new FixedNativeList<WindData>();
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            if (windDataList == null)
                return;

            windDataList.Dispose();
        }

        //=========================================================================================
        public int CreateWind(WindType windType, float main, float turbulence)
        {
            var data = new WindData();

            uint flag = Flag_Enable;
            data.flag = flag;
            data.windType = windType;
            data.transformIndex = -1;
            data.main = main;
            data.turbulence = turbulence;

            int windId = windDataList.Add(data);

            if (windType == WindType.Direction)
                directionalWindList.Add(windId);

            return windId;
        }

        public void RemoveWind(int windId)
        {
            if (windId >= 0)
            {
                windDataList.Remove(windId);

                directionalWindList.Remove(windId);
            }
        }

        /// <summary>
        /// 風の有効フラグ切り替え
        /// </summary>
        /// <param name="windId"></param>
        /// <param name="sw"></param>
        public void SetEnable(int windId, bool sw, Transform target)
        {
            if (windId >= 0)
            {
                WindData data = windDataList[windId];
                data.SetEnable(sw);

                // 連動トランスフォームを登録／解除
                if (sw)
                {
                    if (data.transformIndex == -1)
                    {
                        data.transformIndex = Bone.AddBone(target);
                    }
                }
                else
                {
                    if (data.transformIndex >= 0)
                    {
                        Bone.RemoveBone(data.transformIndex);
                        data.transformIndex = -1;
                    }
                }

                windDataList[windId] = data;
            }
        }

        /// <summary>
        /// 風が有効状態か判定する
        /// </summary>
        /// <param name="windId"></param>
        /// <returns></returns>
        public bool IsActive(int windId)
        {
            if (windId >= 0)
                return windDataList[windId].IsActive();
            else
                return false;
        }

        /// <summary>
        /// 風の状態フラグ設定
        /// </summary>
        /// <param name="windId"></param>
        /// <param name="flag"></param>
        /// <param name="sw"></param>
        public void SetFlag(int windId, uint flag, bool sw)
        {
            if (windId < 0)
                return;
            WindData data = windDataList[windId];
            data.SetFlag(flag, sw);
            windDataList[windId] = data;
        }

        public void SetParameter(int windId, float main, float turbulence)
        {
            if (windId < 0)
                return;
            WindData data = windDataList[windId];
            data.main = main;
            data.turbulence = turbulence;
            windDataList[windId] = data;
        }

        /// <summary>
        /// 方向風のID
        /// </summary>
        public int DirectionalWindId
        {
            get
            {
                if (directionalWindList.Count > 0)
                {
                    // 後から追加されたものを優先する
                    return directionalWindList[directionalWindList.Count - 1];
                }

                return -1;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 風の更新
        /// </summary>
        public void UpdateWind()
        {
            var job = new UpdateWindJob()
            {
                dtime = manager.UpdateTime.DeltaTime,
                elapsedTime = Time.time,

                bonePosList = Bone.bonePosList.ToJobArray(),
                boneRotList = Bone.boneRotList.ToJobArray(),

                windData = windDataList.ToJobArray(),
            };
            Compute.MasterJob = job.Schedule(windDataList.Length, 8, Compute.MasterJob);
        }

        [BurstCompile]
        struct UpdateWindJob : IJobParallelFor
        {
            public float dtime;
            public float elapsedTime;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> boneRotList;

            public NativeArray<WindData> windData;

            // 風データごと
            public void Execute(int index)
            {
                var wdata = windData[index];
                if (wdata.IsActive() == false || wdata.transformIndex < 0)
                    return;

                // コンポーネント姿勢
                var bpos = bonePosList[wdata.transformIndex];
                var brot = boneRotList[wdata.transformIndex];

                // 風量による計算比率
                float ratio = wdata.main / 30.0f; // 風速30を基準

                // 周期（風向きが変わる速度）
                float freq = 1.0f + 2.0f * ratio; // 1.0 - 3.0

                // 風向きのランダム角度
                float rang = 15.0f + 15.0f * ratio; // 15 - 30

                // ノイズ参照
                var noisePos1 = new float2(bpos.x, bpos.z) * 0.1f;
                var noisePos2 = new float2(bpos.x, bpos.z) * 0.1f;
                noisePos1.x += elapsedTime * freq; // 周期（数値を高くするとランダム性が増す）2.0f?
                noisePos2.y += elapsedTime * freq; // 周期（数値を高くするとランダム性が増す）2.0f?
                var nv1 = noise.snoise(noisePos1); // -1.0f～1.0f
                var nv2 = noise.snoise(noisePos2); // -1.0f～1.0f

                // 方向のランダム性
                var ang1 = math.radians(nv1 * rang);
                var ang2 = math.radians(nv2 * rang);
                ang1 *= wdata.turbulence; // 乱流率
                ang2 *= wdata.turbulence; // 乱流率
                var rq = quaternion.Euler(ang1, ang2, 0.0f); // XY
                var dir = math.forward(math.mul(brot, rq)); // ランダムはローカル回転
                wdata.direction = dir;

                // 書き戻し
                windData[index] = wdata;
            }
        }
    }
}
