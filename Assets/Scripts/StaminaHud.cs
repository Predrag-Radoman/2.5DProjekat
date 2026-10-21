using UnityEngine;

/// <summary>
/// Minimalni stamina bar (OnGUI, bez UI asseta) — gornji levi ugao.
/// PlayerController ga automatski dodaje ako je showStaminaHud ukljucen.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class StaminaHud : MonoBehaviour
{
    [SerializeField] private Vector2 offset = new Vector2(18f, 28f);
    [SerializeField] private Vector2 size = new Vector2(220f, 14f);
    [SerializeField] private Color fillColor = new Color(0.25f, 0.85f, 0.35f);
    [SerializeField] private Color lowColor = new Color(0.95f, 0.3f, 0.2f);

    private PlayerController controller;
    private GUIStyle labelStyle;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    private void OnGUI()
    {
        if (controller == null) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 11;
            labelStyle.normal.textColor = Color.white;
        }

        float normalized = controller.StaminaNormalized;
        Rect back = new Rect(offset.x, offset.y, size.x, size.y);
        Rect fill = new Rect(back.x + 2f, back.y + 2f,
            Mathf.Max(0f, (back.width - 4f) * normalized), back.height - 4f);

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(back, Texture2D.whiteTexture);
        GUI.color = Color.Lerp(lowColor, fillColor, normalized);
        GUI.DrawTexture(fill, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(back.x, back.y - 17f, back.width, 16f), "STAMINA", labelStyle);
    }
}
