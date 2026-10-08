using UnityEngine;

// Ads guide, Part 4.2: arrow buttons on the screen for the phone.
// Mover reads TouchControls.Horizontal and TouchControls.Vertical.
// splitLayout = true (default): "left/right" under the left thumb, "up/down" under the right
// thumb, so both thumbs can move at once. splitLayout = false: the cross from the guide.
public class TouchControls : MonoBehaviour
{
    // -1, 0 or 1. Other scripts can read them, but cannot change them.
    public static float Horizontal { get; private set; }
    public static float Vertical { get; private set; }

    [SerializeField] float buttonSize = 120f;  // button size (in "virtual" pixels)
    [SerializeField] float gap = 14f;          // space between buttons
    [SerializeField] float margin = 40f;       // space from the screen edge
    [SerializeField] bool splitLayout = true;
    [SerializeField] bool showOnPC = false;    // tick this to test the buttons in the editor

    Rect upRect, downRect, leftRect, rightRect;
    bool upDown, downDown, leftDown, rightDown;
    Texture2D arrowTexture;

    public void SetMargin(float value) { margin = value; }

    // Like in GameUI: buttons are the same size on any screen
    float GetScale()
    {
        return Mathf.Min(Screen.width, Screen.height) / 720f;
    }

    bool IsVisible()
    {
        // Show on a phone (or on a PC if showOnPC is on)
        if (!Application.isMobilePlatform && !showOnPC) return false;
        // Hide the buttons when the game is over
        GameManager gm = GameManager.Instance;
        return gm == null || (!gm.IsGameOver && !gm.IsLevelComplete);
    }

    void BuildLayout()
    {
        float scale = GetScale();
        float screenW = Screen.width / scale;
        float screenH = Screen.height / scale;
        float s = buttonSize;
        float yBottom = screenH - margin - s;

        if (splitLayout)
        {
            leftRect = new Rect(margin, yBottom, s, s);
            rightRect = new Rect(margin + s + gap, yBottom, s, s);
            downRect = new Rect(screenW - margin - s, yBottom, s, s);
            upRect = new Rect(screenW - margin - s, yBottom - s - gap, s, s);
            return;
        }

        // the cross from the guide, in the bottom left corner
        float x0 = margin;
        float x1 = margin + s + gap;
        float x2 = margin + 2f * (s + gap);
        float yMiddle = yBottom - s - gap;
        float yTop = yMiddle - s - gap;
        upRect = new Rect(x1, yTop, s, s);
        leftRect = new Rect(x0, yMiddle, s, s);
        rightRect = new Rect(x2, yMiddle, s, s);
        downRect = new Rect(x1, yBottom, s, s);
    }

    void Update()
    {
        upDown = downDown = leftDown = rightDown = false;
        if (!IsVisible())
        {
            Horizontal = 0f;
            Vertical = 0f;
            return;
        }

        BuildLayout();
        float scale = GetScale();

        // Check every finger on the screen (this way two fingers work at once)
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
            CheckPoint(t.position, scale);
        }

        // There are no fingers in the editor, so we use the mouse
        if (Input.touchCount == 0 && Input.GetMouseButton(0))
        {
            CheckPoint(Input.mousePosition, scale);
        }

        Horizontal = (rightDown ? 1f : 0f) - (leftDown ? 1f : 0f);
        Vertical = (upDown ? 1f : 0f) - (downDown ? 1f : 0f);
    }

    void CheckPoint(Vector2 screenPos, float scale)
    {
        // Touch: (0,0) is bottom left. GUI: (0,0) is top left. So we flip Y.
        Vector2 p = new Vector2(screenPos.x / scale, (Screen.height - screenPos.y) / scale);
        if (upRect.Contains(p)) upDown = true;
        if (downRect.Contains(p)) downDown = true;
        if (leftRect.Contains(p)) leftDown = true;
        if (rightRect.Contains(p)) rightDown = true;
    }

    void OnGUI()
    {
        if (!IsVisible()) return;
        if (arrowTexture == null) arrowTexture = MakeArrowTexture();

        BuildLayout();
        float scale = GetScale();
        Matrix4x4 scaleMatrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        DrawButton(upRect, 0f, upDown, scaleMatrix);
        DrawButton(rightRect, 90f, rightDown, scaleMatrix);
        DrawButton(downRect, 180f, downDown, scaleMatrix);
        DrawButton(leftRect, 270f, leftDown, scaleMatrix);

        GUI.matrix = Matrix4x4.identity;
        GUI.color = Color.white;
    }

    void DrawButton(Rect r, float angle, bool pressed, Matrix4x4 scaleMatrix)
    {
        GUI.matrix = scaleMatrix;
        // Dark square (lighter when the button is pressed)
        GUI.color = pressed ? new Color(1f, 1f, 1f, 0.55f) : new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);

        // White arrow. We turn it by the needed angle around the center of the button.
        Vector3 pivot = new Vector3(r.center.x, r.center.y, 0f);
        Matrix4x4 rotate = Matrix4x4.TRS(pivot, Quaternion.Euler(0f, 0f, angle), Vector3.one)
                           * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
        GUI.matrix = scaleMatrix * rotate;
        GUI.color = pressed ? new Color(0f, 0f, 0f, 0.9f) : new Color(1f, 1f, 1f, 0.85f);
        float pad = r.width * 0.25f;
        GUI.DrawTexture(new Rect(r.x + pad, r.y + pad, r.width - 2f * pad, r.height - 2f * pad), arrowTexture);
    }

    // Draws a small triangle (pointing up) right in code, no picture file needed
    Texture2D MakeArrowTexture()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        {
            // The triangle is narrow at the top and wide at the bottom
            float halfWidth = (size - 1 - y) / (float)(size - 1) * (size / 2f);
            for (int x = 0; x < size; x++)
            {
                bool inside = Mathf.Abs(x - (size - 1) / 2f) <= halfWidth;
                tex.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        tex.Apply();
        return tex;
    }
}
