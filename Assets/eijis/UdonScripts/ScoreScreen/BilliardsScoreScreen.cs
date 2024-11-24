//#define TKCH_DEBUG_SCORE

//#define TKCH_SYNC_SCORE

using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BilliardsScoreScreen : UdonSharpBehaviour
{
    [SerializeField] private PlayerRow headerRow;
    [SerializeField] private TeamPlayers highGroup; // Orange
    [SerializeField] private TeamPlayers lowGroup; // Blue
    [SerializeField] private BilliardsModule table;
    
    private TeamPlayers[] teamPlayers = new TeamPlayers[2];
    
#if TKCH_SYNC_SCORE
    private bool rowSyncOverrideLock = true;
    
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(LastScoreUpdateSerial))]
    private int lastScoreUpdateSerial = int.MaxValue;
    private bool lastScoreUpdateSerialSynced = false;
#endif
    
    private PlayerRow[] scoreSyncRows = new PlayerRow[10];
#if TKCH_SYNC_SCORE
    private bool[] scoreSynced = new bool[10];

    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ScoreSyncValue0))]
    private uint scoreSyncValue0 = 0xFFFFFFFFu;
    
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ScoreSyncValue1))]
    private uint scoreSyncValue1 = 0xFFFFFFFFu;

    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ScoreSyncValue2))]
    private uint scoreSyncValue2 = 0xFFFFFFFFu;
    
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ScoreSyncValue3))]
    private uint scoreSyncValue3 = 0xFFFFFFFFu;
    
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ScoreSyncValue4))]
    private uint scoreSyncValue4 = 0xFFFFFFFFu;
    
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ScoreSyncValue5))]
    private uint scoreSyncValue5 = 0xFFFFFFFFu;
    
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ScoreSyncValue6))]
    private uint scoreSyncValue6 = 0xFFFFFFFFu;
    
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ScoreSyncValue7))]
    private uint scoreSyncValue7 = 0xFFFFFFFFu;
    
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ScoreSyncValue8))]
    private uint scoreSyncValue8 = 0xFFFFFFFFu;
    
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ScoreSyncValue9))]
    private uint scoreSyncValue9 = 0xFFFFFFFFu;
    
    public int LastScoreUpdateSerial
    {
        get => lastScoreUpdateSerial;
        set
        {
#if TKCH_DEBUG_SCORE
            table._Log($"TKCH BilliardsScoreScreen::LastScoreUpdateSerial lastScoreUpdateSerialSynced => {lastScoreUpdateSerialSynced}, lastScoreUpdateSerial => {lastScoreUpdateSerial}, value => {value}");
#endif
            if (!lastScoreUpdateSerialSynced && !rowSyncOverrideLock)
            {
                lastScoreUpdateSerialSynced = true;
                lastScoreUpdateSerial = value;
                //rowSyncOverrideLock = false;
#if TKCH_DEBUG_SCORE
                table._Log($"TKCH BilliardsScoreScreen::LastScoreUpdateSerial Set to value lastScoreUpdateSerialSynced => {lastScoreUpdateSerialSynced}, lastScoreUpdateSerial => {lastScoreUpdateSerial}");
#endif
            }
        }
    }
    
    public uint ScoreSyncValue0
    {
        get => scoreSyncValue0;
        set
        {
#if TKCH_DEBUG_SCORE
            table._Log($"TKCH BilliardsScoreScreen::ScoreSyncValue0 value => " + string.Format("{0:X8}", value));
#endif
            if (!scoreSynced[0] && !rowSyncOverrideLock)
            {
                scoreSynced[0] = true;
                scoreSyncValue0 = value;
                scoreSyncRows[0].DecodeSyncValueAdd(value);
            }
        }
    }

    public uint ScoreSyncValue1
    {
        get => scoreSyncValue1;
        set
        {
#if TKCH_DEBUG_SCORE
            table._Log($"TKCH BilliardsScoreScreen::ScoreSyncValue1 value => " + string.Format("{0:X8}", value));
#endif
            if (!scoreSynced[1] && !rowSyncOverrideLock)
            {
                scoreSynced[1] = true;
                scoreSyncValue1 = value;
                scoreSyncRows[1].DecodeSyncValueAdd(value);
            }
        }
    }

    public uint ScoreSyncValue2
    {
        get => scoreSyncValue2;
        set
        {
#if TKCH_DEBUG_SCORE
            table._Log($"TKCH BilliardsScoreScreen::ScoreSyncValue2 value => " + string.Format("{0:X8}", value));
#endif
            if (!scoreSynced[2] && !rowSyncOverrideLock)
            {
                scoreSynced[2] = true;
                scoreSyncValue2 = value;
                scoreSyncRows[2].DecodeSyncValueAdd(value);
            }
        }
    }

    public uint ScoreSyncValue3
    {
        get => scoreSyncValue3;
        set
        {
#if TKCH_DEBUG_SCORE
            table._Log($"TKCH BilliardsScoreScreen::ScoreSyncValue3 value => " + string.Format("{0:X8}", value));
#endif
            if (!scoreSynced[3] && !rowSyncOverrideLock)
            {
                scoreSynced[3] = true;
                scoreSyncValue3 = value;
                scoreSyncRows[3].DecodeSyncValueAdd(value);
            }
        }
    }

    public uint ScoreSyncValue4
    {
        get => scoreSyncValue4;
        set
        {
#if TKCH_DEBUG_SCORE
            table._Log($"TKCH BilliardsScoreScreen::ScoreSyncValue4 value => " + string.Format("{0:X8}", value));
#endif
            if (!scoreSynced[4] && !rowSyncOverrideLock)
            {
                scoreSynced[4] = true;
                scoreSyncValue4= value;
                scoreSyncRows[4].DecodeSyncValueAdd(value);
            }
        }
    }

    public uint ScoreSyncValue5
    {
        get => scoreSyncValue5;
        set
        {
#if TKCH_DEBUG_SCORE
            table._Log($"TKCH BilliardsScoreScreen::ScoreSyncValue5 value => " + string.Format("{0:X8}", value));
#endif
            if (!scoreSynced[5] && !rowSyncOverrideLock)
            {
                scoreSynced[5] = true;
                scoreSyncValue5 = value;
                scoreSyncRows[5].DecodeSyncValueAdd(value);
            }
        }
    }

    public uint ScoreSyncValue6
    {
        get => scoreSyncValue6;
        set
        {
#if TKCH_DEBUG_SCORE
            table._Log($"TKCH BilliardsScoreScreen::ScoreSyncValue6 value => " + string.Format("{0:X8}", value));
#endif
            if (!scoreSynced[6] && !rowSyncOverrideLock)
            {
                scoreSynced[6] = true;
                scoreSyncValue6 = value;
                scoreSyncRows[6].DecodeSyncValueAdd(value);
            }
        }
    }

    public uint ScoreSyncValue7
    {
        get => scoreSyncValue7;
        set
        {
#if TKCH_DEBUG_SCORE
            table._Log($"TKCH BilliardsScoreScreen::ScoreSyncValue7 value => " + string.Format("{0:X8}", value));
#endif
            if (!scoreSynced[7] && !rowSyncOverrideLock)
            {
                scoreSynced[7] = true;
                scoreSyncValue7 = value;
                scoreSyncRows[7].DecodeSyncValueAdd(value);
            }
        }
    }

    public uint ScoreSyncValue8
    {
        get => scoreSyncValue8;
        set
        {
#if TKCH_DEBUG_SCORE
            table._Log($"TKCH BilliardsScoreScreen::ScoreSyncValue8 value => " + string.Format("{0:X8}", value));
#endif
            if (!scoreSynced[8] && !rowSyncOverrideLock)
            {
                scoreSynced[8] = true;
                scoreSyncValue8 = value;
                scoreSyncRows[8].DecodeSyncValueAdd(value);
            }
        }
    }

    public uint ScoreSyncValue9
    {
        get => scoreSyncValue9;
        set
        {
#if TKCH_DEBUG_SCORE
            table._Log($"TKCH BilliardsScoreScreen::ScoreSyncValue9 value => " + string.Format("{0:X8}", value));
#endif
            if (!scoreSynced[9] && !rowSyncOverrideLock)
            {
                scoreSynced[9] = true;
                scoreSyncValue9 = value;
                scoreSyncRows[9].DecodeSyncValueAdd(value);
            }
        }
    }
