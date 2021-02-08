// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace MagicaCloth
{
    /// <summary>
    /// ボーンデータ
    /// </summary>
    public class PhysicsManagerBoneData : PhysicsManagerAccess
    {
        //=========================================================================================
        /// <summary>
        /// 管理ボーンリスト
        /// </summary>
        public FixedTransformAccessArray boneList;

        /// <summary>
        /// ボーンワールド位置リスト（※未来予測により補正される場合あり）
        /// </summary>
        public FixedNativeList<float3> bonePosList;

        /// <summary>
        /// ボーンワールド回転リスト（※未来予測により補正される場合あり）
        /// </summary>
        public FixedNativeList<quaternion> boneRotList;

        /// <summary>
        /// ボーンワールドスケールリスト（現在は初期化時に設定のみ不変）
        /// </summary>
        public FixedNativeList<float3> boneSclList;

        /// <summary>
        /// 親ボーンへのインデックス(-1=なし)
        /// </summary>
        public FixedNativeList<int> boneParentIndexList;

        /// <summary>
        /// ボーンワールド位置リスト（オリジナル）
        /// </summary>
        public FixedNativeList<float3> basePosList;

        /// <summary>
        /// ボーンワールド回転リスト（オリジナル）
        /// </summary>
        public FixedNativeList<quaternion> baseRotList;

        //=========================================================================================
        /// <summary>
        /// 復元ボーンリスト
        /// </summary>
        public FixedTransformAccessArray restoreBoneList;

        /// <summary>
        /// 復元ボーンの復元ローカル座標リスト
        /// </summary>
        public FixedNativeList<float3> restoreBoneLocalPosList;

        /// <summary>
        /// 復元ボーンの復元ローカル回転リスト
        /// </summary>
        public FixedNativeList<quaternion> restoreBoneLocalRotList;

        //=========================================================================================
        // ここはライトボーンごと
        /// <summary>
        /// 書き込みボーンリスト
        /// </summary>
        public FixedTransformAccessArray writeBoneList;

        /// <summary>
        /// 書き込みボーンの参照ボーン姿勢インデックス（＋１が入るので注意！）
        /// </summary>
        public FixedNativeList<int> writeBoneIndexList;

        /// <summary>
        /// 書き込みボーンの対応するパーティクルインデックス
        /// </summary>
        public ExNativeMultiHashMap<int, int> writeBoneParticleIndexMap;

        /// <summary>
        /// 読み込みボーンに対応する書き込みボーンのインデックス辞書
        /// </summary>
        Dictionary<int, int> boneToWriteIndexDict = new Dictionary<int, int>();

        /// <summary>
        /// 書き込みボーンの確定位置
        /// 親がいる場合はローカル、いない場合はワールド格納
        /// </summary>
        public FixedNativeList<float3> writeBonePosList;

        /// <summary>
        /// 書き込みボーンの確定回転
        /// 親がいる場合はローカル、いない場合はワールド格納
        /// </summary>
        public FixedNativeList<quaternion> writeBoneRotList;

        //=========================================================================================
        /// <summary>
        /// ボーンリストに変化が合った場合にtrue
        /// </summary>
        public bool hasBoneChanged { get; private set; }

        /// <summary>
        /// プロファイラ用
        /// </summary>
        private CustomSampler SamplerReadBoneScale { get; set; }

        //=========================================================================================
        /// <summary>
        /// 初期設定
        /// </summary>
        public override void Create()
        {
            boneList = new FixedTransformAccessArray();
            bonePosList = new FixedNativeList<float3>();
            boneRotList = new FixedNativeList<quaternion>();
            boneSclList = new FixedNativeList<float3>();
            boneParentIndexList = new FixedNativeList<int>();
            basePosList = new FixedNativeList<float3>();
            baseRotList = new FixedNativeList<quaternion>();

            restoreBoneList = new FixedTransformAccessArray();
            restoreBoneLocalPosList = new FixedNativeList<float3>();
            restoreBoneLocalRotList = new FixedNativeList<quaternion>();

            writeBoneList = new FixedTransformAccessArray();
            writeBoneIndexList = new FixedNativeList<int>();
            writeBoneParticleIndexMap = new ExNativeMultiHashMap<int, int>();
            writeBonePosList = new FixedNativeList<float3>();
            writeBoneRotList = new FixedNativeList<quaternion>();

            // プロファイラ用
            SamplerReadBoneScale = CustomSampler.Create("ReadBoneScale");
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            if (boneList == null)
                return;

            boneList.Dispose();
            bonePosList.Dispose();
            boneRotList.Dispose();
            boneSclList.Dispose();
            boneParentIndexList.Dispose();
            basePosList.Dispose();
            baseRotList.Dispose();

            restoreBoneList.Dispose();
            restoreBoneLocalPosList.Dispose();
            restoreBoneLocalRotList.Dispose();

            writeBoneList.Dispose();
            writeBoneIndexList.Dispose();
            writeBoneParticleIndexMap.Dispose();
            writeBonePosList.Dispose();
            writeBoneRotList.Dispose();
        }

        //=========================================================================================
        /// <summary>
        /// 復元ボーン登録
        /// </summary>
        /// <param name="target"></param>
        /// <param name="lpos"></param>
        /// <param name="lrot"></param>
        /// <returns></returns>
        public int AddRestoreBone(Transform target, float3 lpos, quaternion lrot)
        {
            int restoreBoneIndex;
            if (restoreBoneList.Exist(target))
            {
                // 参照カウンタ＋
                restoreBoneIndex = restoreBoneList.Add(target);
            }
            else
            {
                // 復元ローカル姿勢も登録
                restoreBoneIndex = restoreBoneList.Add(target);
                restoreBoneLocalPosList.Add(lpos);
                restoreBoneLocalRotList.Add(lrot);
                hasBoneChanged = true;
            }

            return restoreBoneIndex;
        }

        /// <summary>
        /// 復元ボーン削除
        /// </summary>
        /// <param name="restoreBoneIndex"></param>
        public void RemoveRestoreBone(int restoreBoneIndex)
        {
            restoreBoneList.Remove(restoreBoneIndex);
            if (restoreBoneList.Exist(restoreBoneIndex) == false)
            {
                // データも削除
                restoreBoneLocalPosList.Remove(restoreBoneIndex);
                restoreBoneLocalRotList.Remove(restoreBoneIndex);
                hasBoneChanged = true;
            }
        }

        /// <summary>
        /// ボーンの復元カウントを返す
        /// </summary>
        public int RestoreBoneCount
        {
            get
            {
                return restoreBoneList.Count;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 利用ボーン登録
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pindex"></param>
        /// <param name="addParent">親ボーンのインデックス保持の有無</param>
        /// <returns></returns>
        public int AddBone(Transform target, int pindex = -1, bool addParent = false)
        {
            int boneIndex;
            if (boneList.Exist(target))
            {
                // 参照カウンタ＋
                boneIndex = boneList.Add(target);
                if (addParent && boneParentIndexList[boneIndex] < 0)
                {
                    boneParentIndexList[boneIndex] = boneList.GetIndex(target.parent);
                }
                basePosList[boneIndex] = new float3(0, -1000000, 0); // 未来予測位置リセット用の数値
            }
            else
            {
                // 新規
                //var pos = target.position;
                //var rot = target.rotation;
                var pos = float3.zero;
                var rot = quaternion.identity;
                boneIndex = boneList.Add(target);
                bonePosList.Add(pos);
                boneRotList.Add(rot);
#if (UNITY_2018 || UNITY_2019_1 || UNITY_2019_2_1 || UNITY_2019_2_2 || UNITY_2019_2_3 || UNITY_2019_2_4 || UNITY_2019_2_5 || UNITY_2019_2_6 || UNITY_2019_2_7 || UNITY_2019_2_8 || UNITY_2019_2_9 || UNITY_2019_2_10 || UNITY_2019_2_11 || UNITY_2019_2_12 || UNITY_2019_2_13)
                // Unity2019.2.13までは現在ランタイムスケールは未対応
                boneSclList.Add(target.lossyScale);
#else
                boneSclList.Add(float3.zero);
#endif
                if (addParent)
                    boneParentIndexList.Add(boneList.GetIndex(target.parent));
                else
                    boneParentIndexList.Add(-1);
                basePosList.Add(new float3(0, -1000000, 0)); // 未来予測位置リセット用の数値
                baseRotList.Add(rot);
                hasBoneChanged = true;
            }

            //Debug.Log("AddBone:" + target.name + " index:" + boneIndex + " parent?:" + boneParentIndexList[boneIndex]);

            // 書き込み設定
            if (pindex >= 0)
            {
                //Debug.Log("AddWriteBone:" + target.name + " index:" + boneIndex + " parent?:" + boneParentIndexList[boneIndex]);

                if (writeBoneList.Exist(target))
                {
                    // 参照カウンタ＋
                    writeBoneList.Add(target);
                }
                else
                {
                    // 新規
                    writeBoneList.Add(target);
                    //Debug.Log("write bone index:" + boneIndex);
                    writeBoneIndexList.Add(boneIndex + 1); // +1を入れるので注意！
                    writeBonePosList.Add(float3.zero);
                    writeBoneRotList.Add(quaternion.identity);
                    hasBoneChanged = true;
                }
                int writeIndex = writeBoneList.GetIndex(target);

                boneToWriteIndexDict.Add(boneIndex, writeIndex);

                // 書き込み姿勢参照パーティクルインデックス登録
                writeBoneParticleIndexMap.Add(writeIndex, pindex);
            }

            return boneIndex;
        }

        /// <summary>
        /// 利用ボーン解除
        /// </summary>
        /// <param name="boneIndex"></param>
        /// <param name="pindex"></param>
        /// <returns></returns>
        public bool RemoveBone(int boneIndex, int pindex = -1)
        {
            //Debug.Log("RemoveBone: index:" + boneIndex + " parent?:" + boneParentIndexList[boneIndex]);

            bool del = false;
            boneList.Remove(boneIndex);
            if (boneList.Exist(boneIndex) == false)
            {
                // データも削除
                bonePosList.Remove(boneIndex);
                boneRotList.Remove(boneIndex);
                boneSclList.Remove(boneIndex);
                boneParentIndexList.Remove(boneIndex);
                basePosList.Remove(boneIndex);
                baseRotList.Remove(boneIndex);
                hasBoneChanged = true;
                del = true;
            }

            // 書き込み設定から削除
            if (pindex >= 0)
            {
                int writeIndex = boneToWriteIndexDict[boneIndex];

                writeBoneList.Remove(writeIndex);
                writeBoneIndexList.Remove(writeIndex);
                writeBoneParticleIndexMap.Remove(writeIndex, pindex);
                writeBonePosList.Remove(writeIndex);
                writeBoneRotList.Remove(writeIndex);
                hasBoneChanged = true;

                if (writeBoneList.Exist(writeIndex) == false)
                {
                    boneToWriteIndexDict.Remove(boneIndex);
                }
            }

            return del;
        }

        /// <summary>
        /// 未来予測をリセットする
        /// </summary>
        /// <param name="boneIndex"></param>
        public void ResetFuturePrediction(int boneIndex)
        {
            basePosList[boneIndex] = new float3(0, -1000000, 0);
        }

        /// <summary>
        /// 読み込みボーン数を返す
        /// </summary>
        public int ReadBoneCount
        {
            get
            {
                return boneList.Count;
            }
        }

        /// <summary>
        /// 書き込みボーン数を返す
        /// </summary>
        public int WriteBoneCount
        {
            get
            {
                return writeBoneList.Count;
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーン情報のリセット
        /// </summary>
        public void ResetBoneFromTransform()
        {
            // ボーン姿勢リセット
            if (RestoreBoneCount > 0)
            {
                var job = new RestoreBoneJob()
                {
                    localPosList = restoreBoneLocalPosList.ToJobArray(),
                    localRotList = restoreBoneLocalRotList.ToJobArray(),
                };
                Compute.MasterJob = job.Schedule(restoreBoneList.GetTransformAccessArray(), Compute.MasterJob);
            }
        }

        /// <summary>
        /// ボーン姿勢の復元
        /// </summary>
        [BurstCompile]
        struct RestoreBoneJob : IJobParallelForTransform
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> localRotList;

            // 復元ボーンごと
            public void Execute(int index, TransformAccess transform)
            {
                transform.localPosition = localPosList[index];
                transform.localRotation = localRotList[index];
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーン情報の読み込み
        /// </summary>
        public void ReadBoneFromTransform()
        {
            // ボーン姿勢読み込み
            if (ReadBoneCount > 0)
            {
                var updateTime = manager.UpdateTime;

                // 未来予測補間率
                float futureRate = updateTime.IsDelay ? updateTime.FuturePredictionRate : 0.0f;

                // 未来予測が不要ならば従来どおり
                if (futureRate < 0.01f)
                {
                    // ボーンから姿勢読み込み（ルートが別れていないとジョブが並列化できないので注意！）
                    var job = new ReadBoneJob0()
                    {
                        bonePosList = bonePosList.ToJobArray(),
                        boneRotList = boneRotList.ToJobArray(),
#if !(UNITY_2018 || UNITY_2019_1 || UNITY_2019_2_1 || UNITY_2019_2_2 || UNITY_2019_2_3 || UNITY_2019_2_4 || UNITY_2019_2_5 || UNITY_2019_2_6 || UNITY_2019_2_7 || UNITY_2019_2_8 || UNITY_2019_2_9 || UNITY_2019_2_10 || UNITY_2019_2_11 || UNITY_2019_2_12 || UNITY_2019_2_13)
                        boneSclList = boneSclList.ToJobArray(),
#endif
                        basePosList = basePosList.ToJobArray(),
                        baseRotList = baseRotList.ToJobArray(),
                    };
                    Compute.MasterJob = job.Schedule(boneList.GetTransformAccessArray(), Compute.MasterJob);
                }
                else
                {
                    // 未来予測あり
                    // 補間率を求める
                    // 急なカクつきで未来予測が大幅にずれる問題を平均deltaTimeを使い緩和させる
                    float ratio = math.saturate(updateTime.AverageDeltaTime / updateTime.DeltaTime) * futureRate;

                    // ボーンから姿勢読み込み（ルートが別れていないとジョブが並列化できないので注意！）
                    var job = new ReadBoneJob1()
                    {
                        ratio = ratio,
                        bonePosList = bonePosList.ToJobArray(),
                        boneRotList = boneRotList.ToJobArray(),
#if !(UNITY_2018 || UNITY_2019_1 || UNITY_2019_2_1 || UNITY_2019_2_2 || UNITY_2019_2_3 || UNITY_2019_2_4 || UNITY_2019_2_5 || UNITY_2019_2_6 || UNITY_2019_2_7 || UNITY_2019_2_8 || UNITY_2019_2_9 || UNITY_2019_2_10 || UNITY_2019_2_11 || UNITY_2019_2_12 || UNITY_2019_2_13)
                        boneSclList = boneSclList.ToJobArray(),
#endif
                        basePosList = basePosList.ToJobArray(),
                        baseRotList = baseRotList.ToJobArray(),
                    };
                    Compute.MasterJob = job.Schedule(boneList.GetTransformAccessArray(), Compute.MasterJob);
                }
            }
        }

        /// <summary>
        /// ボーン姿勢の読込み（未来予測なし)
        /// </summary>
        [BurstCompile]
        struct ReadBoneJob0 : IJobParallelForTransform
        {
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> boneRotList;
#if !(UNITY_2018 || UNITY_2019_1 || UNITY_2019_2_1 || UNITY_2019_2_2 || UNITY_2019_2_3 || UNITY_2019_2_4 || UNITY_2019_2_5 || UNITY_2019_2_6 || UNITY_2019_2_7 || UNITY_2019_2_8 || UNITY_2019_2_9 || UNITY_2019_2_10 || UNITY_2019_2_11 || UNITY_2019_2_12 || UNITY_2019_2_13)
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> boneSclList;
#endif

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> basePosList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> baseRotList;

            // 読み込みボーンごと
            public void Execute(int index, TransformAccess transform)
            {
                float3 pos = transform.position;
                quaternion rot = transform.rotation;
                bonePosList[index] = pos;
                boneRotList[index] = rot;

#if !(UNITY_2018 || UNITY_2019_1 || UNITY_2019_2_1 || UNITY_2019_2_2 || UNITY_2019_2_3 || UNITY_2019_2_4 || UNITY_2019_2_5 || UNITY_2019_2_6 || UNITY_2019_2_7 || UNITY_2019_2_8 || UNITY_2019_2_9 || UNITY_2019_2_10 || UNITY_2019_2_11 || UNITY_2019_2_12 || UNITY_2019_2_13)
                // lossyScale取得(現在はUnity2019.2.14以上のみ)
                // マトリックスから正確なスケール値を算出する（これはTransform.lossyScaleと等価）
                float4x4 m = transform.localToWorldMatrix;
                var irot = math.inverse(rot);
                var m2 = math.mul(new float4x4(irot, float3.zero), m);
                var scl = new float3(m2.c0.x, m2.c1.y, m2.c2.z);
                boneSclList[index] = scl;
#endif

                basePosList[index] = pos;
                baseRotList[index] = rot;
            }
        }

        /// <summary>
        /// ボーン姿勢の読込み（未来予測あり）
        /// </summary>
        [BurstCompile]
        struct ReadBoneJob1 : IJobParallelForTransform
        {
            public float ratio;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> boneRotList;
#if !(UNITY_2018 || UNITY_2019_1 || UNITY_2019_2_1 || UNITY_2019_2_2 || UNITY_2019_2_3 || UNITY_2019_2_4 || UNITY_2019_2_5 || UNITY_2019_2_6 || UNITY_2019_2_7 || UNITY_2019_2_8 || UNITY_2019_2_9 || UNITY_2019_2_10 || UNITY_2019_2_11 || UNITY_2019_2_12 || UNITY_2019_2_13)
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> boneSclList;
#endif

            public NativeArray<float3> basePosList;
            public NativeArray<quaternion> baseRotList;

            // 読み込みボーンごと
            public void Execute(int index, TransformAccess transform)
            {
                float3 pos = transform.position;
                quaternion rot = transform.rotation;
                var oldPos = basePosList[index];
                var oldRot = baseRotList[index];

                if (oldPos.y < -900000)
                {
                    // リセット
                    basePosList[index] = pos;
                    baseRotList[index] = rot;

                    bonePosList[index] = pos;
                    boneRotList[index] = rot;
                }
                else
                {
                    // 前回からの速度から未来予測の更新量を求める
                    var velocityPos = (pos - oldPos) * ratio;
                    var velocityRot = MathUtility.FromToRotation(oldRot, rot, ratio);

                    basePosList[index] = pos;
                    baseRotList[index] = rot;

                    // 未来予測
                    pos += velocityPos;
                    rot = math.mul(velocityRot, rot);

                    bonePosList[index] = pos;
                    boneRotList[index] = rot;
                }

#if !(UNITY_2018 || UNITY_2019_1 || UNITY_2019_2_1 || UNITY_2019_2_2 || UNITY_2019_2_3 || UNITY_2019_2_4 || UNITY_2019_2_5 || UNITY_2019_2_6 || UNITY_2019_2_7 || UNITY_2019_2_8 || UNITY_2019_2_9 || UNITY_2019_2_10 || UNITY_2019_2_11 || UNITY_2019_2_12 || UNITY_2019_2_13)
                // lossyScale取得(現在はUnity2019.2.14以上のみ)
                // マトリックスから正確なスケール値を算出する（これはTransform.lossyScaleと等価）
                float4x4 m = transform.localToWorldMatrix;
                var irot = math.inverse(rot);
                var m2 = math.mul(new float4x4(irot, float3.zero), m);
                var scl = new float3(m2.c0.x, m2.c1.y, m2.c2.z);
                boneSclList[index] = scl;
#endif
            }
        }

        //=========================================================================================
        /// <summary>
        /// メインスレッドによるボーンスケール読み込み（負荷が掛かるのでオプション）
        /// Unity2018ではTransformAccessでlossyScaleを取得できないのでやむを得ず。
        /// </summary>
        public void ReadBoneScaleFromTransform()
        {
            if (ReadBoneCount > 0)
            {
                // プロファイラ計測開始
                SamplerReadBoneScale.Begin();

                for (int i = 0, cnt = boneList.Length; i < cnt; i++)
                {
                    var t = boneList[i];
                    if (t)
                    {
                        boneSclList[i] = t.lossyScale;
                    }
                }

                // プロファイラ計測終了
                SamplerReadBoneScale.End();
            }
        }

        //=========================================================================================
        /// <summary>
        /// 書き込みボーン姿勢をローカル姿勢に変換する
        /// </summary>
        public void ConvertWorldToLocal()
        {
            if (WriteBoneCount > 0)
            {
                var job = new ConvertWorldToLocalJob()
                {
                    writeBoneIndexList = writeBoneIndexList.ToJobArray(),
                    bonePosList = bonePosList.ToJobArray(),
                    boneRotList = boneRotList.ToJobArray(),
                    boneSclList = boneSclList.ToJobArray(),
                    boneParentIndexList = boneParentIndexList.ToJobArray(),

                    writeBonePosList = writeBonePosList.ToJobArray(),
                    writeBoneRotList = writeBoneRotList.ToJobArray(),
                };
                Compute.MasterJob = job.Schedule(writeBoneIndexList.Length, 16, Compute.MasterJob);
            }
        }

        /// <summary>
        /// ボーン姿勢をローカル姿勢に変換する
        /// </summary>
        [BurstCompile]
        struct ConvertWorldToLocalJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> writeBoneIndexList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> boneRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> boneSclList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> boneParentIndexList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> writeBonePosList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> writeBoneRotList;

            // 書き込みボーンごと
            public void Execute(int index)
            {
                int bindex = writeBoneIndexList[index];
                if (bindex == 0)
                    return;
                bindex--; // +1が入っているので-1する

                var pos = bonePosList[bindex];
                var rot = boneRotList[bindex];

                int parentIndex = boneParentIndexList[bindex];
                if (parentIndex >= 0)
                {
                    // 親がいる場合はローカル座標で書き込む
                    var ppos = bonePosList[parentIndex];
                    var prot = boneRotList[parentIndex];
                    var pscl = boneSclList[parentIndex];
                    var iprot = math.inverse(prot);

                    var v = pos - ppos;
                    var lpos = math.mul(iprot, v);
                    lpos /= pscl;
                    var lrot = math.mul(iprot, rot);

                    // マイナススケール対応
                    if (pscl.x < 0 || pscl.y < 0 || pscl.z < 0)
                        lrot = new quaternion(lrot.value * new float4(-math.sign(pscl), 1));

                    writeBonePosList[index] = lpos;
                    writeBoneRotList[index] = lrot;
                }
                else
                {
                    // 親がいない場合はワールド座標で書き込む
                    writeBonePosList[index] = pos;
                    writeBoneRotList[index] = rot;
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーン姿勢をトランスフォームに書き込む
        /// </summary>
        public void WriteBoneToTransform(int bufferIndex)
        {
            if (WriteBoneCount > 0)
            {
                var job = new WriteBontToTransformJob2()
                {
                    writeBoneIndexList = writeBoneIndexList.ToJobArray(bufferIndex),
                    boneParentIndexList = boneParentIndexList.ToJobArray(),
                    writeBonePosList = writeBonePosList.ToJobArray(bufferIndex),
                    writeBoneRotList = writeBoneRotList.ToJobArray(bufferIndex),
                };
                Compute.MasterJob = job.Schedule(writeBoneList.GetTransformAccessArray(), Compute.MasterJob);
            }
        }

        /// <summary>
        /// ボーン姿勢をトランスフォームに書き込む
        /// </summary>
        [BurstCompile]
        struct WriteBontToTransformJob2 : IJobParallelForTransform
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> writeBoneIndexList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> boneParentIndexList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> writeBonePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> writeBoneRotList;

            // 書き込みトランスフォームごと
            public void Execute(int index, TransformAccess transform)
            {
                if (index >= writeBoneIndexList.Length)
                    return;

                int bindex = writeBoneIndexList[index];
                if (bindex == 0)
                    return;
                bindex--; // +1が入っているので-1する

                var pos = writeBonePosList[index];
                var rot = writeBoneRotList[index];

                int parentIndex = boneParentIndexList[bindex];
                if (parentIndex >= 0)
                {
                    // 親を参照する場合はローカル座標で書き込む
                    transform.localPosition = pos;
                    transform.localRotation = rot;
                }
                else
                {
                    // 親がいない場合はワールドで書き込む
                    transform.position = pos;
                    transform.rotation = rot;
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーン情報を書き込みバッファにコピーする
        /// これは遅延実行時のみ
        /// </summary>
        public void CopyBoneBuffer()
        {
            var job0 = new CopyBoneJob0()
            {
                bonePosList = writeBonePosList.ToJobArray(),
                boneRotList = writeBoneRotList.ToJobArray(),

                backBonePosList = writeBonePosList.ToJobArray(1),
                backBoneRotList = writeBoneRotList.ToJobArray(1),
            };
            var jobHandle0 = job0.Schedule(writeBonePosList.Length, 16);

            var job1 = new CopyBoneJob1()
            {
                writeBoneIndexList = writeBoneIndexList.ToJobArray(),

                backWriteBoneIndexList = writeBoneIndexList.ToJobArray(1),
            };
            var jobHandle1 = job1.Schedule(writeBoneIndexList.Length, 16);

            Compute.MasterJob = JobHandle.CombineDependencies(jobHandle0, jobHandle1);
        }

        [BurstCompile]
        struct CopyBoneJob0 : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> boneRotList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> backBonePosList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> backBoneRotList;

            public void Execute(int index)
            {
                backBonePosList[index] = bonePosList[index];
                backBoneRotList[index] = boneRotList[index];
            }
        }

        [BurstCompile]
        struct CopyBoneJob1 : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> writeBoneIndexList;

            [Unity.Collections.WriteOnly]
            public NativeArray<int> backWriteBoneIndexList;

            public void Execute(int index)
            {
                backWriteBoneIndexList[index] = writeBoneIndexList[index];
            }
        }
    }
}
