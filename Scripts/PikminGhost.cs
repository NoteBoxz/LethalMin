using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMin.Pikmin;
using LethalMin.Utils;
using UnityEngine;

namespace LethalMin
{
    public class PikminGhost : MonoBehaviour
    {
        public PikminType LostType = null!;
        public Renderer renderer = null!;
        public Collider collider = null!;
        public System.Random ghostRandom = null!;
        public string InMemoryof = "<ID Not Set>";
        public Color? overrideColor = null;
        public Texture2D[] GhostTextures = new Texture2D[0]; //L hard coded
        Vector3 StartingPosition;
        Color StartingColor;
        float RemoveTimer;
        AudioSource audioSource = null!;

        void Start()
        {
            if (LostType == null)
            {
                LethalMin.Logger.LogError($"{InMemoryof}: No pikmin type assinged to ghost");
                Destroy(gameObject);
                return;
            }
            if (LostType.PikminGhostOverrideModel != null)
            {
                Destroy(transform.Find("Plane").gameObject);
                Instantiate(LostType.PikminGhostOverrideModel, transform);
            }
            if (ghostRandom == null)
            {
                //LethalMin.Logger.LogWarning($"{InMemoryof}: Ghost random is null!");
                ghostRandom = new System.Random(UnityEngine.Random.Range(0, 10000));
            }

            audioSource = GetComponent<AudioSource>();

            audioSource.rolloffMode = LethalMin.GlobalGhostSFX ? AudioRolloffMode.Logarithmic : AudioRolloffMode.Linear;

            if (LostType.SoundPack.GhostSound.Length > 0)
            {
                audioSource.clip = LostType.SoundPack.PullSoundFromDict(PikminSoundPackSounds.GhostSFX, ghostRandom);
            }

            renderer = GetComponentInChildren<Renderer>();

            if (LostType.PikminGhostOverrideTexture != null)
            {
                renderer.material.SetTexture("_MainTex", LostType.PikminGhostOverrideTexture);
            }
            else if (LostType.AnimatedPikminGhostTexture != null && LostType.AnimatedPikminGhostTexture.Length > 0)
            {
                //LethalMin.Logger.LogWarning($"{InMemoryof}: Animated ghost texture is not yet supported!");
                renderer.material.SetTexture("_MainTex", LostType.AnimatedPikminGhostTexture[0]);
            }
            else
            {
                renderer.material.SetTexture("_MainTex", GhostTextures[(int)PikUtils.ConvertCfgGenerationToPikminGeneration(LethalMin.SoulSpriteGeneration.InternalValue)]);
            }
            renderer.material.SetTextureScale("_MainTex", LostType.OverrideGhostTextureTileing);
            renderer.material.SetTextureOffset("_MainTex", LostType.OverrideGhostTextureOffset);

            // Vector2 randomOffset = new Vector2(ghostRandom.Next(0, 1000), ghostRandom.Next(0, 1000));
            // Texture2D texture = PerlinNoiseGenerator.GeneratePerlinNoise(250, 250, 4, 4, randomOffset);
            // renderer.material.SetTexture("_Displacement", texture);

            if (LostType.SetGhostColor)
            {
                renderer.material.color = overrideColor == null ? LostType.PikminPrimaryColor : overrideColor.Value;
            }
            StartingColor = renderer.material.color;

            StartingPosition = transform.position;

            audioSource.Play();
        }

        int animationTimer = 0;
        int animationframe = 0;
        public void Update()
        {
            collider.enabled = LethalMin.AddCollisionToGhostSprites;
            transform.position = new Vector3(transform.position.x, transform.position.y + Time.deltaTime * 1.5f, transform.position.z);
            audioSource.rolloffMode = LethalMin.GlobalGhostSFX ? AudioRolloffMode.Logarithmic : AudioRolloffMode.Linear;
            if (Vector3.Distance(StartingPosition, transform.position) > 10)
            {
                renderer.material.color = new Color
                (renderer.material.color.r,
                renderer.material.color.g,
                renderer.material.color.b,
                renderer.material.color.a - Time.deltaTime * 0.5f);
                RemoveTimer += Time.deltaTime * 0.5f;
            }
            if (RemoveTimer > 1)
            {
                Destroy(gameObject);
            }
            if (LostType.AnimatedPikminGhostTexture != null && LostType.AnimatedPikminGhostTexture.Length > 0)
            {
                UpdateAnimatedGhost();
            }
        }

        void UpdateAnimatedGhost()
        {
            if (LostType.AnimatedPikminGhostTexture == null)
            {
                return;
            }

            animationTimer++;
            if (animationTimer >= LostType.AnimatedPikminGhostFrameHold)
            {
                renderer.material.SetTexture("_MainTex", LostType.AnimatedPikminGhostTexture[animationframe]);
                animationTimer = 0;
                animationframe++;
                if (PikUtils.IsOutOfRange(LostType.AnimatedPikminGhostTexture, animationframe))
                {
                    animationframe = 0; //loop the animation
                }
            }
        }
    }
}
