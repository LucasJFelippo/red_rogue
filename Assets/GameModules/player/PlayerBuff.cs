using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Outline))]
public class PlayerBuffManager : MonoBehaviour
{
    [Header("Configuração de Cores (Status)")]
    public Color baseColor;
    public Color healthColor = Color.green;
    public Color speedColor = Color.yellow;
    public Color damageColor = Color.red;

    [Header("Settings")]
    public float flashDuration = 0.5f;
    private Outline outline;

    private class ActiveBuff
    {
        public ItemPickup.ItemType type;
        public float timeRemaining;
    }

    private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    void Awake()
    {
        outline = GetComponent<Outline>();
        baseColor = outline.OutlineColor;
    }

    void Update()
    {
        UpdateBuffTimers();
        UpdateOutlineColor();
    }

    public void AddBuff(ItemPickup.ItemType type, float duration)
    {
        float visualDuration = duration > 0 ? duration : flashDuration;
        activeBuffs.Add(new ActiveBuff { type = type, timeRemaining = visualDuration });
    }

    private void UpdateBuffTimers()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].timeRemaining -= Time.deltaTime;
            if (activeBuffs[i].timeRemaining <= 0)
            {
                activeBuffs.RemoveAt(i);
            }
        }
    }

    private void UpdateOutlineColor()
    {
        if (activeBuffs.Count == 0)
        {
            outline.OutlineColor = Color.Lerp(outline.OutlineColor, baseColor, Time.deltaTime * 5f);
            return;
        }

        float r = 0, g = 0, b = 0, a = 0;

        foreach (var buff in activeBuffs)
        {
            Color c = GetColorForType(buff.type);
            r += c.r;
            g += c.g;
            b += c.b;
            a += c.a;
        }

        int count = activeBuffs.Count;
        Color targetColor = new Color(r / count, g / count, b / count, a / count);

        outline.OutlineColor = Color.Lerp(outline.OutlineColor, targetColor, Time.deltaTime * 10f);
    }

    private Color GetColorForType(ItemPickup.ItemType type)
    {
        switch (type)
        {
            case ItemPickup.ItemType.Health: return healthColor;
            case ItemPickup.ItemType.Speed: return speedColor;
            case ItemPickup.ItemType.Damage: return damageColor;
            default: return baseColor;
        }
    }
}