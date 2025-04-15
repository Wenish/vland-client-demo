using Mirror;
using MyGame.Events;
using UnityEngine;

public class BuyWeaponManager : NetworkBehaviour {

    public static BuyWeaponManager Instance { get; private set; }

    public WeaponMapping[] weaponMappings;

    [System.Serializable]
    public struct WeaponMapping
    {
        public int weaponId;
        public WeaponData weaponData;
    }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start() {
        if (isServer) {
            EventManager.Instance.Subscribe<BuyWeaponEvent>(OnWeaponBuyEvent);
        }
    }
    void OnDestroy() {
        if (isServer) {
            EventManager.Instance.Subscribe<BuyWeaponEvent>(OnWeaponBuyEvent);
        }
    }

    void OnWeaponBuyEvent(BuyWeaponEvent buyWeaponEvent) {
        Debug.Log($"BuyWeaponEvent: {buyWeaponEvent.WeaponId} {buyWeaponEvent.Buyer.name}");
        foreach (var weaponMapping in weaponMappings) {
            if (weaponMapping.weaponId == buyWeaponEvent.WeaponId) {
                var unitController = buyWeaponEvent.Buyer.Unit.GetComponent<UnitController>();
                var weaponName = weaponMapping.weaponData.weaponName;
                unitController.EquipWeapon(weaponName);
                break;
            }
        }
    }
}