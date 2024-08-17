//#define TKCH_DEBUG_SCORE

using System;
//using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class PlayerRow : UdonSharpBehaviour
{
    [SerializeField] private Text nameText;
    [SerializeField] private Text pointText;
    [SerializeField] private Text shotCountText;
    [SerializeField] private Text safeNoPocketShotCountText;
    [SerializeField] private Text scratchCountText;
    [SerializeField] private Text pocketBallCountText;
    [SerializeField] private Text invalidPocketBallCountText;

    private Text[] allTexts = new Text[7];
    private Text[] scoreTexts = new Text[6];

    // Method is not exposed to Udon: 'scoreDict.Add(textComponent, 0)'
    //private Dictionary<Text, int> scoreDict = new Dictionary<Text, int>();
    //[SerializeField, UdonSynced, FieldChangeCallback(nameof(setScores))]
    private int[] scores = new int[6];
    private bool[] scoreSigned = new bool[6];
    private bool[] scoreEmptyTextOnZero = new bool[6];

    /*
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ScoreSyncValue))]
    private uint scoreSyncValue = 0x0u;
    private bool scoreSynced = false;
    */
    
    [NonSerialized] private BilliardsModule table;

    public BilliardsModule Table
    {
        get
        {
            return table;
        }
        set
        {
#if TKCH_DEBUG_SCORE
            Debug.Log($"TKCH PlayerRow::Table set [{GetInstanceID()}]");
#endif
            table = value;
        }
    }
    
#if TKCH_SYNC_SCORE
    /*
    public uint ScoreSyncValue
    {
        get => scoreSyncValue;
        set
        {
#if TKCH_DEBUG_SCORE
            table._Log($"TKCH PlayerRow::ScoreSyncValue [{GetInstanceID()}] value => " + string.Format("{0:X8}", value));
#endif
            if (!scoreSynced)
            {
                scoreSynced = true;
                scoreSyncValue = value;
                DecodeSyncValueAdd(value);
            }
        }
    }
    */
    
    public void DecodeSyncValueAdd(uint value)
    {
        int point = (int)((value >> 24) & 0xFFu);
        int shotCount = (int)((value >> 17) & 0x3Fu);
        int safeNoPocketShotCount = (int)((value >> 11) & 0x3Fu);
        int scratchCount = (int)((value >> 8) & 0x7u);
        int pocketBallCount = (int)((value >> 4) & 0xFu);
        int invalidPocketBallCount = (int)((value >> 0) & 0xFu);
                
#if TKCH_DEBUG_SCORE
        table._Log($"TKCH PlayerRow::DecodeSyncValueAdd [{GetInstanceID()}] point => {point}, shotCount => {shotCount}, safeNoPocketShotCount => {safeNoPocketShotCount}, scratchCount => {scratchCount}, pocketBallCount => {pocketBallCount}, invalidPocketBallCount => {invalidPocketBallCount}");
#endif

        //SetName("***SYNCED***");
        if (nameText.text == string.Empty)
        {
            nameText.text = "***SYNCED***";
        }
                
        scores[Array.IndexOf(scoreTexts, pointText)] += point;
        scores[Array.IndexOf(scoreTexts, shotCountText)] += shotCount;
        scores[Array.IndexOf(scoreTexts, safeNoPocketShotCountText)] += safeNoPocketShotCount;
        scores[Array.IndexOf(scoreTexts, scratchCountText)] += scratchCount;
        scores[Array.IndexOf(scoreTexts, pocketBallCountText)] += pocketBallCount;
        scores[Array.IndexOf(scoreTexts, invalidPocketBallCountText)] += invalidPocketBallCount;

        UpdateText();

        //nameText.text = "***SYNCED***";
    }
#else
    public void DecodeSyncValue(uint[] values)
    {
        uint value = values[0];
        int point = (int)((value >> 16) & 0xFFFFu); // 0xFFFF
        if (scoreSigned[Array.IndexOf(scoreTexts, pointText)])
        {
            uint u = (value >> 16) & 0x7FFFu;
            if (0 < ((value >> 16) & 0x8000u))
            {
                u |= 0xFFFF8000u;
                u = ~u;
                point = 0 - ((int)u + 1);
            }
            else
            {
                point = (int)u;
            }
        }
        int scratchCount = (int)((value >> 8) & 0xFFu); // 0xFF
        int pocketBallCount = (int)((value >> 0) & 0xFFu); // 0xFF

        value = values[1];
        int shotCount = (int)((value >> 20) & 0xFFFu); // 0xFFF
        int safeNoPocketShotCount = (int)((value >> 10) & 0x3FFu); // 0x3FF
        int invalidPocketBallCount = (int)((value >> 0) & 0x3FFu); // 0x3FF
                
#if TKCH_DEBUG_SCORE
        table._Log($"TKCH PlayerRow::DecodeSyncValue [{GetInstanceID()}] point => {point}, shotCount => {shotCount}, safeNoPocketShotCount => {safeNoPocketShotCount}, scratchCount => {scratchCount}, pocketBallCount => {pocketBallCount}, invalidPocketBallCount => {invalidPocketBallCount}");
#endif

        if (0 <= Array.IndexOf(scoreTexts, pointText)) scores[Array.IndexOf(scoreTexts, pointText)] = point; // - 127;
        if (0 <= Array.IndexOf(scoreTexts, shotCountText)) scores[Array.IndexOf(scoreTexts, shotCountText)] = shotCount;
        if (0 <= Array.IndexOf(scoreTexts, safeNoPocketShotCountText)) scores[Array.IndexOf(scoreTexts, safeNoPocketShotCountText)] = safeNoPocketShotCount;
        if (0 <= Array.IndexOf(scoreTexts, scratchCountText)) scores[Array.IndexOf(scoreTexts, scratchCountText)] = scratchCount;
        if (0 <= Array.IndexOf(scoreTexts, pocketBallCountText)) scores[Array.IndexOf(scoreTexts, pocketBallCountText)] = pocketBallCount;
        if (0 <= Array.IndexOf(scoreTexts, invalidPocketBallCountText)) scores[Array.IndexOf(scoreTexts, invalidPocketBallCountText)] = invalidPocketBallCount;

        UpdateText();
    }
    
    public void DecodeSyncValue_Mini(uint value)
    {
        int shotCount = (int)((value >> 24) & 0xFFu);
        int scratchCount = (int)((value >> 16) & 0xFFu);
        int safeNoPocketShotCount = (int)((value >> 8) & 0xFFu);
        int point = (int)((value >> 0) & 0xFFu);
        if (scoreSigned[Array.IndexOf(scoreTexts, pointText)])
        {
            uint u = (value >> 0) & 0x7Fu;
            if (0 < ((value >> 0) & 0x80u))
            {
                u |= 0xFFFFFF80u;
                u = ~u;
                point = 0 - ((int)u + 1);
            }
            else
            {
                point = (int)u;
            }
        }

        int pocketBallCount = 0;
        int invalidPocketBallCount = 0;
                
#if TKCH_DEBUG_SCORE
        table._Log($"TKCH PlayerRow::DecodeSyncValue_Mini [{GetInstanceID()}] point => {point}, shotCount => {shotCount}, safeNoPocketShotCount => {safeNoPocketShotCount}, scratchCount => {scratchCount}, pocketBallCount => {pocketBallCount}, invalidPocketBallCount => {invalidPocketBallCount}");
#endif

        if (0 <= Array.IndexOf(scoreTexts, pointText)) scores[Array.IndexOf(scoreTexts, pointText)] = point; // - 127;
        if (0 <= Array.IndexOf(scoreTexts, shotCountText)) scores[Array.IndexOf(scoreTexts, shotCountText)] = shotCount;
        if (0 <= Array.IndexOf(scoreTexts, safeNoPocketShotCountText)) scores[Array.IndexOf(scoreTexts, safeNoPocketShotCountText)] = safeNoPocketShotCount;
        if (0 <= Array.IndexOf(scoreTexts, scratchCountText)) scores[Array.IndexOf(scoreTexts, scratchCountText)] = scratchCount;
        if (0 <= Array.IndexOf(scoreTexts, pocketBallCountText)) scores[Array.IndexOf(scoreTexts, pocketBallCountText)] = pocketBallCount;
        if (0 <= Array.IndexOf(scoreTexts, invalidPocketBallCountText)) scores[Array.IndexOf(scoreTexts, invalidPocketBallCountText)] = invalidPocketBallCount;

        UpdateText();
    }
#endif

    private void Start()
    {
        allTexts = new[] { nameText, pointText, shotCountText, safeNoPocketShotCountText, scratchCountText, pocketBallCountText, invalidPocketBallCountText};
        scoreTexts = new[] { pointText, shotCountText, safeNoPocketShotCountText, scratchCountText, pocketBallCountText, invalidPocketBallCountText };
        /*
        foreach (var textComponent in scoreTexts)
        {
            scoreDict.Add(textComponent, 0);
        }
        */
        scoreSigned = new[] { true, false, false, false, false, false };
        scoreEmptyTextOnZero = new[] { false, false, false, false, false, false };
    }

    /*
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
#if TKCH_DEBUG_SCORE
        table._Log("TKCH PlayerRow::OnPlayerJoined()");
#endif

        if (ReferenceEquals(null, player))
        {
#if TKCH_DEBUG_SCORE
            table._Log("TKCH PlayerRow::OnPlayerJoined() player is null");
#endif
            return;
        }
        
        if (player.isLocal)
        {
#if TKCH_DEBUG_SCORE
            table._Log("TKCH PlayerRow::OnPlayerJoined() player isLocal");
#endif
            return;
        }
        
        if (ReferenceEquals(null, Networking.LocalPlayer))
        {
#if TKCH_DEBUG_SCORE
            table._Log("TKCH PlayerRow::OnPlayerJoined() LocalPlayer is null");
#endif
            return;
        }
        
        if (Networking.LocalPlayer.isMaster)
        {
#if TKCH_DEBUG_SCORE
            table._Log("TKCH PlayerRow::OnPlayerJoined() isMaster => {Networking.LocalPlayer.isMaster} RequestSerialization()");

            table._LogError($"TKCH PlayerRow::OnPlayerJoined() point => {scores[Array.IndexOf(scoreTexts, pointText)]}, shotCount => {scores[Array.IndexOf(scoreTexts, shotCountText)]}, safeNoPocketShotCount => {scores[Array.IndexOf(scoreTexts, safeNoPocketShotCountText)]}");
            table._LogError($"TKCH PlayerRow::OnPlayerJoined() scratchCount => {scores[Array.IndexOf(scoreTexts, scratchCountText)]}, pocketBallCount => {scores[Array.IndexOf(scoreTexts, pocketBallCountText)]}, invalidPocketBallCount => {scores[Array.IndexOf(scoreTexts, invalidPocketBallCountText)]}");
#endif

            // scoreSyncValue = 0x0u;
            // scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, pointText)] & 0xFFu) << 24);
            // scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, shotCountText)] & 0x3Fu) << 17);
            // scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, safeNoPocketShotCountText)] & 0x3Fu) << 11);
            // scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, scratchCountText)] & 0x7u) << 8);
            // scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, pocketBallCountText)] & 0xFu) << 4);
            // scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, invalidPocketBallCountText)] & 0xFu) << 0);

            scoreSyncValue = EncodeScoreSyncValue();
            
#if TKCH_DEBUG_SCORE
            table._Log("TKCH PlayerRow::OnPlayerJoined() RequestSerialization() scoreSyncValue => " + string.Format("{0:X8}", scoreSyncValue));
#endif
            RequestSerialization();
        }
    }
    */

#if TKCH_SYNC_SCORE
    public uint EncodeScoreSyncValue()
    {
        uint scoreSyncValue = 0x0u;
        scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, pointText)] & 0xFFu) << 24);
        scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, shotCountText)] & 0x3Fu) << 17);
        scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, safeNoPocketShotCountText)] & 0x3Fu) << 11);
        scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, scratchCountText)] & 0x7u) << 8);
        scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, pocketBallCountText)] & 0xFu) << 4);
        scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, invalidPocketBallCountText)] & 0xFu) << 0);
        return scoreSyncValue;
    }
