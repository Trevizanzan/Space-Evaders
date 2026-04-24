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

    void OnEnable()
    {
        ClearIcons();

        if (PerkManager.Instance != null)
        {
            PerkManager.Instance.OnPerkAdded += AddIcon;
            PerkManager.Instance.OnShieldConsumed += RemoveShieldIcon;

            foreach (var p in PerkManager.Instance.ActivePerks)
                AddIcon(p);
        }
    }

    void OnDisable()
    {
        if (PerkManager.Instance != null)
        {
            PerkManager.Instance.OnPerkAdded -= AddIcon;
            PerkManager.Instance.OnShieldConsumed -= RemoveShieldIcon;
        }
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
        if (perkIconPrefab == null || iconsContainer == null || perk == null) return;
        if (iconInstances.ContainsKey(perk)) return;

        GameObject go = Instantiate(perkIconPrefab, iconsContainer);

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
