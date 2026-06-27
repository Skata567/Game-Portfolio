using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 튜토리얼 하이라이트 시스템
/// 특정 버튼만 밝게 표시하고 나머지는 어둡게 만듦
/// </summary>
public class TutorialHighlight : MonoBehaviour
{
    #region Fields

    [Header("마스크 설정")]
    [SerializeField] private GameObject maskPanel;          // 전체 화면을 덮는 반투명 패널
    [SerializeField] private SpriteRenderer maskSprite;             // 마스크 이미지
    [SerializeField] private Color maskColor = new Color(0, 0, 0, 0.7f); // 마스크 색상 (검정 70% 투명도)

    [Header("하이라이트 설정")]
    [SerializeField] private Color highlightColor = Color.white; // 하이라이트 색상
    [SerializeField] private float highlightScale = 1.1f;   // 하이라이트 시 크기 배율
    [SerializeField] private bool enablePulseEffect = true; // 펄스 효과 활성화 여부
    [SerializeField] private float pulseSpeed = 2f;         // 펄스 속도
    [SerializeField] private float pulseAmount = 0.1f;      // 펄스 크기 변화량

    [Header("예외 버튼 설정")]
    [Tooltip("항상 활성화 상태로 유지할 버튼 (대화 NextButton 등)")]
    [SerializeField] private Button alwaysActiveButton;     // 항상 활성화될 버튼 (NextButton)

    // 현재 하이라이트된 버튼
    private Button currentHighlightedButton;
    private Vector3 originalScale;
    private int originalSortingOrder;
    private List<Button> highlightedButtons = new List<Button>(); // 여러 버튼 하이라이트 시 저장
    private Dictionary<Button, Vector3> originalScales = new Dictionary<Button, Vector3>(); // 여러 버튼의 원래 크기 저장
    private Coroutine pulseCoroutine; // 펄스 코루틴 참조

    // GameObject 하이라이트 관련
    private GameObject currentHighlightedGameObject;
    private Vector3 originalGameObjectScale;
    private Canvas originalGameObjectCanvas;
    private int originalGameObjectSortingOrder;

    

    private struct OriginalButtonState
    {
        public bool interactable;
        public Color color;
    }
    private Dictionary<Button, OriginalButtonState> disabledButtons = new Dictionary<Button, OriginalButtonState>();

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        // 시작 시 마스크 숨김
        if (maskPanel != null)
        {
            maskPanel.SetActive(false);

            // 마스크에 Canvas가 없으면 추가
            Canvas maskCanvas = maskPanel.GetComponent<Canvas>();
            if (maskCanvas == null)
            {
                maskCanvas = maskPanel.AddComponent<Canvas>();
                Debug.Log("[TutorialHighlight] 마스크에 Canvas 추가");
            }

            // Canvas 설정
            maskCanvas.overrideSorting = true;
            maskCanvas.sortingOrder = 1000; // 마스크는 1000

        }

        // 마스크 이미지 설정
        if (maskSprite == null && maskPanel != null)
        {
            maskSprite = maskPanel.GetComponent<SpriteRenderer>();
        }

