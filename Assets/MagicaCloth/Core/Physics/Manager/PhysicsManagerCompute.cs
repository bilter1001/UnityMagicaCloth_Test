// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace MagicaCloth
{
    /// <summary>
    /// 計算処理
    /// </summary>
    public class PhysicsManagerCompute : PhysicsManagerAccess
    {
        /// <summary>
        /// 拘束判定繰り返し回数
        /// </summary>
        //[Header("拘束全体の反復回数")]
        //[Range(1, 8)]
        //public int solverIteration = 2;
        private int solverIteration = 1;

        /// <summary>
        /// 拘束条件
        /// </summary>
        List<PhysicsManagerConstraint> constraints = new List<PhysicsManagerConstraint>();

        public ClampPositionConstraint ClampPosition { get; private set; }
        public ClampDistanceConstraint ClampDistance { get; private set; }
        //public ClampDistance2Constraint ClampDistance2 { get; private set; }
        public ClampRotationConstraint ClampRotation { get; private set; }
        public SpringConstraint Spring { get; private set; }
        public RestoreDistanceConstraint RestoreDistance { get; private set; }
        public RestoreRotationConstraint RestoreRotation { get; private set; }
        public TriangleBendConstraint TriangleBend { get; private set; }
        public ColliderCollisionConstraint Collision { get; private set; }
        public PenetrationConstraint Penetration { get; private set; }
        public ColliderExtrusionConstraint ColliderExtrusion { get; private set; }
        //public ColliderAfterCollisionConstraint AfterCollision { get; private set; }
        //public EdgeCollisionConstraint EdgeCollision { get; private set; }
        //public VolumeConstraint Volume { get; private set; }

        /// <summary>
        /// ワーカーリスト
        /// </summary>
        List<PhysicsManagerWorker> workers = new List<PhysicsManagerWorker>();
        public RenderMeshWorker RenderMeshWorker { get; private set; }
        public VirtualMeshWorker VirtualMeshWorker { get; private set; }
        public MeshParticleWorker MeshParticleWorker { get; private set; }
        public SpringMeshWorker SpringMeshWorker { get; private set; }
        public AdjustRotationWorker AdjustRotationWorker { get; private set; }
        public LineWorker LineWorker { get; private set; }
        public TriangleWorker TriangleWorker { get; private set; }
        //public BaseSkinningWorker BaseSkinningWorker { get; private set; }

        /// <summary>
        /// マスタージョブハンドル
        /// すべてのジョブはこのハンドルに連結される
        /// </summary>
        JobHandle jobHandle;
        private bool runMasterJob = false;

        private int swapIndex = 0;

        /// <summary>
        /// プロファイラ用
        /// </summary>
        public CustomSampler SamplerWriteMesh { get; set; }

        //=========================================================================================
        /// <summary>
        /// 初期設定
        /// </summary>
        public override void Create()
        {
            // 拘束の作成
            // ※この並び順が実行順番となります。

            // 移動制限
            //ClampDistance = new ClampDistanceConstraint();
            //constraints.Add(ClampDistance);

            // コリジョン
            ColliderExtrusion = new ColliderExtrusionConstraint();
            constraints.Add(ColliderExtrusion);
            Penetration = new PenetrationConstraint();
            constraints.Add(Penetration);
            Collision = new ColliderCollisionConstraint();
            constraints.Add(Collision);

            // 移動制限
            ClampDistance = new ClampDistanceConstraint();
            constraints.Add(ClampDistance);
            //ClampDistance2 = new ClampDistance2Constraint();
            //constraints.Add(ClampDistance2);
            //Penatration = new ColliderPenetrationConstraint();
            //constraints.Add(Penatration);

            // コリジョン
            //EdgeCollision = new EdgeCollisionConstraint();
            //constraints.Add(EdgeCollision);
            //Penetration = new PenetrationConstraint();
            //constraints.Add(Penetration);
            //Collision = new ColliderCollisionConstraint();
            //constraints.Add(Collision);

            // 移動制限
            //Penetration = new ColliderPenetrationConstraint(); // コリジョンの前はだめ
            //constraints.Add(Penetration);
            //ClampDistance = new ClampDistanceConstraint();
            //constraints.Add(ClampDistance);
            //Penatration = new ColliderPenetrationConstraint();
            //constraints.Add(Penatration);

            // 主なクロスシミュレーション
            Spring = new SpringConstraint();
            constraints.Add(Spring);
            RestoreDistance = new RestoreDistanceConstraint();
            constraints.Add(RestoreDistance);
            RestoreRotation = new RestoreRotationConstraint();
            constraints.Add(RestoreRotation);

            // コリジョン
            //EdgeCollision = new EdgeCollisionConstraint();
            //constraints.Add(EdgeCollision);
            //Penetration = new PenetrationConstraint();
            //constraints.Add(Penetration);
            //Collision = new ColliderCollisionConstraint();
            //constraints.Add(Collision);
            //Penetration = new PenetrationConstraint();
            //constraints.Add(Penetration);

            // 形状維持
            TriangleBend = new TriangleBendConstraint();
            constraints.Add(TriangleBend);
            //Volume = new VolumeConstraint();
            //constraints.Add(Volume);

            // 移動制限2
            //Penetration = new ColliderPenetrationConstraint();
            //constraints.Add(Penetration);
            ClampPosition = new ClampPositionConstraint();
            constraints.Add(ClampPosition);
            ClampRotation = new ClampRotationConstraint();
            constraints.Add(ClampRotation);

            // コリジョン2
            //AfterCollision = new ColliderAfterCollisionConstraint();
            //constraints.Add(AfterCollision);
            //EdgeCollision = new EdgeCollisionConstraint();
            //constraints.Add(EdgeCollision);
            //Collision = new ColliderCollisionConstraint();
            //constraints.Add(Collision);
            //Penetration = new PenetrationConstraint();
            //constraints.Add(Penetration);

            foreach (var con in constraints)
                con.Init(manager);

            // ワーカーの作成
            // ※この並び順は変更してはいけません。
            RenderMeshWorker = new RenderMeshWorker();
            workers.Add(RenderMeshWorker);
            VirtualMeshWorker = new VirtualMeshWorker();
            workers.Add(VirtualMeshWorker);
            MeshParticleWorker = new MeshParticleWorker();
            workers.Add(MeshParticleWorker);
            SpringMeshWorker = new SpringMeshWorker();
            workers.Add(SpringMeshWorker);
            AdjustRotationWorker = new AdjustRotationWorker();
            workers.Add(AdjustRotationWorker);
            LineWorker = new LineWorker();
            workers.Add(LineWorker);
            TriangleWorker = new TriangleWorker();
            workers.Add(TriangleWorker);
            //BaseSkinningWorker = new BaseSkinningWorker();
            //workers.Add(BaseSkinningWorker);
            foreach (var worker in workers)
                worker.Init(manager);


            // プロファイラ用
            SamplerWriteMesh = CustomSampler.Create("WriteMesh");
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            if (constraints != null)
            {
                foreach (var con in constraints)
                    con.Release();
            }
            if (workers != null)
            {
                foreach (var worker in workers)
                    worker.Release();
            }
        }

        /// <summary>
        /// 各コンストレイント／ワーカーから指定グループのデータを削除する
        /// </summary>
        /// <param name="teamId"></param>
        public void RemoveTeam(int teamId)
        {
            if (MagicaPhysicsManager.Instance.Team.IsValidData(teamId) == false)
                return;

            if (constraints != null)
            {
                foreach (var con in constraints)
                    con.RemoveTeam(teamId);
            }
            if (workers != null)
            {
                foreach (var worker in workers)
                    worker.RemoveGroup(teamId);
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーン姿勢を元の位置に復元する
        /// </summary>
        public void UpdateRestoreBone()
        {
            // 活動チームが１つ以上ある場合のみ更新
            if (Team.ActiveTeamCount > 0)
            {
                // トランスフォーム姿勢のリセット
                Bone.ResetBoneFromTransform();
            }
        }

        /// <summary>
        /// ボーン姿勢を読み込む
        /// </summary>
        public void UpdateReadBone()
        {
            // 活動チームが１つ以上ある場合のみ更新
            if (Team.ActiveTeamCount > 0)
            {
                // トランスフォーム姿勢の読み込み
                Bone.ReadBoneFromTransform();
            }
        }

        /// <summary>
        /// ボーンスケールを読み込む
        /// ★これはメインスレッドなので注意！
        /// ★Unity2019.2.13まではTransformAccessでlossyScaleを取得できないのでやむを得ず
        /// </summary>
        public void UpdateReadBoneScale()
        {
#if (UNITY_2018 || UNITY_2019_1 || UNITY_2019_2_1 || UNITY_2019_2_2 || UNITY_2019_2_3 || UNITY_2019_2_4 || UNITY_2019_2_5 || UNITY_2019_2_6 || UNITY_2019_2_7 || UNITY_2019_2_8 || UNITY_2019_2_9 || UNITY_2019_2_10 || UNITY_2019_2_11 || UNITY_2019_2_12 || UNITY_2019_2_13)
            if (Team.ActiveTeamCount > 0 && UpdateTime.UpdateBoneScale)
            {
                Bone.ReadBoneScaleFromTransform();
            }
#endif
        }

        /// <summary>
        /// メインスレッドで行うチームデータ更新処理
        /// </summary>
        public void UpdateTeamAlways()
        {
            if (manager.IsActive)
            {
                // 常に実行するチームデータ更新
                Team.PreUpdateTeamAlways();
            }
        }

        /// <summary>
        /// クロスシミュレーション計算開始
        /// </summary>
        /// <param name="update"></param>
        public void UpdateStartSimulation(UpdateTimeManager update)
        {
            // 時間
            float dtime = update.DeltaTime;
            float updatePower = update.UpdatePower;
            float updateIntervalTime = update.UpdateIntervalTime;
            int ups = update.UpdatePerSecond;

            // 活動チームが１つ以上ある場合のみ更新
            if (Team.ActiveTeamCount > 0)
            {
                // 今回フレームの更新回数
                int updateCount = Team.CalcMaxUpdateCount(ups, dtime, updateIntervalTime);
                //Debug.Log("updateCount:" + updateCount + " dtime:" + Time.deltaTime);

                // 風更新
                Wind.UpdateWind();

                // チームデータ更新、更新回数確定、ワールド移動影響、テレポート
                Team.PreUpdateTeamData(dtime, updateIntervalTime, ups, updateCount);

                // ワーカー処理
                WarmupWorker();

                // ボーン姿勢をパーティクルにコピーする
                Particle.UpdateBoneToParticle();

                // 物理更新前ワーカー処理
                //MasterJob = RenderMeshWorker.PreUpdate(MasterJob); // 何もなし
                MasterJob = VirtualMeshWorker.PreUpdate(MasterJob); // 仮想メッシュをスキニングしワールド姿勢を求める
                MasterJob = MeshParticleWorker.PreUpdate(MasterJob); // 仮想メッシュ頂点姿勢を連動パーティクルにコピーする
                //MasterJob = SpringMeshWorker.PreUpdate(MasterJob); // 何もなし
                //MasterJob = AdjustRotationWorker.PreUpdate(MasterJob); // 何もなし
                //MasterJob = LineWorker.PreUpdate(MasterJob); // 何もなし
                //MasterJob = BaseSkinningWorker.PreUpdate(MasterJob); // ベーススキニングによりbasePos/baseRotをスキニング

                // パーティクルのリセット判定
                Particle.UpdateResetParticle();

                // 物理更新
                for (int i = 0, cnt = updateCount; i < cnt; i++)
                {
                    UpdatePhysics(updateCount, i, updatePower, updateIntervalTime);
                }

                // 物理演算後処理
                PostUpdatePhysics(updateIntervalTime);

                // 物理更新後ワーカー処理
                MasterJob = TriangleWorker.PostUpdate(MasterJob); // トライアングル回転調整
                MasterJob = LineWorker.PostUpdate(MasterJob); // ラインの回転調整
                MasterJob = AdjustRotationWorker.PostUpdate(MasterJob); // パーティクル回転調整(Adjust Rotation)
                Particle.UpdateParticleToBone(); // パーティクル姿勢をボーン姿勢に書き戻す（ここに挟まないと駄目）
                MasterJob = SpringMeshWorker.PostUpdate(MasterJob); // メッシュスプリング
                MasterJob = MeshParticleWorker.PostUpdate(MasterJob); // パーティクル姿勢を仮想メッシュに書き出す
                MasterJob = VirtualMeshWorker.PostUpdate(MasterJob); // 仮想メッシュ座標書き込み（仮想メッシュトライアングル法線計算）
                MasterJob = RenderMeshWorker.PostUpdate(MasterJob); // レンダーメッシュ座標書き込み（仮想メッシュからレンダーメッシュ座標計算）

                // 書き込みボーン姿勢をローカル姿勢に変換する
                Bone.ConvertWorldToLocal();

                // チームデータ後処理
                Team.PostUpdateTeamData();

            }
        }

        /// <summary>
        /// クロスシミュレーション完了待ち
        /// </summary>
        public void UpdateCompleteSimulation()
        {
            // マスタージョブ完了待機
            CompleteJob();
            runMasterJob = true;
        }

        /// <summary>
        /// ボーン姿勢をトランスフォームに書き込む
        /// </summary>
        public void UpdateWriteBone()
        {
            // ボーン姿勢をトランスフォームに書き出す
            Bone.WriteBoneToTransform(manager.IsDelay ? 1 : 0);
        }

        /// <summary>
        /// メッシュ姿勢をメッシュに書き込む
        /// </summary>
        public void UpdateWriteMesh()
        {
            // プロファイラ計測開始
            SamplerWriteMesh.Begin();

            // メッシュへの頂点書き戻し
            if (Mesh.VirtualMeshCount > 0 && runMasterJob)
            {
                Mesh.FinishMesh(manager.IsDelay ? 1 : 0);
            }

            // プロファイラ計測終了
            SamplerWriteMesh.End();
        }

        /// <summary>
        /// 遅延実行時のボーン読み込みと前回のボーン結果の書き込み
        /// </summary>
        public void UpdateReadWriteBone()
        {
            // 活動チームが１つ以上ある場合のみ更新
            if (Team.ActiveTeamCount > 0)
            {
                // トランスフォーム姿勢の読み込み
                Bone.ReadBoneFromTransform();

                if (runMasterJob)
                {
                    // ボーン姿勢をトランスフォームに書き出す
                    Bone.WriteBoneToTransform(manager.IsDelay ? 1 : 0);
                }
            }
        }

        /// <summary>
        /// 遅延実行時のみボーンの計算結果を書き込みバッファにコピーする
        /// </summary>
        public void UpdateSyncBuffer()
        {
            Bone.writeBoneIndexList.SyncBuffer();
            Bone.writeBonePosList.SyncBuffer();
            Bone.writeBoneRotList.SyncBuffer();

            InitJob();
            Bone.CopyBoneBuffer();
            CompleteJob();
        }

        /// <summary>
        /// 遅延実行時のみメッシュの計算結果をスワップする
        /// </summary>
        public void UpdateSwapBuffer()
        {
            Mesh.renderPosList.SwapBuffer();
            Mesh.renderNormalList.SwapBuffer();
            Mesh.renderTangentList.SwapBuffer();
            Mesh.renderBoneWeightList.SwapBuffer();

            swapIndex ^= 1;

            // 計算済みフラグを立てる
            foreach (var state in Mesh.renderMeshStateDict.Values)
            {
                state.SetFlag(PhysicsManagerMeshData.RenderStateFlag_DelayedCalculated, true);
            }
        }


        //=========================================================================================
        public JobHandle MasterJob
        {
            get
            {
                return jobHandle;
            }
            set
            {
                jobHandle = value;
            }
        }

        /// <summary>
        /// マスタージョブハンドル初期化
        /// </summary>
        public void InitJob()
        {
            jobHandle = default(JobHandle);
        }

        public void ScheduleJob()
        {
            JobHandle.ScheduleBatchedJobs();
        }

        /// <summary>
        /// マスタージョブハンドル完了待機
        /// </summary>
        public void CompleteJob()
        {
            jobHandle.Complete();
            jobHandle = default(JobHandle);
        }

        /// <summary>
        /// 遅延実行時のダブルバッファのフロントインデックス
        /// </summary>
        public int SwapIndex
        {
            get
            {
                return swapIndex;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 物理エンジン更新ループ処理
        /// これは１フレームにステップ回数分呼び出される
        /// 場合によっては１回も呼ばれないフレームも発生するので注意！
        /// </summary>
        /// <param name="updateCount"></param>
        /// <param name="loopIndex"></param>
        /// <param name="dtime"></param>
        void UpdatePhysics(int updateCount, int loopIndex, float updatePower, float updateDeltaTime)
        {
            if (Particle.Count == 0)
                return;

            // フォース影響＋速度更新
            var job1 = new ForceAndVelocityJob()
            {
                updateDeltaTime = updateDeltaTime,
                updatePower = updatePower,
                loopIndex = loopIndex,

                teamDataList = Team.teamDataList.ToJobArray(),
                teamMassList = Team.teamMassList.ToJobArray(),
                teamGravityList = Team.teamGravityList.ToJobArray(),
                teamDragList = Team.teamDragList.ToJobArray(),
                teamMaxVelocityList = Team.teamMaxVelocityList.ToJobArray(),
                //teamDirectionalDampingList = Team.teamDirectionalDampingList.ToJobArray(),

                flagList = Particle.flagList.ToJobArray(),
                teamIdList = Particle.teamIdList.ToJobArray(),
                depthList = Particle.depthList.ToJobArray(),
                basePosList = Particle.basePosList.ToJobArray(),
                baseRotList = Particle.baseRotList.ToJobArray(),

                nextPosList = Particle.InNextPosList.ToJobArray(),
                nextRotList = Particle.InNextRotList.ToJobArray(),
                oldPosList = Particle.oldPosList.ToJobArray(),
                oldRotList = Particle.oldRotList.ToJobArray(),
                frictionList = Particle.frictionList.ToJobArray(),
                oldSlowPosList = Particle.oldSlowPosList.ToJobArray(),

                posList = Particle.posList.ToJobArray(),
                rotList = Particle.rotList.ToJobArray(),
                velocityList = Particle.velocityList.ToJobArray(),

                //boneRotList = Bone.boneRotList.ToJobArray(),
            };
            jobHandle = job1.Schedule(Particle.Length, 64, jobHandle);

            // 拘束条件解決
            if (constraints != null)
            {
                // 拘束解決反復数分ループ
                for (int i = 0; i < solverIteration; i++)
                {
                    foreach (var con in constraints)
                    {
                        if (con != null /*&& con.enabled*/)
                        {
                            // 拘束ごとの反復回数
                            for (int j = 0; j < con.GetIterationCount(); j++)
                            {
                                jobHandle = con.SolverConstraint(updateDeltaTime, updatePower, j, jobHandle);
                            }
                        }
                    }
                }
            }

            // 座標確定
            var job2 = new FixPositionJob()
            {
                updatePower = updatePower,
                updateDeltaTime = updateDeltaTime,

                teamDataList = Team.teamDataList.ToJobArray(),

                flagList = Particle.flagList.ToJobArray(),
                teamIdList = Particle.teamIdList.ToJobArray(),
                nextPosList = Particle.InNextPosList.ToJobArray(),
                nextRotList = Particle.InNextRotList.ToJobArray(),

                basePosList = Particle.basePosList.ToJobArray(),
                baseRotList = Particle.baseRotList.ToJobArray(),

                oldPosList = Particle.oldPosList.ToJobArray(),
                oldRotList = Particle.oldRotList.ToJobArray(),
                oldSlowPosList = Particle.oldSlowPosList.ToJobArray(),

                frictionList = Particle.frictionList.ToJobArray(),

                velocityList = Particle.velocityList.ToJobArray(),
                rotList = Particle.rotList.ToJobArray(),
                posList = Particle.posList.ToJobArray()
            };
            jobHandle = job2.Schedule(Particle.Length, 64, jobHandle);

            // チーム更新カウント減算
            Team.UpdateTeamUpdateCount();
        }

        [BurstCompile]
        struct ForceAndVelocityJob : IJobParallelFor
        {
            public float updateDeltaTime;
            public float updatePower;
            public int loopIndex;

            // チーム
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<CurveParam> teamMassList;
            [Unity.Collections.ReadOnly]
            public NativeArray<CurveParam> teamGravityList;
            [Unity.Collections.ReadOnly]
            public NativeArray<CurveParam> teamDragList;
            [Unity.Collections.ReadOnly]
            public NativeArray<CurveParam> teamMaxVelocityList;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<CurveParam> teamDirectionalDampingList;

            // パーティクル
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseRotList;

            //[Unity.Collections.WriteOnly]
            public NativeArray<float3> nextPosList;
            //[Unity.Collections.WriteOnly]
            public NativeArray<quaternion> nextRotList;

            //[Unity.Collections.WriteOnly]
            public NativeArray<float> frictionList;

            public NativeArray<float3> posList;
            public NativeArray<quaternion> rotList;

            public NativeArray<float3> velocityList;

            public NativeArray<float3> oldPosList;
            public NativeArray<quaternion> oldRotList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> oldSlowPosList;

            // ボーン
            //[Unity.Collections.ReadOnly]
            //public NativeArray<quaternion> boneRotList;

            // パーティクルごと
            public void Execute(int index)
            {
                var flag = flagList[index];
                if (flag.IsValid() == false)
                    return;

                // チームデータ
                int teamId = teamIdList[index];
                var teamData = teamDataList[teamId];

                // ここからは更新がある場合のみ実行（グローバルチームは除く）
                if (teamId != 0 && teamData.IsUpdate() == false)
                    return;

                var oldpos = oldPosList[index];
                var oldrot = oldRotList[index];
                float3 nextPos = oldpos;
                quaternion nextRot = oldrot;

                //if (flag.IsCollider())
                //{
                //    // コライダー
                //    // todo:こっちのほうがよい？
                //    nextPos = basePosList[index];
                //    nextRot = baseRotList[index];
                //}
                if (flag.IsFixed())
                {
                    // キネマティックパーティクル
                    // nextPos/nextRotが前回の姿勢
                    var oldNextPos = nextPosList[index];
                    var oldNextRot = nextRotList[index];

                    // OldPos/Rot から BasePos/Rot に step で補間して現在姿勢とする
                    float stime = teamData.startTime + updateDeltaTime * teamData.runCount;
                    float oldtime = teamData.time - teamData.addTime;
                    float step = math.saturate((stime - oldtime) / teamData.addTime);
                    nextPos = math.lerp(oldpos, basePosList[index], step);
                    nextRot = math.slerp(oldrot, baseRotList[index], step);

                    // 前回の姿勢をoldpos/rotとしてposList/rotListに格納する
                    if (flag.IsCollider() && teamId == 0)
                    {
                        // グローバルコライダー
                        // 移動量と回転量に制限をかける(1.7.5)
                        // 制限をかけないと高速移動／回転時に遠く離れたパーティクルが押し出されてしまう問題が発生する。
                        oldpos = MathUtility.ClampDistance(nextPos, oldNextPos, Define.Compute.GlobalColliderMaxMoveDistance);
                        oldrot = MathUtility.ClampAngle(nextRot, oldNextRot, math.radians(Define.Compute.GlobalColliderMaxRotationAngle));
                    }
                    else
                    {
                        oldpos = oldNextPos;
                        oldrot = oldNextRot;
                    }

                    // debug
                    //nextPos = basePosList[index];
                    //nextRot = baseRotList[index];
                }
                else
                {
                    // 動的パーティクル
                    var depth = depthList[index];
                    var maxVelocity = teamMaxVelocityList[teamId].Evaluate(depth);
                    var drag = teamDragList[teamId].Evaluate(depth);
                    var gravity = teamGravityList[teamId].Evaluate(depth);
                    var mass = teamMassList[teamId].Evaluate(depth);
                    var velocity = velocityList[index];

                    // チームスケール倍率
                    maxVelocity *= teamData.scaleRatio;

                    // massは主に伸縮を中心に調整されるので、フォース適用時は少し調整する
                    mass = (mass - 1.0f) * teamData.forceMassInfluence + 1.0f;

                    // 安定化用の速度ウエイト
                    velocity *= teamData.velocityWeight;

                    // 最大速度
                    velocity = MathUtility.ClampVector(velocity, 0.0f, maxVelocity);

                    // 空気抵抗(90ups基準)
                    // 重力に影響させたくないので先に計算する（※通常はforce適用後に行うのが一般的）
                    velocity *= math.pow(1.0f - drag, updatePower);

                    // フォース
                    // フォースは空気抵抗を無視して加算する
                    float3 force = 0;

                    // 重力
                    // 重力は質量に関係なく一定
#if false
                    // 方向減衰
                    if (teamData.IsFlag(PhysicsManagerTeamData.Flag_DirectionalDamping) && teamData.directionalDampingBoneIndex >= 0)
                    {
                        float3 dampDir = math.mul(boneRotList[teamData.directionalDampingBoneIndex], teamData.directionalDampingLocalDir);
                        var dot = math.dot(dampDir, new float3(0, -1, 0)) * 0.5f + 0.5f; // 1.0(0) - 0.5(90) - 0.0(180)
                        var damp = teamDirectionalDampingList[teamId].Evaluate(dot);
                        gravity *= damp;
                    }
#endif

                    // (最後に質量で割るためここでは質量をかける）
                    force.y += gravity * mass;

                    // 外部フォース
                    if (loopIndex == 0)
                    {
                        switch (teamData.forceMode)
                        {
                            case PhysicsManagerTeamData.ForceMode.VelocityAdd:
                                force += teamData.impactForce;
                                break;
                            case PhysicsManagerTeamData.ForceMode.VelocityAddWithoutMass:
                                force += teamData.impactForce * mass;
                                break;
                            case PhysicsManagerTeamData.ForceMode.VelocityChange:
                                force += teamData.impactForce;
                                velocity = 0;
                                break;
                            case PhysicsManagerTeamData.ForceMode.VelocityChangeWithoutMass:
                                force += teamData.impactForce * mass;
                                velocity = 0;
                                break;
                        }

                        // 外力
                        force += teamData.externalForce;
                    }

                    // 外力チームスケール倍率
                    force *= teamData.scaleRatio;

                    // 速度計算(質量で割る)
                    velocity += (force / mass) * updateDeltaTime;

                    // 速度を理想位置に反映させる
                    nextPos = oldpos + velocity * updateDeltaTime;
                }

                // 予定座標更新 ==============================================================
                // 摩擦減衰
                var friction = frictionList[index];
                friction = friction * Define.Compute.FrictionDampingRate;
                frictionList[index] = friction;
                //frictionList[index] = 0;

                // 移動前の姿勢
                posList[index] = oldpos;
                rotList[index] = oldrot;

                // 予測位置
                nextPosList[index] = nextPos;
                nextRotList[index] = nextRot;

                // コリジョン用
                //velocityList[index] = nextPos;
            }
        }

        [BurstCompile]
        struct FixPositionJob : IJobParallelFor
        {
            public float updatePower;
            public float updateDeltaTime;

            // チーム
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;

            // パーティクルごと
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> nextRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseRotList;

            // パーティクルごと
            public NativeArray<float3> velocityList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotList;

            public NativeArray<float3> oldPosList;
            public NativeArray<quaternion> oldRotList;
            public NativeArray<float3> oldSlowPosList;

            public NativeArray<float3> posList;

            // パーティクルごと
            public void Execute(int index)
            {
                var flag = flagList[index];
                if (flag.IsValid() == false)
                    return;

                // チームデータ
                int teamId = teamIdList[index];
                var teamData = teamDataList[teamId];

                // ここからは更新がある場合のみ実行
                if (teamData.IsUpdate() == false)
                    return;

                // 速度更新(m/s)
                if (flag.IsFixed() == false)
                {
                    // 移動パーティクルのみ
                    var nextPos = nextPosList[index];
                    var nextRot = nextRotList[index];
                    nextRot = math.normalize(nextRot); // 回転蓄積で精度が落ちていくので正規化しておく

                    float3 velocity = 0;

                    // 移動パーティクルのみ速度を更新する
                    var pos = posList[index];

                    // 速度更新(m/s)
                    velocity = (nextPos - pos) / updateDeltaTime;
                    velocity *= teamData.velocityWeight; // 安定化用の速度ウエイト

                    // 摩擦による速度減衰
                    float friction = frictionList[index];
                    //friction *= teamData.friction; // チームごとの摩擦係数
                    velocity *= math.pow(1.0f - math.saturate(friction), updatePower);

                    // 実際の移動速度
                    posList[index] = (nextPos - oldPosList[index]) / updateDeltaTime;

                    // 書き戻し
                    velocityList[index] = velocity;

                    oldPosList[index] = nextPos;
                    oldRotList[index] = nextRot;

                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// 物理演算後処理
        /// </summary>
        /// <param name="updateIntervalTime"></param>
        void PostUpdatePhysics(float updateIntervalTime)
        {
            if (Particle.Count == 0)
                return;

            var job = new PostUpdatePhysicsJob()
            {
                updateIntervalTime = updateIntervalTime,

                teamDataList = Team.teamDataList.ToJobArray(),

                flagList = Particle.flagList.ToJobArray(),
                teamIdList = Particle.teamIdList.ToJobArray(),

                basePosList = Particle.basePosList.ToJobArray(),
                baseRotList = Particle.baseRotList.ToJobArray(),

                oldPosList = Particle.oldPosList.ToJobArray(),
                oldRotList = Particle.oldRotList.ToJobArray(),

                velocityList = Particle.velocityList.ToJobArray(),

                posList = Particle.posList.ToJobArray(),
                rotList = Particle.rotList.ToJobArray(),
                nextPosList = Particle.InNextPosList.ToJobArray(),

                oldSlowPosList = Particle.oldSlowPosList.ToJobArray(),
            };
            jobHandle = job.Schedule(Particle.Length, 64, jobHandle);
        }

        [BurstCompile]
        struct PostUpdatePhysicsJob : IJobParallelFor
        {
            public float updateIntervalTime;

            // チーム
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;

            // パーティクルごと
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;

            // パーティクルごと
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> velocityList;

            public NativeArray<float3> oldPosList;
            public NativeArray<quaternion> oldRotList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> posList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotList;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> nextPosList;

            public NativeArray<float3> oldSlowPosList;

            // パーティクルごと
            public void Execute(int index)
            {
                var flag = flagList[index];
                if (flag.IsValid() == false)
                    return;

                // チームデータ
                int teamId = teamIdList[index];
                var teamData = teamDataList[teamId];

                float3 viewPos = 0;
                quaternion viewRot = quaternion.identity;

                var basePos = basePosList[index];
                var baseRot = baseRotList[index];

                if (flag.IsFixed() == false)
                {
                    // 未来予測
                    // １フレーム前の表示位置と将来の予測位置を、現在のフレーム位置で線形補間する
                    var velocity = velocityList[index]; // 従来
                    //var velocity = posList[index]; // 実際の速度（どうもこっちだとカクつき？があるぞ）

                    var futurePos = oldPosList[index] + velocity * updateIntervalTime;
                    var oldViewPos = oldSlowPosList[index];
                    float oldTime = teamData.time - teamData.addTime;
                    float futureTime = teamData.time + (updateIntervalTime - teamData.nowTime);
                    float interval = futureTime - oldTime;
                    float ratio = teamData.addTime / interval;

                    // todo: 未来予測を切る
                    //futurePos = oldPosList[index];
                    //viewPos = futurePos;

                    viewPos = math.lerp(oldViewPos, futurePos, ratio);
                    viewRot = oldRotList[index];

                    oldSlowPosList[index] = viewPos;
                }
                else
                {
                    // 固定パーティクルの表示位置は常にベース位置
                    viewPos = basePos;
                    viewRot = baseRot;

                    // 固定パーティクルは今回のbasePosを記録する
                    oldPosList[index] = viewPos;
                    oldRotList[index] = viewRot;
                }

                // ブレンド
                if (teamData.blendRatio < 0.99f)
                {
                    viewPos = math.lerp(basePos, viewPos, teamData.blendRatio);
                    viewRot = math.slerp(baseRot, viewRot, teamData.blendRatio);
                    viewRot = math.normalize(viewRot); // 回転蓄積で精度が落ちていくので正規化しておく
                }

                // 表示位置
                posList[index] = viewPos;
                rotList[index] = viewRot;

                // TriangleWorker計算用にnextPosにコピーする
                nextPosList[index] = viewPos;
            }
        }

        //=========================================================================================
        /// <summary>
        /// ワーカーウォームアップ処理実行
        /// </summary>
        void WarmupWorker()
        {
            if (workers == null || workers.Count == 0)
                return;

            for (int i = 0; i < workers.Count; i++)
            {
                var worker = workers[i];
                worker.Warmup();
            }
        }
    }
}
