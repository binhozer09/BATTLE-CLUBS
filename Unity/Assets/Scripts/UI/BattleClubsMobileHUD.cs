using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BattleClubs.Player;
using BattleClubs.Gameplay;

namespace BattleClubs.UI
{
    /// <summary>Runtime Android HUD: virtual joystick, sprint, jump and bat attack.</summary>
    public sealed class BattleClubsMobileHUD : MonoBehaviour
    {
        [SerializeField] private ThirdPersonMotor motor;
        [SerializeField] private BatAttack batAttack;
        [SerializeField] private ReviveInteraction reviveInteraction;
        [SerializeField] private Vector2 joystickSize = new Vector2(190f, 190f);
        [SerializeField] private float joystickRange = 62f;

        private void Awake()
        {
            if (motor == null) motor = FindObjectOfType<ThirdPersonMotor>();
            if (batAttack == null && motor != null) batAttack = motor.GetComponent<BatAttack>();
            if (reviveInteraction == null && motor != null) reviveInteraction = motor.GetComponent<ReviveInteraction>();
            BuildHud();
        }

        private void OnDestroy()
        {
            if (motor != null) motor.SetMoveInput(Vector2.zero);
            if (motor != null) motor.SetSprint(false);
        }

        private void BuildHud()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("BattleClubs_HUD_Canvas",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("BattleClubs_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            RectTransform joystick = CreatePanel(canvas.transform, "VirtualJoystick",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.12f, 0.16f),
                joystickSize, new Color(0.08f, 0.08f, 0.08f, 0.48f));
            Image joystickImage = joystick.GetComponent<Image>();
            joystickImage.raycastTarget = true;

            RectTransform handle = CreatePanel(joystick, "JoystickHandle",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(76f, 76f), new Color(0.9f, 0.9f, 0.9f, 0.8f));
            handle.anchoredPosition = Vector2.zero;
            VirtualJoystick stick = joystick.gameObject.AddComponent<VirtualJoystick>();
            stick.Initialize(motor, handle, joystickRange);

            CreateActionButton(canvas.transform, "CORRER", new Vector2(1f, 0f),
                new Vector2(-300f, 120f), new Vector2(142f, 82f), () => { }, true);
            CreateActionButton(canvas.transform, "PULAR", new Vector2(1f, 0f),
                new Vector2(-145f, 250f), new Vector2(126f, 82f),
                () => { if (motor != null) motor.QueueJump(); }, false);
            CreateActionButton(canvas.transform, "ATACAR", new Vector2(1f, 0f),
                new Vector2(-145f, 110f), new Vector2(142f, 92f),
                () => { if (batAttack != null) batAttack.Attack(); }, false);
            RectTransform reviveRect = CreatePanel(canvas.transform, "Revive_Button",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(150f, 82f), new Color(0.08f, 0.32f, 0.18f, 0.82f));
            reviveRect.anchoredPosition = new Vector2(-310f, 250f);
            Button reviveButton = reviveRect.gameObject.AddComponent<Button>();
            reviveButton.targetGraphic = reviveRect.GetComponent<Image>();
            Text reviveLabel = CreateButtonLabel(reviveRect, "SEGURE PARA REVIVER");
            HoldRevive reviveHold = reviveRect.gameObject.AddComponent<HoldRevive>();
            reviveHold.Initialize(reviveInteraction, reviveLabel);
        }

        private static RectTransform CreatePanel(Transform parent, string objectName,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Color color)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = color;
            return rect;
        }


        private static Text CreateButtonLabel(RectTransform parent, string caption)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text label = labelObject.GetComponent<Text>();
            label.text = caption;
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 20;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static void CreateActionButton(Transform parent, string caption, Vector2 anchor,
            Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, bool hold)
        {
            RectTransform rect = CreatePanel(parent, caption + "_Button", anchor, anchor,
                new Vector2(0.5f, 0.5f), size, new Color(0.12f, 0.12f, 0.12f, 0.72f));
            rect.anchoredPosition = position;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            if (!hold) button.onClick.AddListener(action);

            CreateButtonLabel(rect, caption);

            if (hold)
            {
                HoldSprint sprint = rect.gameObject.AddComponent<HoldSprint>();
                sprint.Initialize(FindObjectOfType<ThirdPersonMotor>());
            }
        }
    }

    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private ThirdPersonMotor motor;
        private RectTransform handle;
        private float range;

        public void Initialize(ThirdPersonMotor target, RectTransform knob, float movementRange)
        {
            motor = target;
            handle = knob;
            range = Mathf.Max(1f, movementRange);
        }

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            if (handle == null) return;
            RectTransform rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            Vector2 input = Vector2.ClampMagnitude(local / range, 1f);
            handle.anchoredPosition = input * range;
            if (motor != null) motor.SetMoveInput(input);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            if (motor != null) motor.SetMoveInput(Vector2.zero);
        }
    }

    public sealed class HoldSprint : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private ThirdPersonMotor motor;
        public void Initialize(ThirdPersonMotor target) => motor = target;
        public void OnPointerDown(PointerEventData eventData) { if (motor != null) motor.SetSprint(true); }
        public void OnPointerUp(PointerEventData eventData) { if (motor != null) motor.SetSprint(false); }
        public void OnPointerExit(PointerEventData eventData) { if (motor != null) motor.SetSprint(false); }
    }
}
namespace BattleClubs.UI
{
    /// <summary>Hold interaction that cancels the local revive when touch is released.</summary>
    public sealed class HoldRevive : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private ReviveInteraction revive;
        private Text label;
        private const string IdleCaption = "SEGURE PARA REVIVER";

        public void Initialize(ReviveInteraction target, Text targetLabel)
        {
            revive = target;
            label = targetLabel;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (revive != null && revive.BeginRevive())
                UpdateCaption();
        }

        private void Update()
        {
            if (revive != null && label != null && revive.IsReviving)
                UpdateCaption();
        }

        private void UpdateCaption()
        {
            if (label != null)
                label.text = "REVIVENDO " + Mathf.RoundToInt(revive.Progress * 100f) + "%";
        }

        public void OnPointerUp(PointerEventData eventData) => StopRevive();
        public void OnPointerExit(PointerEventData eventData) => StopRevive();
        private void OnDisable() => StopRevive();

        private void StopRevive()
        {
            if (revive != null) revive.CancelRevive();
            if (label != null) label.text = IdleCaption;
        }
    }
}
