// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp

using UnityEditor;
using UnityEngine;


namespace MagicaCloth
{
    [System.Serializable]
    public class ClothMonitorUI : ClothMonitorAccess
    {
        // クロス表示
        [SerializeField]
        private bool alwaysClothShow = false;

        [SerializeField]
        private bool drawCloth = true;

        [SerializeField]
        private bool drawClothVertex = true;

        [SerializeField]
        private bool drawClothRadius = true;

        [SerializeField]
        private bool drawClothDepth;

        [SerializeField]
        private bool drawClothBase;

        [SerializeField]
        private bool drawClothCollider = true;

        [SerializeField]
        private bool drawClothStructDistanceLine = true;

        [SerializeField]
        private bool drawClothBendDistanceLine;

        [SerializeField]
        private bool drawClothNearDistanceLine;

        [SerializeField]
        private bool drawClothRotationLine = true;

        [SerializeField]
        private bool drawClothTriangleBend = true;

        [SerializeField]
        private bool drawClothPenetration = false;

        //[SerializeField]
        //private bool drawClothBaseSkinning = false;

        [SerializeField]
        private bool drawClothAxis;

        //[SerializeField]
        //private bool drawClothVolume;

        // デフォーマー表示
        [SerializeField]
        private bool alwaysDeformerShow = false;

        [SerializeField]
        private bool drawDeformer = true;

        [SerializeField]
        private bool drawDeformerVertexPosition;

        [SerializeField]
        private bool drawDeformerLine = true;

        [SerializeField]
        private bool drawDeformerTriangle = true;

        [SerializeField]
        private bool drawDeformerVertexAxis;

        // 風表示
        [SerializeField]
        private bool alwaysWindShow = true;
        [SerializeField]
        private bool drawWind = true;

#if MAGICACLOTH_DEBUG
        // デバッグ用
        [SerializeField]
        private bool drawClothVertexNumber;

        [SerializeField]
        private bool drawClothVertexIndex;

        [SerializeField]
        private bool drawClothFriction;

        [SerializeField]
        private bool drawClothDepthNumber;

        [SerializeField]
        private bool drawPenetrationOrigin;

        //[SerializeField]
        //private bool drawAdjustRotationLine;


        [SerializeField]
        private int debugDrawDeformerTriangleNumber = -1;

        [SerializeField]
        private int debugDrawDeformerVertexNumber = -1;

        [SerializeField]
        private bool drawDeformerVertexNumber;

        [SerializeField]
        private bool drawDeformerTriangleNormal;

        [SerializeField]
        private bool drawDeformerTriangleNumber;

#endif

        //=========================================================================================
        Vector2 scrollPos;

        //=========================================================================================
        public override void Disable()
        {
        }

        public override void Enable()
        {
        }

        protected override void Create()
        {
        }

        public override void Destroy()
        {
        }

        public void OnGUI()
        {
            if (menu == null)
                return;

            Version();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            Information();
            DebugOption();
            EditorGUILayout.EndScrollView();
        }

        //=========================================================================================
        public bool AlwaysClothShow
        {
            get
            {
                return alwaysClothShow;
            }
        }

        public bool AlwaysDeformerShow
        {
            get
            {
                return alwaysDeformerShow;
            }
        }

        public bool DrawDeformer
        {
            get
            {
                return drawDeformer;
            }
        }

        public bool DrawDeformerVertexPosition
        {
            get
            {
                return drawDeformerVertexPosition;
            }
        }

        public bool DrawDeformerLine
        {
            get
            {
                return drawDeformerLine;
            }
        }

        public bool DrawDeformerTriangle
        {
            get
            {
                return drawDeformerTriangle;
            }
        }

        public bool DrawCloth
        {
            get
            {
                return drawCloth;
            }
        }

        public bool DrawClothVertex
        {
            get
            {
                return drawClothVertex;
            }
        }

        public bool DrawClothRadius
        {
            get
            {
                return drawClothRadius;
            }
        }

        public bool DrawClothDepth
        {
            get
            {
                return drawClothDepth;
            }
        }

        public bool DrawClothBase
        {
            get
            {
                return drawClothBase;
            }
        }

