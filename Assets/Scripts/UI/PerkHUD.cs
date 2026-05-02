using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PerkHUD : MonoBehaviour
{
    [SerializeField] private GameObject perkIconPrefab;
    [SerializeField] private Transform iconsContainer;

    private readonly Dictionary<PerkData, GameObject> iconInstances = new Dictionary<PerkData, GameObject>();

    void Start()
    {
        Subscribe();
    }

    void OnEnable()
    {
        // Solo per re-enable dopo il primo Start (es. scene reload con PerkManager già vivo)
        if (PerkManager.Instance != null)
            Subscribe();
    }

    void OnDisable()
    {
        if (PerkManager.Instance != null)
        {
            PerkManager.Instance.OnPerkAdded -= AddIcon;
            PerkManager.Instance.OnShieldConsumed -= RemoveShieldIcon;
        }
    }

    private void Subscribe()
    {
        if (PerkManager.Instance == null) return;

        PerkManager.Instance.OnPerkAdded -= AddIcon;
        PerkManager.Instance.OnShieldConsumed -= RemoveShieldIcon;
        PerkManager.Instance.OnPerkAdded += AddIcon;
        PerkManager.Instance.OnShieldConsumed += RemoveShieldIcon;

        ClearIcons();
        foreach (var p in PerkManager.Instance.ActivePerks)
            AddIcon(p);
    }

    private void ClearIcons()
    {
        if (iconsContainer != null)
        {
            for (int i = iconsContainer.childCount - 1; i >= 0; i--)
                Destroy(iconsContainer.GetChild(i).gameObject);
        }
        iconInstances.Clear();
    }

    private void AddIcon(PerkData perk)
    {
        //Debug.Log($"[PerkHUD] AddIcon called: {(perk != null ? perk.name : "NULL")} | prefab={perkIconPrefab != null} | container={iconsContainer != null}");
        if (perkIconPrefab == null || iconsContainer == null || perk == null) return;
        if (iconInstances.ContainsKey(perk)) return;

        GameObject go = Instantiate(perkIconPrefab, iconsContainer);
        //Debug.Log($"[PerkHUD] Icon instantiated for {perk.name}, children now: {iconsContainer.childCount}");

        var img = go.GetComponentInChildren<Image>();
        if (img != null) img.sprite = perk.icon;

        var txt = go.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.text = FormatValue(perk);

        iconInstances[perk] = go;
    }

    private void RemoveShieldIcon()
    {
        var shield = iconInstances.Keys.FirstOrDefault(p => p.type == PerkType.Shield);
        if (shield == null) return;

        if (iconInstances.TryGetValue(shield, out var go) && go != null)
            Destroy(go);
        iconInstances.Remove(shield);
    }

    private string FormatValue(PerkData p)
    {
        switch (p.type)
        {
            case PerkType.FireRate:
            case PerkType.MoveSpeed:
                return $"+{Mathf.RoundToInt(p.value * 100f)}%";
            case PerkType.Damage:
                return $"+{Mathf.RoundToInt(p.value)}";
            default:
                return "";
        }
    }
}
