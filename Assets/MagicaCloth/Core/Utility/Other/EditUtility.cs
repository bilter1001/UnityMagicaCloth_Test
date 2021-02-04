// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// エディター関連ユーティリティ
    /// </summary>
    public static class EditUtility
    {
        /// <summary>
        /// メッシュの最適化状態をビットフラグで返す
        /// (Define.OptimizeMeshを参照)
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static int GetOptimizeMesh(Mesh mesh)
        {
            if (mesh == null)
                return 0;

            // アセットパス
            var path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path))
                return 0;

            int flag = 0;

            // モデルインポーターを取得
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer)
            {
#if UNITY_2018
                if (importer.optimizeMesh)
                    flag = Define.OptimizeMesh.Unity2018_On;
                else
                    flag = Define.OptimizeMesh.Nothing;
#else
                if (importer.optimizeMeshPolygons)
                    flag |= Define.OptimizeMesh.Unity2019_PolygonOrder;
                if (importer.optimizeMeshVertices)
                    flag |= Define.OptimizeMesh.Unity2019_VertexOrder;
                if (flag == 0)
                    flag = Define.OptimizeMesh.Nothing;
#endif
            }
            else
            {
                // インポーターが取得できない場合はビルドインのメッシュ
#if UNITY_2018
                flag = Define.OptimizeMesh.Unity2018_On;
#else
                flag |= Define.OptimizeMesh.Unity2019_PolygonOrder;
                flag |= Define.OptimizeMesh.Unity2019_VertexOrder;
#endif
            }

            return flag;
        }
    }
}
#endif
