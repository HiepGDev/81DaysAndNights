using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PhuScene
{
    public class LeaderboardUI : MonoBehaviour
    {
        public static LeaderboardUI Instance { get; private set; }

        [Header("Main Panel References")]
        [SerializeField] private GameObject leaderboardOverlayPanel;
        [SerializeField] private Button closeButton;

        [Header("Tab System")]
        [SerializeField] private Button globalTabButton;
        [SerializeField] private Button friendsTabButton;
        [SerializeField] private Color activeTabColor = Color.white;
        [SerializeField] private Color inactiveTabColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        [Header("Pagination UI")]
        [SerializeField] private Button prevPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private TextMeshProUGUI pageStatusText; // e.g. "Page 1 / 5"
        [SerializeField] private int pageSize = 10;

        [Header("Cell Rendering")]
        [SerializeField] private Transform cellContainer;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private GameObject loadingIndicator;

        [Header("State Tracking")]
        [SerializeField] private LeaderboardTab currentTab = LeaderboardTab.Global;
        [SerializeField] private int currentPage = 1; // 1-indexed
        [SerializeField] private int totalPages = 1;
        private bool isLoadingData = false;

        public event Action<LeaderboardPageResult> OnDataReceived;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(HideLeaderboard);
            if (globalTabButton != null) globalTabButton.onClick.AddListener(SelectGlobalTab);
            if (friendsTabButton != null) friendsTabButton.onClick.AddListener(SelectFriendsTab);
            if (prevPageButton != null) prevPageButton.onClick.AddListener(PreviousPage);
            if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);

            UpdateTabVisuals();
            UpdatePaginationButtons();
        }

        /// <summary>
        /// Globally accessible helper to open the leaderboard overlay from any scene.
        /// </summary>
        public static void ShowLeaderboard(LeaderboardTab initialTab = LeaderboardTab.Global)
        {
            if (Instance == null)
            {
                Instance = FindFirstObjectByType<LeaderboardUI>();
            }

            if (Instance != null)
            {
                Instance.OpenLeaderboard(initialTab);
            }
            else
            {
                Debug.LogWarning("[LeaderboardUI] Instance not found in scene.");
            }
        }

        public void OpenLeaderboard(LeaderboardTab initialTab = LeaderboardTab.Global)
        {
            if (leaderboardOverlayPanel != null)
            {
                leaderboardOverlayPanel.SetActive(true);
            }

            // Unlock mouse cursor for interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SetTab(initialTab);
        }

        public void HideLeaderboard()
        {
            if (leaderboardOverlayPanel != null)
            {
                leaderboardOverlayPanel.SetActive(false);
            }

            // Relock mouse cursor if wave is active
            if (WaveManager.Instance != null && WaveManager.Instance.CurrentState == WaveState.WaveActive)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        #region Tab Switching Functions

        public void SelectGlobalTab()
        {
            SetTab(LeaderboardTab.Global);
        }

        public void SelectFriendsTab()
        {
            SetTab(LeaderboardTab.Friends);
        }

        public void SetTab(LeaderboardTab tab)
        {
            currentTab = tab;
            currentPage = 1;
            UpdateTabVisuals();
            FetchAndRefreshData();
        }

        private void UpdateTabVisuals()
        {
            if (globalTabButton != null)
            {
                Image bg = globalTabButton.GetComponent<Image>();
                if (bg != null) bg.color = (currentTab == LeaderboardTab.Global) ? activeTabColor : inactiveTabColor;
            }

            if (friendsTabButton != null)
            {
                Image bg = friendsTabButton.GetComponent<Image>();
                if (bg != null) bg.color = (currentTab == LeaderboardTab.Friends) ? activeTabColor : inactiveTabColor;
            }
        }

        #endregion

        #region Pagination Functions

        public void NextPage()
        {
            if (currentPage < totalPages && !isLoadingData)
            {
                GoToPage(currentPage + 1);
            }
        }

        public void PreviousPage()
        {
            if (currentPage > 1 && !isLoadingData)
            {
                GoToPage(currentPage - 1);
            }
        }

        public void GoToPage(int pageIndex)
        {
            currentPage = Mathf.Clamp(pageIndex, 1, Mathf.Max(1, totalPages));
            FetchAndRefreshData();
        }

        private void UpdatePaginationButtons()
        {
            if (prevPageButton != null) prevPageButton.interactable = (currentPage > 1) && !isLoadingData;
            if (nextPageButton != null) nextPageButton.interactable = (currentPage < totalPages) && !isLoadingData;
            if (pageStatusText != null) pageStatusText.text = $"Page {currentPage} / {Mathf.Max(1, totalPages)}";
        }

        #endregion

        #region Data Fetching & Rendering Stub

        public void FetchAndRefreshData()
        {
            if (isLoadingData) return;
            StartCoroutine(FetchLeaderboardRoutine(currentTab, currentPage));
        }

        private IEnumerator FetchLeaderboardRoutine(LeaderboardTab tab, int page)
        {
            isLoadingData = true;
            if (loadingIndicator != null) loadingIndicator.SetActive(true);
            UpdatePaginationButtons();

            // Simulate network latency (replace with Steamworks / PlayFab / Web API call in production)
            yield return new WaitForSeconds(0.25f);

            LeaderboardPageResult result = GenerateMockLeaderboardData(tab, page, pageSize);

            totalPages = result.totalPages;
            RenderEntries(result.entries);

            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            isLoadingData = false;
            UpdatePaginationButtons();

            OnDataReceived?.Invoke(result);
        }

        private void RenderEntries(List<LeaderboardEntryData> entries)
        {
            if (cellContainer == null) return;

            // Clear existing cell objects
            foreach (Transform child in cellContainer)
            {
                Destroy(child.gameObject);
            }

            if (cellPrefab == null) return;

            foreach (var entry in entries)
            {
                GameObject cellObj = Instantiate(cellPrefab, cellContainer);
                LeaderboardEntryCell cellComp = cellObj.GetComponent<LeaderboardEntryCell>();
                if (cellComp != null)
                {
                    cellComp.Bind(entry);
                }
            }
        }

        /// <summary>
        /// Mock data generator stub for editor testing until online backend integration.
        /// </summary>
        private LeaderboardPageResult GenerateMockLeaderboardData(LeaderboardTab tab, int page, int perPage)
        {
            int totalEntries = tab == LeaderboardTab.Global ? 45 : 12;
            int totalPgs = Mathf.CeilToInt((float)totalEntries / perPage);
            List<LeaderboardEntryData> list = new List<LeaderboardEntryData>();

            int startRank = (page - 1) * perPage + 1;
            int endRank = Mathf.Min(startRank + perPage - 1, totalEntries);

            for (int i = startRank; i <= endRank; i++)
            {
                string name = tab == LeaderboardTab.Friends ? $"Friend_Unit_{i}" : $"Player_{i:D3}";
                bool isLocal = (i == 4);
                int wave = Mathf.Max(1, 20 - i);
                int score = wave * 12500 + (100 - i) * 150;
                float kpm = 45f - (i * 0.8f);
                string timeStr = $"{12 + wave:D2}:{i * 3 % 60:D2}";

                list.Add(new LeaderboardEntryData(i, isLocal ? "You (Local Player)" : name, wave, score, kpm, timeStr, isLocal));
            }

            return new LeaderboardPageResult(tab, page, totalPgs, totalEntries, list);
        }

        #endregion
    }
}
