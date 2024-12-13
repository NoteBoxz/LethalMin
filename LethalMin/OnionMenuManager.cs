using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

namespace LethalMin
{
    public class OnionMenuManager : MonoBehaviour
    {
        [Header("Menu")]
        public GameObject mainPanel;
        private bool isMenuOpen = false;
        public static OnionMenuManager instance;
        private InputAction EscAction;
        private LeaderManager leaderManager;
        private Animator UIAnim;
        public Onion onion;
        private OnionPikmin[] pikminInOnion;
        private Coroutine addCoroutine;
        private Coroutine subtractCoroutine;
        private Button confirmButton;
        private Image BG, Edge1, Edge2;

        [Header("Type Slot")]
        public GameObject typeSlotPrefab;
        public Transform typeSlotContainer;
        private List<GameObject> typeSlotPool = new List<GameObject>();
        private const int DEFAULT_POOL_SIZE = 6;
        public GameObject singleTypeUIPrefab;
        private Dictionary<PikminType, TypeSlotUI> activeTypeSlots = new Dictionary<PikminType, TypeSlotUI>();

        GameObject singleTypeSlotObj;

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
            typeSlotPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Onion/TypeSlot.prefab");
            singleTypeUIPrefab = AssetLoader.LoadAsset<GameObject>("Assets/LethalminAssets/Onion/TypeUI.prefab");
            typeSlotPrefab.AddComponent<TypeSlotUI>();
            singleTypeUIPrefab.AddComponent<TypeSlotUI>();
            LethalMin.Logger.LogInfo($"created {instance}");
        }

        private void Start()
        {
            mainPanel = transform.Find("AcutalUI").gameObject;
            typeSlotContainer = transform.Find("AcutalUI/Scroll View/Viewport/Content");
            UIAnim = mainPanel.GetComponent<Animator>();
            InitializeTypeSlotPool();
            DisableComponents();
            confirmButton = transform.Find("AcutalUI/Confirm").GetComponent<Button>();
            BG = transform.Find("AcutalUI/BG").GetComponent<Image>();
            Edge1 = transform.Find("AcutalUI/Edge").GetComponent<Image>();
            Edge2 = transform.Find("AcutalUI/Edge2").GetComponent<Image>();
            confirmButton.onClick.AddListener(ConfirmWithdraw);
            singleTypeSlotObj = Instantiate(singleTypeUIPrefab, mainPanel.transform);

            if (leaderManager == null || onion == null)
            {
                LethalMin.Logger.LogWarning("LeaderManager or Onion not found in the scene.");
            }
        }

        private void SetupTypeSlots(OnionType onionType)
        {
            // Clear existing active type slots
            singleTypeSlotObj.SetActive(false);
            foreach (var typeSlot in activeTypeSlots.Values)
            {
                ReturnTypeSlotToPool(typeSlot.gameObject);
            }
            activeTypeSlots.Clear();

            // Check if the onion only supports one Pikmin type
            if (onionType.TypesCanHold.Length == 1)
            {
                transform.Find("AcutalUI/Scroll View").gameObject.SetActive(false);
                // Change ActualUI colors
                BG.color = new Color(onionType.TypesCanHold[0].PikminColor.r, onionType.TypesCanHold[0].PikminColor.g
                , onionType.TypesCanHold[0].PikminColor.b, 0.26f);

                Edge1.color = new Color(onionType.TypesCanHold[0].PikminColor2.r,
                onionType.TypesCanHold[0].PikminColor2.g, onionType.TypesCanHold[0].PikminColor2.b);

                Edge2.color = new Color(onionType.TypesCanHold[0].PikminColor2.r,
                onionType.TypesCanHold[0].PikminColor2.g, onionType.TypesCanHold[0].PikminColor2.b);
                // Use the single-type UI prefab
                singleTypeSlotObj.SetActive(true);
                TypeSlotUI typeSlotUI = singleTypeSlotObj.GetComponent<TypeSlotUI>();

                if (typeSlotUI == null)
                {
                    typeSlotUI = singleTypeSlotObj.AddComponent<TypeSlotUI>();
                }

                // Set up the single type slot UI
                SetupTypeSlotUI(typeSlotUI, onionType.TypesCanHold[0], true);

                activeTypeSlots[onionType.TypesCanHold[0]] = typeSlotUI;

            }
            else
            {
                transform.Find("AcutalUI/Scroll View").gameObject.SetActive(true);
                BG.color = new Color(115f / 255f, 115f / 255f, 115f / 255f, 0.26f);
                Edge1.color = new Color(135f / 255f, 135f / 255f, 135f / 255f);
                Edge2.color = new Color(135f / 255f, 135f / 255f, 135f / 255f);
                // Use the original type slot pool for multiple types
                foreach (PikminType pikminType in onionType.TypesCanHold)
                {
                    GameObject typeSlotObj = GetTypeSlotFromPool();
                    TypeSlotUI typeSlotUI = typeSlotObj.GetComponent<TypeSlotUI>();

                    // Set up the type slot UI
                    SetupTypeSlotUI(typeSlotUI, pikminType);

                    activeTypeSlots[pikminType] = typeSlotUI;
                }
            }
        }