#else
    public uint[] EncodeScoreSyncValue()
    {
        uint[] scoreSyncValues = new uint[2];

        uint scoreSyncValue = 0x0u;
        
        //scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, pointText)] & 0xFFFFu) << 16);
        scoreSyncValue |= (uint)(
            (
                scores[Array.IndexOf(scoreTexts, pointText)] &
                (scoreSigned[Array.IndexOf(scoreTexts, pointText)] ? 0x7FFFu : 0xFFFFu)) |
            (scoreSigned[Array.IndexOf(scoreTexts, pointText)] && scores[Array.IndexOf(scoreTexts, pointText)] < 0 ? 0x8000u : 0x0u)
        ) << 16;
        
        scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, scratchCountText)] & 0xFFu) << 8);
        scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, pocketBallCountText)] & 0xFFu) << 0);
        scoreSyncValues[0] = scoreSyncValue;

        scoreSyncValue = 0x0u;
        scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, shotCountText)] & 0xFFFu) << 20);
        scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, safeNoPocketShotCountText)] & 0x3FFu) << 10);
        scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, invalidPocketBallCountText)] & 0x3FFu) << 0);
        scoreSyncValues[1] = scoreSyncValue;
        
        return scoreSyncValues;
    }

    public uint[] EncodeScoreParams(int point, int scratchCount, int pocketBallCount, int shotCount, int safeNoPocketShotCount, int invalidPocketBallCount)
    {
        uint[] scoreSyncValues = new uint[2];

        uint scoreSyncValue = 0x0u;
        
        scoreSyncValue |= (uint)(
            (
                point &
                (scoreSigned[Array.IndexOf(scoreTexts, pointText)] ? 0x7FFFu : 0xFFFFu)) |
            (scoreSigned[Array.IndexOf(scoreTexts, pointText)] && point < 0 ? 0x8000u : 0x0u)
        ) << 16;
        
        scoreSyncValue |= (uint)((scratchCount & 0xFFu) << 8);
        scoreSyncValue |= (uint)((pocketBallCount & 0xFFu) << 0);
        scoreSyncValues[0] = scoreSyncValue;

        scoreSyncValue = 0x0u;
        scoreSyncValue |= (uint)((shotCount & 0xFFFu) << 20);
        scoreSyncValue |= (uint)((safeNoPocketShotCount & 0x3FFu) << 10);
        scoreSyncValue |= (uint)((invalidPocketBallCount & 0x3FFu) << 0);
        scoreSyncValues[1] = scoreSyncValue;
        
        return scoreSyncValues;
    }

    public uint EncodeScoreSyncValue_Mini()
    {
        return EncodeScoreParams_Mini(
            scores[Array.IndexOf(scoreTexts, pointText)],
            scores[Array.IndexOf(scoreTexts, shotCountText)],
            scores[Array.IndexOf(scoreTexts, scratchCountText)],
            scores[Array.IndexOf(scoreTexts, safeNoPocketShotCountText)]
            );
    }

    public uint EncodeScoreParams_Mini(int point, int shotCount, int scratchCount, int safeNoPocketShotCount)
    {
        uint scoreSyncValue = 0x0u;
        scoreSyncValue |= (uint)((shotCount & 0xFFu) << 24);
        scoreSyncValue |= (uint)((scratchCount & 0xFFu) << 16);
        scoreSyncValue |= (uint)((safeNoPocketShotCount & 0xFFu) << 8);
        //scoreSyncValue |= (uint)(((point /* + 127 */ ) & 0xFFu) << 0);
        if (scoreSigned[Array.IndexOf(scoreTexts, pointText)])
        {
            scoreSyncValue |= (uint)(
                point & 0x7Fu | (point < 0 ? 0x80u : 0x0u)
            ) << 0;
        }
        else
        {
            scoreSyncValue |= (uint)(point & 0xFFu) << 0;
        }
        
        return scoreSyncValue;
    }
