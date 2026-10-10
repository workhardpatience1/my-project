using UnityEngine;

// Ads guide, Part 2.4 (+ the fix from 4.5): the hit counter and the windows, drawn with OnGUI.
// Texts are in Uzbek. Plain ' is used in o' and g' so every device font can show it.
public class GameUI : MonoBehaviour
{
    GUIStyle labelStyle;
    GUIStyle smallStyle;
    GUIStyle buttonStyle;

    void OnGUI()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        float scale = Mathf.Min(Screen.width, Screen.height) / 720f; // works for vertical and horizontal screens
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 34;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            smallStyle = new GUIStyle(labelStyle);
            smallStyle.fontSize = 26;
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 32;
        }

        GUI.Label(new Rect(20, 10, 360, 44), "Urilish: " + gm.Hits + " / " + gm.MaxHits, labelStyle);
        GUI.Label(new Rect(w / 2f - 200f, 10, 400, 44), gm.Level + "-daraja", labelStyle);

        float bw = 520f, bh = 90f;
        float x = (w - bw) / 2f;

        if (gm.IsLevelComplete)
        {
            GUI.Box(new Rect(x - 30, 110, bw + 60, 450), "");
            GUI.Label(new Rect(x, 130, bw, 60), "DARAJA O'TILDI!", labelStyle);
            GUI.Label(new Rect(x, 195, bw, 40), "Yulduzlar: " + gm.Stars + " / 3", smallStyle);
            GUI.Label(new Rect(x, 235, bw, 40), "Vaqt: " + gm.RoundTime.ToString("0.0") + " s", smallStyle);
            if (GUI.Button(new Rect(x, 300, bw, bh), "Keyingi daraja", buttonStyle))
                gm.NextLevel();
            if (GUI.Button(new Rect(x, 410, bw, bh), "Qayta o'ynash", buttonStyle))
                gm.Restart();
            return;
        }

        if (!gm.IsGameOver) return;

        if (gm.GameOverAdPending)
        {
            // all lives are gone: the ad comes first, the buttons appear after it
            GUI.Box(new Rect(x - 30, 130, bw + 60, 130), "");
            GUI.Label(new Rect(x, 150, bw, 60), "O'YIN TUGADI", labelStyle);
            GUI.Label(new Rect(x, 205, bw, 40), "Reklama...", smallStyle);
            return;
        }

        GUI.Box(new Rect(x - 30, 130, bw + 60, 430), "");
        GUI.Label(new Rect(x, 150, bw, 60), "O'YIN TUGADI", labelStyle);

        float y = 240f;
        AdManager ads = AdManager.Instance;
        if (gm.CanContinue && ads != null)
        {
            if (ads.IsRewardedReady())
            {
                string text = "Reklama ko'rish: +" + gm.BonusLives + " jon";
                if (GUI.Button(new Rect(x, y, bw, bh), text, buttonStyle))
                    ads.ShowRewarded(gm.ContinueAfterReward);
            }
            else
            {
                // the button is always there; while the ad loads it is grey
                bool wasEnabled = GUI.enabled;
                GUI.enabled = false;
                GUI.Button(new Rect(x, y, bw, bh), "Reklama yuklanmoqda...", buttonStyle);
                GUI.enabled = wasEnabled;
            }
            y += bh + 20f;
        }

        if (GUI.Button(new Rect(x, y, bw, bh), "Qayta o'ynash", buttonStyle))
            gm.Restart();
    }
}
