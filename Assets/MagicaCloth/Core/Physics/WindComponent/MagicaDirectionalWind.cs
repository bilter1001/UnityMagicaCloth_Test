// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 方向性の風（これはワールド全体に影響を与える）
    /// </summary>
    [HelpURL("https://magicasoft.jp/directional-wind/")]
    [AddComponentMenu("MagicaCloth/MagicaDirectionalWind")]
    public partial class MagicaDirectionalWind : WindComponent
    {
        [SerializeField]
        [Range(0.0f, 50.0f)]
        private float main = 5.0f;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float turbulence = 1.0f;

        //=========================================================================================
        private float oldMain = 0;
        private float oldTurbulence = 0;

        //=========================================================================================
        //private void OnValidate()
        //{
        //    if (Application.isPlaying && windId >= 0)
        //    {
        //        MagicaPhysicsManager.Instance.Wind.SetParameter(windId, main, randomAngle);
        //    }
        //}

        protected override void CreateWind()
        {
            windId = MagicaPhysicsManager.Instance.Wind.CreateWind(PhysicsManagerWindData.WindType.Direction, main, turbulence);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (windId >= 0)
            {
                // パラメータ変更チェック
                bool change = false;
                if (main != oldMain)
                    change = true;
                if (turbulence != oldTurbulence)
                    change = true;

                if (change)
                {
                    oldMain = main;
                    oldTurbulence = turbulence;
                    MagicaPhysicsManager.Instance.Wind.SetParameter(windId, main, turbulence);
                }
            }
        }
    }
}
