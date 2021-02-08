// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// Editorインスペクタ表示に関するユーティリティ
    /// </summary>
    public static class EditorInspectorUtility
    {
        /// <summary>
        /// 現在のデータ状態をインスペクタにヘルプボックスで表示する
        /// </summary>
        /// <param name="istatus"></param>
        public static void DispDataStatus(IDataVerify verify)
        {
            if (verify == null)
                return;

            var code = verify.VerifyData();
            //bool valid = verify.VerifyData() == Define.Error.None;
            //var mestype = valid ? MessageType.Info : MessageType.Error;
            var mestype = MessageType.Info;
            if (Define.IsWarning(code))
                mestype = MessageType.Warning;
            if (Define.IsError(code))
                mestype = MessageType.Error;

            EditorGUILayout.HelpBox(verify.GetInformation(), mestype);
        }

        /// <summary>
        /// 現在のデータバージョン状態をインスペクタにヘルプボックスで表示する
        /// </summary>
        /// <param name="core"></param>
        public static void DispVersionStatus(CoreComponent core)
        {
            var code = core.VerityDataVersion();
            if (Define.IsNormal(code))
                return;

            EditorGUILayout.HelpBox(Define.GetErrorMessage(code), MessageType.Warning);
        }

        //===============================================================================
        /// <summary>
        /// ベジェ曲線パラメータのインスペクタ描画と変更操作
        /// </summary>
        /// <param name="title">パラメータ名</param>
        /// <param name="bval">ベジェ曲線パラメータクラス</param>
        /// <param name="minVal">ベジェ曲線の最小値</param>
        /// <param name="maxVal">ベジェ曲線の最大値</param>
        /// <param name="valFmt">パラメータ表示浮動小数点数フォーマット</param>
        /// <returns></returns>
        public static void BezierInspector(
            string title,
            SerializedProperty bval,
            float minVal,
            float maxVal,
            string valFmt = "F2"
            )
        {
            var startValue = bval.FindPropertyRelative("startValue");
            var endValue = bval.FindPropertyRelative("endValue");
            var curveValue = bval.FindPropertyRelative("curveValue");
            var useEndValue = bval.FindPropertyRelative("useEndValue");
            var useCurveValue = bval.FindPropertyRelative("useCurveValue");

            // グラフ描画
            float sv, ev, cv;
            GetBezierValue(bval, out sv, out ev, out cv);
            DrawGraph(sv, ev, cv, minVal, maxVal, valFmt);

            // パラメータ
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(32));
            EditorGUILayout.Slider(startValue, minVal, maxVal, "start");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            useEndValue.boolValue = EditorGUILayout.Toggle(useEndValue.boolValue, GUILayout.Width(32));
            EditorGUI.BeginDisabledGroup(!useEndValue.boolValue);
            EditorGUILayout.Slider(endValue, minVal, maxVal, "end");
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            useCurveValue.boolValue = EditorGUILayout.Toggle(useCurveValue.boolValue, GUILayout.Width(32));
            EditorGUI.BeginDisabledGroup(!useCurveValue.boolValue || !useEndValue.boolValue);
            EditorGUILayout.Slider(curveValue, -1.0f, 1.0f, "curve");
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        static void GetBezierValue(SerializedProperty bval, out float start, out float end, out float curve)
        {
            var startValue = bval.FindPropertyRelative("startValue");
            var endValue = bval.FindPropertyRelative("endValue");
            var curveValue = bval.FindPropertyRelative("curveValue");
            var useEndValue = bval.FindPropertyRelative("useEndValue");
            var useCurveValue = bval.FindPropertyRelative("useCurveValue");

            start = startValue.floatValue;
            end = useEndValue.boolValue ? endValue.floatValue : start;
            curve = useCurveValue.boolValue && useEndValue.boolValue ? curveValue.floatValue : 0.0f;
        }

        // ベジェ曲線グラフ描画
        static void DrawGraph(float startVal, float endVal, float curveVal, float minVal, float maxVal, string valFmt)
        {
            EditorGUILayout.Space();

            // 表示領域
            const float headOffsetX = 40;
            const float tailOffsetX = 10;
            float w = GUILayoutUtility.GetLastRect().width;
            //Rect drect = GUILayoutUtility.GetRect(w, 100f);
            Rect drect = GUILayoutUtility.GetRect(w, 120f);
            //float indentWidth = EditorGUI.indentLevel * 16;
            //drect.x += indentWidth;
            //drect.width -= indentWidth;
            Rect area = new Rect(drect.x + headOffsetX, drect.y, drect.width - headOffsetX - tailOffsetX, drect.height);

            // grid
            Handles.color = new Color(1f, 1f, 1f, 0.5f);
            Handles.DrawLine(new Vector2(area.xMin, area.yMin), new Vector2(area.xMax, area.yMin));
            Handles.DrawLine(new Vector2(area.xMin, area.yMax), new Vector2(area.xMax, area.yMax));
            Handles.DrawLine(new Vector2(area.xMin, area.yMin), new Vector2(area.xMin, area.yMax));
            Handles.DrawLine(new Vector2(area.xMax, area.yMin), new Vector2(area.xMax, area.yMax));

            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            Handles.DrawLine(new Vector2(area.xMin, (area.yMin + area.yMax) * 0.5f), new Vector2(area.xMax, (area.yMin + area.yMax) * 0.5f));
            Handles.DrawLine(new Vector2((area.xMin + area.xMax) * 0.5f, area.yMin), new Vector2((area.xMin + area.xMax) * 0.5f, area.yMax));


            // grid数値
            Handles.Label(new Vector3(drect.xMin, drect.yMin - 4), maxVal.ToString("F1"));
            Handles.Label(new Vector3(drect.xMin, drect.yMax - 12), minVal.ToString("F1"));

            // データ領域
            Rect vrect = new Rect(0.0f, minVal, 1.0f, maxVal - minVal);

            // データ領域での位置
            Vector3 svpos = Rect.PointToNormalized(vrect, new Vector2(0.0f, startVal));
            Vector3 evpos = Rect.PointToNormalized(vrect, new Vector2(1.0f, endVal));

            // 表示のためyを逆転
            svpos.y = 1.0f - svpos.y;
            evpos.y = 1.0f - evpos.y;

            // グラフ上の座標
            Vector3 spos = Rect.NormalizedToPoint(area, svpos);
            Vector3 epos = Rect.NormalizedToPoint(area, evpos);

            // 対角線計算用
            Vector3 spos0 = Rect.NormalizedToPoint(area, new Vector2(0.0f, evpos.y));
            Vector3 epos0 = Rect.NormalizedToPoint(area, new Vector2(1.0f, svpos.y));

            // ベジェ制御点（対角線を補間）
            Vector3 stan = Vector3.Lerp(spos0, epos0, curveVal * 0.5f + 0.5f);
            Vector3 etan = stan;

            // ベジェ描画
            Color bcol = GUI.enabled ? Color.green : new Color(0.0f, 0.5f, 0.0f, 1.0f);
            Handles.DrawBezier(spos, epos, stan, etan, bcol, null, 2.0f);

            // 両端値
            Handles.Label(spos, startVal.ToString(valFmt));
            Handles.Label(epos + new Vector3(-38, 0, 0), endVal.ToString(valFmt));

#if false
            // 制御点描画
            Handles.color = Color.red;
            Handles.DrawWireCube(stan, Vector3.one * 2);
#endif
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        //===============================================================================
        /// <summary>
        /// 折りたたみ制御
        /// </summary>
        /// <param name="foldKey">折りたたみ保存キー</param>
        /// <param name="title"></param>
        /// <param name="drawAct">内容描画アクション</param>
        /// <param name="enableAct">有効フラグアクション(null=無効)</param>
        /// <param name="enable">現在の有効フラグ</param>
        public static void Foldout(
            string foldKey,
            string title = null,
            System.Action drawAct = null,
            System.Action<bool> enableAct = null,
            bool enable = false,
            bool warning = false
            )
        {
            var style = new GUIStyle("ShurikenModuleTitle");
            style.font = new GUIStyle(EditorStyles.label).font;
            style.border = new RectOffset(15, 7, 4, 4);
            style.fixedHeight = 22;
            style.contentOffset = new Vector2(20f, -2f);

            var rect = GUILayoutUtility.GetRect(16f, 22f, style);

            GUI.backgroundColor = warning ? Color.yellow : Color.white;
            GUI.Box(rect, title, style);
            GUI.backgroundColor = Color.white;

            var e = Event.current;
            bool foldOut = EditorPrefs.GetBool(foldKey);

            if (enableAct == null)
            {
                if (e.type == EventType.Repaint)
                {
                    var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
                    EditorStyles.foldout.Draw(toggleRect, false, false, foldOut, false);
                }
            }
            else
            {
                // 有効チェック
                var toggleRect = new Rect(rect.x + 4f, rect.y + 4f, 13f, 13f);
                bool sw = GUI.Toggle(toggleRect, enable, string.Empty, new GUIStyle("ShurikenCheckMark"));
                if (sw != enable)
                {
                    enableAct(sw);
                }
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                foldOut = !foldOut;
                EditorPrefs.SetBool(foldKey, foldOut);
                e.Use();
            }

            if (foldOut && drawAct != null)
            {
                drawAct();
            }
        }

        //===============================================================================
        static bool MinMaxCurveInspector(string title, string valueName, SerializedProperty bval, float minval, float maxval)
        {
            EditorGUI.BeginChangeCheck();

            float sv, ev, cv;
            GetBezierValue(bval, out sv, out ev, out cv);

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", sv, "/", ev, "]");

            Foldout(title, StaticStringBuilder.ToString(), () =>
            {
                if (string.IsNullOrEmpty(valueName) == false)
                    EditorGUILayout.LabelField(valueName);
                BezierInspector(title, bval, minval, maxval);
            });

            return EditorGUI.EndChangeCheck();
        }

        static bool UseMinMaxCurveInspector(
            string title,
            SerializedProperty use,
            string valueName,
            SerializedProperty bval, float minval, float maxval,
            string valFmt = "F2",
            bool warning = false
            )
        {
            EditorGUI.BeginChangeCheck();

            float sv, ev, cv;
            GetBezierValue(bval, out sv, out ev, out cv);

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", sv, "/", ev, "]");

            bool wuse = use.boolValue;
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    if (string.IsNullOrEmpty(valueName) == false)
                        EditorGUILayout.LabelField(valueName);
                    EditorGUI.BeginDisabledGroup(!wuse);
                    BezierInspector(title, bval, minval, maxval, valFmt);
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    wuse = sw;
                },
                wuse,
                warning
                );
            use.boolValue = wuse;

            return EditorGUI.EndChangeCheck();
        }

        public static bool OneSliderInspector(
            string title,
            string name1, SerializedProperty property1, float min1, float max1
            )
        {
            EditorGUI.BeginChangeCheck();

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", property1.floatValue, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUILayout.Slider(property1, min1, max1, name1);
                }
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool TwoSliderInspector(
            string title,
            string name1, SerializedProperty property1, float min1, float max1,
            string name2, SerializedProperty property2, float min2, float max2
            )
        {
            EditorGUI.BeginChangeCheck();

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", property1.floatValue, "/", property2.floatValue, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUILayout.Slider(property1, min1, max1, name1);
                    EditorGUILayout.Slider(property2, min2, max2, name2);
                }
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool UseOneSliderInspector(
            string title, SerializedProperty use,
            string name1, SerializedProperty val1, float min1, float max1
            )
        {
            EditorGUI.BeginChangeCheck();

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", val1.floatValue, "]");
            bool workuse = use.boolValue;
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!workuse);
                    EditorGUILayout.Slider(val1, min1, max1, name1);
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    workuse = sw;
                },
                workuse
                );
            use.boolValue = workuse;

            return EditorGUI.EndChangeCheck();
        }

        public static bool UseTwoSliderInspector(
            string title, SerializedProperty use,
            string name1, SerializedProperty val1, float min1, float max1,
            string name2, SerializedProperty val2, float min2, float max2
            )
        {
            EditorGUI.BeginChangeCheck();

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", val1.floatValue, "/", val2.floatValue, "]");
            bool workuse = use.boolValue;
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!workuse);
                    EditorGUILayout.Slider(val1, min1, max1, name1);
                    EditorGUILayout.Slider(val2, min2, max2, name2);
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    workuse = sw;
                },
                workuse
                );
            use.boolValue = workuse;

            return EditorGUI.EndChangeCheck();
        }

        //===============================================================================
        public static bool WorldInfluenceInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var influenceTarget = cparam.FindPropertyRelative("influenceTarget");
            var moveInfluence = cparam.FindPropertyRelative("worldMoveInfluence");
            var rotInfluence = cparam.FindPropertyRelative("worldRotationInfluence");
            var maxSpeed = cparam.FindPropertyRelative("maxMoveSpeed");

            var useTeleport = cparam.FindPropertyRelative("useResetTeleport");
            var teleportDistance = cparam.FindPropertyRelative("teleportDistance");
            var teleportRotation = cparam.FindPropertyRelative("teleportRotation");

            var stabilizationTime = cparam.FindPropertyRelative("resetStabilizationTime");

            //float sv, ev, cv;
            //GetBezierValue(worldInfluence, out sv, out ev, out cv);

            const string title = "World Influence";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title);
            //StaticStringBuilder.Append(title, " [", sv, "/", ev, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUILayout.PropertyField(influenceTarget);
                    EditorGUILayout.Slider(maxSpeed, 0.0f, 30.0f, "Max Move Speed");
                    EditorGUILayout.LabelField("Movement Influence");
                    BezierInspector("Move Influence", moveInfluence, 0.0f, 1.0f);
                    EditorGUILayout.LabelField("Rotation Influence");
                    BezierInspector("Rotation Influence", rotInfluence, 0.0f, 1.0f);

                    useTeleport.boolValue = EditorGUILayout.Toggle("Reset After Teleport", useTeleport.boolValue);

                    EditorGUI.BeginDisabledGroup(!useTeleport.boolValue);
                    EditorGUILayout.Slider(teleportDistance, 0.0f, 1.0f, "Teleport Distance");
                    EditorGUILayout.Slider(teleportRotation, 0.0f, 180.0f, "Teleport Rotation");
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.Space();
                    //EditorGUILayout.LabelField("Stabilize After Reset");
                    //EditorGUILayout.Slider(stabilizationTime, 0.0f, 3.0f, "Stabilization Time");
                    EditorGUILayout.Slider(stabilizationTime, 0.0f, 1.0f, "Stabilization Time After Reset");
                },
                warning: changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool DistanceDisableInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("useDistanceDisable");
            var referenceObject = cparam.FindPropertyRelative("disableReferenceObject");
            var disableDisance = cparam.FindPropertyRelative("disableDistance");
            var fadeDistance = cparam.FindPropertyRelative("disableFadeDistance");

            const string title = "Distance Disable";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title);
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);

                    EditorGUILayout.HelpBox("If Reference Object is [None], the main camera is referred.", MessageType.None);

                    //EditorGUILayout.PropertyField(referenceObject);
                    referenceObject.objectReferenceValue = EditorGUILayout.ObjectField("Reference Object", referenceObject.objectReferenceValue, typeof(Transform), true);
                    EditorGUILayout.Slider(disableDisance, 0.0f, 100.0f, "Distance");
                    EditorGUILayout.Slider(fadeDistance, 0.0f, 10.0f, "Fade Distance");

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool ExternalForceInspector(SerializedProperty cparam)
        {
            EditorGUI.BeginChangeCheck();

            var massInfluence = cparam.FindPropertyRelative("massInfluence");
            var windInfluence = cparam.FindPropertyRelative("windInfluence");
            var windRandomScale = cparam.FindPropertyRelative("windRandomScale");

            const string title = "External Force";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title);
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUILayout.Slider(massInfluence, 0.0f, 1.0f, "Mass Influence");
                    //EditorGUILayout.LabelField("Wind");
                    EditorGUILayout.Space();
                    EditorGUILayout.Slider(windInfluence, 0.0f, 3.0f, "Wind Influence");
                    EditorGUILayout.Slider(windRandomScale, 0.0f, 2.0f, "Wind Random Scale");
                }
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool RadiusInspector(SerializedProperty cparam)
        {
            return MinMaxCurveInspector("Radius", "Radius", cparam.FindPropertyRelative("radius"), 0.001f, 0.1f);
        }

        public static bool MassInspector(SerializedProperty cparam)
        {
            return MinMaxCurveInspector("Mass", "Mass", cparam.FindPropertyRelative("mass"), 1.0f, 20.0f);
        }

        public static bool GravityInspector(SerializedProperty cparam)
        {
            //return UseMinMaxCurveInspector("Gravity", cparam.FindPropertyRelative("useGravity"), "Gravity Acceleration", cparam.FindPropertyRelative("gravity"), -20.0f, 0.0f);

            EditorGUI.BeginChangeCheck();

            var useGravity = cparam.FindPropertyRelative("useGravity");
            var gravity = cparam.FindPropertyRelative("gravity");
            //var useDirectional = cparam.FindPropertyRelative("useDirectionalDamping");
            //var refObject = cparam.FindPropertyRelative("directionalDampingObject");
            //var directionaDamping = cparam.FindPropertyRelative("directionalDamping");

            float sv, ev, cv;
            GetBezierValue(gravity, out sv, out ev, out cv);

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append("Gravity", " [", sv, "/", ev, "]");

            bool wuse = useGravity.boolValue;
            Foldout("Gravity", StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!wuse);
                    EditorGUILayout.LabelField("Gravity Acceleration");
                    BezierInspector("Gravity", gravity, -20.0f, 0.0f, "F2");

                    //useDirectional.boolValue = EditorGUILayout.Toggle("Directional Damping", useDirectional.boolValue);
                    //refObject.objectReferenceValue = EditorGUILayout.ObjectField("Reference Object", refObject.objectReferenceValue, typeof(Transform), true);
                    //EditorGUILayout.LabelField("Angular Damping");
                    //EditorGUILayout.HelpBox("The horizontal axis is the angle 0-90-180.", MessageType.None);
                    //BezierInspector("Angular Damping", directionaDamping, 0.0f, 1.0f, "F2");

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    wuse = sw;
                },
                wuse
                );
            useGravity.boolValue = wuse;

            return EditorGUI.EndChangeCheck();
        }

        public static bool DragInspector(SerializedProperty cparam)
        {
            return UseMinMaxCurveInspector("Drag", cparam.FindPropertyRelative("useDrag"), "Drag", cparam.FindPropertyRelative("drag"), 0.0f, 0.3f);
        }

        public static bool MaxVelocityInspector(SerializedProperty cparam)
        {
            return UseMinMaxCurveInspector("Max Velocity", cparam.FindPropertyRelative("useMaxVelocity"), "Max Velocity", cparam.FindPropertyRelative("maxVelocity"), 0.01f, 10.0f);
        }

        public static bool TriangleBendInspector(SerializedProperty cparam, bool changed)
        {
            return UseMinMaxCurveInspector("Triangle Bend", cparam.FindPropertyRelative("useTriangleBend"), "Bend Power", cparam.FindPropertyRelative("triangleBend"), 0.0f, 1.0f, warning: changed);
        }

        public static bool DirectionMoveLimitInspector(SerializedProperty cparam)
        {
            return UseMinMaxCurveInspector("Limit Move To Hits", cparam.FindPropertyRelative("useDirectionMoveLimit"), "Move Limit", cparam.FindPropertyRelative("directionMoveLimit"), -0.2f, 0.2f);
        }

        public static bool RestoreRotationInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("useRestoreRotation");
            var power = cparam.FindPropertyRelative("restoreRotation");
            var influence = cparam.FindPropertyRelative("restoreRotationVelocityInfluence");

            const string title = "Restore Rotation";
            float sv, ev, cv;
            GetBezierValue(power, out sv, out ev, out cv);
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append("Restore Rotation", " [", sv, "/", ev, "]");

            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);

                    EditorGUILayout.LabelField("Restore Power");
                    BezierInspector("Restore Power", power, 0.0f, 1.0f);
                    EditorGUILayout.Slider(influence, 0.0f, 1.0f, "Velocity Influence");

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool ClampRotationInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("useClampRotation");
            var angle = cparam.FindPropertyRelative("clampRotationAngle");
            //var stiffness = cparam.FindPropertyRelative("clampRotationStiffness");
            var influence = cparam.FindPropertyRelative("clampRotationVelocityInfluence");

            const string title = "Clamp Rotation";
            float sv, ev, cv;
            GetBezierValue(angle, out sv, out ev, out cv);
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append("Clamp Rotation", " [", sv, "/", ev, "]");

            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);

                    EditorGUILayout.LabelField("Clamp Angle");
                    BezierInspector("Angle", angle, 0.0f, 180.0f);
                    //EditorGUILayout.LabelField("Stiffness");
                    //BezierInspector("Stiffness", stiffness, 0.0f, 1.0f);
                    EditorGUILayout.Slider(influence, 0.0f, 1.0f, "Velocity Influence");

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool CollisionInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("useCollision");
            var friction = cparam.FindPropertyRelative("friction");
            var keepShape = cparam.FindPropertyRelative("keepInitialShape");
            //var dampingRate = cparam.FindPropertyRelative("frictionDampingRate");
            //var useEdgeCollision = cparam.FindPropertyRelative("useEdgeCollision");
            //var edgeRadius = cparam.FindPropertyRelative("edgeCollisionRadius");

            const string title = "Collider Collision";
            Foldout(title, title,
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);

                    keepShape.boolValue = EditorGUILayout.Toggle("Keep Shape", keepShape.boolValue);
                    //useEdgeCollision.boolValue = EditorGUILayout.Toggle("Edge Collision", useEdgeCollision.boolValue);
                    //EditorGUILayout.Slider(edgeRadius, 0.0f, 0.1f, "Edge Radius");
                    EditorGUILayout.Slider(friction, 0.0f, 1.0f, "Friction");
                    //EditorGUILayout.Slider(dampingRate, 0.0f, 0.95f, "Friction Damping Rate");

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool PenetrationInspector(SerializedObject team, SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("usePenetration");
            var mode = cparam.FindPropertyRelative("penetrationMode");
            var axis = cparam.FindPropertyRelative("penetrationAxis");
            var maxDepth = cparam.FindPropertyRelative("penetrationMaxDepth");
            var connectDistance = cparam.FindPropertyRelative("penetrationConnectDistance");
            //var stiffness = cparam.FindPropertyRelative("penetrationStiffness");
            var radius = cparam.FindPropertyRelative("penetrationRadius");
            var ignoreCollider = team.FindProperty("teamData.penetrationIgnoreColliderList");
            var distance = cparam.FindPropertyRelative("penetrationDistance");

            const string title = "Penetration";
            Foldout(title, title,
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);

                    EditorGUILayout.PropertyField(mode);

                    if (mode.enumValueIndex == (int)ClothParams.PenetrationMode.SurfacePenetration)
                    {
                        EditorGUILayout.Slider(maxDepth, 0.0f, 1.0f, "Max Connection Depth");
                        EditorGUILayout.PropertyField(axis);
                        EditorGUILayout.LabelField("Penetration Distance");
                        BezierInspector("Penetration Distance", distance, 0.0f, 1.0f);
                        EditorGUILayout.LabelField("Moving Radius");
                        BezierInspector("Moving Radius", radius, 0.0f, 5.0f);
                    }
                    else if (mode.enumValueIndex == (int)ClothParams.PenetrationMode.ColliderPenetration)
                    {
                        EditorGUILayout.Slider(maxDepth, 0.0f, 1.0f, "Max Connection Depth");
                        EditorGUILayout.LabelField("Connection Distance");
                        BezierInspector("Connection Distance", connectDistance, 0.0f, 1.0f);
                        //EditorGUILayout.LabelField("Stiffness");
                        //BezierInspector("Connection Stiffness", stiffness, 0.0f, 1.0f);
                        EditorGUILayout.LabelField("Penetration Distance");
                        BezierInspector("Penetration Distance", distance, 0.0f, 1.0f);
                        EditorGUILayout.LabelField("Moving Radius");
                        BezierInspector("Moving Radius", radius, 0.0f, 5.0f);
                        //EditorGUILayout.LabelField("Ignore Collider List");
                        EditorGUILayout.PropertyField(ignoreCollider, true);
                    }

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool BaseSkinningInspector(SerializedObject team, SerializedProperty cparam)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("useBaseSkinning");
            //var boneList = team.FindProperty("teamData.skinningBoneList");
            var ignoreCollider = team.FindProperty("teamData.skinningIgnoreColliderList");

            const string title = "Skinning";
            Foldout(title, title,
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);

                    //EditorGUILayout.PropertyField(boneList, true);
                    EditorGUILayout.PropertyField(ignoreCollider, true);

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool ClampDistanceInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("useClampDistanceRatio");
            var minRatio = cparam.FindPropertyRelative("clampDistanceMinRatio");
            var maxRatio = cparam.FindPropertyRelative("clampDistanceMaxRatio");
            var influence = cparam.FindPropertyRelative("clampDistanceVelocityInfluence");

            const string title = "Clamp Distance";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append("Clamp Distance", " [", minRatio.floatValue, "/", maxRatio.floatValue, "]");

            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);

                    EditorGUILayout.Slider(minRatio, 0.0f, 1.0f, "Min Distance Ratio");
                    EditorGUILayout.Slider(maxRatio, 1.0f, 2.0f, "Max Distance Ratio");
                    EditorGUILayout.Slider(influence, 0.0f, 1.0f, "Velocity Influence");

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool RestoreDistanceInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var influence = cparam.FindPropertyRelative("restoreDistanceVelocityInfluence");
            var structStiffness = cparam.FindPropertyRelative("structDistanceStiffness");
            var useBend = cparam.FindPropertyRelative("useBendDistance");
            var bendMaxCount = cparam.FindPropertyRelative("bendDistanceMaxCount");
            var bendStiffness = cparam.FindPropertyRelative("bendDistanceStiffness");
            var useNear = cparam.FindPropertyRelative("useNearDistance");
            var nearLength = cparam.FindPropertyRelative("nearDistanceLength");
            var nearStiffness = cparam.FindPropertyRelative("nearDistanceStiffness");
            var nearMaxCount = cparam.FindPropertyRelative("nearDistanceMaxCount");
            var nearMaxDepth = cparam.FindPropertyRelative("nearDistanceMaxDepth");

            const string title = "Restore Distance";
            Foldout(title, title,
                () =>
                {
                    EditorGUILayout.LabelField("Struct Point [Always ON]");

                    EditorGUILayout.LabelField("Struct Stiffness");
                    BezierInspector("Struct Stiffness", structStiffness, 0.0f, 1.0f);

                    useBend.boolValue = EditorGUILayout.Toggle("Bend Point", useBend.boolValue);
                    EditorGUILayout.IntSlider(bendMaxCount, 1, 6, "Bend Max Connection");
                    EditorGUILayout.LabelField("Bend Stiffness");
                    BezierInspector("Bend Stiffness", bendStiffness, 0.0f, 1.0f);

                    useNear.boolValue = EditorGUILayout.Toggle("Near Point", useNear.boolValue);
                    EditorGUILayout.IntSlider(nearMaxCount, 1, 6, "Near Max Connection");
                    EditorGUILayout.Slider(nearMaxDepth, 0.0f, 1.0f, "Near Max Depth");
                    EditorGUILayout.LabelField("Near Point Length");
                    BezierInspector("Near Point Length", nearLength, 0.0f, 0.5f);
                    EditorGUILayout.LabelField("Near Stiffness");
                    BezierInspector("Near Stiffness", nearStiffness, 0.0f, 1.0f);

                    EditorGUILayout.Slider(influence, 0.0f, 1.0f, "Velocity Influence");
                },
                warning: changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool FullSpringInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var springPower = cparam.FindPropertyRelative("springPower");
            var useSpring = cparam.FindPropertyRelative("useSpring");
            var springRadius = cparam.FindPropertyRelative("springRadius");
            var springScaleX = cparam.FindPropertyRelative("springScaleX");
            var springScaleY = cparam.FindPropertyRelative("springScaleY");
            var springScaleZ = cparam.FindPropertyRelative("springScaleZ");
            var springDirectionAtten = cparam.FindPropertyRelative("springDirectionAtten");
            var springDistanceAtten = cparam.FindPropertyRelative("springDistanceAtten");
            var springIntensity = cparam.FindPropertyRelative("springIntensity");

            const string title = "Spring";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", springPower.floatValue, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!useSpring.boolValue);
                    EditorGUILayout.Slider(springRadius, 0.01f, 0.5f, "Spring Radius");
                    EditorGUILayout.Slider(springScaleX, 0.01f, 1.0f, "Spring Radius Scale X");
                    EditorGUILayout.Slider(springScaleY, 0.01f, 1.0f, "Spring Radius Scale Y");
                    EditorGUILayout.Slider(springScaleZ, 0.01f, 1.0f, "Spring Radius Scale Z");
                    EditorGUILayout.Slider(springPower, 0.0f, 0.1f, "Spring Power");
                    EditorGUILayout.LabelField("Spring Direction Atten");
                    BezierInspector("Spring Direction Atten", springDirectionAtten, 0.0f, 1.0f);
                    EditorGUILayout.LabelField("Spring Distance Atten");
                    BezierInspector("Spring Distance Atten", springDistanceAtten, 0.0f, 1.0f);
                    EditorGUILayout.Slider(springIntensity, 0.1f, 3.0f, "Spring Atten Intensity");
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    useSpring.boolValue = sw;
                },
                useSpring.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool SimpleSpringInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var springPower = cparam.FindPropertyRelative("springPower");
            var useSpring = cparam.FindPropertyRelative("useSpring");

            const string title = "Spring";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", springPower.floatValue, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!useSpring.boolValue);
                    EditorGUILayout.Slider(springPower, 0.0f, 0.1f, "Spring Power");
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    useSpring.boolValue = sw;
                },
                useSpring.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool AdjustRotationInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            //var useAdjustRotation = cparam.FindPropertyRelative("useAdjustRotation");
            var adjustMode = cparam.FindPropertyRelative("adjustMode");
            var adjustRotationPower = cparam.FindPropertyRelative("adjustRotationPower");
            var enumName = adjustMode.enumDisplayNames[adjustMode.enumValueIndex];

            const string title = "Adjust Rotation";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", enumName, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUILayout.PropertyField(adjustMode);
                    EditorGUI.BeginDisabledGroup(adjustMode.enumValueIndex == 0);
                    EditorGUILayout.Slider(adjustRotationPower, -20.0f, 20.0f, "Adjust Rotation Power");
                    EditorGUI.EndDisabledGroup();
                },
                //(sw) =>
                //{
                //    useAdjustRotation.boolValue = sw;
                //},
                //useAdjustRotation.boolValue,
                warning: changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool ClampPositionInspector(SerializedProperty cparam, bool full, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var clampPositionLength = cparam.FindPropertyRelative("clampPositionLength");
            var useClampPositionLength = cparam.FindPropertyRelative("useClampPositionLength");
            var clampPositionRatioX = cparam.FindPropertyRelative("clampPositionRatioX");
            var clampPositionRatioY = cparam.FindPropertyRelative("clampPositionRatioY");
            var clampPositionRatioZ = cparam.FindPropertyRelative("clampPositionRatioZ");
            var influence = cparam.FindPropertyRelative("clampPositionVelocityInfluence");

            float sv, ev, cv;
            GetBezierValue(clampPositionLength, out sv, out ev, out cv);

            const string title = "Clamp Position";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", sv, "/", ev, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!useClampPositionLength.boolValue);
                    EditorGUILayout.LabelField("Clamp Position Length");
                    BezierInspector("Clamp Position Length", clampPositionLength, 0.0f, 1.0f);
                    if (full)
                    {
                        EditorGUILayout.Slider(clampPositionRatioX, 0.0f, 1.0f, "Clamp Position Ratio X");
                        EditorGUILayout.Slider(clampPositionRatioY, 0.0f, 1.0f, "Clamp Position Ratio Y");
                        EditorGUILayout.Slider(clampPositionRatioZ, 0.0f, 1.0f, "Clamp Position Ratio Z");
                    }
                    EditorGUILayout.Slider(influence, 0.0f, 1.0f, "Velocity Influence");
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    useClampPositionLength.boolValue = sw;
                },
                useClampPositionLength.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool VolumeInspector(SerializedProperty cparam)
        {
            EditorGUI.BeginChangeCheck();

            var useVolume = cparam.FindPropertyRelative("useVolume");
            var maxVolumeLength = cparam.FindPropertyRelative("maxVolumeLength");
            var volumeStretchStiffness = cparam.FindPropertyRelative("volumeStretchStiffness");
            var volumeShearStiffness = cparam.FindPropertyRelative("volumeShearStiffness");

            const string title = "Volume";
            Foldout(title, title,
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!useVolume.boolValue);

                    EditorGUILayout.Slider(maxVolumeLength, 0.0f, 0.5f, "Max Volume Length");
                    EditorGUILayout.LabelField("Stretch Stiffness");
                    BezierInspector("Stretch Stiffness", volumeStretchStiffness, 0.0f, 1.0f);
                    EditorGUILayout.LabelField("Shear Stiffness");
                    BezierInspector("Shear Stiffness", volumeShearStiffness, 0.0f, 1.0f);

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    useVolume.boolValue = sw;
                },
                useVolume.boolValue
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool RotationInterpolationInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var avarage = cparam.FindPropertyRelative("useLineAvarageRotation");
            var fixnonrot = cparam.FindPropertyRelative("useFixedNonRotation");

            const string title = "Rotation Interpolation";
            Foldout(title, title,
                () =>
                {
                    fixnonrot.boolValue = EditorGUILayout.Toggle("Fixed Non-Rotation", fixnonrot.boolValue);
                    avarage.boolValue = EditorGUILayout.Toggle("Line Avarage Rotation", avarage.boolValue);
                },
                warning: changed
                );

            return EditorGUI.EndChangeCheck();
        }

        //===============================================================================
        /// <summary>
        /// 水平線を引く
        /// </summary>
        public static void DrawHorizoneLine()
        {
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
        }

        /// <summary>
        /// インスペクタにオブジェクトリストと全選択ボタンを表示する
        /// </summary>
        /// <param name="dlist"></param>
        /// <param name="obj"></param>
        public static void DrawObjectList<T>(SerializedProperty dlist, GameObject obj, bool allselect, bool allclear)
            where T : UnityEngine.Object
        {
            // リスト表示
            EditorGUILayout.PropertyField(dlist, true);

            // 全選択/削除ボタン
            if (allselect || allclear)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (allselect)
                {
                    if (GUILayout.Button("All Select", GUILayout.Width(70), GUILayout.Height(16)))
                    {
                        var newlist = obj.transform.root.GetComponentsInChildren<T>();
                        int cnt = newlist == null ? 0 : newlist.Length;
                        dlist.arraySize = cnt;
                        for (int i = 0; i < cnt; i++)
                        {
                            dlist.GetArrayElementAtIndex(i).objectReferenceValue = newlist[i];
                        }
                    }
                }

                if (allclear)
                {
                    if (GUILayout.Button("Clear", GUILayout.Width(70), GUILayout.Height(16)))
                    {
                        dlist.arraySize = 0;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        //===============================================================================
        /// <summary>
        /// クロスモニターを開くボタン
        /// </summary>
        public static void MonitorButtonInspector()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Open Cloth Monitor", GUILayout.Width(150)))
            {
                ClothMonitorMenu.InitWindow();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

    }
}