        public bool DrawClothCollider
        {
            get
            {
                return drawClothCollider;
            }
        }

        public bool DrawClothStructDistanceLine
        {
            get
            {
                return drawClothStructDistanceLine;
            }
        }

        public bool DrawClothBendDistanceLine
        {
            get
            {
                return drawClothBendDistanceLine;
            }
        }

        public bool DrawClothNearDistanceLine
        {
            get
            {
                return drawClothNearDistanceLine;
            }
        }

        public bool DrawClothRotationLine
        {
            get
            {
                return drawClothRotationLine;
            }
        }

        public bool DrawClothTriangleBend
        {
            get
            {
                return drawClothTriangleBend;
            }
        }

        public bool DrawClothPenetration
        {
            get
            {
                return drawClothPenetration;
            }
        }

        //public bool DrawClothBaseSkinning
        //{
        //    get
        //    {
        //        return drawClothBaseSkinning;
        //    }
        //}

        public bool DrawClothAxis
        {
            get
            {
                return drawClothAxis;
            }
        }

        public bool DrawDeformerVertexAxis
        {
            get
            {
                return drawDeformerVertexAxis;
            }
        }

        //public bool DrawClothVolume
        //{
        //    get
        //    {
        //        return drawClothVolume;
        //    }
        //}


        public bool AlwaysWindShow
        {
            get
            {
                return alwaysWindShow;
            }
        }

        public bool DrawWind
        {
            get
            {
                return drawWind;
            }
        }

#if MAGICACLOTH_DEBUG
        // デバッグ用
        public bool DrawClothVertexNumber
        {
            get
            {
                return drawClothVertexNumber;
            }
        }
        public bool DrawClothVertexIndex
        {
            get
            {
                return drawClothVertexIndex;
            }
        }
        public bool DrawClothFriction
        {
            get
            {
                return drawClothFriction;
            }
        }
        public bool DrawClothDepthNumber
        {
            get
            {
                return drawClothDepthNumber;
            }
        }
        public bool DrawPenetrationOrigin
        {
            get
            {
                return drawPenetrationOrigin;
            }
        }

        //public bool DrawAdjustRotationLine
        //{
        //    get
        //    {
        //        return drawAdjustRotationLine;
        //    }
        //}


        public int DebugDrawDeformerTriangleNumber
        {
            get
            {
                return debugDrawDeformerTriangleNumber;
            }
        }
        public int DebugDrawDeformerVertexNumber
        {
            get
            {
                return debugDrawDeformerVertexNumber;
            }
        }
        public bool DrawDeformerTriangleNormal
        {
            get
            {
                return drawDeformerTriangleNormal;
            }
        }

        public bool DrawDeformerTriangleNumber
        {
            get
            {
                return drawDeformerTriangleNumber;
            }
        }

        public bool DrawDeformerVertexNumber
        {
            get
            {
                return drawDeformerVertexNumber;
            }
        }

#endif

        //=========================================================================================
        void Version()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Magica Cloth. (Version " + AboutMenu.MagicaClothVersion + ")", EditorStyles.boldLabel);
        }

