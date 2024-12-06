//#define TKCH_DEBUG_SCORE

using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class TeamPlayers : UdonSharpBehaviour
{
    [SerializeField] private string teamName;
    [SerializeField] private PlayerRow teamRow;
    [SerializeField] private PlayerRow playerRow1;
    [SerializeField] private PlayerRow playerRow2;
    [SerializeField] private PlayerRow playerRow3;
    [SerializeField] private PlayerRow playerRow4;

    private PlayerRow[] allRows = new PlayerRow[5];
    private PlayerRow[] playerRows = new PlayerRow[4];

    [NonSerialized] private BilliardsModule table;

    public BilliardsModule Table
    {
        get
        {
            return table;
        }
        set
        {
            table = value;
            foreach (PlayerRow playerRow in allRows)
            {
                if (ReferenceEquals(null, playerRow))
                {
                    continue;
                }
                playerRow.Table = table;
            }
        }
    }
    
    private void Start()
    {
#if TKCH_DEBUG_SCORE
        if (ReferenceEquals(null, table))
        {
            Debug.Log($"TKCH TeamPlayers::Start() [{GetInstanceID()}] table is null ? {ReferenceEquals(null, table)}");
        }
        else
        {
            table._Log($"TKCH TeamPlayers::Start() [{GetInstanceID()}] table is null ? {ReferenceEquals(null, table)}");
        }
#endif
        allRows = new[] { teamRow, playerRow1, playerRow2, playerRow3, playerRow4 };
        playerRows = new[] { playerRow1, playerRow2, playerRow3, playerRow4 };
    }

    public void Init()
    {
#if TKCH_DEBUG_SCORE
        if (ReferenceEquals(null, table))
        {
            Debug.Log($"TKCH TeamPlayers::Init() [{GetInstanceID()}] table is null ? {ReferenceEquals(null, table)}");
        }
        else
        {
            table._Log($"TKCH TeamPlayers::Init() [{GetInstanceID()}] table is null ? {ReferenceEquals(null, table)}");
        }
#endif
        allRows = new[] { teamRow, playerRow1, playerRow2, playerRow3, playerRow4 };
        playerRows = new[] { playerRow1, playerRow2, playerRow3, playerRow4 };
        
        foreach (var playerRow in allRows)
        {
            if (ReferenceEquals(null, playerRow))
            {
                continue;
            }
            playerRow.Init();
            playerRow.Clear(false);
        }

        teamRow.SetName("[Team]");
        teamRow.Clear(true);
    }

    public void Clear()
    {
        teamRow.Clear(true);
        foreach (var playerRow in playerRows)
        {
            if (ReferenceEquals(null, playerRow))
            {
                continue;
            }
#if true //TKCH_SYNC_SCORE
            if (string.Empty == playerRow.GetName())
            {
                playerRow.Clear(false);
            }
            else
            {
                playerRow.Clear(true);
            }
#else
            playerRow.Clear(true);
#endif
        }
    }
    
    public void TeamScoreUpdate(
        int playerId,
        bool isScratch,
        bool isFoul,
        int pocketCount,
        int pointCount,
        int shotCount
    )
    {
        teamRow.ScoreUpdate(
            isScratch,
            isFoul,
            pocketCount,
            pointCount,
            shotCount
        );

        if (0 <= playerId && playerId < 4)
        {
            playerRows[playerId].ScoreUpdate(
                isScratch,
                isFoul,
                pocketCount,
                pointCount,
                shotCount
            );
        }
    }

    public PlayerRow GetTeamRow()
    {
        return teamRow;
    }
    
    public PlayerRow GetPlayerRow(int index)
    {
        return playerRows[index];
    }

    public int GetTeamPoint()
    {
        return teamRow.GetPoint();
    }

    public int GetTeamScratchCount()
    {
        return teamRow.GetScratchCount();
    }

    public int GetTeamPocketBallCount()
    {
        return teamRow.GetPocketBallCount();
    }
    
    public int GetTeamShotCount()
    {
        return teamRow.GetShotCount();
    }
    
    public int GetTeamSafeNoPocketShotCount()
    {
        return teamRow.GetSafeNoPocketShotCount();
    }
    
    public int GetTeamInvalidPocketBallCount()
    {
        return teamRow.GetInvalidPocketBallCount();
    }
}
