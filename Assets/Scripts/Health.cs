using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Min(1)] public int maximum = 100;
    [Min(0)] public float invulnerability = 0.6f;
    public int Current { get; private set; }
    public bool IsDead => Current <= 0;
    public float Ratio => (float)Current / maximum;
    public event Action Changed;
    public event Action Died;
    private float nextDamage;
    private void Awake() { Current = maximum; }

    public bool Damage(int amount)
    {
        if (amount <= 0 || IsDead || Time.timeScale == 0 || DialogueUI.IsOpen || Time.time < nextDamage) return false;
        Current = Mathf.Max(0, Current - amount);
        nextDamage = Time.time + invulnerability;
        Changed?.Invoke();
        if (IsDead) Died?.Invoke();
        return true;
    }
    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        Current = Mathf.Min(maximum, Current + amount);
        Changed?.Invoke();
    }
    public void Kill()
    {
        if (IsDead) return;
        Current = 0; Changed?.Invoke(); Died?.Invoke();
    }
}
