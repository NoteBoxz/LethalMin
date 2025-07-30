using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin.Pikmin;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalMin.HUD
{
    public class PikminCounter : MonoBehaviour
    {
        public GameObject ExistanceContainer = null!, FieldContainer = null!, SquadContainer = null!;
        public TMP_Text Existance = null!, Field = null!, Squad = null!;
        public PikminHUDElement element = null!;

        public int previousExistanceCount = -1;
        public int previousPikminCount = -1;
        public int previousSquadCount = -1;
        private Coroutine alphaCoroutine = null!;
        public int currentExistanceCount = 0;
        public int currentPikminCount = 0;
        public int currentSquadCount = 0;

        public void Update()
        {
            if (PikminManager.instance == null)
                return;
            if (PikminManager.instance.LocalLeader == null)
            {
                return;
            }
            currentExistanceCount = PikminManager.instance.PikminAICounter.Count;
            foreach (var onion in PikminManager.instance.Onions)
            {
                currentExistanceCount += onion.PikminInOnion.Count;
            }
            currentPikminCount = PikminManager.instance.PikminAICounter.Count;
            currentSquadCount = PikminManager.instance.LocalLeader.PikminInSquad.Count;

            if (StartOfRound.Instance.inShipPhase || currentExistanceCount == 0)
            {
                element.targetAlpha = 0;
                return;
            }

            if (!LethalMin.DontUpdateHudConfigs)
            {
                UpdateHudPositions();
            }

            if (currentPikminCount != previousPikminCount
            || currentSquadCount != previousSquadCount
            || currentExistanceCount != previousExistanceCount)
            {
                if (alphaCoroutine != null)
                {
                    StopCoroutine(alphaCoroutine);
                }
                alphaCoroutine = StartCoroutine(FlashAlpha());

                previousExistanceCount = currentExistanceCount;
                previousPikminCount = currentPikminCount;
                previousSquadCount = currentSquadCount;
            }

            Existance.text = currentExistanceCount.ToString();
            Field.text = currentPikminCount.ToString();
            Squad.text = currentSquadCount.ToString();
        }

        public void UpdateHudPositions()
        {
            float counterScaleVal = LethalMin.PikminCounterScale.InternalValue;
            Vector3 counterScale = new Vector3(counterScaleVal, counterScaleVal, counterScaleVal);
            transform.localPosition = LethalMin.PikminCounterPosition.InternalValue;
            if (!LethalMin.InVRMode)
                transform.rotation = Quaternion.Euler(LethalMin.PikminCounterRotation.InternalValue);
            transform.localScale = counterScale;
            FieldContainer.SetActive(LethalMin.EnableInFieldCounter.InternalValue);
            ExistanceContainer.SetActive(LethalMin.EnableInExistanceCounter.InternalValue);
            SquadContainer.SetActive(LethalMin.EnableInSquadCounter.InternalValue);
        }

        private IEnumerator FlashAlpha()
        {
            element.targetAlpha = LethalMin.PikminCounterAlphaActive.InternalValue;
            yield return new WaitForSeconds(2f);
            element.targetAlpha = LethalMin.PikminCounterAlphaIdle.InternalValue;
            alphaCoroutine = null!;
        }
    }
}