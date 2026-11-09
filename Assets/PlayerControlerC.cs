using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
public class PlayerControlerC : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    public float fuerzaSalto = 7f;
    public float longitudRaycast = 0.8f;
    public LayerMask capaSuelo;
    public float limiteCaida = -20f;
    [Header("Combate")]
    public KeyCode teclaAtaque = KeyCode.J;
    public int danoAtaque = 30;
    public float alcanceAtaque = 0.7f;
    [Header("Sigilo y paredes")]
    [Range(0.1f, 1f)] public float velocidadSigilo = 0.45f;
    public float velocidadTrepar = 2.5f;
    public float duracionAgarre = 2.5f;
    public Animator animator;
    public bool IsSneaking { get; private set; }
    public bool IsGripping { get; private set; }
    public bool IsHidden => IsSneaking && cover.Count > 0 && Time.time >= revealedUntil;
    public bool IsDead => muerto;
    public bool Grounded => enSuelo;
    private readonly HashSet<CoverZone> cover = new HashSet<CoverZone>();
    private bool enSuelo, muerto;
    private Rigidbody2D rb;
    private Collider2D body;
    private Health health;
    private CapsuleCollider2D capsule;
    private Vector2 standingSize, standingOffset;
    private Vector3 originalScale;
    private float inputX, nextAttack, wallJumpUntil, gripLeft, revealedUntil;
    private float facing = 1;
    private int wallSide;
    private bool gripHeld;
    private float climbInput;
    private int previousHealth;
    private float normalGravity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); body = GetComponent<Collider2D>(); health = GetComponent<Health>();
        normalGravity = rb.gravityScale;
        capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null) { standingSize = capsule.size; standingOffset = capsule.offset; }
        originalScale = transform.localScale;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        health.Died += Morir; health.Changed += OnHealthChanged;
        previousHealth = health.maximum;
        gripLeft = duracionAgarre;
    }
    private void OnDestroy() { if (health != null) { health.Died -= Morir; health.Changed -= OnHealthChanged; } }
    public void EnterCover(CoverZone zone) { cover.Add(zone); }
    public void LeaveCover(CoverZone zone) { cover.Remove(zone); }
    public void SetSneaking(bool value)
    {
        if (!value && capsule != null && IsSneaking)
        {
            Vector2 head = (Vector2)transform.position + standingOffset + Vector2.up * standingSize.y * 0.25f;
            if (Physics2D.OverlapBox(head, new Vector2(standingSize.x * 0.85f, standingSize.y * 0.45f), 0, capaSuelo)) return;
        }
        IsSneaking = value;
        if (capsule != null)
        {
            capsule.size = value ? new Vector2(standingSize.x, standingSize.y * 0.65f) : standingSize;
            capsule.offset = value ? standingOffset - Vector2.up * standingSize.y * 0.175f : standingOffset;
        }
    }
    private void Update()
    {
        if (muerto) return;
        if (Time.timeScale == 0 || DialogueUI.IsOpen) { inputX = 0; return; }
        if (transform.position.y < limiteCaida) { Morir(); return; }
        cover.RemoveWhere(z => z == null);
        SetMovementInput(Input.GetAxisRaw("Horizontal"), Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C),
            Input.GetKey(KeyCode.LeftShift), Input.GetAxisRaw("Vertical"));
        if (Input.GetButtonDown("Jump")) TryJump();
        if (Input.GetKeyDown(teclaAtaque)) Atacar();
        if (animator != null)
        {
            animator.SetFloat("Velocidad", Mathf.Abs(inputX));
            animator.SetBool("EnSuelo", enSuelo);
            animator.SetBool("Sigilo", IsSneaking);
            animator.SetBool("Agarrado", IsGripping);
        }
    }
    public void SetMovementInput(float horizontal, bool sneak, bool grip, float vertical)
    {
        inputX = Mathf.Clamp(horizontal, -1, 1); gripHeld = grip; climbInput = Mathf.Clamp(vertical, -1, 1);
        if (inputX != 0 && Time.time >= wallJumpUntil)
        {
            facing = Mathf.Sign(inputX);
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * facing, originalScale.y, originalScale.z);
        }
        SetSneaking(sneak);
    }
    private void FixedUpdate()
    {
        if (muerto) return;
        if (DialogueUI.IsOpen) { rb.gravityScale = 0; rb.linearVelocity = Vector2.zero; return; }
        rb.gravityScale = normalGravity;
        var bounds = body.bounds;
        enSuelo = Physics2D.BoxCast(new Vector2(bounds.center.x, bounds.min.y + 0.08f),
            new Vector2(bounds.size.x * 0.75f, 0.08f), 0, Vector2.down, 0.12f, capaSuelo).collider != null;
        if (enSuelo) gripLeft = duracionAgarre;
        wallSide = 0;
        if (!enSuelo)
        {
            if (Physics2D.Raycast(bounds.center, Vector2.right, bounds.extents.x + 0.13f, capaSuelo)) wallSide = 1;
            else if (Physics2D.Raycast(bounds.center, Vector2.left, bounds.extents.x + 0.13f, capaSuelo)) wallSide = -1;
        }
        IsGripping = wallSide != 0 && gripHeld && gripLeft > 0 && Time.time >= wallJumpUntil;
        if (Time.time < wallJumpUntil) return;
        if (IsGripping)
        {
            rb.gravityScale = 0;
            gripLeft -= Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(0, climbInput * velocidadTrepar);
        }
        else rb.linearVelocity = new Vector2(inputX * velocidad * (IsSneaking ? velocidadSigilo : 1), rb.linearVelocity.y);
    }
    public void TryJump()
    {
        if (muerto || Time.timeScale == 0 || DialogueUI.IsOpen) return;
        if (IsGripping)
        {
            IsGripping = false; wallJumpUntil = Time.time + 0.22f;
            rb.gravityScale = normalGravity;
            rb.linearVelocity = new Vector2(-wallSide * velocidad, fuerzaSalto);
        }
        else if (enSuelo) { enSuelo = false; rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto); }
    }
    public void Atacar()
    {
        if (muerto || Time.timeScale == 0 || DialogueUI.IsOpen || Time.time < nextAttack) return;
        nextAttack = Time.time + 0.4f; revealedUntil = Time.time + 2;
        if (animator != null) animator.SetTrigger("Atacar");
        var hitEnemies = new HashSet<EnemyController>();
        Vector2 center = (Vector2)transform.position + Vector2.right * facing * 0.6f;
        foreach (var hit in Physics2D.OverlapCircleAll(center, alcanceAtaque))
        {
            var enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy != null && hitEnemies.Add(enemy)) enemy.ReceiveDamage(danoAtaque);
        }
    }
    private void OnHealthChanged()
    {
        if (health.Current < previousHealth && !health.IsDead && animator != null) animator.SetTrigger("Herido");
        previousHealth = health.Current;
    }
    public void Morir()
    {
        if (muerto) return;
        if (!health.IsDead) { health.Kill(); return; }
        muerto = true; inputX = 0; IsGripping = false;
        rb.linearVelocity = Vector2.zero; rb.simulated = false;
        if (animator != null) { animator.updateMode = AnimatorUpdateMode.UnscaledTime; animator.SetTrigger("Morir"); }
        var ui = FindFirstObjectByType<GameManagerUI>(); if (ui != null) ui.TriggerGameOver();
    }
}
