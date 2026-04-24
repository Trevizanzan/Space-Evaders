using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PerkManager : MonoBehaviour
{
    public static PerkManager Instance;

    public float FireRateMultiplier { get; private set; } = 1f;
    public int DamageBonus { get; private set; } = 0;
    public float SpeedMultiplier { get; private set; } = 1f;
    public bool ShieldActive { get; private set; } = false;

    public List<PerkData> ActivePerks { get; } = new List<PerkData>();

    public event Action<PerkData> OnPerkAdded;
    public event Action OnShieldConsumed;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ApplyPerk(PerkData perk)
    {
        if (perk == null) return;

        // ExtraLife: effetto istantaneo, non va tracciato nell'HUD
        if (perk.type == PerkType.ExtraLife)
        {
            var health = FindFirstObjectByType<PlayerHealth>();
            if (health != null) health.AddLife();
            return;
        }

        // Shield: ignora duplicato se già attivo (non stacka)
        if (perk.type == PerkType.Shield && ShieldActive)
            return;

        switch (perk.type)
        {
            case PerkType.FireRate:
                FireRateMultiplier += perk.value;
                break;
            case PerkType.MoveSpeed:
                SpeedMultiplier += perk.value;
                break;
            case PerkType.Damage:
                DamageBonus += Mathf.RoundToInt(perk.value);
                break;
            case PerkType.Shield:
                ShieldActive = true;
                break;
        }

        ActivePerks.Add(perk);
        OnPerkAdded?.Invoke(perk);
    }

    public void OnShieldHit()
    {
        ShieldActive = false;
        var shield = ActivePerks.FirstOrDefault(p => p.type == PerkType.Shield);
        if (shield != null) ActivePerks.Remove(shield);
        OnShieldConsumed?.Invoke();
    }
}
