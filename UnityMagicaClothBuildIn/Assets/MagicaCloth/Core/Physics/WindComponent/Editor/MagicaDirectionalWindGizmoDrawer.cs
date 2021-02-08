// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaDirectionalWindのギズモ表示
    /// </summary>
    public class MagicaDirectionalWindGizmoDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
        static void DrawGizmo(MagicaDirectionalWind scr, GizmoType gizmoType)
        {
            bool selected = (gizmoType & GizmoType.Selected) != 0 || (ClothMonitorMenu.Monitor != null && ClothMonitorMenu.Monitor.UI.AlwaysWindShow);

            if (selected == false)
                return;

            if (ClothMonitorMenu.Monitor == null || ClothMonitorMenu.Monitor.UI.DrawWind)
                DrawGizmo(scr, selected);
        }

        public static void DrawGizmo(MagicaDirectionalWind scr, bool selected)
        {
            // メイン方向
            Gizmos.color = GizmoUtility.ColorWind;
            var pos = scr.transform.position;
            var rot = scr.transform.rotation;
            GizmoUtility.DrawWireArrow(pos, rot, new Vector3(0.5f, 0.5f, 1.0f), true);

            // 実際の方向
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos, pos + scr.CurrentDirection * 0.5f);
        }
    }
}
