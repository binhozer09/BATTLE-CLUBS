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
        private static Sprite circleSprite;
        private static Sprite ringSprite;
        private static Sprite jumpIconSprite, sprintIconSprite, attackIconSprite;
        [SerializeField] private ThirdPersonMotor motor;
        [SerializeField] private BatAttack batAttack;
        [SerializeField] private ThirdPersonCameraRig cameraRig;
        [SerializeField] private Vector2 joystickSize = new Vector2(220f, 220f);
        [SerializeField] private float joystickRange = 76f;

        private void Awake()
        {
            if (motor == null) motor = FindObjectOfType<ThirdPersonMotor>();
            if (batAttack == null && motor != null) batAttack = motor.GetComponent<BatAttack>();
            if (cameraRig == null) cameraRig = FindObjectOfType<ThirdPersonCameraRig>();
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
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.14f, 0.18f),
                joystickSize, new Color(0.08f, 0.08f, 0.08f, 0.38f));
            Image joystickImage = joystick.GetComponent<Image>();
            joystickImage.sprite = GetCircleSprite();
            joystickImage.type = Image.Type.Simple;
            joystickImage.raycastTarget = true;
            AddOverlay(joystick, "Joystick_Rim", GetRingSprite(), new Vector2(0.96f, 0.96f), new Color(1f, 1f, 1f, 0.62f));

            RectTransform handle = CreatePanel(joystick, "JoystickHandle",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(88f, 88f), new Color(0.92f, 0.92f, 0.92f, 0.86f));
            handle.anchoredPosition = Vector2.zero;
            Image handleImage = handle.GetComponent<Image>();
            handleImage.sprite = GetCircleSprite();
            VirtualJoystick stick = joystick.gameObject.AddComponent<VirtualJoystick>();
            stick.Initialize(motor, handle, joystickRange);

            // Right-side touch look area. Action buttons are created afterwards so they stay on top.
            RectTransform lookArea = CreatePanel(canvas.transform, "TouchLookArea",
                new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Color(1f, 1f, 1f, 0.001f));
            TouchLookPad lookPad = lookArea.gameObject.AddComponent<TouchLookPad>();
            lookPad.Initialize(cameraRig);

            CreateActionButton(canvas.transform, "CORRER", new Vector2(1f, 0f),
                new Vector2(-330f, 118f), new Vector2(112f, 112f), () => { }, true);
            CreateActionButton(canvas.transform, "PULAR", new Vector2(1f, 0f),
                new Vector2(-150f, 265f), new Vector2(104f, 104f),
                () => { if (motor != null) motor.QueueJump(); }, false);
            CreateActionButton(canvas.transform, "ATACAR", new Vector2(1f, 0f),
                new Vector2(-150f, 125f), new Vector2(138f, 138f),
                () => { if (batAttack != null) batAttack.Attack(); }, false);
        }

        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null) return circleSprite;
            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.48f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius + 1f - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size);
            return circleSprite;
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

        private static void CreateActionButton(Transform parent, string caption, Vector2 anchor,
            Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, bool hold)
        {
            RectTransform rect = CreatePanel(parent, caption + "_Button", anchor, anchor,
                new Vector2(0.5f, 0.5f), size, new Color(0.055f, 0.065f, 0.075f, 0.78f));
            rect.anchoredPosition = position;
            Image buttonImage = rect.GetComponent<Image>();
            buttonImage.sprite = GetCircleSprite();
            AddOverlay(rect, "Outer_Ring", GetRingSprite(), Vector2.one, new Color(1f, 1f, 1f, 0.88f));
            AddOverlay(rect, "Inner_Ring", GetRingSprite(), new Vector2(0.82f, 0.82f), new Color(1f, 1f, 1f, 0.24f));

            Sprite icon = hold ? GetActionIcon("sprint") : (caption == "PULAR" ? GetActionIcon("jump") : GetActionIcon("attack"));
            AddOverlay(rect, caption + "_Icon", icon, new Vector2(0.48f, 0.48f), Color.white);

            GameObject labelObject = new GameObject("Caption", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(rect, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.05f, 0.04f);
            labelRect.anchorMax = new Vector2(0.95f, 0.27f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text label = labelObject.GetComponent<Text>();
            label.text = caption;
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 15;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(1f, 1f, 1f, 0.92f);
            label.raycastTarget = false;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            if (!hold) button.onClick.AddListener(action);
            if (hold)
            {
                HoldSprint sprint = rect.gameObject.AddComponent<HoldSprint>();
                sprint.Initialize(FindObjectOfType<ThirdPersonMotor>());
            }
        }

        private static void AddOverlay(RectTransform parent, string objectName, Sprite sprite, Vector2 scale, Color color)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.Scale(parent.sizeDelta, scale);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
        }

        private static Sprite GetRingSprite()
        {
            if (ringSprite != null) return ringSprite;
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.47f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1.5f - Mathf.Abs(d - radius)));
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            ringSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return ringSprite;
        }

        private static Sprite GetActionIcon(string kind)
        {
            if (kind == "jump" && jumpIconSprite != null) return jumpIconSprite;
            if (kind == "sprint" && sprintIconSprite != null) return sprintIconSprite;
            if (kind == "attack" && attackIconSprite != null) return attackIconSprite;
            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
            if (kind == "jump")
            {
                DrawIconLine(pixels, size, new Vector2(48, 18), new Vector2(48, 73), 7);
                DrawIconLine(pixels, size, new Vector2(25, 48), new Vector2(48, 73), 7);
                DrawIconLine(pixels, size, new Vector2(71, 48), new Vector2(48, 73), 7);
            }
            else if (kind == "sprint")
            {
                DrawIconLine(pixels, size, new Vector2(18, 63), new Vector2(43, 63), 6);
                DrawIconLine(pixels, size, new Vector2(28, 47), new Vector2(54, 47), 6);
                DrawIconLine(pixels, size, new Vector2(39, 31), new Vector2(65, 31), 6);
                DrawIconLine(pixels, size, new Vector2(52, 18), new Vector2(77, 48), 8);
                DrawIconLine(pixels, size, new Vector2(77, 48), new Vector2(48, 70), 8);
            }
            else
            {
                DrawIconLine(pixels, size, new Vector2(24, 70), new Vector2(65, 29), 7);
                DrawIconLine(pixels, size, new Vector2(61, 33), new Vector2(76, 18), 15);
                DrawIconLine(pixels, size, new Vector2(20, 75), new Vector2(31, 64), 10);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            if (kind == "jump") jumpIconSprite = sprite;
            else if (kind == "sprint") sprintIconSprite = sprite;
            else attackIconSprite = sprite;
            return sprite;
        }

        private static void DrawIconLine(Color[] pixels, int size, Vector2 a, Vector2 b, float width)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - width));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + width));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - width));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + width));
            Vector2 ab = b - a;
            float lengthSq = Mathf.Max(0.001f, ab.sqrMagnitude);
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
                    if (Vector2.Distance(p, a + ab * t) <= width * 0.5f) pixels[y * size + x] = Color.white;
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
    /// <summary>Drag-to-look input for the right side of the Android screen.</summary>
    public sealed class TouchLookPad : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private ThirdPersonCameraRig cameraRig;
        private Vector2 previousPosition;
        private bool tracking;

        public void Initialize(ThirdPersonCameraRig target) => cameraRig = target;

        public void OnPointerDown(PointerEventData eventData)
        {
            previousPosition = eventData.position;
            tracking = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!tracking) return;
            Vector2 delta = eventData.position - previousPosition;
            previousPosition = eventData.position;
            if (cameraRig != null) cameraRig.AddLookInput(delta * 0.12f);
        }

        private void OnDisable() => tracking = false;
    }
}
