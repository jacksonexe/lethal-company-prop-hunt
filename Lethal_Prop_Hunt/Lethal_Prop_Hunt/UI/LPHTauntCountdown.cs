using GameNetcodeStuff;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Trouble_In_Company_Town.UI
{
    public class LPHTauntCountdown : MonoBehaviour
    {
        public PlayerControllerB Player;

        private TextMeshProUGUI _timer;

        public LPHTauntCountdown(HUDManager hudManager)
        {
            this.FindComponents(hudManager);
        }

        // Based on https://github.com/Treyo1928/TreysHealthText-Lethal-Company-Mod
        private void FindComponents(HUDManager hudManager)
        {
            GameObject topLeftCorner = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner");

            GameObject textObj = new GameObject("TauntCooldownText");
            textObj.transform.SetParent(topLeftCorner.transform, false);

            _timer = textObj.AddComponent<TextMeshProUGUI>();

            if (hudManager.weightCounter != null)
            {
                TextMeshProUGUI weightText = hudManager.weightCounter;
                _timer.font = weightText.font;
                _timer.fontSize = weightText.fontSize;
                _timer.color = Color.white;
                _timer.alignment = TextAlignmentOptions.Center;
                _timer.enableAutoSizing = weightText.enableAutoSizing;
                _timer.fontSizeMin = weightText.fontSizeMin;
                _timer.fontSizeMax = weightText.fontSizeMax;

                if (weightText.fontMaterial != null)
                {
                    _timer.fontSharedMaterial = new Material(weightText.fontMaterial);
                }

                if (weightText.transform.parent != null)
                {
                    RectTransform weightCounterParentRect = weightText.transform.parent.GetComponent<RectTransform>();
                    if (weightCounterParentRect != null)
                    {
                        RectTransform killCooldownTextRect = _timer.GetComponent<RectTransform>();
                        killCooldownTextRect.localRotation = weightCounterParentRect.localRotation;
                    }
                }
            }
            else
            {
                _timer.fontSize = 24;
                _timer.color = Color.white;
                _timer.alignment = TextAlignmentOptions.Center;
            }

            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);

            int XOffset = 0;
            int YOffset = -120;
            rectTransform.anchoredPosition = new Vector2(-53 + XOffset, -95 + YOffset);
        }

        public void SetTimer(int currentCooldown)
        {
            if (currentCooldown <= 0)
            {
                _timer.text = "Taunting";
            }
            else
            {
                _timer.text = "Auto Taunt: " + currentCooldown + "s";
            }
        }

        internal void HideTimer()
        {
            ((TMP_Text)_timer).text = "";
        }
    }
}
