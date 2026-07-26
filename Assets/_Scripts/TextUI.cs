using TMPro;
using UnityEngine;

/// <summary>
/// 캐릭터 머리 위에 표시되는 대화 UI.
/// World Space Canvas를 사용하며 카메라 방향을 바라보도록 회전한다.
/// </summary>

public class TextUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _text;

    [SerializeField]
    private Canvas _canvas;

    private Transform _camera;


    private void Start() 
    {
        FindCamera();
    }

    private void FindCamera()
    {
        GameObject cameraObject = GameObject.FindGameObjectWithTag("MainCamera");

        if (cameraObject != null)
        {
            _camera = cameraObject.GetComponent<Camera>().transform;
        }
    }

    private void LateUpdate()
    {
        if(_camera == null)
        {
            FindCamera();
        }

        LookAtCamera();
    }

    /// <summary>
    /// 카메라 방향으로 회전
    /// </summary>
    private void LookAtCamera()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - _camera.position);
    }
}