#endif
    
    /*
    public override void OnDeserialization()
    {
#if TKCH_DEBUG_SCORE
        table._Log("TKCH PlayerRow::OnDeserialization()");
#endif
        //UpdateText();
    }
    */

    private void UpdateText()
    {
        if (nameText.text == string.Empty)
        {
            return;
        }   
        
        foreach (Text textComponent in scoreTexts)
        {
            if (ReferenceEquals(null, textComponent))
            {
                continue;
            }

            int score = scores[Array.IndexOf(scoreTexts, textComponent)];
            bool emptyTextOnZero = scoreEmptyTextOnZero[Array.IndexOf(scoreTexts, textComponent)];
            textComponent.text = (emptyTextOnZero && (score == 0)) ? "" : $"{score}";
        }
    }

    public void Clear()
    {
#if TKCH_DEBUG_SCORE
        table._Log("TKCH PlayerRow::Clear() [{GetInstanceID()}]");
        //Debug.Log($"TKCH PlayerRow::Clear() [{GetInstanceID()}]");
#endif
        foreach (Text textComponent in allTexts)
        {
            if (ReferenceEquals(null, textComponent))
            {
                continue;
            }
            textComponent.text = string.Empty;
        }
        for (int i = 0; i < scores.Length; i++)
        {
            scores[i] = 0;
        }
    }

    public void Init()
    {
#if TKCH_DEBUG_SCORE
        table._Log($"TKCH PlayerRow::Init() [{GetInstanceID()}]");
        //Debug.Log($"TKCH PlayerRow::Init() [{GetInstanceID()}]");
#endif
        foreach (Text textComponent in scoreTexts)
        {
            if (ReferenceEquals(null, textComponent))
            {
                continue;
            }
            textComponent.text = "0";
            //scoreDict[textComponent] = 0;
        }
        for (int i = 0; i < scores.Length; i++)
        {
            scores[i] = 0;
        }
    }
    
    public void SetName(string str)
    {
#if TKCH_DEBUG_SCORE
        table._Log($"TKCH PlayerRow::SetName() [{GetInstanceID()}] nameText => {nameText.text}, str => {str}");
        //Debug.Log($"TKCH PlayerRow::SetName() [{GetInstanceID()}] {str}");
#endif
        if (nameText.text == string.Empty && str != string.Empty)
        {
            Init();
        }
        else if (nameText.text != string.Empty && str == string.Empty)
        {
            Clear();
        }
        nameText.text = str;
    }

    public string GetName()
    {
        return nameText.text;
    }
    
    
    public void ScoreUpdate(
        bool isScratch,
        bool isFoul,
        int pocketCount,
        int pointCount,
        int shotCount
    )
    {
#if TKCH_DEBUG_SCORE
        table._Log($"TKCH PlayerRow::ScoreUpdate() [{GetInstanceID()}]");
        //Debug.Log("TKCH PlayerRow::ScoreUpdate()");
#endif
        //scoreDict[shotCountText]++;
        int idx = Array.IndexOf(scoreTexts, shotCountText);
        if (scores[idx] < 0xFFF)
        {
            scores[idx] += shotCount;
        }
        
        //scoreDict[pointText] += pointCount;
        idx = Array.IndexOf(scoreTexts, pointText);
        if (scores[idx] < 0xFFFF)
        {
            scores[idx] += pointCount;
        }
        
        if (isScratch || isFoul) // とりあえず、ノータッチをファウルとしてここにカウントする
        {
            //scoreDict[scratchCountText]++;
            idx = Array.IndexOf(scoreTexts, scratchCountText);
            if (scores[idx] < 0xFF)
            {
                scores[idx]++;
            }
        }

        if (isFoul)
        {
            //scoreDict[foulCountText]++;
            //scoreDict[invalidPocketBallCountText] += pocketCount;
            idx = Array.IndexOf(scoreTexts, invalidPocketBallCountText);
            if (scores[idx] < 0x3FF)
            {
                scores[idx] += pocketCount;
            }
        }
        else
        {
            //scoreDict[pocketBallCountText] += pocketCount;
            idx = Array.IndexOf(scoreTexts, pocketBallCountText);
            if (scores[idx] < 0x3FF)
            {
                scores[idx] += pocketCount;
            }
        }

        if (0 == pocketCount && !isFoul)
        {
            //scoreDict[safeNoPocketShotCountText]++;
            idx = Array.IndexOf(scoreTexts, safeNoPocketShotCountText);
            if (scores[idx] < 0x3FF)
            {
                scores[idx]++;
            }
        }

        UpdateText();
    }

    public int GetPoint()
    {
        return scores[Array.IndexOf(scoreTexts, pointText)];
    }

    public int GetScratchCount()
    {
        return scores[Array.IndexOf(scoreTexts, scratchCountText)];
    }
    
    public int GetPocketBallCount()
    {
        return scores[Array.IndexOf(scoreTexts, pocketBallCountText)];
    }

    public int GetShotCount()
    {
        return scores[Array.IndexOf(scoreTexts, shotCountText)];
    }

    public int GetSafeNoPocketShotCount()
    {
        return scores[Array.IndexOf(scoreTexts, safeNoPocketShotCountText)];
    }

    public int GetInvalidPocketBallCount()
    {
        return scores[Array.IndexOf(scoreTexts, invalidPocketBallCountText)];
    }

    public void SetPointSigned(bool signed)
    {
        scoreSigned[Array.IndexOf(scoreTexts, pointText)] = signed;
    }

    public void SetSafeNoPocketShotCountEmptyTextOnZero(bool emptyTextOnZero)
    {
        scoreEmptyTextOnZero[Array.IndexOf(scoreTexts, safeNoPocketShotCountText)] = emptyTextOnZero;
        UpdateText();
    }
    
    public void SetInvalidPocketBallCountEmptyTextOnZero(bool emptyTextOnZero)
    {
#if TKCH_DEBUG_SCORE
        table._Log($"TKCH PlayerRow::SetInvalidPocketBallCountEmptyTextOnZero() [{GetInstanceID()}]");
        //Debug.Log("TKCH PlayerRow::SetInvalidPocketBallCountEmptyTextOnZero()");
#endif
        scoreEmptyTextOnZero[Array.IndexOf(scoreTexts, invalidPocketBallCountText)] = emptyTextOnZero;
        UpdateText();
    }
}
