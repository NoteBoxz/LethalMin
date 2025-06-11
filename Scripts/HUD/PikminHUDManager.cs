using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin.Pikmin;
using LethalMin.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalMin.HUD
{
    public class PikminHUDManager : MonoBehaviour
    {
        public HUDElement element = null!;
        public static PikminHUDManager instance = null!;
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

        public enum HUDLayoutPresets
        {
            Default,
            Legacy,
            Simplifyed,
            VRHands,
            VRFace
        }

        public PikminHUDElement FrameContainer = null!;
        public PikminFrame SelectedNext = null!, SelectedPrev = null!, Selected = null!;
        public Dictionary<PikminType, int> TypesWithCounts = new Dictionary<PikminType, int>();

        // Cache variables
        private Dictionary<PikminType, int> _cachedTypesWithCounts = new Dictionary<PikminType, int>();
        private Dictionary<PikminType, bool> _cachedUnselectableTypes = new Dictionary<PikminType, bool>();
        private List<PikminType> _cachedTypes = new List<PikminType>();
        private int _lastSquadCount = -1;
        public bool _cacheNeedsRefresh = true;


        public GameObject InstanceDanger = null!;
        public TMP_Text DangerCount = null!;
        public Image InDangerIcon = null!;
        public Image LeaveingEarlyIcon = null!;
        public Sprite WarningIcon = null!, SafeIcon = null!;
        public float MaxFrameContainerAlpha = 1f;

        float UpdateInDangerTimer = 0.1f;
        float ChangesTimer = 0.1f;
        public Coroutine? HideSelectedCoroutine = null;

        public void SetLayout(HUDLayoutPresets layout)
        {
            switch (layout)
            {
                case HUDLayoutPresets.Default:
                    LethalMin.PikminSelectionPosition.StringEntry.BoxedValue = LethalMin.PikminSelectionPosition.ConvertToString(new Vector3(0, -120, 28));
                    LethalMin.PikminSelectionRotation.StringEntry.BoxedValue = LethalMin.PikminSelectionRotation.ConvertToString(new Vector3(0f, 0f, 0f));
                    LethalMin.PikminSelectionScale.Entry.BoxedValue = 1f;
                    LethalMin.EnableCurSlot.Entry.BoxedValue = true;
                    LethalMin.EnableNextSlot.Entry.BoxedValue = true;
                    LethalMin.EnablePreviousSlot.Entry.BoxedValue = true;
                    LethalMin.PikminSelectionAlpha.Entry.BoxedValue = 1f;
                    LethalMin.PikminCounterPosition.StringEntry.BoxedValue = LethalMin.PikminCounterPosition.ConvertToString(new Vector3(8, -225, 0));
                    LethalMin.PikminCounterRotation.StringEntry.BoxedValue = LethalMin.PikminCounterRotation.ConvertToString(new Vector3(0f, 0f, 0f));
                    LethalMin.PikminCounterScale.Entry.BoxedValue = 1f;
                    LethalMin.EnableInExistanceCounter.Entry.BoxedValue = true;
                    LethalMin.EnableInFieldCounter.Entry.BoxedValue = true;
                    LethalMin.EnableInSquadCounter.Entry.BoxedValue = true;
                    LethalMin.PikminCounterAlphaActive.Entry.BoxedValue = 1f;
                    LethalMin.PikminCounterAlphaIdle.Entry.BoxedValue = 0.15f;
                    LethalMin.HideSelectedWhenScanNotifcation.Entry.BoxedValue = true;
                    break;
                case HUDLayoutPresets.Legacy:
                    LethalMin.PikminSelectionPosition.StringEntry.BoxedValue = LethalMin.PikminSelectionPosition.ConvertToString(new Vector3(300 - 160, 28));
                    LethalMin.PikminSelectionRotation.StringEntry.BoxedValue = LethalMin.PikminSelectionRotation.ConvertToString(new Vector3(0f, 15f, 0f));
                    LethalMin.PikminSelectionScale.Entry.BoxedValue = 1f;
                    LethalMin.EnableCurSlot.Entry.BoxedValue = true;
                    LethalMin.EnableNextSlot.Entry.BoxedValue = true;
                    LethalMin.EnablePreviousSlot.Entry.BoxedValue = true;
                    LethalMin.PikminSelectionAlpha.Entry.BoxedValue = 1f;
                    LethalMin.PikminCounterPosition.StringEntry.BoxedValue = LethalMin.PikminCounterPosition.ConvertToString(new Vector3(300, -210, 28));
                    LethalMin.PikminCounterRotation.StringEntry.BoxedValue = LethalMin.PikminCounterRotation.ConvertToString(new Vector3(0f, 15f, 0f));
                    LethalMin.PikminCounterScale.Entry.BoxedValue = 1f;
                    LethalMin.EnableInExistanceCounter.Entry.BoxedValue = true;
                    LethalMin.EnableInFieldCounter.Entry.BoxedValue = true;
                    LethalMin.EnableInSquadCounter.Entry.BoxedValue = true;
                    LethalMin.PikminCounterAlphaActive.Entry.BoxedValue = 1f;
                    LethalMin.PikminCounterAlphaIdle.Entry.BoxedValue = 0.5f;
                    LethalMin.HideSelectedWhenScanNotifcation.Entry.BoxedValue = false;
                    break;
                case HUDLayoutPresets.Simplifyed:
                    LethalMin.PikminSelectionPosition.StringEntry.BoxedValue = LethalMin.PikminSelectionPosition.ConvertToString(new Vector3(0, -120, 28));
                    LethalMin.PikminSelectionRotation.StringEntry.BoxedValue = LethalMin.PikminSelectionRotation.ConvertToString(new Vector3(0f, 0f, 0f));
                    LethalMin.PikminSelectionScale.Entry.BoxedValue = 1f;
                    LethalMin.EnableCurSlot.Entry.BoxedValue = true;
                    LethalMin.EnableNextSlot.Entry.BoxedValue = false;
                    LethalMin.EnablePreviousSlot.Entry.BoxedValue = false;
                    LethalMin.PikminSelectionAlpha.Entry.BoxedValue = 1f;
                    LethalMin.PikminCounterPosition.StringEntry.BoxedValue = LethalMin.PikminCounterPosition.ConvertToString(new Vector3(0, -130, 28));
                    LethalMin.PikminCounterRotation.StringEntry.BoxedValue = LethalMin.PikminCounterRotation.ConvertToString(new Vector3(0f, 0f, 0f));
                    LethalMin.PikminCounterScale.Entry.BoxedValue = 1f;
                    LethalMin.EnableInExistanceCounter.Entry.BoxedValue = false;
                    LethalMin.EnableInFieldCounter.Entry.BoxedValue = false;
                    LethalMin.EnableInSquadCounter.Entry.BoxedValue = true;
                    LethalMin.PikminCounterAlphaActive.Entry.BoxedValue = 1f;
                    LethalMin.PikminCounterAlphaIdle.Entry.BoxedValue = 0.15f;
                    LethalMin.HideSelectedWhenScanNotifcation.Entry.BoxedValue = true;
                    break;
                case HUDLayoutPresets.VRFace:
                    LethalMin.PikminSelectionPosition.StringEntry.BoxedValue = LethalMin.PikminSelectionPosition.ConvertToString(new Vector3(-78, -5, 35));
                    LethalMin.PikminSelectionRotation.StringEntry.BoxedValue = LethalMin.PikminSelectionRotation.ConvertToString(new Vector3(0f, 0f, 0f));
                    LethalMin.PikminSelectionScale.Entry.BoxedValue = 1f;
                    LethalMin.EnableCurSlot.Entry.BoxedValue = true;
                    LethalMin.EnableNextSlot.Entry.BoxedValue = true;
                    LethalMin.EnablePreviousSlot.Entry.BoxedValue = true;
                    LethalMin.PikminSelectionAlpha.Entry.BoxedValue = 1f;
                    LethalMin.PikminCounterPosition.StringEntry.BoxedValue = LethalMin.PikminCounterPosition.ConvertToString(new Vector3(-78, -141, 35));
                    LethalMin.PikminCounterRotation.StringEntry.BoxedValue = LethalMin.PikminCounterRotation.ConvertToString(new Vector3(0f, 0f, 0f));
                    LethalMin.PikminCounterScale.Entry.BoxedValue = 1f;
                    LethalMin.EnableInExistanceCounter.Entry.BoxedValue = true;
                    LethalMin.EnableInFieldCounter.Entry.BoxedValue = true;
                    LethalMin.EnableInSquadCounter.Entry.BoxedValue = true;
                    LethalMin.PikminCounterAlphaActive.Entry.BoxedValue = 1f;
                    LethalMin.PikminCounterAlphaIdle.Entry.BoxedValue = 0.15f;
                    LethalMin.HideSelectedWhenScanNotifcation.Entry.BoxedValue = true;
                    break;
                case HUDLayoutPresets.VRHands:
                    LethalMin.PikminSelectionPosition.StringEntry.BoxedValue = LethalMin.PikminSelectionPosition.ConvertToString(new Vector3(-59, 27, 0));
                    LethalMin.PikminSelectionRotation.StringEntry.BoxedValue = LethalMin.PikminSelectionRotation.ConvertToString(new Vector3(0f, 0f, 0f));
                    LethalMin.PikminSelectionScale.Entry.BoxedValue = 1f;
                    LethalMin.EnableCurSlot.Entry.BoxedValue = true;
                    LethalMin.EnableNextSlot.Entry.BoxedValue = true;
                    LethalMin.EnablePreviousSlot.Entry.BoxedValue = true;
                    LethalMin.PikminSelectionAlpha.Entry.BoxedValue = 1f;
                    LethalMin.PikminCounterPosition.StringEntry.BoxedValue = LethalMin.PikminCounterPosition.ConvertToString(new Vector3(-59, -78, 0));
                    LethalMin.PikminCounterRotation.StringEntry.BoxedValue = LethalMin.PikminCounterRotation.ConvertToString(new Vector3(0f, 0f, 0f));
                    LethalMin.PikminCounterScale.Entry.BoxedValue = 1f;
                    LethalMin.EnableInExistanceCounter.Entry.BoxedValue = true;
                    LethalMin.EnableInFieldCounter.Entry.BoxedValue = true;
                    LethalMin.EnableInSquadCounter.Entry.BoxedValue = true;
                    LethalMin.PikminCounterAlphaActive.Entry.BoxedValue = 1f;
                    LethalMin.PikminCounterAlphaIdle.Entry.BoxedValue = 0.15f;
                    LethalMin.HideSelectedWhenScanNotifcation.Entry.BoxedValue = false;
                    break;
            }
        }

        public void Start()
        {
            if (!LethalMin.InVRMode)
            {
                AutoFixVRPreset();
            }
        }
        private void AutoFixVRPreset()
        {
            // Skip if auto-preset is disabled
            if (!LethalMin.AutoSetHudVRPreset)
                return;

            // Skip if the correct preset is already set
            bool isIncorrectPresetAlreadySet =
            LethalMin.HUDPreset.InternalValue == HUDLayoutPresets.VRFace || LethalMin.HUDPreset.InternalValue == HUDLayoutPresets.VRHands;

            if (!isIncorrectPresetAlreadySet)
                return;

            LethalMin.HUDPreset.Entry.BoxedValue = HUDLayoutPresets.Default;
            SetLayout(HUDLayoutPresets.Default);
        }
        void Update()
        {
            if (StartOfRound.Instance.inShipPhase)
            {
                FrameContainer.targetAlpha = 0;
            }
        }

        void LateUpdate()
        {
            CheckForSquadChanges();
            UpdateFrames();
            if (!LethalMin.DontUpdateHudConfigs)
            {
                UpdateHudPositions();
            }
            InstanceDanger.SetActive(LeaveingEarlyIcon.enabled);

            if (InstanceDanger == null || LeaveingEarlyIcon.enabled == false)
            {
                return;
            }

            if (UpdateInDangerTimer >= 0f)
            {
                UpdateInDangerTimer -= Time.deltaTime;
            }
            else
            {
                UpdateInDanger();
                UpdateInDangerTimer = 0.1f;
            }
        }

        public void UpdateHudPositions()
        {
            float selectionScaleVal = LethalMin.PikminSelectionScale.InternalValue;
            Vector3 selectionScale = new Vector3(selectionScaleVal, selectionScaleVal, selectionScaleVal);
            FrameContainer.transform.localPosition = LethalMin.PikminSelectionPosition.InternalValue;
            if (!LethalMin.InVRMode)
                FrameContainer.transform.rotation = Quaternion.Euler(LethalMin.PikminSelectionRotation.InternalValue);
            FrameContainer.transform.localScale = selectionScale;
            MaxFrameContainerAlpha = LethalMin.PikminSelectionAlpha.InternalValue;
            Selected.gameObject.SetActive(LethalMin.EnableCurSlot.InternalValue);
            SelectedNext.gameObject.SetActive(LethalMin.EnableNextSlot.InternalValue);
            SelectedPrev.gameObject.SetActive(LethalMin.EnablePreviousSlot.InternalValue);
        }

        public void UpdateInDanger()
        {
            int count = 0;
            foreach (PikminAI pikmin in PikminManager.instance.PikminAIs)
            {
                if (pikmin.IsWildPikmin || pikmin.IsDeadOrDying || !PikChecks.DoesPikminHaveRegisteredOnion(pikmin))
                {
                    continue;
                }

                if (!PikminManager.instance.IsPikminInSafetyRange(pikmin))
                {
                    count++;
                }
            }
            DangerCount.text = count.ToString();
            if (count > 0)
            {
                InDangerIcon.sprite = WarningIcon;
            }
            else
            {
                InDangerIcon.sprite = SafeIcon;
            }
            InDangerIcon.transform.position = LethalMin.IsDependencyLoaded("NoteBoxz.PikminStyledClock") ?
            new Vector3(-21.4066f, -58.7827f, 12.1124f) : new Vector3(-21.3226f, -58.5506f, 12.022f);
        }

        private void CheckForSquadChanges()
        {
            if (PikminManager.instance == null || PikminManager.instance.LocalLeader == null)
            {
                return;
            }

            Leader leader = PikminManager.instance.LocalLeader;

            // Check if squad count changed
            if (_lastSquadCount != leader.PikminInSquad.Count)
            {
                _lastSquadCount = leader.PikminInSquad.Count;
                _cacheNeedsRefresh = true;
                return;
            }

            if (ChangesTimer >= 0f)
            {
                ChangesTimer -= Time.deltaTime;
                return;
            }
            else
            {
                ChangesTimer = 0.1f;
            }

            // Check for any state changes in pikmin (unselectable status)
            foreach (var pikmin in leader.PikminInSquad)
            {
                if (pikmin.UnselectableChanged)
                {
                    _cacheNeedsRefresh = true;
                    pikmin.UnselectableChanged = false;
                    return;
                }
            }
        }

        public void UpdateFrames()
        {
            if (PikminManager.instance == null || PikminManager.instance.LocalLeader == null)
            {
                return;
            }

            Leader leader = PikminManager.instance.LocalLeader;
            if (leader.PikminInSquad.Count == 0 || StartOfRound.Instance.inShipPhase)
            {
                FrameContainer.targetAlpha = 0;
                SelectedNext.type = null!;
                Selected.type = null!;
                SelectedPrev.type = null!;
                _lastSquadCount = 0;
                _cacheNeedsRefresh = true;
                return;
            }


            if (HideSelectedCoroutine == null)
                FrameContainer.targetAlpha = MaxFrameContainerAlpha;



            // Only recalculate if cache needs refresh
            if (_cacheNeedsRefresh)
            {
                _cachedTypesWithCounts = leader.GetPikminTypesInSquadWithCount();
                _cachedTypes = leader.GetPikminTypesInSquad();

                // Cache unselectable status for each type
                _cachedUnselectableTypes.Clear();
                foreach (var kvp in _cachedTypesWithCounts)
                {
                    PikminType type = kvp.Key;
                    _cachedUnselectableTypes[type] = leader.IsAllOfTypeUnslectable(type, _cachedTypesWithCounts);
                }

                //LethalMin.Logger.LogInfo($"(HudCache) {leader.Controller.playerUsername} Updated cache");

                _cacheNeedsRefresh = false;
            }

            SelectedNext.type = leader.GetNextType(_cachedTypes);
            Selected.type = leader.GetSelectedType(_cachedTypes);
            SelectedPrev.type = leader.GetPreviousType(_cachedTypes);

            TypesWithCounts = _cachedTypesWithCounts;

            Selected.Unselectable = Selected.type != null && _cachedUnselectableTypes.ContainsKey(Selected.type) ?
                _cachedUnselectableTypes[Selected.type] : false;

            SelectedPrev.Unselectable = SelectedPrev.type != null && _cachedUnselectableTypes.ContainsKey(SelectedPrev.type) ?
                _cachedUnselectableTypes[SelectedPrev.type] : false;

            SelectedNext.Unselectable = SelectedNext.type != null && _cachedUnselectableTypes.ContainsKey(SelectedNext.type) ?
                _cachedUnselectableTypes[SelectedNext.type] : false;

            SelectedNext.Count = SelectedNext.type != null ? TypesWithCounts[SelectedNext.type] : 0;
            Selected.Count = Selected.type != null ? TypesWithCounts[Selected.type] : 0;
            SelectedPrev.Count = SelectedPrev.type != null ? TypesWithCounts[SelectedPrev.type] : 0;
        }

        public IEnumerator TemporarlyHideSelectedPikmin(float seconds)
        {
            FrameContainer.targetAlpha = 0;
            yield return new WaitForSeconds(seconds);
            FrameContainer.targetAlpha = MaxFrameContainerAlpha;
            HideSelectedCoroutine = null;
        }
    }
}