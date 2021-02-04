// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// ボーン置換インターフェース
    /// </summary>
    public interface IBoneReplace
    {
        /// <summary>
        /// ボーンを置換する
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        void ReplaceBone(Dictionary<Transform, Transform> boneReplaceDict);
    }
}
