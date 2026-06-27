using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrototypeRT
{
    /// <summary>
    /// 키 설정 패널의 버튼, 현재 키 표시 텍스트, 자동 줍기 토글, 키 변경 안내창을 관리합니다.
    /// 실제 키 저장/중복 검사는 KeyBindingManager가 담당하고, 이 클래스는 UI 연결과 표시만 담당합니다.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class KeySettingUIManager : MonoBehaviour
    {
        /// <summary>
        /// 키 설정 한 줄에 필요한 참조 묶음입니다.
        /// 예: "상단 이동" 줄이면 action은 MoveUp, changeButton은 변경 키 버튼, keyText는 현재 키 표시 텍스트입니다.
        /// </summary>
        [Serializable]
        public class BindingRow
        {
            [Tooltip("이 줄에서 변경할 게임 입력 항목입니다. 예: MoveUp, Inventory, QuickSlot1")]
            public KeyBindingAction action;

            [Tooltip("누르면 '변경할 키를 눌러주세요' 상태로 들어가는 버튼입니다.")]
            public Button changeButton;

            [Tooltip("현재 배정된 키 이름을 보여줄 TextMeshPro 텍스트입니다.")]
            public TMP_Text keyText;
        }

        [Header("키 설정 줄")]
        [Tooltip("키 설정 UI의 각 줄을 순서대로 연결합니다. 이동/인벤/조사/줍기/퀵슬롯 줄마다 하나씩 넣으면 됩니다.")]
        [SerializeField] private BindingRow[] bindingRows;

        [Header("자동 줍기")]
        [Tooltip("자동 줍기를 ON/OFF로 바꾸는 버튼입니다.")]
        [SerializeField] private Button autoPickupToggleButton;
        [Tooltip("자동 줍기 상태를 ON 또는 OFF로 표시할 텍스트입니다.")]
        [SerializeField] private TMP_Text autoPickupStateText;

        [Header("키 변경 안내창")]
        [Tooltip("새 키 입력을 기다리는 동안 켜지는 검정 투명 안내 패널입니다.")]
        [SerializeField] private GameObject rebindPromptPanel;
        [Tooltip("'변경할 키를 눌러주세요' 같은 안내 문구를 표시할 텍스트입니다.")]
        [SerializeField] private TMP_Text rebindPromptText;
        [Tooltip("중복 키 또는 취소 메시지를 표시할 선택 텍스트입니다. 없어도 동작합니다.")]
        [SerializeField] private TMP_Text statusText;

        [Header("표시 문구")]
        [Tooltip("키 변경 버튼을 눌렀을 때 안내창에 표시할 문구입니다.")]
        [SerializeField] private string waitingMessage = "\ubcc0\uacbd\ud560 \ud0a4\ub97c \ub20c\ub7ec\uc8fc\uc138\uc694";
        [Tooltip("이미 다른 기능에 쓰는 키를 눌렀을 때 표시할 문구입니다.")]
        [SerializeField] private string duplicateMessage = "\uc774\ubbf8 \uc0ac\uc6a9 \uc911\uc778 \ud0a4\uc785\ub2c8\ub2e4";
        [Tooltip("중복 키 안내가 완전히 사라지는 데 걸리는 시간입니다. 너무 짧으면 사용자가 메시지를 못 볼 수 있습니다.")]
        [SerializeField] private float duplicateMessageFadeDuration = 0.5f;

        private static readonly KeyCode[] AllKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));

        private BindingRow _pendingRow;

        // 중복 키 안내가 사라지는 도중에 다른 안내가 들어오면 이전 페이드를 멈춰야 합니다.
        // 코루틴 참조를 저장해두면 취소/성공/패널 비활성화 같은 경로에서 안전하게 중단할 수 있습니다.
        private Coroutine _statusFadeCoroutine;

        /// <summary>현재 키 변경 입력을 기다리는 중인지 확인합니다.</summary>
        public bool IsRebinding => _pendingRow != null;

        private void Awake()
        {
            RegisterRows();
            RegisterAutoPickupToggle();
            SetPromptVisible(false);
            RefreshAll();
        }

        private void OnEnable()
        {
            KeyBindingManager.OnBindingChanged += OnBindingChanged;
            KeyBindingManager.OnAutoPickupChanged += OnAutoPickupChanged;
            RefreshAll();
        }

        private void OnDisable()
        {
            KeyBindingManager.OnBindingChanged -= OnBindingChanged;
            KeyBindingManager.OnAutoPickupChanged -= OnAutoPickupChanged;

            // 패널이 꺼지는 순간 코루틴이 중단되면 TMP 색상의 알파가 0 근처에 남을 수 있습니다.
            // 다음에 패널을 다시 열었을 때 메시지가 투명하게 보이지 않는 문제를 막기 위해 알파를 원복합니다.
            StopStatusFade();
            SetStatusAlpha(1f);

            if (IsRebinding)
                CancelRebind(false);
        }

        /// <summary>
        /// 키 변경 대기 중에는 ESC로 취소하거나, 키보드 키를 눌러 배정을 시도합니다.
        /// 중복 키라면 변경하지 않고 대기 상태를 유지합니다.
        /// </summary>
        private void Update()
        {
            if (!IsRebinding)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelRebind(true);
                return;
            }

            if (!TryGetPressedKeyboardKey(out KeyCode pressedKey))
                return;

            KeyBindingAction action = _pendingRow.action;
            if (!KeyBindingManager.TrySetKey(action, pressedKey))
            {
                // 중복 키는 치명적인 오류가 아니라 짧은 안내만 필요한 상황입니다.
                // 대기 상태는 유지하되, 안내 문구는 잠깐 보여준 뒤 자동으로 사라지게 처리합니다.
                SetStatusWithFade(duplicateMessage, duplicateMessageFadeDuration);
                KeyBindingManager.EndInputCapture();
                KeyBindingManager.BeginInputCapture();
                return;
            }

            SetStatus(string.Empty);
            CancelRebind(true);
        }

        /// <summary>Inspector에 연결된 각 변경 버튼에 클릭 이벤트를 연결합니다.</summary>
        private void RegisterRows()
        {
            if (bindingRows == null)
                return;

            foreach (BindingRow row in bindingRows)
            {
                if (row == null || row.changeButton == null)
                    continue;

                BindingRow capturedRow = row;
                row.changeButton.onClick.AddListener(() => BeginRebind(capturedRow));
            }
        }

        /// <summary>자동 줍기 버튼에 ON/OFF 토글 이벤트를 연결합니다.</summary>
        private void RegisterAutoPickupToggle()
        {
            if (autoPickupToggleButton == null)
                return;

            autoPickupToggleButton.onClick.AddListener(() =>
                KeyBindingManager.SetAutoPickupEnabled(!KeyBindingManager.AutoPickupEnabled));
        }

        /// <summary>특정 키 설정 줄의 키 변경 대기 상태를 시작합니다.</summary>
        private void BeginRebind(BindingRow row)
        {
            if (row == null)
                return;

            _pendingRow = row;
            KeyBindingManager.BeginInputCapture();
            SetPromptVisible(true);
            SetPrompt(waitingMessage);
            SetStatus(string.Empty);
        }

        /// <summary>키 변경 대기 상태를 끝내고 안내창을 닫습니다.</summary>
        private void CancelRebind(bool consumeInput)
        {
            _pendingRow = null;
            SetPromptVisible(false);

            if (consumeInput)
                KeyBindingManager.EndInputCapture();
            else if (KeyBindingManager.InputCaptureActive)
                KeyBindingManager.EndInputCapture();
        }

        /// <summary>모든 키 표시와 자동 줍기 표시를 현재 저장값으로 다시 그립니다.</summary>
        private void RefreshAll()
        {
            if (bindingRows != null)
            {
                foreach (BindingRow row in bindingRows)
                    RefreshRow(row);
            }

            RefreshAutoPickup();
        }

        /// <summary>한 줄의 현재 키 표시 텍스트를 갱신합니다.</summary>
        private void RefreshRow(BindingRow row)
        {
            if (row == null || row.keyText == null)
                return;

            row.keyText.text = GetDisplayName(KeyBindingManager.GetKey(row.action));
        }

        /// <summary>자동 줍기 ON/OFF 표시를 갱신합니다.</summary>
        private void RefreshAutoPickup()
        {
            if (autoPickupStateText != null)
                autoPickupStateText.text = KeyBindingManager.AutoPickupEnabled ? "ON" : "OFF";
        }

        /// <summary>KeyBindingManager에서 키 변경 이벤트가 오면 해당 줄만 갱신합니다.</summary>
        private void OnBindingChanged(KeyBindingAction action, KeyCode key)
        {
            if (bindingRows == null)
                return;

            foreach (BindingRow row in bindingRows)
            {
                if (row != null && row.action == action)
                    RefreshRow(row);
            }
        }

        /// <summary>자동 줍기 변경 이벤트가 오면 ON/OFF 표시를 갱신합니다.</summary>
        private void OnAutoPickupChanged(bool enabled)
        {
            RefreshAutoPickup();
        }

        /// <summary>
        /// 이번 프레임에 눌린 키보드 키를 찾습니다.
        /// ESC는 취소 처리용이라 여기서는 제외하고, 마우스/조이스틱도 v1에서는 제외합니다.
        /// </summary>
        private static bool TryGetPressedKeyboardKey(out KeyCode key)
        {
            for (int i = 0; i < AllKeyCodes.Length; i++)
            {
                KeyCode candidate = AllKeyCodes[i];
                if (candidate == KeyCode.None || candidate == KeyCode.Escape)
                    continue;

                string keyName = candidate.ToString();
                if (keyName.StartsWith("Mouse", StringComparison.Ordinal) ||
                    keyName.StartsWith("Joystick", StringComparison.Ordinal))
                    continue;

                if (Input.GetKeyDown(candidate))
                {
                    key = candidate;
                    return true;
                }
            }

            key = KeyCode.None;
            return false;
        }

        /// <summary>Inspector/UI에 보여줄 키 이름을 사람이 읽기 좋게 바꿉니다.</summary>
        private static string GetDisplayName(KeyCode key)
        {
            return key switch
            {
                KeyCode.Alpha0 => "0",
                KeyCode.Alpha1 => "1",
                KeyCode.Alpha2 => "2",
                KeyCode.Alpha3 => "3",
                KeyCode.Alpha4 => "4",
                KeyCode.Alpha5 => "5",
                KeyCode.Alpha6 => "6",
                KeyCode.Alpha7 => "7",
                KeyCode.Alpha8 => "8",
                KeyCode.Alpha9 => "9",
                KeyCode.Space => "Space",
                KeyCode.Tab => "Tab",
                _ => key.ToString()
            };
        }

        /// <summary>키 변경 안내 패널을 켜거나 끕니다.</summary>
        private void SetPromptVisible(bool visible)
        {
            if (rebindPromptPanel != null && rebindPromptPanel.activeSelf != visible)
                rebindPromptPanel.SetActive(visible);
        }

        /// <summary>키 변경 안내 문구를 설정합니다.</summary>
        private void SetPrompt(string message)
        {
            if (rebindPromptText != null)
                rebindPromptText.text = message;
        }

        /// <summary>
        /// 상태/오류 문구를 즉시 설정합니다.
        /// 다른 메시지가 들어온 경우 이전 페이드가 계속 알파를 건드리지 않도록 먼저 중단합니다.
        /// </summary>
        private void SetStatus(string message)
        {
            StopStatusFade();

            if (statusText == null)
                return;

            SetStatusAlpha(1f);
            statusText.text = message;
        }

        /// <summary>
        /// 중복 키 안내처럼 플레이어가 잠깐만 확인하면 되는 메시지를 표시한 뒤 서서히 숨깁니다.
        /// 취소 메시지처럼 유지되어야 하는 문구는 SetStatus를 직접 사용해야 합니다.
        /// </summary>
        private void SetStatusWithFade(string message, float fadeDuration)
        {
            SetStatus(message);

            if (statusText == null || string.IsNullOrEmpty(message))
                return;

            _statusFadeCoroutine = StartCoroutine(FadeStatusText(fadeDuration));
        }

        /// <summary>
        /// statusText의 알파를 1에서 0으로 줄여 메시지를 사라지게 합니다.
        /// Time.timeScale이 0이어도 키 설정 UI는 정상 동작해야 하므로 unscaledDeltaTime을 사용합니다.
        /// </summary>
        private IEnumerator FadeStatusText(float fadeDuration)
        {
            // 0초 이하 값이 들어와도 나누기 오류나 한 프레임 깜빡임이 생기지 않도록 최소 시간을 보장합니다.
            float duration = Mathf.Max(0.01f, fadeDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetStatusAlpha(Mathf.Lerp(1f, 0f, elapsed / duration));
                yield return null;
            }

            // 텍스트를 지운 뒤 알파를 다시 1로 돌려둡니다.
            // 이렇게 해두면 다음 메시지는 별도 초기화 없이 항상 정상 밝기로 표시됩니다.
            SetStatusAlpha(0f);
            statusText.text = string.Empty;
            SetStatusAlpha(1f);
            _statusFadeCoroutine = null;
        }

        /// <summary>
        /// 진행 중인 상태 메시지 페이드를 중단합니다.
        /// 새 메시지 표시, 패널 비활성화, 키 변경 성공/취소 시 이전 코루틴이 남지 않게 합니다.
        /// </summary>
        private void StopStatusFade()
        {
            if (_statusFadeCoroutine == null)
                return;

            StopCoroutine(_statusFadeCoroutine);
            _statusFadeCoroutine = null;
        }

        /// <summary>
        /// 상태 메시지 색상 중 투명도만 바꿉니다.
        /// Inspector에서 지정한 글자 색은 유지하고, 페이드에 필요한 알파 값만 조정합니다.
        /// </summary>
        private void SetStatusAlpha(float alpha)
        {
            if (statusText == null)
                return;

            Color color = statusText.color;
            color.a = alpha;
            statusText.color = color;
        }
    }
}
