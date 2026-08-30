using System.Collections.Generic;
using MyGame.Events;
using ShadowInfection.DI;
using ShadowInfection.Input;
using ShadowInfection.Interactions;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionZone : MonoBehaviour, IVendorInteractable
{
    [Header("Scriptable Config")]
    [SerializeField]
    private InteractionZoneDefinition zoneDefinition;

    [Header("Inline Fallback")]
    public int interactionId;
    public InteractionType interactionType;
    public int goldCost = 0;

    [Header("Vendor")]
    [SerializeField]
    private VendorDefinition vendorCatalog;
    [SerializeField]
    private VendorTab defaultTab = VendorTab.Buy;

    public VendorDefinition VendorCatalog => vendorCatalog;
    public VendorTab DefaultTab => defaultTab;

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
    private CatalogVendorSession previewSession;
    private readonly Dictionary<uint, CatalogVendorSession> sessionsByPlayer = new Dictionary<uint, CatalogVendorSession>();

    private void OnEnable()
    {
        InteractionZoneRegistry.RegisterOrDefer(this);
    }

    public IVendorSession GetVendorSession()
    {
        return GetOrCreatePreviewSession();
    }

    public IVendorSession GetSessionFor(PlayerController player)
    {
        if (player == null || vendorCatalog == null)
            return null;

        if (!sessionsByPlayer.TryGetValue(player.netId, out var session) || session.Catalog != vendorCatalog)
        {
            session = new CatalogVendorSession(vendorCatalog, this);
            sessionsByPlayer[player.netId] = session;
        }

        return session;
    }

    public void EndSessionFor(PlayerController player)
    {
        if (player != null)
            sessionsByPlayer.Remove(player.netId);
    }

    private CatalogVendorSession GetOrCreatePreviewSession()
    {
        if (vendorCatalog == null)
            return null;

        if (previewSession == null || previewSession.Catalog != vendorCatalog)
            previewSession = new CatalogVendorSession(vendorCatalog, this);

        return previewSession;
    }

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
                GameMessages.Publish(new UnitEnteredInteractionZone(unit, this));
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
            GameMessages.Publish(new UnitExitedInteractionZone(unit, this));
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
            GameMessages.Publish(new UnitExitedInteractionZone(unit, this));
        }
    }

    private void OnUnitRevivedInZone(UnitController unit)
    {
        // Only fire if the unit is still in the zone
        if (unitsInZone.Contains(unit))
        {
            GameMessages.Publish(new UnitEnteredInteractionZone(unit, this));
        }
    }

    private void OnDisable()
    {
        InteractionZoneRegistry.UnregisterOrDefer(this);

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

        if (InteractionType == InteractionType.OpenVendor)
            return prompt;

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
        var interactKey = ResolveInteractBind();
        if (!string.IsNullOrWhiteSpace(promptLineOverride))
            return InputBindingDisplay.ApplyInteractPrompt(promptLineOverride, interactKey);

        if (zoneDefinition != null && !string.IsNullOrWhiteSpace(zoneDefinition.customPromptLine))
            return InputBindingDisplay.ApplyInteractPrompt(zoneDefinition.customPromptLine, interactKey);

        var remainder = InteractionType switch
        {
            InteractionType.OpenGate => "to open the gate",
            InteractionType.OpenVendor => "to trade",
            _ => "to interact"
        };
        return "Press " + InputBindingDisplay.ToPromptLabel(interactKey) + " " + remainder;
    }

    private static InputBindingKey ResolveInteractBind()
    {
        if (GameServices.TryGet<IInputBindingSession>(out var session)
            && session.TryGetDisplayBind(PlayerActionId.Interact, out var key)
            && !key.IsEmpty)
            return key;

        return InputBindingKey.Keyboard(Key.F);
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

        return string.Empty;
    }
}

public enum InteractionType : byte
{
    OpenGate = 0,
    OpenVendor = 3
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