#endif
    
    // private void Start()
    public void Init()
    {
        headerRow.Table = table;
        teamPlayers = new[] { highGroup, lowGroup };
        foreach (var group in teamPlayers)
        {
            if (ReferenceEquals(null, group))
            {
                continue;
            }
            
            group.Init();
            group.Table = table;
            // group.Init();
        }
        //UpdateLeftBallCount(0);
        
#if TKCH_SYNC_SCORE
        for (int i = 0; i < scoreSynced.Length; i++)
        {
            scoreSynced[i] = false;
        }
#endif
        
        int scoreSyncRowsIndex = 0;
        for (int i = 0; i < teamPlayers.Length; i++)
        {
            if (ReferenceEquals(null, teamPlayers[i]))
            {
                continue;
            }
            
            scoreSyncRows[scoreSyncRowsIndex++] = teamPlayers[i].GetTeamRow();
            for (int j = 0; j < 4; j++)
            {
                scoreSyncRows[scoreSyncRowsIndex++] = teamPlayers[i].GetPlayerRow(j);
            }
        }
    }   
    
#if TKCH_SYNC_SCORE
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
#if TKCH_DEBUG_SCORE
        table._Log("TKCH BilliardsScoreScreen::OnPlayerJoined()");
#endif

        if (ReferenceEquals(null, player))
        {
#if TKCH_DEBUG_SCORE
            table._Log("TKCH BilliardsScoreScreen::OnPlayerJoined() player is null");
#endif
            return;
        }
        
        if (player.isLocal)
        {
#if TKCH_DEBUG_SCORE
            table._Log("TKCH BilliardsScoreScreen::OnPlayerJoined() player isLocal");
#endif
            return;
        }
        
        if (ReferenceEquals(null, Networking.LocalPlayer))
        {
#if TKCH_DEBUG_SCORE
            table._Log("TKCH BilliardsScoreScreen::OnPlayerJoined() LocalPlayer is null");
#endif
            return;
        }

        if (Networking.LocalPlayer.isMaster)
        {
#if TKCH_DEBUG_SCORE
            table._Log("TKCH BilliardsScoreScreen::OnPlayerJoined() isMaster => {Networking.LocalPlayer.isMaster} RequestSerialization()");
#endif
            rowSyncOverrideLock = false;

            scoreSyncValue0 = scoreSyncRows[0].EncodeScoreSyncValue();
            scoreSyncValue1 = scoreSyncRows[1].EncodeScoreSyncValue();
            scoreSyncValue2 = scoreSyncRows[2].EncodeScoreSyncValue();
            scoreSyncValue3 = scoreSyncRows[3].EncodeScoreSyncValue();
            scoreSyncValue4 = scoreSyncRows[4].EncodeScoreSyncValue();
            scoreSyncValue5 = scoreSyncRows[5].EncodeScoreSyncValue();
            scoreSyncValue6 = scoreSyncRows[6].EncodeScoreSyncValue();
            scoreSyncValue7 = scoreSyncRows[7].EncodeScoreSyncValue();
            scoreSyncValue8 = scoreSyncRows[8].EncodeScoreSyncValue();
            scoreSyncValue9 = scoreSyncRows[9].EncodeScoreSyncValue();
            
            RequestSerialization();
        }
    }
