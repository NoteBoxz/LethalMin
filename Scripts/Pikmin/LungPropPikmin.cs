using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LethalMin.Pikmin
{
    public class LungPropPikmin : MonoBehaviour
    {
        public void Start()
        {
            gameObject.GetComponentInChildren<PikminItem>().OnItemGrabbed.AddListener(lung => PikminDisconnect(lung));
        }

        public void PikminDisconnect(PikminItem itm)
        {
            LungProp itmLung = itm.ItemScript.GetComponent<LungProp>();
            if (itmLung == null)
            {
                LethalMin.Logger.LogError($"({itm.gameObject.name})LungPropPatch.PikminDisconnect: LungProp not found");
                return;
            }
            if (itmLung.isLungDocked)
            {
                itmLung.isLungDocked = false;
                if (itmLung.disconnectAnimation != null)
                {
                    itmLung.StopCoroutine(itmLung.disconnectAnimation);
                }
                itmLung.disconnectAnimation = itmLung.StartCoroutine(itmLung.DisconnectFromMachinery());
            }
            if (itmLung.isLungDockedInElevator)
            {
                itmLung.isLungDockedInElevator = false;
                itmLung.gameObject.GetComponent<AudioSource>().PlayOneShot(itmLung.disconnectSFX);
                _ = itmLung.isLungPowered;
            }
        }
    }
}
