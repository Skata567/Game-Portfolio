using UnityEngine;

public class ShopUIManager : MonoBehaviour
{
    [SerializeField] private GameObject MonsterShopPanel;
    [SerializeField] private GameObject RuneShopPanel;
    [SerializeField] private GameObject GoldExchangeShopPanel;

    [SerializeField] private GameObject GoldexchangeCostPanel;
    [SerializeField] private GameObject ExchangeCheckPanel;



    public void OnClickedMshop()
    {
        SetActivePanel(MonsterShopPanel);
    }
    public void OnClickedRshop()
    {
        SetActivePanel(RuneShopPanel);
    }

    public void OnClickedGshop()
    {
        SetActivePanel(GoldExchangeShopPanel);
    }

    public void OnClickedGexchangeCostPanel()
    {
        GoldExchangeShopPanel.SetActive(true);
    }
    public void OnClickedEcheck()
    {
        ExchangeCheckPanel.SetActive(true);
    }
    public void OffClickedGexchangeCostPanel()
    {
        GoldexchangeCostPanel.SetActive(false);
    }
    public void OffClickedEcheck()
    {
        ExchangeCheckPanel.SetActive(false);
    }

    private void SetActivePanel(GameObject targetPanel)
    {
        RuneShopPanel.SetActive(false);
        MonsterShopPanel.SetActive(false);
        GoldExchangeShopPanel.SetActive(false);

        targetPanel.SetActive(true);
    }
}
