using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using LethalMin.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalMin.HUD
{
    public class PikminEndOfGameStatUIElements : MonoBehaviour
    {
        public PikminRaisedTextBox[] PikminRaisedTexts = new PikminRaisedTextBox[4];
        public TMP_Text KilledText = null!, LeftBehindText = null!;
        public TMP_Text LeftText = null!;
        public GameObject Greater = null!, Same = null!, Less = null!;

        public void FillEndGameStats()
        {
            foreach (Onion onion in PikminManager.instance.Onions)
            {
                PikminManager.instance.EndOfGameStats.PikminLeft += onion.PikminInOnion.Count;
            }

            for (int i = 0; i < PikminRaisedTexts.Length; i++)
            {
                PikminRaisedTextBox text = PikminRaisedTexts[i];
                if (StartOfRound.Instance.ClientPlayerList.Count > 4 && LethalMin.HideResultsWhenMoreThanFour)
                {
                    text.gameObject.SetActive(false);
                    continue;
                }
                Leader lead = PikminManager.instance.EndOfGameStats.PikminRaised.Keys.ElementAt(i);
                text.gameObject.SetActive(PikChecks.IsPlayerConnected(lead.Controller));
                text.text.text = PikminManager.instance.EndOfGameStats.PikminRaised[lead].ToString();
            }

            KilledText.text = PikminManager.instance.EndOfGameStats.PikminKilled.ToString();
            LeftBehindText.text = PikminManager.instance.EndOfGameStats.PikminLeftBehind.ToString();

            int left = PikminManager.instance.EndOfGameStats.PikminLeft;
            int leftLastTime = PikminManager.instance.EndOfGameStats.PikminLeftLastRound;

            LeftText.text = left.ToString();

            if (left > leftLastTime)
            {
                Greater.SetActive(true);
                Same.SetActive(false);
                Less.SetActive(false);
            }
            else if (left < leftLastTime)
            {
                Greater.SetActive(false);
                Same.SetActive(false);
                Less.SetActive(true);
            }
            else
            {
                Greater.SetActive(false);
                Same.SetActive(true);
                Less.SetActive(false);
            }
        }
    }
}