#else
    public uint[] EncodeScoreSyncValues()
    {
#if TKCH_DEBUG_SCORE
        table._Log("TKCH BilliardsScoreScreen::EncodeScoreSyncValues()");
#endif
        int validRowCount = 0;
        for (int i = 0; i < scoreSyncRows.Length; i++)
        {
            if (!ReferenceEquals(null, scoreSyncRows[i]))
            {
                validRowCount++;
            }
        }
#if TKCH_DEBUG_SCORE
        table._Log($"  validRowCount = {validRowCount}");
#endif

        uint[] encodeScoreSyncValues = new uint[validRowCount * 2];
        for (int i = 0, j = 0; i < scoreSyncRows.Length; i++)
        {
            if (ReferenceEquals(null, scoreSyncRows[i]))
            {
                continue;
            }
            uint[] encodeScoreSyncValue = scoreSyncRows[i].EncodeScoreSyncValue();
            encodeScoreSyncValues[j*2] = encodeScoreSyncValue[0];
            encodeScoreSyncValues[(j*2)+1] = encodeScoreSyncValue[1];
            j++;
        }

        return encodeScoreSyncValues;
    }
    
    public uint[] EncodeScoreParams(int point, int scratchCount, int pocketBallCount, int shotCount, int safeNoPocketShotCount, int invalidPocketBallCount)
    {
        if (scoreSyncRows.Length <= 0)
        {
            return null;
        }

        return scoreSyncRows[0].EncodeScoreParams(point, scratchCount, pocketBallCount, shotCount, safeNoPocketShotCount, invalidPocketBallCount);
    }
    
    public uint EncodeScoreParams_Mini(int point, int shotCount, int scratchCount, int safeNoPocketShotCount)
    {
        if (scoreSyncRows.Length <= 0)
        {
            return 0xDEADBEAFu;
        }

        return scoreSyncRows[0].EncodeScoreParams_Mini(point, shotCount, scratchCount, safeNoPocketShotCount);
    }

    public uint EncodeScoreParams_Frame5(int totalPoint, int[] scores)
    {
        if (scoreSyncRows.Length <= 0)
        {
            return 0xDEADBEAFu;
        }

        return scoreSyncRows[0].EncodeScoreParams_Frame5(totalPoint, scores);
    }

    public void DecodeScoreSyncValues(uint[] scoreSyncValues)
    {
#if TKCH_DEBUG_SCORE
        table._Log("TKCH BilliardsScoreScreen::DecodeScoreSyncValues()");
#endif
        for (int i = 0, j = 0 ; i < scoreSyncRows.Length; i++)
        {
            if (ReferenceEquals(null, scoreSyncRows[i]))
            {
                continue;
            }
            uint[] scoreSyncValue = new[] { scoreSyncValues[j*2], scoreSyncValues[(j*2) + 1] };
            scoreSyncRows[i].DecodeSyncValue(scoreSyncValue);
            j++;
        }
    }
    
    public void DecodeScoreSyncValues_Mini(uint[] scoreSyncValues)
    {
#if TKCH_DEBUG_SCORE
        table._Log("TKCH BilliardsScoreScreen::DecodeScoreSyncValues_Mini()");
#endif
        for (int i = 0, j = 0; i < scoreSyncRows.Length; i++)
        {
            if (ReferenceEquals(null, scoreSyncRows[i]))
            {
                continue;
            }
            scoreSyncRows[i].DecodeSyncValue_Mini(scoreSyncValues[j]);
            j++;
        }
    }

    public void DecodeScoreSyncValues_Frame5(uint[] scoreSyncValues)
    {
#if TKCH_DEBUG_SCORE
        table._Log($"TKCH BilliardsScoreScreen::DecodeScoreSyncValues_Frame5( scoreSyncValues.Length = {scoreSyncValues.Length} )");
        // table._Log($"TKCH  vsTeamZeroSumMode = {vsTeamZeroSumMode}");
        table._Log($"TKCH  scoreSyncRows.Length {scoreSyncRows.Length}");
#endif
        /*
        if (vsTeamZeroSumMode)
        {
            int[][] scoreMatrix = new[] { new int[6], new int[6], new int[6], new int[6] };
            
            for (int i = 0, j = 0; i < scoreSyncRows.Length; i++)
            {
                if (scoreSyncValues.Length <= j)
                {
                    continue;
                }
                if (ReferenceEquals(null, scoreSyncRows[i]))
                {
                    continue;
                }
                int[] decodedSyncValues = scoreSyncRows[i].DecodedSyncValues_Frame5(scoreSyncValues[j]);
#if TKCH_DEBUG_SCORE
                table._Log($"TKCH  decodedSyncValues.Length {decodedSyncValues.Length}");
#endif
                Array.Copy(decodedSyncValues, scoreMatrix[j], scoreMatrix[j].Length);
                j++;
            }

            int activeTeamCount = 0;
            for (int i = 0; i < teamPlayers.Length; i++)
            {
                if (ReferenceEquals(null, teamPlayers[i]) || 
                    teamPlayers[i].GetTeamRow().GetName() == "")
                {
                    break;
                }
            
                activeTeamCount++;
            }
#if TKCH_DEBUG_SCORE
            table._Log($"TKCH  activeTeamCount {activeTeamCount}");
#endif
            
            for (int i = 0; i < 6; i++)
            {
                int[] subTotals = new int[activeTeamCount];
                
                for (int j = 0; j < activeTeamCount; j++)
                {
                    subTotals[j] = scoreMatrix[j][i] * (activeTeamCount - 1);
                    for (int k = 0; k < activeTeamCount; k++)
                    {
                        if (j == k)
                        {
                            continue;
                        }
                        
                        subTotals[j] -= scoreMatrix[k][i];
                    }
                }
                
                for (int j = 0; j < activeTeamCount; j++)
                {
                    scoreMatrix[j][i] = subTotals[j];
                }
            }

            for (int i = 0, j = 0; i < scoreSyncRows.Length; i++)
            {
                if (ReferenceEquals(null, scoreSyncRows[i]))
                {
                    continue;
                }
                scoreSyncRows[i].SetScoreByArray(scoreMatrix[j]);
                j++;
            }
        }
        else
        */
        {
#if TKCH_DEBUG_SCORE
            table._Log($"TKCH  scoreSyncRows is null ? {ReferenceEquals(null, scoreSyncRows)}");
            table._Log($"TKCH  scoreSyncRows.Length = {scoreSyncRows.Length}");
#endif
            for (int i = 0, j = 0; i < scoreSyncRows.Length; i++)
            {
                if (ReferenceEquals(null, scoreSyncRows[i]))
                {
                    continue;
                }
                scoreSyncRows[i].DecodeSyncValue_Frame5(scoreSyncValues[j]);
                j++;
            }
        }
        
        // if (!ReferenceEquals(null, cascadeScoreScreen))
        // {
        //     cascadeScoreScreen.DecodeScoreSyncValues_Frame5(scoreSyncValues);
        // }
    }
