using System;
using System.Collections.Generic;

namespace PhuScene
{
    public enum LeaderboardTab
    {
        Global,
        Friends
    }

    [Serializable]
    public struct LeaderboardEntryData
    {
        public int rank;
        public string playerName;
        public int waveReached;
        public int score;
        public float kpm;
        public string survivalTimeFormatted;
        public bool isLocalPlayer;

        public LeaderboardEntryData(int rank, string playerName, int waveReached, int score, float kpm, string survivalTimeFormatted, bool isLocalPlayer = false)
        {
            this.rank = rank;
            this.playerName = playerName;
            this.waveReached = waveReached;
            this.score = score;
            this.kpm = kpm;
            this.survivalTimeFormatted = survivalTimeFormatted;
            this.isLocalPlayer = isLocalPlayer;
        }
    }

    [Serializable]
    public struct LeaderboardPageResult
    {
        public LeaderboardTab tab;
        public int pageIndex;        // 1-indexed
        public int totalPages;
        public int totalEntries;
        public List<LeaderboardEntryData> entries;

        public LeaderboardPageResult(LeaderboardTab tab, int pageIndex, int totalPages, int totalEntries, List<LeaderboardEntryData> entries)
        {
            this.tab = tab;
            this.pageIndex = pageIndex;
            this.totalPages = totalPages;
            this.totalEntries = totalEntries;
            this.entries = entries ?? new List<LeaderboardEntryData>();
        }
    }
}
