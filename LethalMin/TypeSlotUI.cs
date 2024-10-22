using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LethalMin;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

namespace LethalMin
{
    public class TypeSlotUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI inTotalTxt;
        public TextMeshProUGUI inOnionTxt;
        public TextMeshProUGUI inSquadTxt;
        public TextMeshProUGUI withdrawTxt;
        public TextMeshProUGUI promptTxt;
        public Button addButton;
        public Button subtractButton;
        public Image BG, BG2;

        void Awake()
        {
            inTotalTxt = transform.Find("InTotalTXT").GetComponent<TextMeshProUGUI>();
            inOnionTxt = transform.Find("InOnionTXT").GetComponent<TextMeshProUGUI>();
            inSquadTxt = transform.Find("InSquadTXT").GetComponent<TextMeshProUGUI>();
            promptTxt = transform.Find("Prompt").GetComponent<TextMeshProUGUI>();
            withdrawTxt = transform.Find("UnusedDueToUgly/WithdrawTXT").GetComponent<TextMeshProUGUI>();
            addButton = transform.Find("Add").GetComponent<Button>();
            subtractButton = transform.Find("Subtract").GetComponent<Button>();
            BG = transform.Find("Image").GetComponent<Image>();
            BG2 = transform.Find("Image (1)").GetComponent<Image>();
        }
    }
}