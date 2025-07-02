using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using LethalMin.Pikmin;
using LethalMin.Utils;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LethalMin.HUD
{
    public class OnionHUDManager : MonoBehaviour
    {
        public HUDElement element = null!;
        public static OnionHUDManager instance = null!;
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public bool IsMenuOpen = false;
        public Transform SlotContainer = null!, SingleContainer = null!;
        public GameObject ActualUI = null!;
        public Image BG = null!, Edge1 = null!, Edge2 = null!;
        public Animator Anim = null!;
        public Onion currentOnion = null!;
        public List<OnionHUDSlot> slots = new List<OnionHUDSlot>();
        public AudioSource audio = null!;
        public AudioClip OpenAC = null!, CloseAC = null!, DenyUpAC = null!, DenyDownAC = null!;
        public AudioClip UpAC = null!, DownAC = null!, ConfirmAC = null!;
        public InputAction TenWithdrawAction = null!;

        public void Start()
        {
            ActualUI.SetActive(false);

            if (LethalMin.UseInputUtils)
            {
                TenWithdrawAction = LethalMin.InputClassInstace.OnionHudSpeed;
                TenWithdrawAction.Enable();
            }
            else
            {
                TenWithdrawAction = new InputAction(LethalMin.OnionHudSpeedAction);
                TenWithdrawAction.AddBinding(LethalMin.OnionHudSpeedAction);
                TenWithdrawAction.Enable();
            }
        }

        public void OpenMenu()
        {
            if (IsMenuOpen)
            {
                return;
            }
            IsMenuOpen = true;
            ActualUI.SetActive(true);
            Anim.Play("UIopen");
            audio.PlayOneShot(OpenAC);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseMenu()
        {
            if (!IsMenuOpen)
            {
                return;
            }
            IsMenuOpen = false;
            Anim.Play("UIClose");
            audio.PlayOneShot(CloseAC);
            if (!StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen && HUDManager.Instance.currentSpecialMenu == SpecialHUDMenu.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            foreach (OnionHUDSlot slot in slots)
            {
                Destroy(slot.gameObject);
            }
            if (currentOnion != null)
            {
                currentOnion.ResetWithDrawAmmount();
            }
            slots.Clear();
            currentOnion = null!;
        }

        public void Confirm()
        {
            LethalMin.Logger.LogDebug($"Confirmed Button Pressed, sending withdraw request for {currentOnion?.TypesToExchange.Count} types to server.");
            if (currentOnion != null && currentOnion.TypesToExchange.Values.Count > 0)
            {
                currentOnion.WithdrawTypesServerRpc(currentOnion.NetworkObject, StartOfRound.Instance.localPlayerController.OwnerClientId,
                currentOnion.TypesToExchange.Keys.Select(p => p.PikminTypeID).ToArray(), currentOnion.TypesToExchange.Values.ToArray());
            }
            CloseMenu();
        }

        public void Close()
        {
            LethalMin.Logger.LogDebug("Close Button Pressed, closing Onion HUD.");
            CloseMenu();
        }

        void Update()
        {
            if (IsMenuOpen && !ActualUI.activeSelf)
            {
                ActualUI.SetActive(true);
            }
            if (IsMenuOpen && (StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen || HUDManager.Instance.hudHidden || HUDManager.Instance.currentSpecialMenu != SpecialHUDMenu.None))
            {
                LethalMin.Logger.LogInfo($"autoCloseReason: {StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen} {HUDManager.Instance.hudHidden} {HUDManager.Instance.currentSpecialMenu}");
                CloseMenu();
            }
        }


        public void SetCurrentOnion(Onion onion)
        {
            currentOnion = onion;
            if (onion.TryGetComponent(out BaseOnion Bonion))
            {
                OpenAC = Bonion.pack.MenuOpenSound;
                CloseAC = Bonion.pack.MenuCloseSound;
                DenyUpAC = Bonion.pack.MenuDenyUpSound;
                DenyDownAC = Bonion.pack.MenuDenyDownSound;
                UpAC = Bonion.pack.MenuUpSound;
                DownAC = Bonion.pack.MenuDownSound;
                ConfirmAC = Bonion.pack.MenuConfirmSound;
            }
            else
            {
                OpenAC = LethalMin.DefaultOnionSoundPack.MenuOpenSound;
                CloseAC = LethalMin.DefaultOnionSoundPack.MenuCloseSound;
                DenyUpAC = LethalMin.DefaultOnionSoundPack.MenuDenyUpSound;
                DenyDownAC = LethalMin.DefaultOnionSoundPack.MenuDenyDownSound;
                UpAC = LethalMin.DefaultOnionSoundPack.MenuUpSound;
                DownAC = LethalMin.DefaultOnionSoundPack.MenuDownSound;
                ConfirmAC = LethalMin.DefaultOnionSoundPack.MenuConfirmSound;
            }
            int slotsToCreate = onion.onionType.TypesCanHold.Length;
            if (slotsToCreate == 0)
            {
                LethalMin.Logger.LogError($"Onion {onion.onionType.TypeName} has no types it can hold, cannot set Onion HUD!");
                return;
            }
            if (slots.Count > 0)
            {
                foreach (OnionHUDSlot slot in slots)
                {
                    if (slot == null)
                    {
                        continue;
                    }
                    Destroy(slot.gameObject);
                }
            }
            slots.Clear();
            if (slotsToCreate == 1)
            {
                GameObject go = LethalMin.SingleOnionHUDSlotPrefab;
                GameObject Instance = Instantiate(go, SingleContainer);
                OnionHUDSlot slot = Instance.GetComponent<OnionHUDSlot>();
                slot.type = onion.onionType.TypesCanHold[0];
                Color Pcolor = onion.onionType.TypesCanHold[0].PikminSecondaryColor;
                BG.color = new Color(Pcolor.r, Pcolor.g, Pcolor.b, 0.26f);
                Edge1.color = onion.onionType.TypesCanHold[0].PikminPrimaryColor;
                Edge2.color = onion.onionType.TypesCanHold[0].PikminPrimaryColor;
                slots.Add(slot);
            }
            else
            {
                for (int i = 0; i < slotsToCreate; i++)
                {
                    GameObject go = LethalMin.OnionHUDSlotPrefab;
                    GameObject Instance = Instantiate(go, SlotContainer);
                    OnionHUDSlot slot = Instance.GetComponent<OnionHUDSlot>();
                    slot.type = onion.onionType.TypesCanHold[i];
                    BG.color = new Color(0.45f, 0.45f, 0.45f, 0.26f);
                    Edge1.color = new Color(0.52f, 0.52f, 0.52f);
                    Edge2.color = new Color(0.52f, 0.52f, 0.52f);
                    slots.Add(slot);
                }
            }
        }
    }
}