        private void SetupTypeSlotUI(TypeSlotUI typeSlotUI, PikminType pikminType, bool IsSingle = false)
        {
            if (typeSlotUI == null)
            {
                LethalMin.Logger.LogError("TypeSlotUI is null");
                CloseMenu();
                return;
            }
            if (IsSingle)
            {
                // Set the color of the BG based on the Pikmin type
                Color bgColor = pikminType.PikminColor2;
                typeSlotUI.BG.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0.26f);
                typeSlotUI.BG2.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0.26f);
            }
            else
            {
                // Set the color of the BG based on the Pikmin type
                Color bgColor = pikminType.PikminColor;
                typeSlotUI.BG.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0.26f);
                typeSlotUI.BG2.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0.26f);
            }

            // Set up button listeners
            typeSlotUI.addButton.onClick.RemoveAllListeners();
            typeSlotUI.subtractButton.onClick.RemoveAllListeners();

            EventTrigger addTrigger = typeSlotUI.addButton.gameObject.GetComponent<EventTrigger>();
            EventTrigger subtractTrigger = typeSlotUI.subtractButton.gameObject.GetComponent<EventTrigger>();

            if (addTrigger == null) addTrigger = typeSlotUI.addButton.gameObject.AddComponent<EventTrigger>();
            if (subtractTrigger == null) subtractTrigger = typeSlotUI.subtractButton.gameObject.AddComponent<EventTrigger>();

            addTrigger.triggers.Clear();
            subtractTrigger.triggers.Clear();

            AddEventTriggerListener(addTrigger, EventTriggerType.PointerDown, (data) => { StartAddingPikmin(pikminType); });
            AddEventTriggerListener(addTrigger, EventTriggerType.PointerUp, (data) => { StopAddingPikmin(pikminType); });
            AddEventTriggerListener(addTrigger, EventTriggerType.PointerExit, (data) => { StopAddingPikmin(pikminType); });

            AddEventTriggerListener(subtractTrigger, EventTriggerType.PointerDown, (data) => { StartSubtractingPikmin(pikminType); });
            AddEventTriggerListener(subtractTrigger, EventTriggerType.PointerUp, (data) => { StopSubtractingPikmin(pikminType); });
            AddEventTriggerListener(subtractTrigger, EventTriggerType.PointerExit, (data) => { StopSubtractingPikmin(pikminType); });

            UpdateTypeSlotUI(typeSlotUI, pikminType);
        }

        private void InitializeTypeSlotPool()
        {
            for (int i = 0; i < DEFAULT_POOL_SIZE; i++)
            {
                CreateTypeSlot();
            }
        }

        private GameObject CreateTypeSlot()
        {
            GameObject typeSlot = Instantiate(typeSlotPrefab, typeSlotContainer);
            typeSlot.transform.parent = typeSlotContainer;
            typeSlot.SetActive(false);
            typeSlotPool.Add(typeSlot);
            return typeSlot;
        }

        private GameObject GetTypeSlotFromPool()
        {
            foreach (GameObject typeSlot in typeSlotPool)
            {
                if (!typeSlot.activeInHierarchy)
                {
                    typeSlot.SetActive(true);
                    return typeSlot;
                }
            }

            return CreateTypeSlot();
        }

        private void ReturnTypeSlotToPool(GameObject typeSlot)
        {
            typeSlot.SetActive(false);
        }

        private void AddEventTriggerListener(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = eventType;
            entry.callback.AddListener((data) => { action((BaseEventData)data); });
            trigger.triggers.Add(entry);
        }
        private void StartAddingPikmin(PikminType pikminType)
        {
            if (addCoroutine != null) StopCoroutine(addCoroutine);
            addCoroutine = StartCoroutine(ContinuouslyAddPikmin(pikminType));
        }

        private void StopAddingPikmin(PikminType pikminType)
        {
            if (addCoroutine != null)
            {
                StopCoroutine(addCoroutine);
                addCoroutine = null;
            }
        }

        private void StartSubtractingPikmin(PikminType pikminType)
        {
            if (subtractCoroutine != null) StopCoroutine(subtractCoroutine);
            subtractCoroutine = StartCoroutine(ContinuouslySubtractPikmin(pikminType));
        }

        private void StopSubtractingPikmin(PikminType pikminType)
        {
            if (subtractCoroutine != null)
            {
                StopCoroutine(subtractCoroutine);
                subtractCoroutine = null;
            }
        }

        private IEnumerator ContinuouslyAddPikmin(PikminType pikminType)
        {
            while (true)
            {
                AddPikmin(pikminType);
                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator ContinuouslySubtractPikmin(PikminType pikminType)
        {
            while (true)
            {
                SubtractPikmin(pikminType);
                yield return new WaitForSeconds(0.1f);
            }
        }

        public void DisableComponents()
        {
            isMenuOpen = false;
            mainPanel.SetActive(false);
        }

        public void OpenMenu(Onion curonion, LeaderManager curLeader)
        {
            if (curonion == null)
            {
                LethalMin.Logger.LogError("CurOnion Is some how ");
                return;
            }
            if (curLeader == null)
            {
                LethalMin.Logger.LogError("CurLeader is ");
                return;
            }
            onion = curonion;
            leaderManager = curLeader;
            if (mainPanel == null)
            {
                mainPanel = transform.Find("AcutalUI").gameObject;
                if (mainPanel == null)
                {
                    LethalMin.Logger.LogInfo("Somehow this UI is null");
                }
                UIAnim = mainPanel.GetComponent<Animator>();
            }
            if (WaitAnimTimer != null)
            {
                StopCoroutine(WaitAnimTimer);
            }
            mainPanel.SetActive(true);
            UIAnim.Play("UIopen");
            GetComponent<AudioSource>().PlayOneShot(LethalMin.OnionMeunOpen);
            isMenuOpen = true;
            SetupTypeSlots(curonion.type);
            if (onion.type.TypesCanHold.Length == 1)
            {

            }
            UpdateUIText();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        Coroutine WaitAnimTimer;
        public void CloseMenu()
        {
            if (isMenuOpen == false) { return; }
            if (WaitAnimTimer != null)
            {
                StopCoroutine(WaitAnimTimer);
            }
            UIAnim.Play("UIClose");
            GetComponent<AudioSource>().PlayOneShot(LethalMin.OnionMeunClose);
            WaitAnimTimer = StartCoroutine(DisableMeun());
            isMenuOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        IEnumerator DisableMeun()
        {
            yield return new WaitForSeconds(0.45f);
            mainPanel.SetActive(false);
        }
        void Update()
        {
            if (isMenuOpen && leaderManager != null && leaderManager.Controller.quickMenuManager.isMenuOpen)
            {
                CloseMenu();
            }
            if (isMenuOpen && leaderManager != null && leaderManager.Controller.isPlayerDead)
            {
                CloseMenu();
            }
        }

        private Dictionary<PikminType, int> withdrawAmounts = new Dictionary<PikminType, int>();

        private void AddPikmin(PikminType pikminType)
        {
            int pikminInOnion = onion.GetPikminInOnionByType(pikminType);
            if (!withdrawAmounts.ContainsKey(pikminType)) withdrawAmounts[pikminType] = 0;

            if (withdrawAmounts[pikminType] < pikminInOnion && GetTotalWithdrawAmount() + FindObjectsOfType<PikminAI>().Length <= LethalMin.MaxMinValue)
            {
                withdrawAmounts[pikminType]++;
                UpdateUIText();
            }
        }

        private void SubtractPikmin(PikminType pikminType)
        {
            int pikminInSquad = leaderManager.GetFollowingPikminByType(pikminType).Count;
            if (!withdrawAmounts.ContainsKey(pikminType)) withdrawAmounts[pikminType] = 0;

            if (withdrawAmounts[pikminType] > -pikminInSquad)
            {
                withdrawAmounts[pikminType]--;
                UpdateUIText();
            }
        }

        private int GetTotalWithdrawAmount()
        {
            return withdrawAmounts.Values.Sum();
        }

        private void UpdateUIText()
        {
            foreach (var kvp in activeTypeSlots)
            {
                PikminType pikminType = kvp.Key;
                TypeSlotUI typeSlotUI = kvp.Value;
                UpdateTypeSlotUI(typeSlotUI, pikminType);
            }
        }

        private void UpdateTypeSlotUI(TypeSlotUI typeSlotUI, PikminType pikminType)
        {
            int pikminInSquad = leaderManager.GetFollowingPikminByType(pikminType).Count;
            int pikminInOnion = onion.GetPikminInOnionByType(pikminType);
            int totalPikmin = pikminInOnion + pikminInSquad;
            int withdrawAmount = GetWithdrawAmount(pikminType);

            typeSlotUI.inTotalTxt.text = $"{totalPikmin}";
            typeSlotUI.inOnionTxt.text = $"{pikminInOnion - withdrawAmount}";
            typeSlotUI.inSquadTxt.text = $"{pikminInSquad + withdrawAmount}";
            typeSlotUI.withdrawTxt.text = $"{withdrawAmount}";
            typeSlotUI.promptTxt.text = $"{pikminType.PikminName}";

            typeSlotUI.addButton.interactable = totalPikmin == 0 || (withdrawAmount < pikminInOnion) || (withdrawAmount <= LethalMin.MaxMinValue);
            typeSlotUI.subtractButton.interactable = totalPikmin == 0 || (withdrawAmount > -pikminInSquad);
        }


        private int GetWithdrawAmount(PikminType pikminType)
        {
            if (withdrawAmounts.TryGetValue(pikminType, out int amount))
            {
                return amount;
            }
            return 0;
        }

        private void ConfirmWithdraw()
        {
            bool anyChanges = false;

            foreach (var kvp in withdrawAmounts)
            {
                PikminType pikminType = kvp.Key;
                int amount = kvp.Value;

                if (amount != 0)
                {
                    anyChanges = true;
                    int pikminInSquad = leaderManager.GetFollowingPikminByType(pikminType).Count;
                    int pikminInOnion = onion.GetPikminInOnionByType(pikminType);

                    if (amount > 0)
                    {
                        // Withdraw Pikmin from onion to squad
                        int toWithdraw = Mathf.Min(amount, pikminInOnion);
                        Vector3 spawnPosition = onion.transform.Find("SpawnPos") != null
                             onion.transform.Find("SpawnPos").position
                            : leaderManager.transform.position;

                        onion.CreatePikminServerRpc(toWithdraw, pikminType.PikminTypeID, spawnPosition, new NetworkObjectReference(leaderManager.Controller.NetworkObject));
                    }
                    else if (amount < 0)
                    {
                        // Return Pikmin from squad to onion
                        int toReturn = Mathf.Min(-amount, pikminInSquad);
                        PikminData[] pikminToReturn = leaderManager.GetFollowingPikminByType(pikminType)
        .OrderBy(p => p.GrowStage)
        .Take(toReturn)
        .Select(p => new PikminData { GrowStage = p.GrowStage, NetworkObjectId = p.NetworkObjectId, PikminTypeID = pikminType.PikminTypeID })
        .ToArray();

                        onion.ReturnPikminToOnionServerRpc(pikminToReturn, new NetworkObjectReference(leaderManager.NetworkObject));
                    }
                }
            }

            if (anyChanges)
            {
                withdrawAmounts.Clear();
                CloseMenu();
            }
            else
            {
                CloseMenu();
            }
        }
    }
}