        void Information()
        {
            StaticStringBuilder.Clear();

            int teamCnt = 0;
            int activeTeamCnt = 0;

            int sharedVirtualMeshCnt = 0;
            int virtualMeshCnt = 0;
            int sharedChildMeshCnt = 0;
            int sharedRenderMeshCnt = 0;
            int renderMeshCnt = 0;

            int virtualMeshVertexCnt = 0;
            int virtualMeshTriangleCnt = 0;
            int renderMeshVertexCnt = 0;

            int virtualMeshUseCnt = 0;
            int virtualMeshVertexUseCnt = 0;
            int renderMeshUseCnt = 0;
            int renderMeshVertexUseCnt = 0;

            int particleCnt = 0;
            int colliderCnt = 0;
            int restoreBoneCnt = 0;
            int readBoneCnt = 0;
            int writeBoneCnt = 0;

            if (EditorApplication.isPlaying && MagicaPhysicsManager.IsInstance())
            {
                var manager = MagicaPhysicsManager.Instance;
                teamCnt = manager.Team.TeamCount;
                activeTeamCnt = manager.Team.ActiveTeamCount;

                sharedVirtualMeshCnt = manager.Mesh.SharedVirtualMeshCount;
                virtualMeshCnt = manager.Mesh.VirtualMeshCount;
                sharedChildMeshCnt = manager.Mesh.SharedChildMeshCount;
                sharedRenderMeshCnt = manager.Mesh.SharedRenderMeshCount;
                renderMeshCnt = manager.Mesh.RenderMeshCount;

                virtualMeshVertexCnt = manager.Mesh.VirtualMeshVertexCount;
                virtualMeshTriangleCnt = manager.Mesh.VirtualMeshTriangleCount;
                renderMeshVertexCnt = manager.Mesh.RenderMeshVertexCount;

                virtualMeshUseCnt = manager.Mesh.VirtualMeshUseCount;
                virtualMeshVertexUseCnt = manager.Mesh.VirtualMeshVertexUseCount;
                renderMeshUseCnt = manager.Mesh.RenderMeshUseCount;
                renderMeshVertexUseCnt = manager.Mesh.RenderMeshVertexUseCount;

                particleCnt = manager.Particle.Count;
                colliderCnt = manager.Particle.ColliderCount;
                restoreBoneCnt = manager.Bone.RestoreBoneCount;
                readBoneCnt = manager.Bone.ReadBoneCount;
                writeBoneCnt = manager.Bone.WriteBoneCount;
            }

            StaticStringBuilder.AppendLine("Cloth Team: ", teamCnt);
            StaticStringBuilder.AppendLine("Active Cloth Team: ", activeTeamCnt);
            StaticStringBuilder.AppendLine();

            StaticStringBuilder.AppendLine("Shared Virtual Mesh: ", sharedVirtualMeshCnt);
            StaticStringBuilder.AppendLine("Virtual Mesh: ", virtualMeshCnt);
            StaticStringBuilder.AppendLine("Shared Child Mesh: ", sharedChildMeshCnt);
            StaticStringBuilder.AppendLine("Shared Render Mesh: ", sharedRenderMeshCnt);
            StaticStringBuilder.AppendLine("Render Mesh: ", renderMeshCnt);
            StaticStringBuilder.AppendLine();

            StaticStringBuilder.AppendLine("Virtual Mesh Vertex: ", virtualMeshVertexCnt);
            StaticStringBuilder.AppendLine("Virtual Mesh Triangle: ", virtualMeshTriangleCnt);
            StaticStringBuilder.AppendLine("Render Mesh Vertex: ", renderMeshVertexCnt);
            StaticStringBuilder.AppendLine();

            StaticStringBuilder.AppendLine("Virtual Mesh Used: ", virtualMeshUseCnt);
            StaticStringBuilder.AppendLine("Virtual Mesh Vertex Used: ", virtualMeshVertexUseCnt);
            StaticStringBuilder.AppendLine("Render Mesh Used: ", renderMeshUseCnt);
            StaticStringBuilder.AppendLine("Render Mesh Vertex Used: ", renderMeshVertexUseCnt);
            StaticStringBuilder.AppendLine();

            StaticStringBuilder.AppendLine("Particle: ", particleCnt);
            StaticStringBuilder.AppendLine("Collider: ", colliderCnt);
            StaticStringBuilder.AppendLine("Restore Transform: ", restoreBoneCnt);
            StaticStringBuilder.AppendLine("Read Transform: ", readBoneCnt);
            StaticStringBuilder.Append("Write Transform: ", writeBoneCnt);

            EditorGUILayout.HelpBox(StaticStringBuilder.ToString(), MessageType.Info);
        }

