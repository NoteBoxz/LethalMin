using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using GameNetcodeStuff;
using System.Linq;

namespace LethalMin
{
    public class DualOnion : Onion
    {
        public GameObject MainMesh;
        public GameObject Beam;
        public GameObject Cone;
        public bool IsBeamOut, InteractionSetup;

        public override void Start()
        {
            AnimPos = transform.Find("PikminAnimPos");
            SetupMesh();
            SetupBeam();
            Cone = transform.Find("BeamZone/Cone").gameObject;
            HideCone();
            Beam.SetActive(false);
            base.Start();
        }

        protected override void SetupInteractTrigger()
        {
            base.SetupInteractTrigger();
            InteractionSetup = true;
        }

        public void Update()
        {
            if (Beam == null)
            {
                Beam = transform.Find("BeamZone").gameObject;
            }

            if (Cone == null)
            {
                Cone = transform.Find("BeamZone/Cone").gameObject;
            }

            bool inShipPhase = StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving
             || !StartOfRound.Instance.shipHasLanded;

            if (inShipPhase && IsBeamOut)
            {
                IsBeamOut = false;
                HideCone();
            }
            else if (!inShipPhase && !Beam.activeSelf)
            {
                if (RoundManager.Instance.currentLevel.sceneName == "CompanyBuilding" && !LethalMin.CanWalkAtCompany())
                    return;
                IsBeamOut = true;
                ShowCone();
                Beam.SetActive(true);
            }
        }
        private void SetupBeam()
        {
            Beam = transform.Find("BeamZone").gameObject;
            Beam.SetActive(false);
        }
        private void ShowCone()
        {
            LethalMin.Logger.LogInfo("ShowCalled");
            if (Cone != null)
            {
                Beam.SetActive(true);
                Cone.GetComponent<Animator>().Play("ShowCone");
            }
        }

        private void HideCone()
        {
            LethalMin.Logger.LogInfo("HideCalled");
            if (Cone != null)
            {
                Cone.GetComponent<Animator>().Play("HideCone");
                StartCoroutine(DisableConeAfterAnimation());
            }
        }

        private IEnumerator DisableConeAfterAnimation()
        {
            yield return new WaitForSeconds(1.1f);
            Beam.SetActive(false);
        }


        private void SetupMesh()
        {
            MainMesh = transform.Find("PikminContainerNew").gameObject;
            if (MainMesh == null)
            {
                LethalMin.Logger.LogError("MainMesh not found in DualOnion.");
                return;
            }
        }
    }
}