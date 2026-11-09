using UnityEngine;

[RequireComponent(typeof(Health), typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    public Transform player;
    public float detectionRange = 7;
    public float maxDistance = 12;
    public float jumpHeightTolerance = 2;
    public float speed = 3.5f;
    public LayerMask groundMask = 1 << 6;
    public int attackDamage = 15;
    public float attackRange = 1.1f;
    public float attackInterval = 1.2f;
    public float combatMemory = 3;
    public bool InCombat => Time.time < alertedUntil;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Health health;
    private Collider2D body;
    private PlayerControlerC playerControl;
    private Vector3 startPosition;
    private float alertedUntil, nextAttack;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>(); body = GetComponent<Collider2D>();
        health.Died += Die;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        startPosition = transform.position;
    }
    private void Start()
    {
        if (player == null) { var go = GameObject.FindGameObjectWithTag("Player"); if (go != null) player = go.transform; }
        if (player != null) playerControl = player.GetComponent<PlayerControlerC>();
    }
    public bool CanDetectPlayer()
    {
        if (player == null || playerControl == null || playerControl.IsDead) return false;
        float range = playerControl.IsHidden ? 0.9f : detectionRange * (playerControl.IsSneaking ? 0.4f : 1);
        Vector2 offset = player.position - transform.position;
        return offset.magnitude <= range && !Physics2D.Raycast(transform.position, offset.normalized, offset.magnitude, groundMask);
    }
    private void FixedUpdate()
    {
        if (health.IsDead || player == null) return;
        if (DialogueUI.IsOpen || (playerControl != null && playerControl.IsDead)) { rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); return; }
        if (CanDetectPlayer()) alertedUntil = Time.time + combatMemory;
        float distance = Vector2.Distance(player.position, transform.position);
        float direction = 0;
        if (InCombat && distance <= maxDistance)
        {
            float dx = player.position.x - transform.position.x;
            if (spriteRenderer != null) spriteRenderer.flipX = dx < 0;
            if (distance <= attackRange && Time.time >= nextAttack &&
                !Physics2D.Linecast(transform.position, player.position, groundMask))
            {
                player.GetComponent<Health>()?.Damage(attackDamage);
                nextAttack = Time.time + attackInterval;
            }
            else if (Mathf.Abs(player.position.y - transform.position.y) <= jumpHeightTolerance && Mathf.Abs(dx) > attackRange * 0.8f)
                direction = Mathf.Sign(dx);
        }
        else if (Mathf.Abs(transform.position.x - startPosition.x) > 0.2f) direction = Mathf.Sign(startPosition.x - transform.position.x);
        if (direction != 0 && body != null)
        {
            var b = body.bounds;
            Vector2 edge = new Vector2(b.center.x + direction * (b.extents.x + 0.18f), b.min.y + 0.2f);
            bool floorAhead = Physics2D.Raycast(edge, Vector2.down, 0.8f, groundMask);
            bool wallAhead = Physics2D.Raycast(b.center, Vector2.right * direction, b.extents.x + 0.15f, groundMask);
            if (!floorAhead || wallAhead) direction = 0;
        }
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
    }
    public void ReceiveDamage(int amount)
    {
        if (health.Damage(amount)) alertedUntil = Time.time + combatMemory;
    }
    private void Die() { rb.linearVelocity = Vector2.zero; Destroy(gameObject); }
    private void OnDestroy() { if (health != null) health.Died -= Die; }
}