#endif

    /*
    public override void OnDeserialization()
    {
#if TKCH_DEBUG_SCORE
        table._Log("TKCH BilliardsScoreScreen::OnDeserialization()");
#endif
    }
    */
    
#if TKCH_SYNC_SCORE
    public void Clear(int scoreUpdateSerial)
#else
    public void Clear()
#endif
    {
#if TKCH_SYNC_SCORE
#if TKCH_DEBUG_SCORE
        table._Log($"TKCH BilliardsScoreScreen::Clear() lastScoreUpdateSerial => {lastScoreUpdateSerial} (set to [scoreUpdateSerial => {scoreUpdateSerial}])");
#endif
        lastScoreUpdateSerial = scoreUpdateSerial; //0;
#endif
        foreach (var group in teamPlayers)
        {
            if (ReferenceEquals(null, group))
            {
                continue;
            }

            group.Clear();
        }        
    }

    public void ScoreUpdate(
#if TKCH_SYNC_SCORE
        int scoreUpdateSerial,
#endif
        int teamId,
        int playerId,
        bool isScratch,
        bool isFoul,
        int pocketCount,
        int pointCount,
        int shotCount
    )
    {
#if TKCH_SYNC_SCORE
#if TKCH_DEBUG_SCORE
        table._Log($"TKCH BilliardsScoreScreen::ScoreUpdate() lastScoreUpdateSerial => {lastScoreUpdateSerial}, scoreUpdateSerial => {scoreUpdateSerial}");
#endif

        if (lastScoreUpdateSerial >= scoreUpdateSerial)
        {
#if TKCH_DEBUG_SCORE
            table._Log("TKCH BilliardsScoreScreen::ScoreUpdate() skip update.");
#endif
            return;
        }
        
#if TKCH_DEBUG_SCORE
        table._Log($"TKCH BilliardsScoreScreen::ScoreUpdate() call TeamPlayers::TeamScoreUpdate() teamId => {teamId}");
#endif
        lastScoreUpdateSerial = scoreUpdateSerial;
#endif
        teamPlayers[teamId].TeamScoreUpdate(
            (playerId < 0 ? playerId : (playerId / 2)),
            isScratch,
            isFoul,
            pocketCount,
            pointCount,
            shotCount
            );
    }