        if (maskSprite != null)
        {
            maskSprite.color = maskColor;
            maskSprite.sortingOrder = 1000; // 클릭 차단
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 특정 버튼 하이라이트
    /// </summary>
    public void HighlightButton(string buttonName)
    {
        // 기존 하이라이트 해제
        ClearHighlight();

        // 버튼 찾기
        Button targetButton = FindButtonByName(buttonName);

        if (targetButton == null)
        {
            Debug.LogWarning($"[TutorialHighlight] 버튼을 찾을 수 없습니다: {buttonName}");
            return;
        }

        HighlightButton(targetButton);
    }

    /// <summary>
    /// 특정 버튼 하이라이트 (Button 직접 전달)
    /// </summary>
    public void HighlightButton(Button button)
    {
        if (button == null)
        {
            Debug.LogWarning("[TutorialHighlight] 버튼이 null입니다!");
            return;
        }

        Debug.Log($"[TutorialHighlight] HighlightButton 시작: {button.name}, interactable={button.interactable}");

        // 마스크 활성화
        if (maskPanel != null)
        {
            maskPanel.SetActive(true);
            Debug.Log("[TutorialHighlight] 마스크 활성화");
        }

        // 다른 모든 버튼 비활성화
        DisableAllButtonsExcept(button);
        Debug.Log($"[TutorialHighlight] DisableAllButtonsExcept 완료, {button.name} 제외");

        // 명시적으로 버튼 활성화 (DisableAllButtonsExcept는 건너뛰기만 함)
        button.interactable = true;
        Debug.Log($"[TutorialHighlight] {button.name}.interactable = true 설정 완료");

        // 버튼 강조
        currentHighlightedButton = button;
        originalScale = button.transform.localScale;

        // 크기 확대
        button.transform.localScale = originalScale * highlightScale;

        // 버튼에만 Canvas 추가 (부모는 건드리지 않음)
        Canvas buttonCanvas = button.GetComponent<Canvas>();
        if (buttonCanvas == null)
        {
            buttonCanvas = button.gameObject.AddComponent<Canvas>();
        }
        buttonCanvas.overrideSorting = true;
        originalSortingOrder = buttonCanvas.sortingOrder;
        buttonCanvas.sortingOrder = 10000;

        if (button.GetComponent<GraphicRaycaster>() == null)
        {
            button.gameObject.AddComponent<GraphicRaycaster>();
        }

        // 펄스 효과 시작
        if (enablePulseEffect)
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
            }
            pulseCoroutine = StartCoroutine(PulseSingleButton(button));
        }

