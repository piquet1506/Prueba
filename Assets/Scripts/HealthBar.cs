using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Health target;
    public Image picture;
    [Tooltip("Orden: vida completa a vacia.")]
    public Sprite[] frames;
    public bool enemyBar;
    public float proximity = 5f;
    public Vector3 worldOffset = new Vector3(0, 1.1f, 0);
    public CanvasGroup visibility;
    public Text valueText;
    private RectTransform rect;
    private Canvas canvas;
    private Transform player;
    private float displayed = 1;
    private float flash;
    private float lastRatio = 1;
    private void Start()
    {
        rect = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        var obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null) player = obj.transform;
        if (target != null) displayed = lastRatio = target.Ratio;
        if (enemyBar && visibility != null) visibility.alpha = 0;
    }
    private void LateUpdate()
    {
        if (target == null) { if (enemyBar) Destroy(gameObject); return; }
        if (target.Ratio < lastRatio) flash = 0.35f;
        lastRatio = target.Ratio;
        displayed = Mathf.MoveTowards(displayed, target.Ratio, Time.unscaledDeltaTime * 2f);
        flash = Mathf.Max(0, flash - Time.unscaledDeltaTime);
        picture.color = flash > 0 ? Color.Lerp(Color.white, new Color(1, 0.35f, 0.35f), flash / 0.35f) : Color.white;
        if (frames != null && frames.Length > 0)
            picture.sprite = frames[Mathf.Clamp(Mathf.RoundToInt((1 - displayed) * (frames.Length - 1)), 0, frames.Length - 1)];
        if (valueText != null) valueText.text = target.Current + " / " + target.maximum;
        if (!enemyBar) return;
        Camera cam = Camera.main;
        if (cam == null || player == null) return;
        var enemy = target.GetComponent<EnemyController>();
        bool nearby = Vector2.Distance(target.transform.position, player.position) <= proximity;
        Vector3 screen = cam.WorldToScreenPoint(target.transform.position + worldOffset);
        bool shown = !target.IsDead && screen.z > 0 && (nearby || (enemy != null && enemy.InCombat));
        visibility.alpha = Mathf.MoveTowards(visibility.alpha, shown ? 1 : 0, Time.unscaledDeltaTime * 6);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screen, null, out Vector2 local);
        rect.anchoredPosition = local;
    }
}
