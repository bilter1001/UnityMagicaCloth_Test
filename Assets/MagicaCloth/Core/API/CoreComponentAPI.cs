// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp

using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// CoreComponent API
    /// </summary>
    public abstract partial class CoreComponent : MonoBehaviour, IShareDataObject, IDataVerify, IEditorMesh, IEditorCloth, IDataHash, IBoneReplace
    {
        /// <summary>
        /// コンポーネントのボーンを入れ替え再セットアップします。
        /// Swap component bones and set up again.
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        public void ReplaceComponentBone(Dictionary<Transform, Transform> boneReplaceDict)
        {
            ChangeAvatar(boneReplaceDict);
        }
    }
}
