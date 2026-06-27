using UnityEngine;

/// <summary>
/// 튜토리얼 페이지 ScriptableObject
/// 가이드 패널에 표시될 페이지 데이터
/// </summary>
[CreateAssetMenu(fileName = "New Page", menuName = "Tutorial/Page")]
public class TutorialPage : ScriptableObject
{
    public string pageTitle;        // 페이지 제목

    [TextArea]
    public string pageText1;        // 첫번째 내용 텍스트
    [TextArea]
    public string pageText2;        // 두번째 내용 텍스트

    public Sprite pageImage;        // 이미지

    public Sprite smallImage;       // 작은 이미지
}
