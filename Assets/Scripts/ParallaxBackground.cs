using UnityEngine;

// Runs after Cinemachine has positioned the rendered camera.
[DefaultExecutionOrder(10000)]
public class ParallaxBackground : MonoBehaviour
{
    [Range(0, 1)] public float horizontalFollow = 0.7f;
    [Range(0, 1)] public float verticalFollow = 0.8f;
    public bool repeatHorizontally = true;
    private Camera view;
    private Vector3 origin;
    private Vector3 cameraOrigin;
    private float width;
    private void Start()
    {
        view = Camera.main;
        if (view == null) return;
        origin = transform.position; cameraOrigin = view.transform.position;
        var sprite = GetComponent<SpriteRenderer>();
        width = sprite.bounds.size.x;
        if (!repeatHorizontally || width <= 0) return;
        int copies = Mathf.CeilToInt(view.orthographicSize * 2 * view.aspect / width) + 1;
        for (int i = -copies; i <= copies; i++)
        {
            if (i == 0) continue;
            var tile = new GameObject("Continuacion del fondo").AddComponent<SpriteRenderer>();
            tile.transform.SetParent(transform, false);
            tile.transform.localPosition = Vector3.right * i * sprite.sprite.bounds.size.x;
            tile.sprite = sprite.sprite; tile.sharedMaterial = sprite.sharedMaterial;
            tile.sortingLayerID = sprite.sortingLayerID; tile.sortingOrder = sprite.sortingOrder;
            tile.color = sprite.color;
        }
    }
    private void LateUpdate()
    {
        if (view == null) return;
        Vector3 delta = view.transform.position - cameraOrigin;
        Vector3 position = origin + new Vector3(delta.x * horizontalFollow, delta.y * verticalFollow, 0);
        if (repeatHorizontally && width > 0) position.x += Mathf.Round((view.transform.position.x - position.x) / width) * width;
        transform.position = position;
    }
}
