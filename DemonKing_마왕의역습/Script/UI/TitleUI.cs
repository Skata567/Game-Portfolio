using UnityEngine;

// 타이틀 화면의 UI를 관리하는 클래스
public class TitleUI : MonoBehaviour
{
    [SerializeField] private GameObject SettingPanel;       // 설정 패널
    [SerializeField] private GameObject GameExitPanel;      // 게임 종료 패널
    [SerializeField] private GameObject Bg;                 // 배경
    [SerializeField] private GameObject assetLicensePanel;
    void Update()
    {
        HandleEscapeKey();
    }

    // 설정 패널을 활성화
    public void OnSettingPanel()
    {
        SettingPanel.SetActive(true);
    }

    // 설정 패널을 비활성화
    public void OffSettingPanel()
    {
        SettingPanel.SetActive(false);
    }

    // 게임 종료 패널을 활성화 
    public void OnExitPanel()
    {
        Bg.SetActive(true);
        GameExitPanel.SetActive(true);
    }

    // 게임 종료 패널을 비활성화 
    public void OffExitPanel()
    {
        Bg.SetActive(false);
        GameExitPanel.SetActive(false);
    }

    public void OffAssetLicensePanel()
    {
        assetLicensePanel.SetActive(false);
    }

    public void OnAssetLicensePanel()
    {
        assetLicensePanel.SetActive(true);
    }

    private void HandleEscapeKey()
    {
        // 튜토리얼 모드거나 시작 패널이 보이면 ESC 무시
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SettingPanel.activeInHierarchy)
                OffSettingPanel();     
            else
                OnSettingPanel();
        }
    }

}
