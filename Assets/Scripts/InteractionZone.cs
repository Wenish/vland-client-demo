using System.Collections.Generic;
using MyGame.Events;
using UnityEngine;

public class InteractionZone : MonoBehaviour
{
    [Header("Scriptable Config")]
    [SerializeField]
    private InteractionZoneDefinition zoneDefinition;

    [Header("Inline Fallback")]
    public int interactionId;
    public InteractionType interactionType;
    public int goldCost = 0;

    [Header("Tooltip Overrides")]
    [SerializeField]
    [Tooltip("Optional custom first line. Leave empty to use default action text.")]
    private string promptLineOverride;

    [SerializeField]
    [Tooltip("Optional explicit purchase/open summary shown in tooltip.")]
    private string purchaseSummaryOverride;

    public int InteractionId => zoneDefinition != null ? zoneDefinition.interactionId : interactionId;
    public InteractionType InteractionType => zoneDefinition != null ? zoneDefinition.interactionType : interactionType;
    public int GoldCost => zoneDefinition != null ? zoneDefinition.goldCost : goldCost;

    private HashSet<UnitController> unitsInZone = new HashSet<UnitController>();
    private Dictionary<UnitController, System.Action> deathListeners = new Dictionary<UnitController, System.Action>();
    private Dictionary<UnitController, System.Action> reviveListeners = new Dictionary<UnitController, System.Action>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<UnitController>(out var unit))
        {
            if (unitsInZone.Contains(unit)) return;
            unitsInZone.Add(unit);
            // Listen for death
            System.Action onDied = () => OnUnitDiedInZone(unit);
            unit.OnDied += onDied;
            deathListeners[unit] = onDied;
            // Listen for revive
            System.Action onRevive = () => OnUnitRevivedInZone(unit);
            unit.OnRevive += onRevive;
            reviveListeners[unit] = onRevive;
            if (!unit.IsDead)
            {
                EventManager.Instance.Publish(new UnitEnteredInteractionZone(unit, this));
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<UnitController>(out var unit))
        {
            RemoveUnitFromZone(unit);
        }
    }

    private void OnUnitDiedInZone(UnitController unit)
    {
        // Remove interaction ability, but keep listeners for revive
        if (unitsInZone.Contains(unit))
        {
            EventManager.Instance.Publish(new UnitExitedInteractionZone(unit, this));
        }
    }

    private void RemoveUnitFromZone(UnitController unit)
    {
        if (unit == null) return;
        if (unitsInZone.Remove(unit))
        {
            if (deathListeners.TryGetValue(unit, out var onDied))
            {
                unit.OnDied -= onDied;
                deathListeners.Remove(unit);
            }
            if (reviveListeners.TryGetValue(unit, out var onRevive))
            {
                unit.OnRevive -= onRevive;
                reviveListeners.Remove(unit);
            }
            EventManager.Instance.Publish(new UnitExitedInteractionZone(unit, this));
        }
    }

    private void OnUnitRevivedInZone(UnitController unit)
    {
        // Only fire if the unit is still in the zone
        if (unitsInZone.Contains(unit))
        {
            EventManager.Instance.Publish(new UnitEnteredInteractionZone(unit, this));
        }
    }

    void OnDisable()
    {
        foreach (var unit in new List<UnitController>(unitsInZone))
        {
            RemoveUnitFromZone(unit);
        }
        unitsInZone.Clear();
        deathListeners.Clear();
        reviveListeners.Clear();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        if (TryGetComponent<SphereCollider>(out var sphereCollider))
        {
            Gizmos.DrawWireSphere(transform.position, sphereCollider.radius * transform.localScale.x);
        }
        else if (TryGetComponent<BoxCollider>(out var boxCollider))
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }

    public string BuildTooltipText()
    {
        var prompt = ResolvePromptLine();

        if (TryGetComponent<UpgradeStationZone>(out var upgradeStationZone) && upgradeStationZone.HasMultipleOffers)
        {
            var offerLines = upgradeStationZone.GetTooltipOfferLines();
            if (!string.IsNullOrWhiteSpace(offerLines))
            {
                return prompt + "\n" + offerLines;
            }
        }

        var purchaseSummary = ResolvePurchaseSummary();

        if (!string.IsNullOrWhiteSpace(purchaseSummary))
        {
            prompt += $"\n[Buy: {purchaseSummary}]";
        }

        if (GoldCost > 0)
        {
            prompt += $"\n[Cost: {GoldCost} Gold]";
        }

        return prompt;
    }

    private string ResolvePromptLine()
    {
        if (!string.IsNullOrWhiteSpace(promptLineOverride))
        {
            return promptLineOverride;
        }

        if (zoneDefinition != null && !string.IsNullOrWhiteSpace(zoneDefinition.customPromptLine))
        {
            return zoneDefinition.customPromptLine;
        }

        switch (InteractionType)
        {
            case InteractionType.OpenGate:
                return "Press F to open the gate";
            case InteractionType.BuyWeapon:
                return "Press F to buy a weapon";
            case InteractionType.BuyUpgrade:
                if (TryGetComponent<UpgradeStationZone>(out var upgradeStationZone) && upgradeStationZone.HasMultipleOffers)
                {
                    return "Press 1-9 to buy an upgrade";
                }
                return "Press F to buy an upgrade";
            default:
                return "Press F to interact";
        }
    }

    private string ResolvePurchaseSummary()
    {
        if (!string.IsNullOrWhiteSpace(purchaseSummaryOverride))
        {
            return purchaseSummaryOverride;
        }

        if (zoneDefinition != null && !string.IsNullOrWhiteSpace(zoneDefinition.purchaseSummary))
        {
            return zoneDefinition.purchaseSummary;
        }

        if (TryGetComponent<UpgradeStationZone>(out var upgradeStationZone))
        {
            var summary = upgradeStationZone.GetTooltipPurchaseSummary();
            if (!string.IsNullOrWhiteSpace(summary))
            {
                return summary;
            }
        }

        switch (InteractionType)
        {
            case InteractionType.BuyWeapon:
                return $"Weapon #{InteractionId}";
            case InteractionType.BuyUpgrade:
                return "Configured upgrade";
            default:
                return string.Empty;
        }
    }
}

public enum InteractionType : byte
{
    OpenGate,
    BuyWeapon,
    BuyUpgrade
}

[CreateAssetMenu(fileName = "InteractionZoneDefinition", menuName = "Game/Interaction/Zone Definition")]
public class InteractionZoneDefinition : ScriptableObject
{
    [Header("Core")]
    public int interactionId;
    public InteractionType interactionType;
    [Min(0)]
    public int goldCost;

    [Header("Tooltip")]
    [Tooltip("Overrides the first tooltip line. Leave empty to use defaults.")]
    public string customPromptLine;
    [Tooltip("What the player can buy/open at this station. Example: Shotgun, Armor Tier 1.")]
    public string purchaseSummary;
}