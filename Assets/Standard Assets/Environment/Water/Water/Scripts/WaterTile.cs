using System;
using UnityEngine;

namespace UnityStandardAssets.Water
{
    [ExecuteInEditMode]
    public class WaterTile : MonoBehaviour
    {
        public PlanarReflection reflection;
        public WaterBase waterBase;


        public void Start()
        {
            AcquireComponents();
        }


        void AcquireComponents()
        {
            if (!reflection)
            {
                if (transform.parent)
                {
                    reflection = transform.parent.GetComponent<PlanarReflection>();
                }
                else
                {
                    reflection = transform.GetComponent<PlanarReflection>();
                }
            }

            if (!waterBase)
            {
                if (transform.parent)
                {
                    waterBase = transform.parent.GetComponent<WaterBase>();
                }
                else
                {
                    waterBase = transform.GetComponent<WaterBase>();
                }
            }
        }


#if UNITY_EDITOR
        public void Update()
        {
            AcquireComponents();
        }
#endif


        public void OnWillRenderObject()
        {
            if (reflection)
            {
                // ���������� ����� - ������ ���������, ������� �� ������
                Camera cam = Camera.current;
                if (cam && cam.isActiveAndEnabled)
                {
                    reflection.WaterTileBeingRendered(transform, cam);
                }
            }
        }
    }
}