        Debug.Log($"[TutorialHighlight] 버튼 하이라이트: {button.name}");
    }

    /// <summary>
    /// 여러 버튼을 동시에 하이라이트 (나머지는 비활성화)
    /// 전투씬에서 여러 부대 또는 스킬 버튼을 동시에 활성화할 때 사용
    /// </summary>
    public void HighlightMultipleButtons(params Button[] buttons)
    {
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning("[TutorialHighlight] 하이라이트할 버튼이 없습니다!");
            return;
        }

        // 마스크 활성화
        if (maskPanel != null)
        {
            maskPanel.SetActive(true);
        }

        // 다른 모든 버튼 비활성화
        DisableAllButtonsExceptMultiple(buttons);

        // 명시적으로 모든 하이라이트 버튼 활성화
        foreach (var btn in buttons)
        {
            if (btn != null) btn.interactable = true;
        }

        // 하이라이트된 버튼 리스트 초기화
        highlightedButtons.Clear();
        originalScales.Clear();

        // 각 버튼 강조 (크기 확대 및 최상위 레이어)
        foreach (var button in buttons)
        {
            if (button == null) continue;

            // 원래 크기 저장
            originalScales[button] = button.transform.localScale;

            // 크기 확대
            button.transform.localScale *= highlightScale;

            // 버튼을 최상위 레이어로 올리기
            Canvas buttonCanvas = button.GetComponent<Canvas>();
            if (buttonCanvas == null)
            {
                buttonCanvas = button.gameObject.AddComponent<Canvas>();
            }
            buttonCanvas.overrideSorting = true;
            buttonCanvas.sortingOrder = 1000;

            // GraphicRaycaster 추가
            if (button.GetComponent<GraphicRaycaster>() == null)
            {
                button.gameObject.AddComponent<GraphicRaycaster>();
            }

            // 리스트에 추가
            highlightedButtons.Add(button);

            Debug.Log($"[TutorialHighlight] 버튼 하이라이트: {button.name}");
        }

        // 펄스 효과 시작
        if (enablePulseEffect && highlightedButtons.Count > 0)
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
            }
            pulseCoroutine = StartCoroutine(PulseMultipleButtons());
        }
    }

    /// <summary>
    /// 버튼 이름 리스트로 여러 버튼 하이라이트
    /// </summary>
    public void HighlightButtonsByNames(params string[] buttonNames)
    {
        if (buttonNames == null || buttonNames.Length == 0)
        {
            Debug.LogWarning("[TutorialHighlight] 하이라이트할 버튼 이름이 없습니다!");
            return;
        }

        System.Collections.Generic.List<Button> foundButtons = new System.Collections.Generic.List<Button>();

        foreach (var buttonName in buttonNames)
        {
            Button btn = FindButtonByName(buttonName);
            if (btn != null)
            {
                foundButtons.Add(btn);
            }
            else
            {
                Debug.LogWarning($"[TutorialHighlight] 버튼을 찾을 수 없습니다: {buttonName}");
            }
        }

        if (foundButtons.Count > 0)
        {
            HighlightMultipleButtons(foundButtons.ToArray());
        }
    }


    /// <summary>
    /// GameObject 하이라이트 (GB_Node 같은 GameObject용)
    /// </summary>
    public void HighlightGameObject(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("[TutorialHighlight] GameObject가 null입니다!");
            return;
        }

        // 활성화 체크 추가
        if (!obj.activeInHierarchy)
        {
            Debug.LogWarning($"[TutorialHighlight] GameObject가 비활성화 상태입니다! (이름: {obj.name}, 씬: {obj.scene.name})");
            Debug.LogWarning("[TutorialHighlight] 비활성화된 오브젝트는 하이라이트할 수 없습니다!");
            return;
        }

        // 씬 체크 추가 (DontDestroyOnLoad 씬의 노드만 하이라이트)
        if (obj.scene.name != "DontDestroyOnLoad" && obj.scene.name != "MainScene")
        {
            Debug.LogWarning($"[TutorialHighlight] GameObject가 잘못된 씬에 있습니다! (씬: {obj.scene.name}, 이름: {obj.name})");
            Debug.LogWarning("[TutorialHighlight] DontDestroyOnLoad 또는 MainScene에 있는 오브젝트만 하이라이트 가능합니다!");
            return;
        }

        Debug.Log($"[TutorialHighlight] 하이라이트 대상 확인: {obj.name}, active={obj.activeInHierarchy}, 씬={obj.scene.name}, InstanceID={obj.GetInstanceID()}");

        // 기존 하이라이트 해제
        ClearHighlight();

        // 마스크 활성화
        if (maskPanel != null)
        {
            maskPanel.SetActive(true);
        }

        // GameObject 강조
        currentHighlightedGameObject = obj;
        originalGameObjectScale = obj.transform.localScale;

        // 크기 확대
        obj.transform.localScale = originalGameObjectScale * highlightScale;

        // GameObject를 최상위 레이어로 올리기 (마스크보다 위로)
        originalGameObjectCanvas = obj.GetComponent<Canvas>();
        Canvas objCanvas = originalGameObjectCanvas;

        if (objCanvas == null)
        {
            objCanvas = obj.AddComponent<Canvas>();
        }

        // 원래 sorting order 저장
        originalGameObjectSortingOrder = objCanvas.sortingOrder;

        objCanvas.overrideSorting = true;
        objCanvas.sortingOrder = 10001; // 마스크보다 훨씬 높게 설정

        // GraphicRaycaster 추가 (클릭 가능하도록)
        if (obj.GetComponent<GraphicRaycaster>() == null)
        {
            obj.AddComponent<GraphicRaycaster>();
        }

        // 자식 오브젝트의 Canvas도 같이 sorting order 올리기
        Canvas[] childCanvases = obj.GetComponentsInChildren<Canvas>();
        foreach (var childCanvas in childCanvases)
        {
            if (childCanvas != objCanvas)
            {
                if (!childCanvas.overrideSorting)
                {
                    childCanvas.overrideSorting = true;
                }
                childCanvas.sortingOrder = 10002; // 부모보다 더 높게
            }
        }

        // SpriteRenderer도 같이 sorting order 올리기 (GB_Node에 있을 경우)
        SpriteRenderer[] spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (var spriteRenderer in spriteRenderers)
        {
            spriteRenderer.sortingOrder = 10001;
            Debug.Log($"[TutorialHighlight] SpriteRenderer sorting order 설정: {spriteRenderer.name} = 10001");
        }

        // 모든 버튼 비활성화
        DisableAllButtons();

        // 펄스 효과 시작
        if (enablePulseEffect)
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
            }
            pulseCoroutine = StartCoroutine(PulseGameObject(obj));
        }

        StartCoroutine(VerifyAndFixSortingOrder(obj, objCanvas));

        Debug.Log($"[TutorialHighlight] GameObject 하이라이트: {obj.name}");
    }

    /// <summary>
    /// 마스크는 유지하고 하이라이트만 바꾸기 (전투씬 연속 하이라이트용)
    /// </summary>
    public void ChangeHighlightButton(Button newButton)
    {
        if (newButton == null)
        {
            Debug.LogWarning("[TutorialHighlight] 새 버튼이 null입니다!");
            return;
        }

        // 펄스 효과 중지
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        // 이전 단일 하이라이트 버튼 원래대로 (크기만)
        if (currentHighlightedButton != null)
        {
            currentHighlightedButton.transform.localScale = originalScale;
            Canvas buttonCanvas = currentHighlightedButton.GetComponent<Canvas>();
            if (buttonCanvas != null)
            {
                buttonCanvas.sortingOrder = originalSortingOrder;
                buttonCanvas.overrideSorting = false;
            }
        }

        // 여러 하이라이트 버튼들 원래대로 (크기만)
        foreach (var button in highlightedButtons)
        {
            if (button != null && originalScales.ContainsKey(button))
            {
                button.transform.localScale = originalScales[button];

                Canvas buttonCanvas = button.GetComponent<Canvas>();
                if (buttonCanvas != null)
                {
                    buttonCanvas.overrideSorting = false;
                }
            }
        }

        highlightedButtons.Clear();
        originalScales.Clear();

        // GameObject 하이라이트 해제 (크기만)
        if (currentHighlightedGameObject != null)
        {
            currentHighlightedGameObject.transform.localScale = originalGameObjectScale;

            Canvas objCanvas = currentHighlightedGameObject.GetComponent<Canvas>();
            if (objCanvas != null)
            {
                objCanvas.overrideSorting = false;
            }

            currentHighlightedGameObject = null;
            originalGameObjectCanvas = null;
        }

        // 마스크는 유지! (SetActive 하지 않음)

        // 기존 버튼 활성화
        EnableAllButtons();

        // 새로운 버튼 제외하고 다시 모든 버튼 비활성화
        DisableAllButtonsExcept(newButton);

        // 명시적으로 newButton 활성화 (안전장치)
        newButton.interactable = true;

        // 새 버튼 하이라이트
        currentHighlightedButton = newButton;
        originalScale = newButton.transform.localScale;

        // 크기 확대
        newButton.transform.localScale = originalScale * highlightScale;

        // 버튼을 최상위 레이어로 올리기
        Canvas newButtonCanvas = newButton.GetComponent<Canvas>();
        if (newButtonCanvas == null)
        {
            newButtonCanvas = newButton.gameObject.AddComponent<Canvas>();
        }
        newButtonCanvas.overrideSorting = true;
        originalSortingOrder = newButtonCanvas.sortingOrder;
        newButtonCanvas.sortingOrder = 1000;

        // GraphicRaycaster 추가 (클릭 가능하도록)
        if (newButton.GetComponent<GraphicRaycaster>() == null)
        {
            newButton.gameObject.AddComponent<GraphicRaycaster>();
        }

        // 펄스 효과 시작
        if (enablePulseEffect)
        {
            pulseCoroutine = StartCoroutine(PulseSingleButton(newButton));
        }

        Debug.Log($"[TutorialHighlight] 하이라이트 변경: {newButton.name} (마스크 유지)");
    }

    /// <summary>
    /// 하이라이트 해제
    /// </summary>
    public void ClearHighlight()
    {
        // 펄스 효과 중지
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        // 마스크 비활성화
        if (maskPanel != null)
        {
            maskPanel.SetActive(false);
        }

        // 단일 하이라이트된 버튼 원래대로
        if (currentHighlightedButton != null)
        {
            currentHighlightedButton.transform.localScale = originalScale;

            Canvas buttonCanvas = currentHighlightedButton.GetComponent<Canvas>();
            if (buttonCanvas != null)
            {
                buttonCanvas.sortingOrder = originalSortingOrder;
                buttonCanvas.overrideSorting = false;
            }

            currentHighlightedButton = null;
        }

        // 여러 하이라이트된 버튼들 원래대로
        foreach (var button in highlightedButtons)
        {
            if (button != null && originalScales.ContainsKey(button))
            {
                button.transform.localScale = originalScales[button];

                Canvas buttonCanvas = button.GetComponent<Canvas>();
                if (buttonCanvas != null)
                {
                    buttonCanvas.overrideSorting = false;
                }
            }
        }

        highlightedButtons.Clear();
        originalScales.Clear();

        // GameObject 하이라이트 해제
        if (currentHighlightedGameObject != null)
        {
            currentHighlightedGameObject.transform.localScale = originalGameObjectScale;

            // Canvas 복원
            /*            Canvas objCanvas = currentHighlightedGameObject.GetComponent<Canvas>();
                        GraphicRaycaster objGraphicRaycaster = currentHighlightedGameObject.GetComponent<GraphicRaycaster>();
                        if (objCanvas != null || objGraphicRaycaster != null)
                        {
                            // 원래 Canvas가 있었으면 원래대로 복원
                            if (originalGameObjectCanvas != null)
                            {              
                                objCanvas.sortingOrder = originalGameObjectSortingOrder;
                                objCanvas.overrideSorting = false;
                            }
                            else
                            {*//*
                                Destroy(objGraphicRaycaster);
                                // 원래 Canvas가 없었으면 제거*//*
                                Destroy(objCanvas);
                            }
                        }*/

            currentHighlightedGameObject = null;
            originalGameObjectCanvas = null;
        }

        // 모든 버튼 다시 활성화
        EnableAllButtons();

        Debug.Log("[TutorialHighlight] 하이라이트 해제");
    }

    /// <summary>
    /// 마스크만 켜기 (버튼 잠금 - 대화 중 사용)
    /// </summary>
    public void EnableMaskOnly()
    {
        if (maskPanel != null)
        {
            maskPanel.SetActive(true);
        }

        // 모든 버튼 비활성화
        DisableAllButtons();

        Debug.Log("[TutorialHighlight] 마스크만 활성화 (대화 중 버튼 잠금)");
    }

    /// <summary>
    /// 마스크만 끄기 (버튼 잠금 해제 - 대화 완료 후 사용)
    /// </summary>
    public void DisableMaskOnly()
    {
        if (maskPanel != null)
        {
            maskPanel.SetActive(false);
        }

        // 모든 버튼 다시 활성화
        EnableAllButtons();

        Debug.Log("[TutorialHighlight] 마스크 비활성화 (버튼 잠금 해제)");
    }

    /// <summary>
    /// GameObject의 sortingOrder만 복원 (노드 클릭 후 사용)
    /// </summary>
    public void RestoreNodeSortingOrder()
    {
        if (currentHighlightedGameObject == null)
        {
            Debug.LogWarning("[TutorialHighlight] 복원할 GameObject가 없습니다!");
            return;
        }

        // Canvas sortingOrder 복원
        Canvas objCanvas = currentHighlightedGameObject.GetComponent<Canvas>();
        if (objCanvas != null)
        {
            objCanvas.sortingOrder = originalGameObjectSortingOrder;
            objCanvas.overrideSorting = false;
            Debug.Log($"[TutorialHighlight] Canvas sortingOrder 복원: {originalGameObjectSortingOrder}");
        }

        // SpriteRenderer sortingOrder 복원
        SpriteRenderer[] spriteRenderers = currentHighlightedGameObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in spriteRenderers)
        {
            sr.sortingOrder = 0;  // 기본값으로 복원
            Debug.Log($"[TutorialHighlight] SpriteRenderer '{sr.name}' sortingOrder 복원: 0");
        }

        // 자식 Canvas들 sortingOrder 복원
        Canvas[] childCanvases = currentHighlightedGameObject.GetComponentsInChildren<Canvas>();
        foreach (var child in childCanvases)
        {
            if (child != objCanvas)
            {
                child.sortingOrder = 0;
                child.overrideSorting = false;
                Debug.Log($"[TutorialHighlight] 자식 Canvas '{child.name}' sortingOrder 복원: 0");
            }
        }

        Debug.Log("[TutorialHighlight] 노드 sortingOrder 복원 완료");
    }

    /// <summary>
    /// 마스크 색상 변경
    /// </summary>
    public void SetMaskColor(Color color)
    {
        maskColor = color;
        if (maskSprite != null)
        {
            maskSprite.color = maskColor;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 이름으로 버튼 찾기
    /// </summary>
    private Button FindButtonByName(string buttonName)
    {
        // 모든 버튼 찾기 (비활성화된 것도 포함)
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var button in allButtons)
        {
            if (button.name.Equals(buttonName, System.StringComparison.OrdinalIgnoreCase))
            {
                return button;
            }

            // 버튼 이름에 포함되어 있는지도 확인
            if (button.name.ToLower().Contains(buttonName.ToLower()))
            {
                return button;
            }
        }

        return null;
    }

    /// <summary>
    /// 특정 버튼을 제외한 모든 버튼 비활성화
    /// </summary>
    private void DisableAllButtonsExcept(Button exceptButton)
    {
        disabledButtons.Clear();

        // 모든 버튼 찾기
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var button in allButtons)
        {
            // 제외할 버튼들: exceptButton과 alwaysActiveButton
            if (button == exceptButton) continue;
            if (button == alwaysActiveButton) continue; // NextButton은 항상 활성화

            var graphic = button.GetComponent<Graphic>();

            // 원래 상태 저장 (색상 포함)
            disabledButtons[button] = new OriginalButtonState
            {
                interactable = button.interactable,
                color = (graphic != null) ? graphic.color : Color.white
            };

            // 비활성화
            button.interactable = false;

            // 시각적으로도 어둡게 (옵션)
            if (graphic != null)
            {
                graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, graphic.color.a * 0.5f);
            }
        }

        Debug.Log($"[TutorialHighlight] {disabledButtons.Count}개 버튼 비활성화 (제외: {exceptButton?.name}, alwaysActive: {alwaysActiveButton?.name})");
    }

    /// <summary>
    /// 여러 버튼을 제외한 모든 버튼 비활성화
    /// </summary>
    private void DisableAllButtonsExceptMultiple(Button[] exceptButtons)
    {
        disabledButtons.Clear();

        // 모든 버튼 찾기
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var button in allButtons)
        {
            // alwaysActiveButton은 항상 예외
            if (button == alwaysActiveButton) continue;

            // 제외 리스트에 포함된 버튼은 건너뛰기
            bool isException = false;
            foreach (var exceptBtn in exceptButtons)
            {
                if (button == exceptBtn)
                {
                    isException = true;
                    break;
                }
            }
            if (isException) continue;

            var graphic = button.GetComponent<Graphic>();

            // 원래 상태 저장
            disabledButtons[button] = new OriginalButtonState
            {
                interactable = button.interactable,
                color = (graphic != null) ? graphic.color : Color.white
            };

            // 비활성화
            button.interactable = false;

            // 시각적으로도 어둡게
            if (graphic != null)
            {
                graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, graphic.color.a * 0.5f);
            }
        }

        Debug.Log($"[TutorialHighlight] {disabledButtons.Count}개 버튼 비활성화 (제외: {exceptButtons.Length}개, alwaysActive: {alwaysActiveButton?.name})");
    }

    /// <summary>
    /// 모든 버튼 비활성화 (GameObject 하이라이트 시 사용)
    /// </summary>
    private void DisableAllButtons()
    {
        disabledButtons.Clear();

        // 모든 버튼 찾기
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var button in allButtons)
        {
            // alwaysActiveButton은 항상 예외
            if (button == alwaysActiveButton) continue;

            var graphic = button.GetComponent<Graphic>();

            // 원래 상태 저장
            disabledButtons[button] = new OriginalButtonState
            {
                interactable = button.interactable,
                color = (graphic != null) ? graphic.color : Color.white
            };

            // 비활성화
            button.interactable = false;

            // 시각적으로도 어둡게
            if (graphic != null)
            {
                graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, graphic.color.a * 0.5f);
            }
        }

        Debug.Log($"[TutorialHighlight] {disabledButtons.Count}개 버튼 비활성화 (alwaysActive: {alwaysActiveButton?.name})");
    }

    /// <summary>
    /// 모든 버튼 다시 활성화
    /// </summary>
    private void EnableAllButtons()
    {
        foreach (var kvp in disabledButtons)
        {
            var button = kvp.Key;
            var originalState = kvp.Value;

            if (button != null)
            {
                button.interactable = originalState.interactable;

                // 색상 원래대로 복원
                var graphic = button.GetComponent<Graphic>();
                if (graphic != null)
                {
                    graphic.color = originalState.color;
                }
            }
        }

        disabledButtons.Clear();

        Debug.Log("[TutorialHighlight] 모든 버튼 활성화");
    }



    /// <summary>
    /// 항상 활성화할 버튼 설정 (대화 NextButton 등)
    /// </summary>
    public void SetAlwaysActiveButton(Button button)
    {
        alwaysActiveButton = button;
        Debug.Log($"[TutorialHighlight] 항상 활성화 버튼 설정: {button?.name}");
    }

    #endregion

    #region Pulse Effect

    /// <summary>
    /// 단일 버튼 펄스 애니메이션
    /// </summary>
    private System.Collections.IEnumerator PulseSingleButton(Button button)
    {
        if (button == null) yield break;

        Vector3 baseScale = button.transform.localScale;

        while (true)
        {
            // 버튼이 파괴되었는지 체크 (씬 전환 시 발생 가능)
            if (button == null || button.transform == null)
            {
                Debug.LogWarning("[TutorialHighlight] 펄스 버튼이 파괴되어 코루틴 중지");
                yield break;
            }

            // 확대
            float elapsed = 0f;
            float duration = 1f / pulseSpeed;

            while (elapsed < duration)
            {
                // 매 프레임마다 버튼 유효성 체크
                if (button == null || button.transform == null)
                {
                    Debug.LogWarning("[TutorialHighlight] 펄스 버튼이 파괴되어 코루틴 중지");
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * pulseAmount;
                button.transform.localScale = baseScale * scale;
                yield return null;
            }

            // 원래 크기로 돌아감
            if (button != null && button.transform != null)
            {
                button.transform.localScale = baseScale;
            }
        }
    }

    /// <summary>
    /// 여러 버튼 펄스 애니메이션
    /// </summary>
    private System.Collections.IEnumerator PulseMultipleButtons()
    {
        if (highlightedButtons.Count == 0) yield break;

        while (true)
        {
            // 확대
            float elapsed = 0f;
            float duration = 1f / pulseSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * pulseAmount;

                foreach (var button in highlightedButtons)
                {
                    if (button != null && originalScales.ContainsKey(button))
                    {
                        button.transform.localScale = originalScales[button] * scale;
                    }
                }

                yield return null;
            }

            // 원래 크기로 돌아감
            foreach (var button in highlightedButtons)
            {
                if (button != null && originalScales.ContainsKey(button))
                {
                    button.transform.localScale = originalScales[button];
                }
            }
        }
    }

    /// <summary>
    /// GameObject 펄스 애니메이션
    /// </summary>
    private System.Collections.IEnumerator PulseGameObject(GameObject obj)
    {
        if (obj == null) yield break;

        Vector3 baseScale = obj.transform.localScale;

        while (true)
        {
            // 확대
            float elapsed = 0f;
            float duration = 1f / pulseSpeed;

            while (elapsed < duration)
            {
                if (obj == null) yield break; // 오브젝트가 파괴되었는지 체크

                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * pulseAmount;
                obj.transform.localScale = baseScale * scale;
                yield return null;
            }

            // 원래 크기로 돌아감
            if (obj != null)
            {
                obj.transform.localScale = baseScale;
            }
        }
    }

    private System.Collections.IEnumerator VerifyAndFixSortingOrder(GameObject obj, Canvas objCanvas)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            yield return new WaitForSeconds(0.1f);

            bool needsFix = false;

            // 10번 시도 후에도 실패
            Debug.Log($"[TutorialHighlight] 튜토리얼 노드 찾는중 시도 {attempt}번째");

            // objCanvas sortingOrder 체크 및 수정
            if (objCanvas.sortingOrder != 10001)
            {
                objCanvas.sortingOrder = 10001;
                needsFix = true;
            }

            // SpriteRenderer들 체크 및 수정
            SpriteRenderer[] spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in spriteRenderers)
            {
                if (sr.sortingOrder != 10001)
                {
                    sr.sortingOrder = 10001;
                    needsFix = true;
                }
            }

            // 자식 Canvas들 체크 및 수정
            Canvas[] childCanvases = obj.GetComponentsInChildren<Canvas>();
            foreach (var child in childCanvases)
            {
                if (child != objCanvas && child.sortingOrder != 10002)
                {
                    child.sortingOrder = 10002;
                    needsFix = true;
                }
            }

            // 모두 정상이면 성공 로그 후 종료
            if (!needsFix)
            {
                Debug.Log("[TutorialHighlight] 노드 하이라이트 sorting order 검증 완료");
                yield break;
            }
        }

        Debug.LogWarning("[TutorialHighlight] 노드 하이라이트 sorting order 설정 실패");
    }

    #endregion

    #region Auto-Setup (옵션)

    void Awake()
    {
        // maskPanel이 없으면 자동 생성
        if (maskPanel == null)
        {
            CreateMaskPanel();
        }
    }

    /// <summary>
    /// 마스크 패널 자동 생성
    /// </summary>
    private void CreateMaskPanel()
    {
        // Canvas 찾기
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogWarning("[TutorialHighlight] Canvas를 찾을 수 없어 마스크 패널을 생성할 수 없습니다!");
            return;
        }

        // 마스크 패널 생성
        GameObject maskObj = new GameObject("TutorialMask");
        maskObj.transform.SetParent(canvas.transform, false);

        // RectTransform 설정 (전체 화면)
        RectTransform rectTransform = maskObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        // Canvas 추가 (sorting order 제어를 위해)
        Canvas maskCanvas = maskObj.AddComponent<Canvas>();
        maskCanvas.overrideSorting = true;
        maskCanvas.sortingOrder = 1000; // 높은 값으로 설정

        // GraphicRaycaster 추가 (클릭 차단용)
        maskObj.AddComponent<GraphicRaycaster>();

        // Image 추가
        maskSprite = maskObj.AddComponent<SpriteRenderer>();
        maskSprite.color = maskColor;
        maskSprite.sortingOrder = 1000;

        // 최상위 레이어로 설정
        maskObj.transform.SetAsLastSibling();

        maskPanel = maskObj;
        maskPanel.SetActive(false);

        Debug.Log("[TutorialHighlight] 마스크 패널 자동 생성 완료 (Canvas sortingOrder=1000)");
    }

    #endregion
}
