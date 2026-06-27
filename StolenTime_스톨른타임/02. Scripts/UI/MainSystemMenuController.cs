using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PrototypeRT
{
    /// <summary>
    /// 메인씬에서 시스템 메뉴만 제어하는 가벼운 컨트롤러입니다.
    /// 플레이씬의 UIManager는 인벤토리/특성/확인창까지 함께 관리하므로,
    /// 메인씬처럼 설정 UI만 필요한 곳에서는 이 스크립트로 의존성을 줄입니다.
    /// </summary>
    public class MainSystemMenuController : MonoBehaviour
    {
        public enum MainSystemPanel
        {
            KeySetting,
            AudioSetting,
            GameExit,
            Credit
        }

        [Header("창 열기/닫기")]
        [Tooltip("시스템 메뉴 전체 루트입니다. 이 오브젝트가 켜지고 꺼지면서 시스템 창 전체가 표시됩니다.")]
        [SerializeField] private GameObject windowRoot;

        [Tooltip("메인 화면에서 시스템 창을 여는 버튼입니다. 비워두면 Inspector OnClick에 OpenSystemWindow를 직접 연결해도 됩니다.")]
        [SerializeField] private Button openWindowButton;

        [Tooltip("시스템 창을 바로 닫는 버튼입니다. 닫기 버튼이 없다면 비워둬도 됩니다.")]
        [SerializeField] private Button closeWindowButton;

        [Tooltip("씬 시작 시 시스템 창을 바로 열어둘지 정합니다. 메인 메뉴 첫 화면에 항상 보일 UI라면 켜둡니다.")]
        [SerializeField] private bool openOnStart;

        [Tooltip("시스템 창을 닫거나 이전 패널로 돌아갈 키입니다. 메인씬에서는 ESC 고정 용도로 사용합니다.")]
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Header("기본 표시")]
        [Tooltip("시스템 창을 처음 열었을 때 오른쪽에 보여줄 기본 패널입니다.")]
        [SerializeField] private MainSystemPanel defaultPanel = MainSystemPanel.KeySetting;

        [Header("왼쪽 메뉴 버튼")]
        [Tooltip("키 설정 패널을 여는 버튼입니다. 비워두면 Inspector OnClick에 OpenKeySetting을 직접 연결해도 됩니다.")]
        [SerializeField] private Button keySettingButton;

        [Tooltip("오디오 설정 패널을 여는 버튼입니다. 비워두면 Inspector OnClick에 OpenAudioSetting을 직접 연결해도 됩니다.")]
        [SerializeField] private Button audioSettingButton;

        [Tooltip("게임 종료 확인 패널을 여는 버튼입니다. 비워두면 Inspector OnClick에 OpenGameExit를 직접 연결해도 됩니다.")]
        [SerializeField] private Button gameExitButton;

        [Tooltip("저작권/크레딧 패널을 여는 버튼입니다. 비워두면 Inspector OnClick에 OpenCredit을 직접 연결해도 됩니다.")]
        [SerializeField] private Button creditButton;

        [Header("오른쪽 표시 패널")]
        [Tooltip("키 설정 UI가 들어있는 오른쪽 패널입니다.")]
        [SerializeField] private GameObject keySettingPanel;

        [Tooltip("오디오 설정 UI가 들어있는 오른쪽 패널입니다.")]
        [SerializeField] private GameObject audioSettingPanel;

        [Tooltip("게임 종료 확인 UI가 들어있는 오른쪽 패널입니다.")]
        [SerializeField] private GameObject gameExitPanel;

        [Tooltip("저작권/크레딧 UI가 들어있는 오른쪽 패널입니다.")]
        [SerializeField] private GameObject creditPanel;

        [Header("게임 종료 확인 버튼")]
        [Tooltip("게임 종료를 최종 실행하는 버튼입니다. 확인창을 쓰지 않으면 비워둬도 됩니다.")]
        [SerializeField] private Button confirmExitButton;

        [Tooltip("게임 종료 확인창에서 취소할 버튼입니다. 누르면 직전에 보던 패널로 돌아갑니다.")]
        [SerializeField] private Button cancelExitButton;

        private readonly Stack<MainSystemPanel> _panelHistory = new Stack<MainSystemPanel>();

        private MainSystemPanel _currentPanel;
        private bool _hasCurrentPanel;

        private void Awake()
        {
            RegisterButtons();

            // 창이 닫혀 있어도 기본 패널 상태를 미리 맞춰두면,
            // 나중에 창을 열 때 빈 오른쪽 영역이 잠깐 보이는 일을 막을 수 있습니다.
            OpenPanel(defaultPanel, false);
            SetWindowVisible(openOnStart);
        }

        private void Update()
        {
            if (!IsWindowVisible())
                return;

            // 키 변경 UI가 입력을 기다리는 중이면 ESC는 "창 닫기"가 아니라 "키 변경 취소"로 써야 합니다.
            // 같은 프레임에 두 UI가 동시에 ESC를 먹으면 키 변경 취소와 창 닫기가 겹치므로 여기서는 무시합니다.
            if (KeyBindingManager.InputCaptureActive || KeyBindingManager.WasInputCapturedThisFrame())
                return;

            if (Input.GetKeyDown(closeKey))
                CloseSystemWindow();
        }

        private void OnDestroy()
        {
            UnregisterButtons();
        }

        /// <summary>시스템 창을 열고 기본 패널부터 다시 보여줍니다.</summary>
        public void OpenSystemWindow()
        {
            _panelHistory.Clear();
            OpenPanel(defaultPanel, false);
            SetWindowVisible(true);
        }

        /// <summary>시스템 창을 닫고, 이전 패널 기록을 초기화합니다.</summary>
        public void CloseSystemWindow()
        {
            _panelHistory.Clear();
            SetWindowVisible(false);
        }

        /// <summary>시스템 버튼 하나로 열기/닫기를 모두 처리하고 싶을 때 사용합니다.</summary>
        public void ToggleSystemWindow()
        {
            if (IsWindowVisible())
                CloseSystemWindow();
            else
                OpenSystemWindow();
        }

        public void OpenDefaultPanel()
        {
            OpenPanel(defaultPanel, true);
        }

        public void OpenKeySetting()
        {
            OpenPanel(MainSystemPanel.KeySetting, true);
        }

        public void OpenAudioSetting()
        {
            OpenPanel(MainSystemPanel.AudioSetting, true);
        }

        public void OpenGameExit()
        {
            // 종료 패널은 별도 캔버스라 시스템 창(windowRoot)을 켜지 않고 단독으로만 표시합니다.
            SetActive(gameExitPanel, true);
        }

        public void CloseGameExit()
        {
            SetActive(gameExitPanel, false);
        }

        public void OpenCredit()
        {
            OpenPanel(MainSystemPanel.Credit, true);
        }

        /// <summary>
        /// 마지막에 연 패널부터 하나씩 닫습니다.
        /// 더 돌아갈 패널이 없으면 시스템 창 전체를 닫습니다.
        /// </summary>
        public void CloseLastOpenedLayer()
        {
            if (!IsWindowVisible())
                return;

            if (_panelHistory.Count > 0)
            {
                OpenPanel(_panelHistory.Pop(), false);
                return;
            }

            CloseSystemWindow();
        }

        public void QuitGame()
        {
            // 에디터에서는 Application.Quit이 동작하지 않으므로 플레이 모드만 종료합니다.
            // 빌드에서는 실제 게임 종료로 이어집니다.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OpenPanel(MainSystemPanel panel, bool recordHistory)
        {
            if (!IsWindowVisible())
                SetWindowVisible(true);

            if (recordHistory && _hasCurrentPanel && _currentPanel != panel)
                _panelHistory.Push(_currentPanel);

            _currentPanel = panel;
            _hasCurrentPanel = true;

            // 오른쪽 영역은 한 번에 하나만 보여야 합니다.
            // 여러 패널이 동시에 켜지면 버튼 클릭 영역이나 스크롤 입력이 겹쳐 예상 못 한 입력이 발생할 수 있습니다.
            SetActive(keySettingPanel, panel == MainSystemPanel.KeySetting);
            SetActive(audioSettingPanel, panel == MainSystemPanel.AudioSetting);
            SetActive(gameExitPanel, panel == MainSystemPanel.GameExit);
            SetActive(creditPanel, panel == MainSystemPanel.Credit);
        }

        private bool IsWindowVisible()
        {
            return windowRoot == null || windowRoot.activeInHierarchy;
        }

        private void SetWindowVisible(bool visible)
        {
            SetActive(windowRoot, visible);
        }

        private void RegisterButtons()
        {
            RegisterButton(openWindowButton, OpenSystemWindow);
            RegisterButton(closeWindowButton, CloseSystemWindow);
            RegisterButton(keySettingButton, OpenKeySetting);
            RegisterButton(audioSettingButton, OpenAudioSetting);
            RegisterButton(gameExitButton, OpenGameExit);
            RegisterButton(creditButton, OpenCredit);
            RegisterButton(confirmExitButton, QuitGame);
            RegisterButton(cancelExitButton, CloseGameExit);
        }

        private void UnregisterButtons()
        {
            UnregisterButton(openWindowButton, OpenSystemWindow);
            UnregisterButton(closeWindowButton, CloseSystemWindow);
            UnregisterButton(keySettingButton, OpenKeySetting);
            UnregisterButton(audioSettingButton, OpenAudioSetting);
            UnregisterButton(gameExitButton, OpenGameExit);
            UnregisterButton(creditButton, OpenCredit);
            UnregisterButton(confirmExitButton, QuitGame);
            UnregisterButton(cancelExitButton, CloseGameExit);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        private static void RegisterButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.AddListener(action);
        }

        private static void UnregisterButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
        }
    }
}
