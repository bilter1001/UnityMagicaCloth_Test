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
    /// コライダーコリジョン拘束
    /// </summary>
    public class ColliderAfterCollisionConstraint : PhysicsManagerConstraint
    {
        public override void Create()
        {
        }

        public override void RemoveTeam(int teamId)
        {
        }

        public void ChangeParam(int teamId, bool useCollision)
        {
            Manager.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_AfterCollision, useCollision);

            //var teamData = Manager.Team.teamDataList[teamId];

            //var pstart = teamData.particleChunk.startIndex;
            //for (int i = 0; i < teamData.particleChunk.dataLength; i++)
            //{
            //    int pindex = pstart + i;
            //    var flag = Manager.Particle.flagList[pindex];
            //    if (flag.IsKinematic() == false)
            //    {
            //        Manager.Particle.SetCollision(pindex, useCollision);
            //    }
            //}
        }

        public override void Release()
        {
        }

        //=========================================================================================
        public override JobHandle SolverConstraint(float dtime, float updatePower, int iteration, JobHandle jobHandle)
        {
            if (Manager.Particle.ColliderCount <= 0)
                return jobHandle;

            // コリジョン拘束
            var job1 = new AfterCollisionJob()
            {
                flagList = Manager.Particle.flagList.ToJobArray(),
                teamIdList = Manager.Particle.teamIdList.ToJobArray(),
                radiusList = Manager.Particle.radiusList.ToJobArray(),
                nextPosList = Manager.Particle.InNextPosList.ToJobArray(),
                nextRotList = Manager.Particle.InNextRotList.ToJobArray(),
                posList = Manager.Particle.posList.ToJobArray(),
                rotList = Manager.Particle.rotList.ToJobArray(),
                localPosList = Manager.Particle.localPosList.ToJobArray(),
                basePosList = Manager.Particle.basePosList.ToJobArray(),
                baseRotList = Manager.Particle.baseRotList.ToJobArray(),
                transformIndexList = Manager.Particle.transformIndexList.ToJobArray(),
                oldPosList = Manager.Particle.oldPosList.ToJobArray(),

                //colliderMap = Manager.Team.colliderMap.Map,
                colliderList = Manager.Team.colliderList.ToJobArray(),

                boneSclList = Manager.Bone.boneSclList.ToJobArray(),

                teamDataList = Manager.Team.teamDataList.ToJobArray(),

                outNextPosList = Manager.Particle.OutNextPosList.ToJobArray(),
                frictionList = Manager.Particle.frictionList.ToJobArray(),
                velocityList = Manager.Particle.velocityList.ToJobArray(),
            };
            jobHandle = job1.Schedule(Manager.Particle.Length, 64, jobHandle);
            Manager.Particle.SwitchingNextPosList();

            return jobHandle;
        }

        //=========================================================================================
        /// <summary>
        /// コリジョン拘束ジョブ
        /// 移動パーティクルごとに計算
        /// </summary>
        [BurstCompile]
        struct AfterCollisionJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> radiusList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> nextRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> posList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> transformIndexList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosList;

            //[Unity.Collections.ReadOnly]
            //public NativeMultiHashMap<int, int> colliderMap;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> colliderList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> boneSclList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> outNextPosList;
            public NativeArray<float> frictionList;
            //[Unity.Collections.ReadOnly]
            public NativeArray<float3> velocityList;

            // パーティクルごと
            public void Execute(int index)
            {
                // 初期化コピー
                float3 nextpos = nextPosList[index];
                outNextPosList[index] = nextpos;

                var flag = flagList[index];
                //if (flag.IsValid() == false || flag.IsCollision() == false || flag.IsCollider())
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

                var radius = radiusList[index].x;
                var basepos = basePosList[index];

                // チームごとに判定[グローバル(0)]->[自身のチーム(team)]
                int colliderTeam = 0;

                // 移動前位置
                //var oldpos = oldPosList[index];
                var oldpos = velocityList[index];

                // コライダーループ
                //float mindist = 100.0f;
                for (int i = 0; i < 2; i++)
                {
                    // チーム内のコライダーをループ
                    var c = teamDataList[colliderTeam].colliderChunk;

                    int dataIndex = c.startIndex;
                    for (int j = 0; j < c.useLength; j++, dataIndex++)
                    {
                        int cindex = colliderList[dataIndex];

                        var cflag = flagList[cindex];
                        if (cflag.IsValid() == false)
                            continue;

                        //float dist = 100.0f;
                        if (cflag.IsFlag(PhysicsManagerParticleData.Flag_Plane))
                        {
                            // 平面コライダー判定
                            //dist = PlaneColliderDetection(ref nextpos, radius, cindex);
                        }
                        else if (cflag.IsFlag(PhysicsManagerParticleData.Flag_CapsuleX))
                        {
                            // カプセルコライダー判定
                            //dist = CapsuleColliderDetection(ref nextpos, basepos, radius, cindex, new float3(1, 0, 0), keep);
                            CapsuleColliderDetection(ref nextpos, oldpos, radius, cindex, new float3(1, 0, 0));
                        }
                        else if (cflag.IsFlag(PhysicsManagerParticleData.Flag_CapsuleY))
                        {
                            // カプセルコライダー判定
                            //dist = CapsuleColliderDetection(ref nextpos, basepos, radius, cindex, new float3(0, 1, 0), keep);
                            CapsuleColliderDetection(ref nextpos, oldpos, radius, cindex, new float3(0, 1, 0));
                        }
                        else if (cflag.IsFlag(PhysicsManagerParticleData.Flag_CapsuleZ))
                        {
                            // カプセルコライダー判定
                            //dist = CapsuleColliderDetection(ref nextpos, basepos, radius, cindex, new float3(0, 0, 1), keep);
                            CapsuleColliderDetection(ref nextpos, oldpos, radius, cindex, new float3(0, 0, 1));
                        }
                        else if (cflag.IsFlag(PhysicsManagerParticleData.Flag_Box))
                        {
                            // ボックスコライダー判定
                            // ★まだ未実装
                        }
                        else
                        {
                            // 球コライダー判定
                            SphereColliderDetection(ref nextpos, oldpos, radius, cindex);
                        }

                        // コライダーとの距離
                        //mindist = math.min(mindist, dist);
                    }

                    // 自身のチームに切り替え
                    if (team > 0)
                        colliderTeam = team;
                    else
                        break;
                }

                // 摩擦係数
                // 衝突判定の有無に関わらずコライダーとの距離が一定以内ならば摩擦を発生させる
                //const float frictionMul = 1.0f / Define.Compute.CollisionFrictionRange;
                //if (mindist < Define.Compute.CollisionFrictionRange)
                //{
                //    float f = math.saturate(1.0f - mindist * frictionMul);
                //    f = math.pow(f, 4.0f); // 強い減衰
                //    frictionList[index] = f;
                //}

                // 書き戻し
                outNextPosList[index] = nextpos;
                //flagList[index] = flag;

                velocityList[index] = nextpos;

                // コリジョンの速度影響は100%にしておく
                // コリジョン衝突による速度影響は非常に重要！
                // 速度影響を抑えると容易に突き抜けるようになってしまう
            }

            //=====================================================================================
            /* bool IntersectSegmentPlane(float3 a, float3 b, float3 p, float3 pn, out float3 opos)
            {
                var ab = b - a;
                float pd = math.dot(pn, p);
                float t = (pd - math.dot(pn, a)) / math.dot(pn, ab);
                if (t >= 0.0f && t <= 1.0f)
                {
                    opos = a + t * ab;
                    return true;
                }

                opos = 0;
                return false;
            }*/

            void SphereColliderDetection(ref float3 nextpos, float3 oldpos, float radius, int cindex)
            {
                var cpos = nextPosList[cindex];
                var cradius = radiusList[cindex];

                // スケール
                var tindex = transformIndexList[cindex];
                var cscl = boneSclList[tindex];
                cradius *= cscl.x; // X軸のみを見る

                // 移動前のパーティクル位置から押し出し平面を求める
                var v = oldpos - cpos;
                var len = math.length(v);
                var n = math.normalize(v);
                //var c = cpos + n * (cradius + radius);
                len = math.min(len, cradius.x + radius);
                len *= 0.999f;
                var c = cpos + n * len;

                // 平面衝突判定
                float3 opos;
                if (MathUtility.IntersectSegmentPlane(oldpos, nextpos, c, n, out opos))
                {
                    nextpos = opos;
                }


                // c = 平面位置
                // n = 平面方向
                // 平面衝突判定と押し出し
                //MathUtility.IntersectPointPlaneDist(c, n, nextpos, out nextpos);
            }

            void CapsuleColliderDetection(ref float3 nextpos, float3 oldpos, float radius, int cindex, float3 dir)
            {
                var cpos = nextPosList[cindex];
                var crot = nextRotList[cindex];

                // x = 長さ（片側）
                // y = 始点半径
                // z = 終点半径
                //var lpos = localPosList[cindex];
                var cradius = radiusList[cindex];

                // スケール
                var tindex = transformIndexList[cindex];
                var cscl = boneSclList[tindex];
                float scl = math.dot(cscl, dir); // dirの軸のスケールを使用する
                cradius *= scl;

                // カプセル始点と終点
                float3 l = math.mul(crot, dir * cradius.x);
                float3 spos = cpos - l;
                float3 epos = cpos + l;
                float sr = cradius.y;
                float er = cradius.z;

                // 移動前のパーティクル位置から押し出し平面を求める
                //float3 c = 0, n = 0;
                float t = MathUtility.ClosestPtPointSegmentRatio(oldpos, spos, epos);
                float r = math.lerp(sr, er, t);
                float3 d = math.lerp(spos, epos, t);
                float3 v = oldpos - d;
                var len = math.length(v);
                float3 n = math.normalize(v);
                len = math.min(len, r + radius);
                len *= 0.999f;
                //float3 c = d + n * (r + radius);
                float3 c = d + n * len;

                // 平面衝突判定
                float3 opos;
                if (MathUtility.IntersectSegmentPlane(oldpos, nextpos, c, n, out opos))
                {
                    nextpos = opos;
                }


                // c = 平面位置
                // n = 平面方向
                // 平面衝突判定と押し出し
                //return MathUtility.IntersectPointPlaneDist(c, n, nextpos, out nextpos);
            }


            //=====================================================================================
            /// <summary>
            /// 球衝突判定
            /// </summary>
            /// <param name="nextpos"></param>
            /// <param name="pos"></param>
            /// <param name="radius"></param>
            /// <param name="cindex"></param>
            /// <param name="friction"></param>
            /// <returns></returns>
            float SphereColliderDetection(ref float3 nextpos, float3 basepos, float radius, int cindex, bool keep)
            {
                var cpos = nextPosList[cindex];
                var cradius = radiusList[cindex];

                // スケール
                var tindex = transformIndexList[cindex];
                var cscl = boneSclList[tindex];
                cradius *= cscl.x; // X軸のみを見る

                // 移動前のコライダーに対するローカル位置から移動後コライダーの押し出し平面を求める
                float3 c = 0, n = 0, v = 0;
                if (keep)
                {
                    // 形状キープ
                    // 物理OFFの基本状態から拘束を決定
                    var cbasepos = basePosList[cindex];
                    v = basepos - cbasepos;
                    var iq = math.inverse(baseRotList[cindex]);
                    var lv = math.mul(iq, v);
                    v = math.mul(nextRotList[cindex], lv);
                }
                else
                {
                    var coldpos = posList[cindex];
                    v = nextpos - coldpos;
                }
                n = math.normalize(v);
                c = cpos + n * (cradius + radius);

                // c = 平面位置
                // n = 平面方向
                // 平面衝突判定と押し出し
                return MathUtility.IntersectPointPlaneDist(c, n, nextpos, out nextpos);
            }

            /// <summary>
            /// カプセル衝突判定
            /// </summary>
            /// <param name="nextpos"></param>
            /// <param name="pos"></param>
            /// <param name="radius"></param>
            /// <param name="cindex"></param>
            /// <param name="dir"></param>
            /// <param name="friction"></param>
            /// <returns></returns>
            float CapsuleColliderDetection(ref float3 nextpos, float3 basepos, float radius, int cindex, float3 dir, bool keep)
            {
                // lpos.x = 長さ（片側）
                // lpos.y = 始点半径
                // lpos.z = 終点半径
                var cpos = nextPosList[cindex];
                var crot = nextRotList[cindex];
                var lpos = localPosList[cindex];

                // スケール
                var tindex = transformIndexList[cindex];
                var cscl = boneSclList[tindex];
                float scl = math.dot(cscl, dir); // dirの軸のスケールを使用する
                lpos *= scl;

                float3 c = 0, n = 0;

                if (keep)
                {
                    // 形状キープ
                    // 物理OFFの基本状態から拘束を決定
                    var cbasepos = basePosList[cindex];
                    var cbaserot = baseRotList[cindex];

                    // カプセル始点と終点
                    float3 l = math.mul(cbaserot, dir * lpos.x);
                    float3 spos = cbasepos - l;
                    float3 epos = cbasepos + l;
                    float sr = lpos.y;
                    float er = lpos.z;

                    // 移動前のコライダー位置から押し出し平面を割り出す
                    float t = MathUtility.ClosestPtPointSegmentRatio(basepos, spos, epos);
                    float r = math.lerp(sr, er, t);
                    float3 d = math.lerp(spos, epos, t);
                    float3 v = basepos - d;

                    // 移動前コライダーのローカルベクトル
                    var iq = math.inverse(cbaserot);
                    float3 lv = math.mul(iq, v);

                    // 移動後コライダーに変換
                    l = math.mul(crot, dir * lpos.x);
                    spos = cpos - l;
                    epos = cpos + l;
                    d = math.lerp(spos, epos, t);
                    v = math.mul(crot, lv);
                    n = math.normalize(v);
                    c = d + n * (r + radius);
                }
                else
                {
                    var coldpos = posList[cindex];
                    var coldrot = rotList[cindex];

                    // カプセル始点と終点
                    float3 l = math.mul(coldrot, dir * lpos.x);
                    float3 spos = coldpos - l;
                    float3 epos = coldpos + l;
                    float sr = lpos.y;
                    float er = lpos.z;

                    // 移動前のコライダー位置から押し出し平面を割り出す
                    float t = MathUtility.ClosestPtPointSegmentRatio(nextpos, spos, epos);
                    float r = math.lerp(sr, er, t);
                    float3 d = math.lerp(spos, epos, t);
                    float3 v = nextpos - d;

                    // 移動前コライダーのローカルベクトル
                    var iq = math.inverse(coldrot);
                    float3 lv = math.mul(iq, v);

                    // 移動後コライダーに変換
                    l = math.mul(crot, dir * lpos.x);
                    spos = cpos - l;
                    epos = cpos + l;
                    d = math.lerp(spos, epos, t);
                    v = math.mul(crot, lv);
                    n = math.normalize(v);
                    c = d + n * (r + radius);
                }


                // c = 平面位置
                // n = 平面方向
                // 平面衝突判定と押し出し
                return MathUtility.IntersectPointPlaneDist(c, n, nextpos, out nextpos);
            }

            /// <summary>
            /// 平面衝突判定
            /// </summary>
            /// <param name="nextpos"></param>
            /// <param name="radius"></param>
            /// <param name="cindex"></param>
            float PlaneColliderDetection(ref float3 nextpos, float radius, int cindex)
            {
                // 平面姿勢
                var cpos = nextPosList[cindex];
                var crot = nextRotList[cindex];

                // 平面法線
                float3 n = math.mul(crot, math.up());

                // パーティクル半径分オフセット
                cpos += n * radius;

                // c = 平面位置
                // n = 平面方向
                // 平面衝突判定と押し出し
                // 平面との距離を返す（押し出しの場合は0.0）
                return MathUtility.IntersectPointPlaneDist(cpos, n, nextpos, out nextpos);
            }
        }
    }
}