#if TKCH_SYNC_SCORE
    public void UnlockRowSyncOverride()
    {
#if TKCH_DEBUG_SCORE
        table._Log("TKCH BilliardsScoreScreen::UnlockRowSyncOverride()");
#endif
        rowSyncOverrideLock = false;
    }
#endif
    
    public void UpdateLeftBallCount(int leftBallCount)
    {
        //headerRow.SetName($"残り：{leftBallCount}");
        headerRow.SetName($"　　　{leftBallCount}");
    }
    
    public void UpdateGameNameWithNumber(string ganeName, int number)
    {
        headerRow.SetName($"{ganeName} [{number}]");
        
        // if (!ReferenceEquals(null, cascadeScoreScreen))
        // {
        //     cascadeScoreScreen.UpdateGameNameWithNumber(ganeName, number);
        // }
    }

    public void UpdateHeaderTextByColIndex(string text, int colIndex)
    {
        headerRow.SetTextByColIndex(text, colIndex);
        
        // if (!ReferenceEquals(null, cascadeScoreScreen))
        // {
        //     cascadeScoreScreen.UpdateHeaderTextByColIndex(text, colIndex);
        // }
    }
    
    public void UpdateTeamName(int teamIndex, string teamName)
    {
#if TKCH_DEBUG_SCORE
        table._Log($"TKCH BilliardsScoreScreen::UpdateTeamName() teamIndex => {teamIndex}, teamName => {teamName}");
        table._Log($"  teamPlayers.Length => {teamPlayers.Length}");
        table._Log($"  ReferenceEquals(null, teamPlayers[{teamIndex}]) => {ReferenceEquals(null, teamPlayers[teamIndex])}");
        table._Log($"  ReferenceEquals(null, teamPlayers[{teamIndex}].GetTeamRow()) => {ReferenceEquals(null, teamPlayers[teamIndex].GetTeamRow())}");
#endif
        teamPlayers[teamIndex].GetTeamRow().SetName(teamName);
        
        // if (!ReferenceEquals(null, cascadeScoreScreen))
        // {
        //     cascadeScoreScreen.UpdateTeamName(teamIndex, teamName);
        // }
    }

    public void UpdatePlayer(int playerId, string playerName)
    {
#if TKCH_DEBUG_SCORE
        table._Log($"TKCH BilliardsScoreScreen::UpdatePlayer() playerId => {playerId}, playerName => {playerName}");
#elif TKCH_SYNC_SCORE
        table._Log($"TKCH BilliardsScoreScreen::UpdatePlayer() playerId => {playerId}, playerName => {playerName}, rowSyncOverrideLock => {rowSyncOverrideLock}");
#endif
#if TKCH_SYNC_SCORE
        if (rowSyncOverrideLock && playerName == "")
        {
            return;
        }
#endif
        
#if TKCH_DEBUG_SCORE
        //table._Log($"TKCH BilliardsScoreScreen::UpdatePlayer() playerId => {playerId}, playerName => {playerName}, rowSyncOverrideLock => {rowSyncOverrideLock}");
#endif
        
        int teamIndex = playerId % 2;
        if (teamIndex < teamPlayers.Length && !ReferenceEquals(null, teamPlayers[teamIndex]))
        {
            var playerRow = teamPlayers[teamIndex].GetPlayerRow((int)(playerId / 2));
            if (!ReferenceEquals(null, playerRow))
            {
                playerRow.SetName(playerName);
            }
        }
    }

    public int WinnerTeamId()
    {
        // if (highGroup.GetTeamPoint() > lowGroup.GetTeamPoint())
        // {
        //     return 0;
        // }
        // else if (highGroup.GetTeamPoint() < lowGroup.GetTeamPoint())
        // {
        //     return 1;
        // }
        //
        // return -1;

        int winnerTeamId = -1;
        int maxPoint = Int32.MinValue;
        
        for (int i = 0; i < teamPlayers.Length; i++)
        {
            if (ReferenceEquals(null, teamPlayers[i]))
            {
                continue;
            }

            int teamPoint = teamPlayers[i].GetTeamPoint();
            if (maxPoint < teamPoint)
            {
                winnerTeamId = i;
                maxPoint = teamPoint;
            }
            else if (maxPoint == teamPoint)
            {
                return -1;
            }
        }

        return winnerTeamId;
    }
    
    public int GetTeamPoint(int teamIndex)
    {
        return teamPlayers[teamIndex].GetTeamPoint();
    }

    public int GetTeamPocketBallCount(int teamIndex)
    {
        return teamPlayers[teamIndex].GetTeamPocketBallCount();
    }
    
    public int GetTeamShotCount(int teamIndex)
    {
        return teamPlayers[teamIndex].GetTeamShotCount();
    }
    
    public int GetTeamSafeNoPocketShotCount(int teamIndex)
    {
        return teamPlayers[teamIndex].GetTeamSafeNoPocketShotCount();
    }
    
    public int GetTeamScratchCount(int teamIndex)
    {
        return teamPlayers[teamIndex].GetTeamScratchCount();
    }
    
    public int GetTeamInvalidPocketBallCount(int teamIndex)
    {
        return teamPlayers[teamIndex].GetTeamInvalidPocketBallCount();
    }

    public int[] GetTeamScores(int teamIndex)
    {
        return teamPlayers[teamIndex].GetTeamRow().GetScores();
    }

    public int GetTeamScoreByColIndex(int teamIndex, int colIndex)
    {
        return teamPlayers[teamIndex].GetTeamRow().GetScoreByColIndex(colIndex);
    }

    public void SetTeamSafeNoPocketShotCountEmptyTextOnZero(int teamIndex, bool emptyTextOnZero)
    {
        teamPlayers[teamIndex].GetTeamRow().SetSafeNoPocketShotCountEmptyTextOnZero(emptyTextOnZero);
    }
}
