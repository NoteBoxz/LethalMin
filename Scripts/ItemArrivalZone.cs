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
        private float checkDistance = -1f;
        private EntranceTeleport entranceTeleport = null!;
        private DepositItemsDesk desk = null!;
        private VehicleController vehicleController = null!;
        private MineshaftElevatorController mineShaftElevator = null!;

        public static void CreateZoneOnObject(GameObject obj, ArrivalZoneType type)
        {
            ItemArrivalZone newZone = obj.AddComponent<ItemArrivalZone>();
            newZone.zoneType = type;
        }

        public void Start()
        {
            PikminManager.instance.ItemArrivalZones.Add(this);

            switch (zoneType)
            {
                case ArrivalZoneType.Exit:
                    entranceTeleport = GetComponent<EntranceTeleport>();
                    positionCheck = entranceTeleport.entrancePoint;
                    checkDistance = 15f;
                    break;
                case ArrivalZoneType.Ship:
                    zoneCollider = StartOfRound.Instance.shipInnerRoomBounds;
                    break;
                case ArrivalZoneType.Crusier:
                    vehicleController = GetComponent<VehicleController>();
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
                    return LethalMin.CanPathOutsideWhenInside;

                case ArrivalZoneType.Ship:
                    return itemChecking.settings.CanProduceSprouts && PikminManager.instance.Onions.Count > 0
                    || LethalMin.TakeItemsToOnionOnCompany && LethalMin.OnCompany && !itemChecking.ItemScript.itemProperties.isScrap;

                case ArrivalZoneType.Crusier:
                    return Vector3.Distance(positionCheck.position, StartOfRound.Instance.shipBounds.transform.position) < 50f;

                case ArrivalZoneType.MineElevator:
                    return !mineShaftElevator.elevatorMovingDown && LethalMin.CanPathOutsideWhenInside.InternalValue;

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
            return ElevatorSystem.elevatorFloor == 1 && LethalMin.CanPathOutsideWhenInside.InternalValue;
        }

        public bool ZENEROS_CBMOOZ(PikminItem itemChecking)
        {
            return !StartOfRound.Instance.localPlayerController.isInsideFactory;
        }

        public bool IsItemInZone(PikminItem item)
        {
            float dist = Vector3.Distance(item.transform.position, positionCheck.position);
            bool inBounds = zoneCollider == null ? false : zoneCollider.bounds.Contains(item.transform.position);
            bool withinRange = checkDistance == -1 ? false : dist <= checkDistance;

            return !CanBeMovedOutofZone(item) && (inBounds || withinRange);
        }

        public void OnDestroy()
        {
            PikminManager.instance.ItemArrivalZones.Remove(this);
        }
    }
}