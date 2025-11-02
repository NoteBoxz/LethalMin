using System.Collections;
using System.Collections.Generic;
using LethalMin.Pikmin;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalMin.HUD
{
    public class PikminFrame : MonoBehaviour
    {
        public PikminType? type = null!;
        public Image Border = null!;
        public Image Icon = null!;
        public TMP_Text Counter = null!;
        public int Count;
        public bool Unselectable = false;

        void Update()
        {
            if (type == null)
            {
                Border.color = Color.gray;
                Icon.color = Color.white;
                Icon.sprite = null;
                Icon.enabled = false;
                Counter.text = "0";
                return;
            }
            if (!Unselectable)
            {
                Border.color = type.PikminSecondaryColor;
                Icon.sprite = type.PikminIcon;
                Icon.color = Color.white;
                Icon.enabled = true;
                Counter.text = Count.ToString();
            }
            else
            {
                Color GrayedOut = new Color(0.25f, 0.25f, 0.25f, 0.75f);
                Border.color = type.PikminSecondaryColor * GrayedOut;
                Icon.color = GrayedOut;
                Icon.sprite = type.PikminIcon;
                Icon.enabled = true;
                Counter.text = "X";
            }

        }
    }
}
