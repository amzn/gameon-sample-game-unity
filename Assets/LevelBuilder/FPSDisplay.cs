using UnityEngine;

namespace LevelBuilder
{
    public class FPSDisplay : MonoBehaviour {
        float deltaTime = 0.0f;
        void Update () {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }
        void OnGUI () {
            int w = Screen.width, h = Screen.height;
            GUIStyle style = new GUIStyle ();
            Rect rect = new Rect (0, 4, w, h * 2 / 100);
            style.alignment = TextAnchor.UpperCenter;
            style.fontSize = h * 2 / 100;
            style.normal.textColor = new Color (1.0f, 1.0f, 1.0f, 1.0f);
            style.normal.background = Texture2D.blackTexture;
            float fps = 1.0f / deltaTime;
            //float msec = deltaTime * 1000.0f;
            //string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
            string text = string.Format ("{0:0.} fps", fps);
            //GUI.Label(rect, text, style);
            GUI.Box (rect, text, style);
        }
    }
}