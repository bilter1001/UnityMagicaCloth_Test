// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp

namespace MagicaCloth
{
    public static partial class Define
    {
        /// <summary>
        /// メッシュ最適化組み合わせ（ビットフラグ）
        /// </summary>
        public static class OptimizeMesh
        {
            public const int Unknown = 0x00000000;
            public const int Nothing = 0x00000001;

            public const int Unity2018_On = 0x00000010;

            public const int Unity2019_PolygonOrder = 0x00000100;
            public const int Unity2019_VertexOrder = 0x00000200;
        }
    }
}
