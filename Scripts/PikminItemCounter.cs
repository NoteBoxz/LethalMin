using TMPro;
using UnityEngine;

namespace LethalMin
{
    public class PikminItemCounter : MonoBehaviour
    {
        public TMP_Text DeviderCounter = null!;
        public TMP_Text PikminOnItemCounter = null!;
        public TMP_Text PikminNeededCounter = null!;
        public PikminItem item = null!;
        public Color PrimaryAccent, SecondaryAccent;
        public float Offset;



        void Update()
        {
            if (item == null)
            {
                LethalMin.Logger.LogWarning($"({gameObject.name})'s item is null, destroying counter.");
                Destroy(gameObject);
                return;
            }
            Color NotEnoughColor = new Color(PrimaryAccent.r, PrimaryAccent.g, PrimaryAccent.b, 0.5f);
            Color NotEnoughOutlineColor = new Color(SecondaryAccent.r, SecondaryAccent.g, SecondaryAccent.b, 0.75f);
            Color EnoughColor = new Color(PrimaryAccent.r, PrimaryAccent.g, PrimaryAccent.b, 1f);
            Color EnoughOutlineColor = new Color(SecondaryAccent.r, SecondaryAccent.g, SecondaryAccent.b, 1);


            PikminOnItemCounter.text = $"{item.TotalCarryStrength}";
            PikminNeededCounter.text = $"{item.CarryStrengthNeeded}";

            if (item.ItemScript != null)
            {
                transform.localPosition = new Vector3
                (item.ItemScript.transform.position.x,
                 item.ItemScript.transform.position.y + Offset,
                 item.ItemScript.transform.position.z);
            }

            if (item.IsBeingCarried)
            {
                DeviderCounter.color = EnoughColor;
                PikminOnItemCounter.color = EnoughColor;
                PikminNeededCounter.color = EnoughColor;

                DeviderCounter.outlineColor = EnoughOutlineColor;
                PikminOnItemCounter.outlineColor = EnoughOutlineColor;
                PikminNeededCounter.outlineColor = EnoughOutlineColor;
            }
            else
            {
                DeviderCounter.color = NotEnoughColor;
                PikminOnItemCounter.color = NotEnoughOutlineColor;
                PikminNeededCounter.color = NotEnoughColor;

                DeviderCounter.outlineColor = NotEnoughOutlineColor;
                PikminOnItemCounter.outlineColor = NotEnoughColor;
                PikminNeededCounter.outlineColor = NotEnoughOutlineColor;
            }
        }


        public void SetCounterColor(Color colorA, Color? ColorB = null)
        {
            PrimaryAccent = colorA;
            SecondaryAccent = ColorB == null ? Color.black : ColorB.Value;
        }
    }
}