        void DebugOption()
        {
            EditorGUI.BeginChangeCheck();


            EditorInspectorUtility.Foldout("Cloth Team Gizmos", "Cloth Team Gizmos",
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!drawCloth);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        alwaysClothShow = EditorGUILayout.Toggle("Always Show", alwaysClothShow);
                        drawClothVertex = EditorGUILayout.Toggle("Particle Position", drawClothVertex);
                        drawClothRadius = EditorGUILayout.Toggle("Particle Radius", drawClothRadius);
                        drawClothDepth = EditorGUILayout.Toggle("Particle Depth", drawClothDepth);
                        drawClothBase = EditorGUILayout.Toggle("Particle Base", drawClothBase);
                        drawClothAxis = EditorGUILayout.Toggle("Particle Axis", drawClothAxis);
                        drawClothCollider = EditorGUILayout.Toggle("Collider", drawClothCollider);
                        drawClothStructDistanceLine = EditorGUILayout.Toggle("Struct Distance Line", drawClothStructDistanceLine);
                        drawClothBendDistanceLine = EditorGUILayout.Toggle("Bend Distance Line", drawClothBendDistanceLine);
                        drawClothNearDistanceLine = EditorGUILayout.Toggle("Near Distance Line", drawClothNearDistanceLine);
                        drawClothRotationLine = EditorGUILayout.Toggle("Rotation Line", drawClothRotationLine);
                        drawClothTriangleBend = EditorGUILayout.Toggle("Triangle Bend", drawClothTriangleBend);
                        drawClothPenetration = EditorGUILayout.Toggle("Penetration", drawClothPenetration);
                        //drawClothBaseSkinning = EditorGUILayout.Toggle("Base Skinning", drawClothBaseSkinning);
#if MAGICACLOTH_DEBUG
                        drawClothVertexNumber = EditorGUILayout.Toggle("[D] Particle Number", drawClothVertexNumber);
                        drawClothVertexIndex = EditorGUILayout.Toggle("[D] Particle Index", drawClothVertexIndex);
                        drawClothFriction = EditorGUILayout.Toggle("[D] Particle Friction", drawClothFriction);
                        drawClothDepthNumber = EditorGUILayout.Toggle("[D] Particle Depth", drawClothDepthNumber);
                        drawPenetrationOrigin = EditorGUILayout.Toggle("[D] Penetration Origin", drawPenetrationOrigin);
                        //drawClothVolume = EditorGUILayout.Toggle("Volume", drawClothVolume);
                        //drawAdjustRotationLine = EditorGUILayout.Toggle("Adjust Rotation Line", drawAdjustRotationLine);
#endif
                    }
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    drawCloth = sw;
                },
                drawCloth
                );

            EditorInspectorUtility.Foldout("Deformer Gizmos", "Deformer Gizmos",
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!drawDeformer);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        alwaysDeformerShow = EditorGUILayout.Toggle("Always Show", alwaysDeformerShow);
                        drawDeformerVertexPosition = EditorGUILayout.Toggle("Vertex Position", drawDeformerVertexPosition);
                        drawDeformerVertexAxis = EditorGUILayout.Toggle("Vertex Axis", drawDeformerVertexAxis);
                        drawDeformerLine = EditorGUILayout.Toggle("Line", drawDeformerLine);
                        drawDeformerTriangle = EditorGUILayout.Toggle("Triangle", drawDeformerTriangle);
#if MAGICACLOTH_DEBUG
                        drawDeformerVertexNumber = EditorGUILayout.Toggle("[D] Vertex Number", drawDeformerVertexNumber);
                        debugDrawDeformerVertexNumber = EditorGUILayout.IntField("[D] Vertex Number", debugDrawDeformerVertexNumber);
                        drawDeformerTriangleNormal = EditorGUILayout.Toggle("[D] Triangle Normal", drawDeformerTriangleNormal);
                        drawDeformerTriangleNumber = EditorGUILayout.Toggle("[D] Triangle Number", drawDeformerTriangleNumber);
                        debugDrawDeformerTriangleNumber = EditorGUILayout.IntField("[D] Triangle Number", debugDrawDeformerTriangleNumber);
#endif
                    }
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    drawDeformer = sw;
                },
                drawDeformer
                );

            EditorInspectorUtility.Foldout("Wind Gizmos", "Wind Gizmos",
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!drawWind);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        alwaysWindShow = EditorGUILayout.Toggle("Always Show", alwaysWindShow);
                    }
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    drawWind = sw;
                },
                drawWind
                );

            if (EditorGUI.EndChangeCheck())
            {
                // Sceneビュー更新
                SceneView.RepaintAll();
            }
        }

    }
}
