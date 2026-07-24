using UnityEngine;

/// <summary>
/// 대화 이벤트 실행 관리
/// JSON 이벤트 ID를 실제 기능과 연결
/// </summary>
public class DialogueEventManager : MonoBehaviour
{
    public static DialogueEventManager Instance { get; private set; }


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);

            return;
        }


        Instance = this;
    }



    /// <summary>
    /// 대화 이벤트 실행
    /// 이벤트 ID에 따른 기능 호출
    /// </summary>
    public void Execute(string eventID)
    {
        Debug.Log($"Dialogue Event Execute : {eventID}");

        switch(eventID)
        {
            case "OpenShop":
                if(ShopNPC.Instance == null)
                {
                    Debug.LogError("ShopNPC Instance Missing");

                    return;
                }
                ShopNPC.Instance.ToggleShop();
                break;

            case "OpenEnhance":
                if(ShopNPC.Instance == null)
                {
                    Debug.LogError("EnhanceNPC Instance Missing");

                    return;
                }
                EnhanceNPC.Instance.ToggleEnhanceUI();
                break;

            //case "RecruitNPC":
            //    RecruitService.Instance.Recruit();
            //    break;
        }
    }
}