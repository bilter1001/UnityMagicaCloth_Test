// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    public class WindController : MonoBehaviour
    {
        [SerializeField]
        private WindZone unityWindZone = null;
        [SerializeField]
        private float unityWindZoneScale = 0.1f;
        [SerializeField]
        private Renderer arrowRenderer = null;
        [SerializeField]
        private Gradient arrowGradient = new Gradient();

        private float angleY = 180.0f;
        private float angleX = 0.0f;

        void Start()
        {
        }



        public void OnDirectionY(float value)
        {
            angleY = value;
            UpdateDirection();
        }

        public void OnDirectionX(float value)
        {
            angleX = value;
            UpdateDirection();
        }

        public void OnMain(float value)
        {
            Wind.Main = value;

            // Link Unit Wind Zone
            if (unityWindZone)
            {
                unityWindZone.windMain = value * unityWindZoneScale;
            }

            // arrow color
            if (arrowRenderer)
            {
                var t = Mathf.InverseLerp(0.0f, 50.0f, value);
                var col = arrowGradient.Evaluate(t);
                arrowRenderer.material.color = col;
            }
        }

        public void OnTurbulence(float value)
        {
            Wind.Turbulence = value;
        }

        private MagicaDirectionalWind Wind
        {
            get
            {
                return GetComponent<MagicaDirectionalWind>();
            }
        }

        private void UpdateDirection()
        {
            transform.rotation = Quaternion.Euler(angleX, angleY, 0.0f);
        }
    }
}
