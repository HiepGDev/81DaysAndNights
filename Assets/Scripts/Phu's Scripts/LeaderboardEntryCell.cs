using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PhuScene
{
    public class LeaderboardEntryCell : MonoBehaviour
    {
        [Header("UI Text References")]
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI kpmText;
        [SerializeField] private TextMeshProUGUI timeText;

        [Header("Visual Customization")]
        [SerializeField] private Image backgroundGraphic;
        [SerializeField] private Color defaultBgColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);
        [SerializeField] private Color localPlayerBgColor = new Color(0.2f, 0.6f, 0.2f, 0.8f);
        [SerializeField] private Color topThreeRankColor = new Color(1.0f, 0.84f, 0.0f, 1.0f); // Gold

        public void Bind(LeaderboardEntryData data)
        {
            if (rankText != null)
            {
                rankText.text = $"#{data.rank}";
                if (data.rank <= 3)
                {
                    rankText.color = topThreeRankColor;
                }
            }

            if (playerNameText != null)
            {
                playerNameText.text = data.playerName;
            }

            if (waveText != null)
            {
                waveText.text = $"Wave {data.waveReached}";
            }

            if (scoreText != null)
            {
                scoreText.text = data.score.ToString("N0");
            }

            if (kpmText != null)
            {
                kpmText.text = $"{data.kpm:F1} KPM";
            }

            if (timeText != null)
            {
                timeText.text = data.survivalTimeFormatted;
            }

            if (backgroundGraphic != null)
            {
                backgroundGraphic.color = data.isLocalPlayer ? localPlayerBgColor : defaultBgColor;
            }
        }
    }
}
