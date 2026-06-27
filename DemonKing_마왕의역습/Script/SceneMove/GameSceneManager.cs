using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSceneManager : MonoBehaviour
{

    private void Start()
    {
       
    }

    public void GoTitleScene()
    {
        LoadingSceneManager.LoadScene("TitleScene");
    }

    public void GoLoadingScene()
    {
        LoadingSceneManager.LoadScene("LoadingScene");
    }

    public void GameStart()
    {
        LoadingSceneManager.LoadScene("MainScene");
    }

    public void GameCradits()
    {
        LoadingSceneManager.LoadScene("CreditScene");
    }

    public void GameEnding()
    {
        LoadingSceneManager.LoadScene("GameEndingScene");
    }

    public void GameOver()
    {
        LoadingSceneManager.LoadScene("GameOverScene");
    }

    public void GameIntro()
    {
        LoadingSceneManager.LoadScene("IntroScene");
    }

    public void GameExit()
    {
       Application.Quit();

    }

    public void NYH()
    {
        LoadingSceneManager.LoadScene("NYH");
    }

    public void GoBattleScene()
    {

        if (TutorialManager.IsTutorialMode)
        {
            return;
        }

        var squadManager = SquadFormationManager.Instance;
        if (squadManager != null && !squadManager.ValidateForBattle())
        {
            return;
        }


        var mapSelectCon = FindFirstObjectByType<MapSelectionController>();
        mapSelectCon.EnterSelectedStage();

        if(StaticInfoManager.isElite)
        {
            LoadingSceneManager.LoadScene("ex_Battle_View");
        }
        else if(StaticInfoManager.isBoss)
        {
            LoadingSceneManager.LoadScene("ex_Battle_View");
        }
        else
        {
            LoadingSceneManager.LoadScene("ex_Battle_View");
        }
    }
}
