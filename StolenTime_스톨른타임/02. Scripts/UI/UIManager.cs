using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PrototypeRT
{
    // InventoryPanel 안의 큰 탭, 시스템 하위 탭, 확인창을 한 곳에서 전환한다.
    public class UIManager : MonoBehaviour
    {
        private const string InputBlockSource = "UIManager.Window";

        public enum MainTab
        {
            Inventory,
            TuckSung,
            System
        }

        public enum SystemTab
        {
            None,
            KeySetting,
            AudioSetting
        }

        [Header("Window Root")]
        [Tooltip("Tab으로 열고 닫는 전체 UI 창 루트. 예: InventoryPanel")]
        [SerializeField] private GameObject windowRoot;
        [Header("Main Buttons")]
        [Tooltip("인벤토리 탭을 여는 오른쪽 버튼.")]
        [SerializeField] private Button inventoryButton;
        [Tooltip("특성 탭을 여는 오른쪽 버튼.")]
        [SerializeField] private Button tuckSungButton;
        [Tooltip("시스템 탭을 여는 오른쪽 버튼.")]
        [SerializeField] private Button systemButton;
        [Tooltip("메인에서 시스템 창을 여는 버튼.")]
        [SerializeField] private Button mainToSystemButton;

        [Header("Main Panels")]
        [Tooltip("인벤토리 내용 패널. InventoryBT를 누르면 이것만 켜진다.")]
        [SerializeField] private GameObject inventoryPanel;
        [Tooltip("특성 내용 패널. TuckSungBT를 누르면 이것만 켜진다.")]
        [SerializeField] private GameObject tuckSungPanel;
        [Tooltip("시스템 내용 패널. SystemBT를 누르면 이것만 켜진다.")]
        [SerializeField] private GameObject systemPanel;

        [Header("System Buttons")]
        [Tooltip("키 설정 항목이 Button 컴포넌트일 때 연결.")]
        [SerializeField] private Button keySettingButton;
        [Tooltip("오디오 설정 항목이 Button 컴포넌트일 때 연결.")]
        [SerializeField] private Button audioSettingButton;
        [Tooltip("키 설정 항목이 Button이 아니라 Text/Image 오브젝트일 때 연결.")]
        [SerializeField] private GameObject keySettingClickTarget;
        [Tooltip("오디오 설정 항목이 Button이 아니라 Text/Image 오브젝트일 때 연결.")]
        [SerializeField] private GameObject audioSettingClickTarget;

        [Header("System Panels")]
        [Tooltip("키 설정 항목을 눌렀을 때 켜지는 패널.")]
        [SerializeField] private GameObject keySettingPanel;
        [Tooltip("오디오 설정 항목을 눌렀을 때 켜지는 패널.")]
        [SerializeField] private GameObject audioSettingPanel;

        [Header("Confirm Panel Open Buttons")]
        [Tooltip("게임 종료 확인창을 여는 버튼. Button 컴포넌트가 있으면 여기에 연결.")]
        [SerializeField] private Button gameExitOpenButton;
        [Tooltip("게임 종료 확인창을 여는 클릭 대상. Text/Image만 있을 때 연결.")]
        [SerializeField] private GameObject gameExitOpenClickTarget;
        [Tooltip("메인 화면 확인창을 여는 버튼. Button 컴포넌트가 있으면 여기에 연결.")]
        [SerializeField] private Button goToMainOpenButton;
        [Tooltip("메인 화면 확인창을 여는 클릭 대상. Text/Image만 있을 때 연결.")]
        [SerializeField] private GameObject goToMainOpenClickTarget;

        [Header("Confirm Panels")]
        [Tooltip("게임 종료 확인창 패널.")]
        [SerializeField] private GameObject gameExitPanel;
        [Tooltip("메인 화면 이동 확인창 패널.")]
        [SerializeField] private GameObject goToMainPanel;

        [Header("Confirm Cancel Buttons")]
        [Tooltip("게임 종료 확인창의 취소 버튼. 누르면 확인창만 닫힌다.")]
        [SerializeField] private Button gameExitCancelButton;
        [Tooltip("메인 화면 이동 확인창의 취소 버튼. 누르면 확인창만 닫힌다.")]
        [SerializeField] private Button goToMainCancelButton;

        [Header("Controllers")]
        [Tooltip("인벤토리 탭이 열렸을 때만 드래그/하이라이트 입력을 허용할 컨트롤러.")]
        [SerializeField] private InventoryController inventoryController;

        [Header("Defaults")]
        [Tooltip("전체 창이 처음 열리거나 Tab으로 다시 켜질 때 기본으로 보여줄 메인 탭.")]
        [SerializeField] private MainTab defaultMainTab = MainTab.Inventory;
        [Tooltip("시스템 탭에 처음 들어갔을 때 기본으로 보여줄 하위 탭.")]
        [SerializeField] private SystemTab defaultSystemTab = SystemTab.KeySetting;

        private MainTab _currentMainTab;
        private SystemTab _currentSystemTab;
        private bool _lastWindowActive;
        private GameObject _topModalPanel;

        private void Awake()
        {
            _currentMainTab = defaultMainTab;
            _currentSystemTab = defaultSystemTab;

            RegisterButton(inventoryButton, OpenInventory);
            RegisterButton(tuckSungButton, OpenTuckSung);
            RegisterButton(systemButton, OpenSystem);
            RegisterButton(mainToSystemButton, ToggleSystemWindow);
            RegisterButton(keySettingButton, OpenKeySetting);
            RegisterButton(audioSettingButton, OpenAudioSetting);
            RegisterButton(gameExitOpenButton, OpenGameExitPanel);
            RegisterButton(goToMainOpenButton, OpenGoToMainPanel);
            RegisterButton(gameExitCancelButton, CloseTopModalPanel);
            RegisterButton(goToMainCancelButton, CloseTopModalPanel);

            RegisterClickTarget(keySettingClickTarget, OpenKeySetting);
            RegisterClickTarget(audioSettingClickTarget, OpenAudioSetting);
            RegisterClickTarget(gameExitOpenClickTarget, OpenGameExitPanel);
            RegisterClickTarget(goToMainOpenClickTarget, OpenGoToMainPanel);
        }

        private void Start()
        {
            CloseAllModalPanels();
            OpenMainTab(defaultMainTab);
            _lastWindowActive = IsWindowActive();
            SyncInventoryInput();
            SyncUiInputBlock();
        }

        private void LateUpdate()
        {
            if (KeyBindingManager.InputCaptureActive || KeyBindingManager.WasInputCapturedThisFrame())
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!IsWindowActive())
                {
                    // 보스 인트로/사망 연출/키 입력 캡처처럼 게임 입력을 막는 연출 중에는
                    // ESC가 시스템창 열기로 새어 나가지 않게 한다.
                    // 이미 창이 열린 상태의 ESC 닫기는 아래 CloseTopUI 흐름에서 계속 허용한다.
                    if (KeyBindingManager.ShouldBlockGameplayInput)
                        return;

                    OpenSystemWindow();
                    return;
                }

                CloseTopUI();
            }

            bool isWindowActive = IsWindowActive();
            if (isWindowActive == _lastWindowActive) return;

            _lastWindowActive = isWindowActive;

            if (isWindowActive)
                OpenInventory();
            else
                CloseAllModalPanels();

            SyncInventoryInput();
            SyncUiInputBlock();
        }

        public void OpenInventory()
        {
            OpenMainTab(MainTab.Inventory);
        }

        public void OpenTuckSung()
        {
            OpenMainTab(MainTab.TuckSung);
        }

        public void OpenSystem()
        {
            OpenMainTab(MainTab.System);
        }

        public void OpenSystemWindow()
        {
            // 외부 버튼이나 코드가 직접 호출해도 입력 차단 중에는 새 시스템창을 열지 않는다.
            // 단, 이미 창이 열려 있는 경우에는 내부 탭 전환/정리 흐름을 막지 않기 위해
            // IsWindowActive()가 false일 때만 차단한다.
            if (!IsWindowActive() && KeyBindingManager.ShouldBlockGameplayInput)
                return;

            // 게임 진행 중 ESC키 또는 메인 시스템 버튼으로 들어올 때는 전체 창을 먼저 켠 뒤 시스템 탭을 바로 보여줍니다.
            // _lastWindowActive를 같이 맞춰두지 않으면 LateUpdate의 창 활성 변화 감지가 기본 인벤토리 탭으로 덮어쓸 수 있습니다.
            if (windowRoot != null && !windowRoot.activeSelf)
                windowRoot.SetActive(true);

            CloseAllModalPanels();
            ShowSystemTab();
            _lastWindowActive = IsWindowActive();
            SyncInventoryInput();
            SyncUiInputBlock();
        }

        public void ToggleSystemWindow()
        {
            // 메인 UI의 시스템 버튼도 ESC와 같은 정책을 따른다.
            // 보스 인트로 같은 컷신 중에는 새 UI를 열 수 없고,
            // 이미 열린 시스템창을 닫는 동작은 아래 기존 토글 로직으로 처리한다.
            if (!IsWindowActive() && KeyBindingManager.ShouldBlockGameplayInput)
                return;

            // 이미 시스템 탭이 열린 상태에서 시스템 버튼을 다시 누르면 창을 닫습니다.
            // 다른 탭이 열려 있거나 창이 닫혀 있으면 시스템 탭으로 전환합니다.
            if (IsWindowActive() && _currentMainTab == MainTab.System && _topModalPanel == null)
            {
                if (windowRoot != null)
                    windowRoot.SetActive(false);

                CloseAllModalPanels();
                _lastWindowActive = IsWindowActive();
                SyncInventoryInput();
                SyncUiInputBlock();
                return;
            }

            OpenSystemWindow();
        }

        private void ShowSystemTab()
        {
            OpenMainTab(MainTab.System);
        }

        public void OpenKeySetting()
        {
            OpenSystemTab(SystemTab.KeySetting);
        }

        public void OpenAudioSetting()
        {
            OpenSystemTab(SystemTab.AudioSetting);
        }

        public void OpenGameExitPanel()
        {
            OpenModalPanel(gameExitPanel);
        }

        public void OpenGoToMainPanel()
        {
            OpenModalPanel(goToMainPanel);
        }

        public void CloseTopModalPanel()
        {
            if (_topModalPanel != null && _topModalPanel.activeSelf)
            {
                _topModalPanel.SetActive(false);
                _topModalPanel = null;
                SyncInventoryInput();
                SyncUiInputBlock();
                return;
            }

            CloseAllModalPanels();
            SyncInventoryInput();
            SyncUiInputBlock();
        }

        public void OpenMainTab(MainTab tab)
        {
            _currentMainTab = tab;

            SetActive(inventoryPanel, tab == MainTab.Inventory);
            SetActive(tuckSungPanel, tab == MainTab.TuckSung);
            SetActive(systemPanel, tab == MainTab.System);

            if (tab == MainTab.System && _currentSystemTab == SystemTab.None)
                _currentSystemTab = defaultSystemTab;

            if (tab == MainTab.System)
                OpenSystemTab(_currentSystemTab);
            else
                HideSystemSubPanels();

            SyncInventoryInput();
            SyncUiInputBlock();
        }

        public void OpenSystemTab(SystemTab tab)
        {
            _currentSystemTab = tab;

            SetActive(keySettingPanel, tab == SystemTab.KeySetting);
            SetActive(audioSettingPanel, tab == SystemTab.AudioSetting);
        }

        private void OpenModalPanel(GameObject panel)
        {
            if (panel == null) return;

            if (!IsWindowActive() && windowRoot != null)
                windowRoot.SetActive(true);

            CloseAllModalPanels();
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            _topModalPanel = panel;
            SyncInventoryInput();
            SyncUiInputBlock();
        }

        private void CloseTopUI()
        {
            if (_topModalPanel != null && _topModalPanel.activeSelf)
            {
                CloseTopModalPanel();
                return;
            }

            if (IsWindowActive() && windowRoot != null)
            {
                windowRoot.SetActive(false);
                CloseAllModalPanels();
                SyncInventoryInput();
                SyncUiInputBlock();
            }
        }

        private void CloseAllModalPanels()
        {
            SetActive(gameExitPanel, false);
            SetActive(goToMainPanel, false);
            _topModalPanel = null;
        }

        private void SyncInventoryInput()
        {
            bool canUseInventory = IsWindowActive() && _currentMainTab == MainTab.Inventory && _topModalPanel == null;
            inventoryController?.SetInventoryOpen(canUseInventory);
        }

        private void SyncUiInputBlock()
        {
            // windowRoot가 실제로 켜진 동안은 인벤/특성/시스템/확인창 어디든 게임 입력을 막습니다.
            // 인벤토리 내부 드래그는 InventoryController가 따로 관리하므로 여기서는 플레이어/카메라 입력만 막는 게이트에 등록합니다.
            bool shouldBlock = windowRoot != null && windowRoot.activeInHierarchy;
            KeyBindingManager.SetUiInputBlock(InputBlockSource, shouldBlock);
        }

        private void OnDisable()
        {
            KeyBindingManager.SetUiInputBlock(InputBlockSource, false);
        }

        private void OnDestroy()
        {
            KeyBindingManager.SetUiInputBlock(InputBlockSource, false);
        }

        private bool IsWindowActive()
        {
            return windowRoot == null || windowRoot.activeInHierarchy;
        }

        private void HideSystemSubPanels()
        {
            SetActive(keySettingPanel, false);
            SetActive(audioSettingPanel, false);
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null && target.activeSelf != value)
                target.SetActive(value);
        }

        private static void RegisterButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(action);
        }

        private static void RegisterClickTarget(GameObject target, UnityEngine.Events.UnityAction action)
        {
            if (target == null) return;

            if (target.TryGetComponent(out Button button))
            {
                button.onClick.AddListener(action);
                return;
            }

            EventTrigger trigger = target.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = target.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };
            entry.callback.AddListener(_ => action.Invoke());
            trigger.triggers.Add(entry);
        }
    }
}
