using UnityEngine;

[RequireComponent(typeof(LoadingSceneController))]
public class LodingScene : MonoBehaviour
{
    private void Start()
    {
        // 기존 씬에 LodingScene 오타 이름 컴포넌트가 이미 붙어 있을 수 있어 호환용으로 남겨둔다.
        // 새 작업에서는 LoadingSceneController를 직접 붙이는 쪽을 권장한다.
        LoadingSceneController loadingSceneController = GetComponent<LoadingSceneController>();
        if (loadingSceneController == null)
        {
            // RequireComponent가 보통 자동으로 붙여주지만, 누락된 경우 런타임에서 한 번 더 보정한다.
            loadingSceneController = gameObject.AddComponent<LoadingSceneController>();
        }

        // 실제 로딩 처리는 LoadingSceneController에 위임해 로직이 두 파일로 갈라지지 않게 한다.
        loadingSceneController.BeginLoad();
    }
}
