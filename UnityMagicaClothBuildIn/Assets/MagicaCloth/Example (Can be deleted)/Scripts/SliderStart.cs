// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using UnityEngine;
using UnityEngine.UI;

namespace MagicaCloth
{
    public class SliderStart : MonoBehaviour
    {
        [SerializeField]
        private Text text = null;

        [SerializeField]
        private string lable = "";

        //[SerializeField]
        //private string format = "0.00";

        void Start()
        {
            var slider = GetComponent<Slider>();
            if (slider)
            {
                slider.onValueChanged.AddListener(OnChangeValue);

                var val = slider.value;
                slider.value = 0.001f;
                slider.value = val;
            }
        }

        private void OnChangeValue(float value)
        {
            if (text)
            {
                text.text = string.Format("{0} ({1:0.0})", lable, value);
            }
        }
    }
}
