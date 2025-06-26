using UnityEngine;
using UnityStandardAssets.Water;

[ExecuteInEditMode]
[RequireComponent(typeof(Water))]
public class WaterFix : MonoBehaviour
{
    private Water water;
    private Camera currentCamera;

    void OnEnable()
    {
        water = GetComponent<Water>();
        Camera.onPreCull += OnCameraPreCull;
    }

    void OnDisable()
    {
        Camera.onPreCull -= OnCameraPreCull;
    }

    void OnCameraPreCull(Camera cam)
    {
        if (!water.enabled || !water.GetComponent<Renderer>().enabled)
            return;

        // Добавляем проверки видимости воды
        if (!IsWaterVisible(cam))
            return;

        // Проверяем размеры камеры
        if (cam.pixelWidth < 32 || cam.pixelHeight < 32)
            return;

        currentCamera = cam;

        // Вызываем обновление параметров воды перед рендерингом
        water.PreCullUpdate();
        water.RenderWaterEffects(cam);
    }

    private bool IsWaterVisible(Camera cam)
    {
        Renderer waterRenderer = water.GetComponent<Renderer>();
        if (waterRenderer == null)
            return false;

        // Проверяем, находится ли вода в frustum камеры
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        return GeometryUtility.TestPlanesAABB(planes, waterRenderer.bounds);
    }
}
