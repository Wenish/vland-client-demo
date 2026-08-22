using Mirror;
using MyGame.Events;
using R3;
using UnityEngine;

public class BuyWeaponManager : NetworkBehaviour {

    public WeaponMapping[] weaponMappings;
    private DisposableBag serverSubscriptions;

    [System.Serializable]
    public struct WeaponMapping
    {
        public int weaponId;
        public WeaponData weaponData;
    }

    void Start() {
        if (isServer) {
            GameMessages.Subscribe<BuyWeaponEvent>(ref serverSubscriptions, OnWeaponBuyEvent);
        }
    }
    void OnDestroy() {
        serverSubscriptions.Dispose();
        serverSubscriptions = new DisposableBag();
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