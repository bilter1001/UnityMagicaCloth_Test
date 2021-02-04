// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    public class CopyObject : MonoBehaviour
    {
        public int count = 1;
        public float radius = 5;
        public GameObject prefab;

        private void Awake()
        {
        }

        void Start()
        {
            CreateObject();
        }

        void CreateObject()
        {
            Random.InitState(0);
            for (int i = 0; i < count; i++)
            {
                var obj = GameObject.Instantiate(prefab);
                var lpos = Random.insideUnitCircle * radius;
                obj.transform.position = transform.position + new Vector3(lpos.x, 0.0f, lpos.y);
            }
        }
    }
}
