using UnityEngine;

/// <summary>
/// 던전 전반의 안개 및 대기 환경 설정을 관리하는 컴포넌트입니다.
/// </summary>
public class DungeonAtmosphereController : MonoBehaviour
{

    /// <summary>
    /// 설정된 대기 환경 데이터를 렌더링 세팅에 반영합니다.
    /// </summary>
    public void ApplyAtmosphere(DungeonData dungeonData)
    {
        if (dungeonData.DungeonAtmosphere == null)
        {
            Debug.LogWarning("[DungeonAtmosphereController] _atmosphereData가 할당되지 않았습니다.");
            return;
        }

        RenderSettings.fog = dungeonData.DungeonAtmosphere.UseFog;
        
        if (dungeonData.DungeonAtmosphere.UseFog == true)
        {
            RenderSettings.fogColor = dungeonData.DungeonAtmosphere.FogColor;
            RenderSettings.fogMode = dungeonData.DungeonAtmosphere.FogMode;
            RenderSettings.fogDensity = dungeonData.DungeonAtmosphere.FogDensity;
            RenderSettings.fogStartDistance = dungeonData.DungeonAtmosphere.FogStartDistance;
            RenderSettings.fogEndDistance = dungeonData.DungeonAtmosphere.FogEndDistance;
        }

        RenderSettings.ambientLight = dungeonData.DungeonAtmosphere.AmbientLight;

        Debug.Log($"[DungeonAtmosphereController] 던전 대기 환경 적용 완료: 안개 사용 여부 = {dungeonData.DungeonAtmosphere.UseFog}");
    }

}