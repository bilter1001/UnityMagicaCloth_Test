// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaDirectionalWind API
    /// </summary>
    public partial class MagicaDirectionalWind : WindComponent
    {
        /// <summary>
        /// 風量
        /// Air flow.
        /// </summary>
        public float Main
        {
            get
            {
                return main;
            }
            set
            {
                main = Mathf.Clamp(value, 0.0f, 50.0f);
            }
        }

        /// <summary>
        /// 乱流率
        /// Turbulence rate.
        /// </summary>
        public float Turbulence
        {
            get
            {
                return turbulence;
            }
            set
            {
                turbulence = Mathf.Clamp01(value);
            }
        }

        /// <summary>
        /// 基準となる風向き
        /// Standard wind direction.
        /// </summary>
        public Vector3 MainDirection
        {
            get
            {
                return transform.forward;
            }
        }

        /// <summary>
        /// 実際の風向き
        /// Actual wind direction.
        /// </summary>
        public Vector3 CurrentDirection
        {
            get
            {
                if (windId >= 0)
                {
                    return MagicaPhysicsManager.Instance.Wind.windDataList[windId].direction;
                }
                else
                    return MainDirection;
            }
        }
    }
}
