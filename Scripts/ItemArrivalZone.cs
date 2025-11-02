using LCOffice.Components;
using LethalMin.Routeing;
using UnityEngine;

namespace LethalMin
{
    public class ItemArrivalZone : MonoBehaviour
    {
        public enum ArrivalZoneType
        {
            Exit,
            Ship,
            Crusier,
            Counter,
            MineElevator,
            OfficeElvator,
            Zelevator,
            Forever
        }

        private ArrivalZoneType zoneType;
        private Transform positionCheck = null!;
        private Collider zoneCollider = null!;
        private float checkDistance = 0.1f;
        private EntranceTeleport entranceTeleport = null!;
        private DepositItemsDesk desk = null!;
        private PikminVehicleController vehicleController = null!;
        private MineshaftElevatorController mineShaftElevator = null!;

        public static void CreateZoneOnObject(GameObject obj, ArrivalZoneType type)
        {
            ItemArrivalZone newZone = obj.AddComponent<ItemArrivalZone>();
            newZone.zoneType = type;
            LethalMin.Logger.LogInfo($"Created Item Arrival Zone of type {type} on object {obj.name}");
        }

        public void Start()
        {
            PikminManager.instance.ItemArrivalZones.Add(this);

            switch (zoneType)
            {
                case ArrivalZoneType.Exit:
                    entranceTeleport = GetComponent<EntranceTeleport>();
                    positionCheck = entranceTeleport.entrancePoint;
                    checkDistance = 5f;
                    break;
                case ArrivalZoneType.Ship:
                    zoneCollider = StartOfRound.Instance.shipInnerRoomBounds;
                    break;
                case ArrivalZoneType.Crusier:
                    vehicleController = GetComponent<PikminVehicleController>();
                    positionCheck = vehicleController.transform;
                    break;
                case ArrivalZoneType.Counter:
                    desk = GetComponent<DepositItemsDesk>();
                    positionCheck = desk.triggerCollider.transform;
                    checkDistance = 10f;
                    break;
                case ArrivalZoneType.MineElevator:
                    mineShaftElevator = GetComponent<MineshaftElevatorController>();
                    break;
            }
        }

        public bool CanBeMovedOutofZone(PikminItem itemChecking)
        {
            switch (zoneType)
            {
                case ArrivalZoneType.Exit:
                    return LethalMin.UseExitsWhenCarryingItems;

                case ArrivalZoneType.Ship:
                    return itemChecking.settings.CanProduceSprouts && PikminManager.instance.Onions.Count > 0
                    || LethalMin.TakeItemsToOnionOnCompany && LethalMin.OnCompany && itemChecking.ItemScript.itemProperties.isScrap;

                case ArrivalZoneType.Crusier:
                    return vehicleController.IsNearByShip() || vehicleController.controller.carDestroyed;

                case ArrivalZoneType.MineElevator:
                    return !mineShaftElevator.elevatorMovingDown && LethalMin.UseExitsWhenCarryingItems.InternalValue;

                case ArrivalZoneType.Counter:
                    return !itemChecking.ItemScript.itemProperties.isScrap;

                case ArrivalZoneType.OfficeElvator:
                    return LCOFFICE_CBMOOZ(itemChecking);

                case ArrivalZoneType.Zelevator:
                    return ZENEROS_CBMOOZ(itemChecking);

                case ArrivalZoneType.Forever:
                    return false;

                default:
                    return true;
            }
        }

        public bool LCOFFICE_CBMOOZ(PikminItem itemChecking)
        {
            FloorData? currentFloor = PikminRouteManager.Instance.GetFloorFromPosition(itemChecking.transform.position)!;
            return currentFloor != null && currentFloor.Exits != null && currentFloor.Exits.Count > 0f;
        }

        public bool ZENEROS_CBMOOZ(PikminItem itemChecking)
        {
            return !StartOfRound.Instance.localPlayerController.isInsideFactory;
        }

        public bool IsItemInZone(PikminItem item)
        {
            bool inBounds = zoneCollider == null ? false : zoneCollider.bounds.Contains(item.transform.position);
            bool withinRange = positionCheck == null ? false : Vector3.Distance(item.transform.position, positionCheck.position) <= checkDistance;

            return !CanBeMovedOutofZone(item) && (inBounds || withinRange);
        }

        public void OnDestroy()
        {
            PikminManager.instance.ItemArrivalZones.Remove(this);
        }
    }
}