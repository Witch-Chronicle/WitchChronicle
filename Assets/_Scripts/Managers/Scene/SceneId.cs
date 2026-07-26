/// <summary>
/// 씬 이름을 문자열 대신 타입 안전하게 참조하기 위한 enum.
/// * enum 값 이름이 실제 Build Settings에 등록된 씬 이름과 정확히 일치해야 함(대소문자 포함).
/// </summary>
public enum SceneId
{
    Main,
    //DungeonScene2,
    Dungeon,
    Battle
}