#if UNITY_ANDROID
#define HT_QUEST
#endif

#if !HT_QUEST || true
#define HT8B_DEBUGGER
#endif

//#define TKCH_DEBUG_IN_KITCHEN
//#define TKCH_DEBUG_UPON_FOOT
//#define TKCH_DEBUG_BREAKING_FOUL
//#define TKCH_DEBUG_CALLSHOT_DELAY
//#define TKCH_DEBUG_CALLSHOT_POCKETEDBALL
//#define TKCH_DEBUG_SCORE
//#define TKCH_DEBUG_WINRACKCOUNT
//#define TKCH_DEBUG_SEMIAUTO_CALL
//#define TKCH_DEBUG_SEMIAUTO_CALL_SIDE
//#define TKCH_DEBUG_AVG
#define TKCH_DEBUG_14RACK
//#define TKCH_DEBUG_BREAKBALL
//#define TKCH_DEBUG_OPENING_BREAK
//#define TKCH_DEBUG_DENYBALLS
//#define TKCH_DEBUG_NEXT_BREAK
//#define TKCH_DEBUG_CUEBALL_OVER_KICHENLINE
//#define TKCH_DEBUG_SPECIAL_PENALTY
#define TKCH_DEBUG_CLEAR_CHAINED_FOUL

#define TKCH_CALLSHOT_CALLEDPBALL_DELAY
#define TKCH_CALLSHOT_CALLEDPOCKET_DELAY

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using System;
using System.IO;
using Metaphira.Modules.CameraOverride;
using Unity.Mathematics;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class BilliardsModule : UdonSharpBehaviour
{
    [NonSerialized] public readonly string[] DEPENDENCIES = new string[] { nameof(CameraOverrideModule) };
    [NonSerialized] public readonly string VERSION = "6.0.0 straight";

    // table model properties
    [NonSerialized] public float k_TABLE_WIDTH; // horizontal span of table
    [NonSerialized] public float k_TABLE_HEIGHT; // vertical span of table
    [NonSerialized] public float k_CUSHION_RADIUS; // The roundess of colliders
    [NonSerialized] public float k_POCKET_RADIUS; // Full diameter of pockets
    [NonSerialized] public float k_INNER_RADIUS; // Pocket 'hitbox' cylinder
    [NonSerialized] public Vector3 k_vE; // corner pocket data
    [NonSerialized] public Vector3 k_vF; // side pocket data
    [NonSerialized] public GameObject[] pockets;
    [NonSerialized] public Vector3 k_rack_position = new Vector3();
    private Vector3 k_rack_direction = new Vector3();
    private GameObject auto_rackPosition;
    [NonSerialized] public GameObject auto_pocketblockers;
    private GameObject auto_colliderBaseVFX;
    [NonSerialized] public Transform table;
    [NonSerialized] public GameObject[] pointPocketMarkers;
    [NonSerialized] public GameObject[] pointPocketMarkerSphere;
    private float findNearestPocket_x;
    private float findNearestPocket_n;

    // table colors
    [SerializeField] [HideInInspector] public Color k_colour_foul;        // v1.6: ( 1.2, 0.0, 0.0, 1.0 )
    [SerializeField] [HideInInspector] public Color k_colour_default;     // v1.6: ( 1.0, 1.0, 1.0, 1.0 )
    [SerializeField] [HideInInspector] public Color k_colour_off = new Color(0.01f, 0.01f, 0.01f, 1.0f);

    // 8/9 ball
    [SerializeField] [HideInInspector] public Color k_teamColour_spots;   // v1.6: ( 0.00, 0.75, 1.75, 1.0 )
    [SerializeField] [HideInInspector] public Color k_teamColour_stripes; // v1.6: ( 1.75, 0.25, 0.00, 1.0 )

    // 4 ball
    [SerializeField] [HideInInspector] public Color k_colour4Ball_team_0; // v1.6: ( )
    [SerializeField] [HideInInspector] public Color k_colour4Ball_team_1; // v1.6: ( 2.0, 1.0, 0.0, 1.0 )

    // fabrics
    [SerializeField] [HideInInspector] public Color k_fabricColour_8ball; // v1.6: ( 0.3, 0.3, 0.3, 1.0 )
    [SerializeField] [HideInInspector] public Color k_fabricColour_9ball; // v1.6: ( 0.1, 0.6, 1.0, 1.0 )
    [SerializeField] [HideInInspector] public Color k_fabricColour_4ball; // v1.6: ( 0.15, 0.75, 0.3, 1.0 )

    // cue guideline
    private readonly Color k_aimColour_aim = new Color(0.7f, 0.7f, 0.7f, 1.0f);
    private readonly Color k_aimColour_locked = new Color(1.0f, 1.0f, 1.0f, 1.0f);

    // textures
    [SerializeField] public Texture[] textureSets;
    [SerializeField] public ModelData[] tableModels;
    [SerializeField] public Texture2D[] tableSkins;
    [SerializeField] public Texture2D[] cueSkins;

    // hooks
    [SerializeField] public UdonBehaviour tableSkinHook;
    [SerializeField] public UdonBehaviour cueSkinHook;
    [SerializeField] public UdonBehaviour nameColorHook;

    // globals
    [NonSerialized] public AudioSource aud_main;
    [NonSerialized] public UdonBehaviour callbacks;
    private Vector3[][] initialPositions = new Vector3[5][];
    private uint[] initialBallsPocketed = new uint[5];

    // constants
    private const float k_BALL_RADIUS = 0.03f;
    private const float k_BALL_DIAMETRE = 0.06f;
    private const float k_BALL_PL_X = 0.03f; // break placement X
    private const float k_BALL_PL_Y = 0.05196152422f; // sin(60) * 0.06
    private const float k_RANDOMIZE_F = 0.0001f;
    private const float k_SPOT_POSITION_X = 0.5334f; // First X position of the racked balls
    private const float k_SPOT_CAROM_X = 0.8001f; // Spot position for carom mode
    private readonly int[] break_order_8ball = { 9, 2, 10, 11, 1, 3, 4, 12, 5, 13, 14, 6, 15, 7, 8 };
    private readonly int[] break_order_straight = { 15, 3, 4, 5, 7, 8, 1, 9, 10, 11, 6, 12, 13, 14, 2 };
#if TKCH_DEBUG_14RACK
    private readonly int[] ball_id_to_number = { 0, 8, 1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12, 13, 14, 15 };
#endif
    private readonly int[] break_order_9ball = { 2, 3, 4, 5, 9, 6, 7, 8, 1 };
    private readonly int[] break_rows_9ball = { 0, 1, 2, 1, 0 };
    private readonly uint straight_pocket_mask = 0xFFFEu;
#if TKCH_DEBUG_SEMIAUTO_CALL || TKCH_DEBUG_SEMIAUTO_CALL_SIDE ||TKCH_DEBUG_NEXT_BREAK
    private bool debugLogFlg = false;
#endif

    #region InspectorValues
    [Header("Debug")]
    [SerializeField] public string logLabel;

    [Header("Call Shot")]
    [SerializeField] public GameObject markerCalledBall;
    [SerializeField] Material calledBallMarkerBlue;
    [SerializeField] Material calledBallMarkerOrange;
    [SerializeField] Material calledBallMarkerWhite;

    [Header("Straight")]
    [SerializeField] public BilliardsScoreScreen scoreScreen;
    [SerializeField] public GameObject markerHeadSpot;
    [SerializeField] public GameObject markerCenterSpot;
    [SerializeField] public GameObject markerFootSpot;
    [SerializeField] public GameObject requestBreakOrange;
    [SerializeField] public GameObject requestBreakBlue;

    [Header("Managers")]
    [SerializeField] public NetworkingManager networkingManager;
    [SerializeField] public PracticeManager practiceManager;
    [SerializeField] public RepositionManager repositionManager;
    [SerializeField] public DesktopManager desktopManager;
    [SerializeField] public CameraManager cameraManager;
    [SerializeField] public GraphicsManager graphicsManager;
    [SerializeField] public LegacyPhysicsManager legacyPhysicsManager;
    [SerializeField] public StandardPhysicsManager standardPhysicsManager;
    [SerializeField] public BetaPhysicsManager betaPhysicsManager;
    [SerializeField] public MenuManager menuManager;

    [Header("Camera Module")]
    [SerializeField] public UdonSharpBehaviour cameraModule;

    [Space(10)]
    [Header("Sound Effects")]
    [SerializeField] AudioClip snd_Intro;
    [SerializeField] AudioClip snd_Sink;
    [SerializeField] AudioClip snd_NewTurn;
    [SerializeField] AudioClip snd_PointMade;
    [SerializeField] public AudioClip snd_btn;
    [SerializeField] public AudioClip snd_spin;
    [SerializeField] public AudioClip snd_spinstop;
    [SerializeField] AudioClip snd_hitball;

    [Space(10)]
    [Header("Internal (no touching!)")]
    // Other scripts
    [SerializeField] public CueController[] cueControllers;

    // GameObjects
    [SerializeField] public GameObject[] balls;
    [SerializeField] public GameObject guideline;
    [SerializeField] public GameObject devhit;
    [SerializeField] public GameObject markerObj;
    [SerializeField] public GameObject marker9ball;

    // Texts
    [SerializeField] Text ltext;
    [SerializeField] Text infReset;

    [SerializeField] ReflectionProbe reflection_main;
    #endregion

    // debugger
    [NonSerialized] public int PERF_MAIN = 0;
    [NonSerialized] public int PERF_PHYSICS_MAIN = 1;
    [NonSerialized] public int PERF_PHYSICS_VEL = 2;
    [NonSerialized] public int PERF_PHYSICS_BALL = 3;
    [NonSerialized] public int PERF_PHYSICS_CUSHION = 4;
    [NonSerialized] public int PERF_PHYSICS_POCKET = 5;

    [NonSerialized] public const int PERF_MAX = 6;
    private string[] perfNames = new string[] {
      "main",
      "physics",
      "physicsVel",
      "physicsBall",
      "physicsCushion",
      "physicsPocket"
   };
    private float[] perfCounters = new float[PERF_MAX];
    private float[] perfTimings = new float[PERF_MAX];
    private float[] perfStart = new float[PERF_MAX];
    private const int LOG_MAX = 32;
    private int LOG_LEN = 0;
    private int LOG_PTR = 0;
    private string[] LOG_LINES = new string[32];
    
    // cached copies of networked data, may be different from local game state
    [NonSerialized] public string[] playerNamesCached = new string[4];

    // local game state
    [NonSerialized] public bool lobbyOpen;
    [NonSerialized] public bool gameLive;
    [NonSerialized] public uint gameModeLocal;
    [NonSerialized] public int goalPointsLocal = 20;
    [NonSerialized] public uint rackConditionLocal = 1;
    [NonSerialized] public bool semiAutoCallBallLocal;
    [NonSerialized] public bool semiAutoCallPocketLocal;
    [NonSerialized] public uint timerLocal;
    [NonSerialized] public bool teamsLocal;
    [NonSerialized] public bool noGuidelineLocal;
    [NonSerialized] public bool noLockingLocal;
    [NonSerialized] public uint ballsPocketedLocal;
    [NonSerialized] public uint targetPocketedLocal;
    [NonSerialized] public uint otherPocketedLocal;
    [NonSerialized] public uint denyBallsLocal;
    [NonSerialized] public uint pointPocketsLocal;
    [NonSerialized] public uint calledBallsLocal;
    [NonSerialized] public uint teamIdLocal;
    [NonSerialized] public uint fourBallCueBallLocal;
    [NonSerialized] public bool isTableOpenLocal;
    [NonSerialized] public uint teamColorLocal;
    [NonSerialized] public string[] playerNamesLocal = new string[4];
    [NonSerialized] public string tournamentRefereeLocal;
    [NonSerialized] public int[] fbScoresLocal = new int[2];
    [NonSerialized] public int[] totalPointsLocal = new int[2];
    [NonSerialized] public int[] shotCountsLocal = new int[2];
    [NonSerialized] public int[] shotSuccessCountsLocal = new int[2];
    [NonSerialized] public int[] chainedPointsLocal = new int[2];
    [NonSerialized] public int[] chainedFoulsLocal = new int[2];
    // [NonSerialized] public bool noCushionLocal;
    [NonSerialized] public uint specialPenaltyLocal;
    [NonSerialized] public uint winningTeamLocal;
    [NonSerialized] public uint previewWinningTeamLocal;
    [NonSerialized] public int activeCueSkin;
    [NonSerialized] public int tableSkinLocal;
    [NonSerialized] public int physicsModeLocal;
    [NonSerialized] public int stateIdLocal;
    private byte gameStateLocal = byte.MaxValue;
    private byte turnStateLocal = byte.MaxValue;
    private int timerStartLocal;
    private uint repositionStateLocal;
    private uint nextBallRepositionStateLocal;
    private int tableModelLocal;
    private bool callShotLockLocal;
    private bool calledBallOff = false;
    private bool calledPocketOff = false;
    private int inningCountLocal;
    private int[] winRackCountLocal = new int[2];
    private int breakBallIdLocal;
    private bool isOpeningBreakLocal = true;
#if TKCH_CALLSHOT_CALLEDPBALL_DELAY || TKCH_CALLSHOT_CALLEDPOCKET_DELAY
    private float callShotDelay = 0.4f;
#endif
#if TKCH_CALLSHOT_CALLEDPBALL_DELAY
    private int calledBallId = -2;
    private float calledBallIdDelayTimestamp = 0;
#endif
#if TKCH_CALLSHOT_CALLEDPOCKET_DELAY
    private int calledPocketId = -2;
    private float calledPocketIdDelayTimestamp = 0;
#endif
    // private bool semiAutoCalledBall;
    private bool semiAutoCalledPocket;
    private float semiAutoCalledTimeBall;

    // physics simulation data, must be reset before every simulation
    [NonSerialized] public bool isLocalSimulationRunning;
    private bool isLocalSimulationOurs = false;

    private uint ballsPocketedOrig;
    private uint targetPocketedOrig;
    private uint otherPocketedOrig;
    private int firstHit = 0;
    private int secondHit = 0;
    private int thirdHit = 0;
    private int cushionAfterFirstHit = 0;
    private uint cushionObjectiveBallsOnBreak = 0x0u;
    private bool afterBreak;

    private bool fbMadePoint = false;
    private bool fbMadeFoul = false;

    // game state data
    [NonSerialized] public Vector3[] ballsP = new Vector3[16];
    [NonSerialized] public Vector3[] ballsV = new Vector3[16];
    [NonSerialized] public Vector3[] ballsW = new Vector3[16];

    [NonSerialized] public bool canPlayLocal;
    [NonSerialized] public bool isGuidelineValid;
    [NonSerialized] public bool canHitCueBall = false;
    [NonSerialized] public bool isReposition = false;
    [NonSerialized] public float repoMaxX;
    [NonSerialized] public bool timerRunning = false;

    [NonSerialized] public int localPlayerId = -1;
    [NonSerialized] public uint localTeamId = 0u;

    [NonSerialized] public UdonSharpBehaviour currentPhysicsManager;
    [NonSerialized] public CueController activeCue;

    // some udon optimizations
    [NonSerialized] public bool is8Ball = false;
    [NonSerialized] public bool is9Ball = false;
    [NonSerialized] public bool is4Ball = false;
    [NonSerialized] public bool isJp4Ball = false;
    [NonSerialized] public bool isKr4Ball = false;
    [NonSerialized] public bool isStraight = false;
    [NonSerialized] public bool isPracticeMode = false;
    [NonSerialized] public CameraOverrideModule cameraOverrideModule;
    public string[] moderators = new string[0];

    [NonSerialized] public Vector3[] pcketLocations = new Vector3[6];

    private Vector3[] findEasiestBallAndPocketConditions = new Vector3[]
    {
        // x:deg, y:t2p, z:c2t
        new Vector3(60.0f, 0.09f, 0.09f), // 0.3f, 0.3f // 0.06 * 5
        new Vector3(60.0f, 0.09f, 0.36f), // 0.3f, 0.6f // 0.06 * 5
        new Vector3(45.0f, 0.36f, 0.36f), // 0.6f, 0.6f // 0.06 * 10
        new Vector3(30.0f, 0.81f, 1.42f), // 0.9f, 1.2f // 0.06 * 12
        new Vector3(60.0f, 0.09f, float.MaxValue), // 0.3f, - // 0.06 * 5
        new Vector3(30.0f, 0.81f, 0.81f), // 0.9f, 0.9f // 0.06 * 12
        new Vector3(45.0f, 0.36f, float.MaxValue), // 0.6f, - // 0.06 * 10
        new Vector3(30.0f, 0.81f, float.MaxValue), // 0.9f, - // 0.06 * 12
        new Vector3(15.0f, float.MaxValue, float.MaxValue),
        new Vector3(30.0f, float.MaxValue, float.MaxValue),
        new Vector3(45.0f, float.MaxValue, float.MaxValue),
        new Vector3(60.0f, float.MaxValue, float.MaxValue),
        new Vector3(float.MaxValue, float.MaxValue, float.MaxValue)
    };

    
    private void OnEnable()
    {
        // scoreScreen.SetPointSigned(false);
        scoreScreen.SetTeamInvalidPocketBallCountEmptyTextOnZero(0, true);
        scoreScreen.SetTeamInvalidPocketBallCountEmptyTextOnZero(1, true);
        logLabel = string.IsNullOrEmpty(logLabel) ? string.Empty : " " + logLabel;

        _LogInfo("initializing billiards module");

        cameraOverrideModule = (CameraOverrideModule)_GetModule(nameof(CameraOverrideModule));

        initializeRack();

        resetCachedData();

        currentPhysicsManager = standardPhysicsManager;

        setTableModel(0, false);

        aud_main = this.GetComponent<AudioSource>();

        for (int i = 0; i < balls.Length; i++)
        {
            balls[i].GetComponentInChildren<Repositioner>(true)._Init(this, i);
        }

        gameModeLocal = 4;
        isStraight = true;
        rackConditionLocal = 1;
        semiAutoCallBallLocal = true;
        semiAutoCallPocketLocal = true;
        pointPocketsLocal = 0;
        calledBallsLocal = 0;
        
        networkingManager._Init(this);
        networkingManager.gameModeSynced = (byte)gameModeLocal;
        networkingManager.rackConditionSynced = (byte)rackConditionLocal;
        networkingManager.semiAutoCallBallSynced = semiAutoCallBallLocal;
        networkingManager.semiAutoCallPocketSynced = semiAutoCallPocketLocal;
        networkingManager.pointPocketsSynced = pointPocketsLocal;
        networkingManager.calledBallsSynced = calledBallsLocal;

        practiceManager._Init(this);
        repositionManager._Init(this);
        desktopManager._Init(this);
        cameraManager._Init(this);
        graphicsManager._Init(this);
        legacyPhysicsManager._Init(this);
        standardPhysicsManager._Init(this);
        betaPhysicsManager._Init(this);
        menuManager._Init(this);

        currentPhysicsManager.SendCustomEvent("_InitConstants");

        physicsModeLocal = 1;
        networkingManager.physicsModeSynced = 1;


#if HT8B_DEBUGGER
        this.transform.Find("debugger").gameObject.SetActive(true);
#endif

        this.transform.Find("intl.balls/guide/guide_display").GetComponent<MeshRenderer>().material.SetMatrix("_BaseTransform", this.transform.worldToLocalMatrix);

        reflection_main.RenderProbe();

#if UNITY_EDITOR
        graphicsManager._OnGameStarted();
        menuManager.menuSettings.transform.localScale = Vector3.zero;
#endif
        
        pcketLocations[0] = k_vE;
        pcketLocations[1] = new Vector3(k_vE.x, k_vE.y, -k_vE.z);
        pcketLocations[2] = new Vector3(-k_vE.x, k_vE.y, k_vE.z);
        pcketLocations[3] = new Vector3(-k_vE.x, k_vE.y, -k_vE.z);
        pcketLocations[4] = k_vF; // side pocket
        pcketLocations[5] = new Vector3(k_vF.x, k_vF.y, -k_vF.z); // side pocket
        findNearestPocket_x = k_vE.x / 2;
        findNearestPocket_n = findNearestPocket_x / k_vE.z;
    }

    private void FixedUpdate()
    {
        currentPhysicsManager.SendCustomEvent("_FixedTick");
    }

    private void Update()
    {
        networkingManager._Tick();

        desktopManager._Tick();
        menuManager._Tick();

        _BeginPerf(PERF_MAIN);
        practiceManager._Tick();
        repositionManager._Tick();
        cameraManager._Tick();
        graphicsManager._Tick();
        tickTimer();
        _Update9BallMarker();
        _UpdateCalledBallMarker();

        networkingManager._FlushBuffer();
        _EndPerf(PERF_MAIN);

        if (perfCounters[PERF_MAIN] % 500 == 0) _RedrawDebugger();
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (Networking.LocalPlayer == null) return;

        if (!lobbyOpen) return;

        VRCPlayerApi gameHost = _GetPlayerByName(playerNamesLocal[0]);
        if (!Utilities.IsValid(gameHost))
        {
            // host left. if they were the only ones in-game, instance master tries to close the lobby. otherwise, everyone in-lobby tries
            int otherPlayers = 0;
            for (int i = 0; i < 4; i++)
            {
                if (playerNamesLocal[i] == "") continue;

                VRCPlayerApi possiblePlayer = _GetPlayerByName(playerNamesLocal[i]);
                if (!Utilities.IsValid(possiblePlayer)) continue;

                otherPlayers++;
            }

            if ((otherPlayers == 0 && Networking.LocalPlayer.isMaster) || (otherPlayers > 0 && localPlayerId != -1))
            {
                networkingManager._OnLobbyClosed();
            }
        }
        else if (gameHost.isLocal)
        {
            // only host updates player list
            for (int i = 0; i < 4; i++)
            {
                if (playerNamesLocal[i] == "") continue;

                VRCPlayerApi possiblePlayer = _GetPlayerByName(playerNamesLocal[i]);
                if (Utilities.IsValid(possiblePlayer)) continue;

                networkingManager._OnKickLobby(i);
            }
        }
    }

    public UdonSharpBehaviour _GetModule(string type)
    {
        string[] parts = cameraModule.GetUdonTypeName().Split('.');
        if (parts[parts.Length - 1] == type)
        {
            return cameraModule;
        }
        return null;
    }

    #region Triggers
    public void _TriggerLobbyOpen()
    {
        if (lobbyOpen) return;

        scoreScreen.Clear();
        Array.Copy(scoreScreen.EncodeScoreSyncValues(), networkingManager.scoreSyncRows, networkingManager.scoreSyncRows.Length);

        networkingManager._OnLobbyOpened();
    }

    public void _TriggerLobbyClosed()
    {
        networkingManager._OnLobbyClosed();
    }

    public void _TriggerTeamsChanged(bool teamsEnabled)
    {
        networkingManager._OnTeamsChanged(teamsEnabled);
    }

    public void _TriggerNoGuidelineChanged(bool noGuidelineEnabled)
    {
        networkingManager._OnNoGuidelineChanged(noGuidelineEnabled);
    }

    public void _TriggerNoLockingChanged(bool noLockingEnabled)
    {
        networkingManager._OnNoLockingChanged(noLockingEnabled);
    }

    public void _TriggerRackCondisionChanged(uint rackCondition)
    {
        networkingManager._OnRackCondisionChanged(rackCondition);
    }

    public void _TriggerSemiAutoCallChanged(bool semiAutoCallEnabled)
    {
        networkingManager._OnSemiAutoCallChanged(semiAutoCallEnabled);
    }

    // public void _TriggerSemiAutoCallBallChanged(bool semiAutoCallBallEnabled)
    // {
    //     networkingManager._OnSemiAutoCallBallChanged(semiAutoCallBallEnabled);
    // }
    //
    // public void _TriggerSemiAutoCallPocketChanged(bool semiAutoCallPocketEnabled)
    // {
    //     networkingManager._OnSemiAutoCallPocketChanged(semiAutoCallPocketEnabled);
    // }

    public void _TriggerTimerChanged(uint timerSelected)
    {
        networkingManager._OnTimerChanged(timerSelected);
    }

    public void _TriggerGameModeChanged(uint newGameMode)
    {
        networkingManager._OnGameModeChanged(newGameMode);
    }

    public void _TriggerGoalPointsChanged(int goalPoints)
    {
        networkingManager._OnGoalPointsChanged(goalPoints);
    }

    public void _TriggerGlobalSettingsUpdated(string newTournamentReferee, int newPhysicsMode, int newTableModel, int newTableSkin)
    {
        networkingManager._OnGlobalSettingsChanged(newTournamentReferee, (byte)newPhysicsMode, (byte)newTableModel, (byte)newTableSkin);
    }

    public void _TriggerCueBallHit()
    {
        if (localTeamId != teamIdLocal && !isPracticeMode) return; // is there a better way to do this?

        _LogWarn("trying to propagate cue ball hit, linear velocity is " + ballsV[0].ToString("F4") + " and angular velocity is " + ballsW[0].ToString("F4"));

        if (float.IsNaN(ballsV[0].x) || float.IsNaN(ballsV[0].y) || float.IsNaN(ballsV[0].z) || float.IsNaN(ballsW[0].x) || float.IsNaN(ballsW[0].y) || float.IsNaN(ballsW[0].z))
        {
            ballsV[0] = Vector3.zero;
            ballsW[0] = Vector3.zero;
            return;
        }

        _TriggerCueDeactivate();

        if (0 < pointPocketsLocal && 0 < calledBallsLocal)
        {
            shotCountsLocal[teamIdLocal]++;
            networkingManager.shotCountsSynced[teamIdLocal] = shotCountsLocal[teamIdLocal];
        }
            
        networkingManager._OnHitBall(ballsV[0], ballsW[0]);
    }

    public void _TriggerOtherBallHit(int ballId, bool desktop)
    {
        if (localTeamId != teamIdLocal && !isPracticeMode) return; // is there a better way to do this?
         
        if (callShotLockLocal)
        {
            return;
        }
        
#if TKCH_CALLSHOT_CALLEDPBALL_DELAY
        if (!desktop && calledBallId == ballId)
        {
            return;
        }

        if (!desktop && Time.time < calledBallIdDelayTimestamp + callShotDelay)
        {
            return;
        }
        else
        {
#if TKCH_DEBUG_CALLSHOT_DELAY
            _LogInfo($"  calledBallId = {calledBallId}, calledBallIdDelayTimestamp = {calledBallIdDelayTimestamp}, callShotDelay = {callShotDelay}");
#endif
            calledBallIdDelayTimestamp = Time.time;
            calledBallId = ballId;
        }
#endif

#if TKCH_CALLSHOT_CALLEDPBALL_DELAY
        int id = calledBallId;
#else
        int id = ballId;
#endif

#if TKCH_DEBUG_CALLSHOT_POCKETEDBALL
        _LogInfo($"  id = {id}, ballsPocketedLocal = {ballsPocketedLocal:X4}, (0x1 << id) = {(0x1 << id):X4}");
#endif
        if (0 < id && 0 != (ballsPocketedLocal & (0x1 << id)))
        {
#if TKCH_DEBUG_CALLSHOT_POCKETEDBALL
            _LogInfo("  return");
#endif
            return;
        }

        if (id < 0)
        {
            calledBallOff = true;
            return;
        }

        if (!calledBallOff && !desktop)
        {
            return;
        }

        uint calledBalls = calledBallsLocal;
        calledBalls |= 0x1u << id;
        if (calledBalls == calledBallsLocal)
        {
            calledBalls ^= 0x1u << id;
        }

        if (calledBallsLocal != 0 && calledBalls != 0 && !desktop)
        {
            return;
        }

        if (Networking.LocalPlayer == null || Networking.GetOwner(activeCue.gameObject) != Networking.LocalPlayer) return;

        bool enable = (calledBallsLocal < calledBalls);
        if (enable) semiAutoCalledTimeBall = (Networking.GetServerTimeInMilliseconds() - timerStartLocal) / 1000.0f;
        
        networkingManager._OnCalledBallChanged(enable, (uint)id);
        calledBallOff = false;

        //aud_main.PlayOneShot(snd_btn);
    }

    public void _TriggerPocketHit(int pocketId, bool desktop)
    {
        if (localTeamId != teamIdLocal && !isPracticeMode) return; // is there a better way to do this?

        if (callShotLockLocal)
        {
            return;
        }
        
#if TKCH_CALLSHOT_CALLEDPOCKET_DELAY
        if (!desktop && calledPocketId == pocketId)
        {
            return;
        }

        if (!desktop && Time.time < calledPocketIdDelayTimestamp + callShotDelay)
        {
            return;
        }
        else
        {
#if TKCH_DEBUG_CALLSHOT_DELAY
            _LogInfo($"  calledPocketId = {calledPocketId}, calledPocketIdDelayTimestamp = {calledPocketIdDelayTimestamp}, callShotDelay = {callShotDelay}");
#endif
            calledPocketIdDelayTimestamp = Time.time;
            calledPocketId = pocketId;
        }
#endif

#if TKCH_CALLSHOT_CALLEDPOCKET_DELAY
        int id = calledPocketId;
#else
        int id = pocketId;
#endif

        if (id < 0)
        {
            calledPocketOff = true;
            return;
        }

        if (!calledPocketOff && !desktop)
        {
            return;
        }

        uint pointPockets = pointPocketsLocal;
        pointPockets |= 0x1u << id;
        if (pointPockets == pointPocketsLocal)
        {
            pointPockets ^= 0x1u << id;
        }

        if (pointPocketsLocal != 0 && pointPockets != 0 && !desktop)
        {
            return;
        }

        if (Networking.LocalPlayer == null || Networking.GetOwner(activeCue.gameObject) != Networking.LocalPlayer) return;

        bool enable = (pointPocketsLocal < pointPockets);
        
        networkingManager._OnPocketChanged(enable, (uint)id);
        calledPocketOff = false;
        
        //aud_main.PlayOneShot(snd_btn);
    }

    public void _TriggerCueActivate()
    {
        if (!isOurTurn()) return;

        if (Vector3.Distance(activeCue._GetCuetip().transform.position, ballsP[0]) < k_BALL_RADIUS)
        {
            _TriggerCueDeactivate();
            return;
        }

        canHitCueBall = true;
        this._TriggerOnPlayerPrepareShoot();

#if !HT_QUEST
        this.transform.Find("intl.balls/guide/guide_display").GetComponent<MeshRenderer>().material.SetColor("_Colour", k_aimColour_locked);
#endif
    }

    public void _TriggerCueDeactivate()
    {
        canHitCueBall = false;

#if !HT_QUEST
        guideline.gameObject.transform.Find("guide_display").GetComponent<MeshRenderer>().material.SetColor("_Colour", k_aimColour_aim);
#endif
    }

    public void _OnPickupCue()
    {
        if (!Networking.LocalPlayer.IsUserInVR()) desktopManager._OnPickupCue();
    }

    public void _OnDropCue()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer != null && !localPlayer.IsUserInVR()) desktopManager._OnDropCue();
    }

    public void _TriggerOnPlayerPrepareShoot()
    {
        networkingManager._OnPlayerPrepareShoot();
    }

    public void _OnPlayerPrepareShoot()
    {
        cameraManager._OnPlayerPrepareShoot();
    }

    public void _TriggerPlaceBall(int idx)
    {
        if (!canPlayLocal) return; // in case player was forced to drop ball since someone else took the shot

        // practiceManager._Record();

        bool consumeReposition = false;
        if (idx == 0)
        {
            currentPhysicsManager.SendCustomEvent("_IsCueBallTouching");
            bool isTouching = (bool)currentPhysicsManager.GetProgramVariable("outIsTouching");

            consumeReposition = !isTouching;
        }

        networkingManager._OnRepositionBalls(ballsP, consumeReposition);
    }

    public void _TriggerGameStart()
    {
        _LogYes("starting game");

        networkingManager.inningCountSynced = 0;

        networkingManager._OnGameStart(initialBallsPocketed[gameModeLocal], initialPositions[gameModeLocal]);
    }

    public void _TriggerJoinTeam(int teamId)
    {
        if (localPlayerId != -1) return;

        _LogInfo("joining team " + teamId);

        localPlayerId = networkingManager._OnJoinTeam(teamId);
        if (localPlayerId != -1)
        {
            localTeamId = (uint)(localPlayerId & 0x1u);

            playerNamesLocal[localPlayerId] = Networking.LocalPlayer.displayName;
            menuManager._RefreshLobbyOpen();
            menuManager._RefreshPlayerList();
        }
        else
        {
            _LogWarn("failed to join team " + teamId + ", did someone else beat you to it?");
        }
    }

    public void _TriggerLeaveLobby()
    {
        if (localPlayerId == -1) return;

        _LogInfo("leaving lobby");
        
        networkingManager._OnLeaveLobby(localPlayerId);
        playerNamesLocal[localPlayerId] = "";
        localPlayerId = -1;
        localTeamId = 0;
        menuManager._RefreshLobbyOpen();
        menuManager._RefreshPlayerList();
    }

    public void _TriggerGameReset()
    {
        string self = Networking.LocalPlayer.displayName;

        if (!gameLive)
        {
            if (lobbyOpen && _IsModerator(Networking.LocalPlayer))
            {
                networkingManager._OnLobbyClosed();
            }
            return;
        }

        string[] allowedPlayers = playerNamesLocal;
        if (!string.IsNullOrEmpty(tournamentRefereeLocal))
        {
            allowedPlayers = new string[] { tournamentRefereeLocal };
        }

        bool allPlayersOffline = true;
        bool isAllowedPlayer = false;
        foreach (string allowedPlayer in allowedPlayers)
        {
            if (allPlayersOffline && _GetPlayerByName(allowedPlayer) != null) allPlayersOffline = false;

            if (allowedPlayer == self) isAllowedPlayer = true;
        }

        if (allPlayersOffline || isAllowedPlayer || _IsModerator(Networking.LocalPlayer))
        {
            _LogInfo("force resetting game");

            networkingManager._OnGameReset();
        }
        else
        {
            string playerStr = "";
            bool has = false;
            foreach (string allowedPlayer in allowedPlayers)
            {
                if (string.IsNullOrEmpty(allowedPlayer)) continue;
                if (has) playerStr += ", ";
                has = true;

                playerStr += graphicsManager._FormatName(allowedPlayer);
            }

            infReset.text = "Only these players may reset:\n" + playerStr;
        }
    }
    #endregion

    public bool _CanUseTableSkin(string owner, int skin)
    {
        if (tableSkinHook == null) return false;

        tableSkinHook.SetProgramVariable("inOwner", owner);
        tableSkinHook.SetProgramVariable("inSkin", skin);
        tableSkinHook.SendCustomEvent("_CanUseTableSkin");

        return (bool)tableSkinHook.GetProgramVariable("outCanUse");
    }

    public bool _CanUseCueSkin(string owner, int skin)
    {
        if (cueSkinHook == null) return false;

        cueSkinHook.SetProgramVariable("inOwner", owner);
        cueSkinHook.SetProgramVariable("inSkin", skin);
        cueSkinHook.SendCustomEvent("_CanUseCueSkin");

        return (bool)cueSkinHook.GetProgramVariable("outCanUse");
    }


    #region NetworkingClient
    // the order is important, unfortunately
    public void _OnRemoteDeserialization()
    {
        _LogInfo("processing latest remote state (packet=" + networkingManager.packetIdSynced + ", state=" + networkingManager.stateIdSynced + ")");
        Debug.Log("[BilliardsModule" + logLabel + "] latest game state is " + networkingManager._EncodeGameState());
#if TKCH_DEBUG_NEXT_BREAK
        _LogInfo($"  gameStateLocal = {gameStateLocal}, networkingManager.gameStateSynced = {networkingManager.gameStateSynced}");
#endif
#if TKCH_DEBUG_DENYBALLS
        _LogInfo($"  networkingManager.denyBallsSynced = {networkingManager.denyBallsSynced:X4}");
#endif
#if TKCH_DEBUG_WINRACKCOUNT
        _LogInfo($"  networkingManager.winRackCountSynced = {networkingManager.winRackCountSynced[0]}-{networkingManager.winRackCountSynced[1]}");
#endif
#if TKCH_DEBUG_AVG
        _LogInfo($"  networkingManager.shotCountsSynced = {networkingManager.shotCountsSynced[0]}-{networkingManager.shotCountsSynced[1]}");
        _LogInfo($"  networkingManager.shotSuccessCountsSynced = {networkingManager.shotSuccessCountsSynced[0]}-{networkingManager.shotSuccessCountsSynced[1]}");
#endif

        // propagate game settings first
        onRemoteGlobalSettingsUpdated(
            networkingManager.tournamentRefereeSynced,
            networkingManager.physicsModeSynced,
            networkingManager.tableModelSynced,
            networkingManager.tableSkinSynced
        );
        onRemoteGameSettingsUpdated(
            networkingManager.gameModeSynced,
            networkingManager.goalPointsSynced,
            networkingManager.timerSynced,
            networkingManager.teamsSynced,
            networkingManager.noGuidelineSynced,
            networkingManager.noLockingSynced,
            networkingManager.rackConditionSynced,
            networkingManager.semiAutoCallBallSynced,
            networkingManager.semiAutoCallPocketSynced
        );

        if (gameStateLocal != networkingManager.gameStateSynced && networkingManager.gameStateSynced == 1)
        {
            Array.Clear(playerNamesCached, 0, playerNamesCached.Length);
        }

        // propagate valid players second
        onRemotePlayersChanged(networkingManager.playerNamesSynced);

        bool scoreUpdate = true;
        if (lobbyOpen || gameLive)
        {
            if (networkingManager.stateIdSynced <= 1)
            {
                scoreScreen.Clear();
                scoreUpdate = false;
#if TKCH_DEBUG_SCORE
                _LogInfo($"  EmptyTextOnZero 01 teamIdLocal = {teamIdLocal}, teamColorLocal = {teamColorLocal}");
#endif
            }
        }
        
#if TKCH_DEBUG_SCORE
        _LogInfo($"  scoreUpdate = {scoreUpdate}");
#endif
        if (scoreUpdate)
        {
#if TKCH_DEBUG_SCORE
            _LogInfo($"  networkingManager.scoreSyncRows = {networkingManager.scoreSyncRows[0]:X8}-{networkingManager.scoreSyncRows[1]:X8}");
            _LogInfo($"                                    {networkingManager.scoreSyncRows[2]:X8}-{networkingManager.scoreSyncRows[3]:X8}");
#endif
            scoreScreen.DecodeScoreSyncValues(networkingManager.scoreSyncRows);
        }
        
#if TKCH_DEBUG_TIMEOUT || TKCH_DEBUG_SPECIAL_PENALTY
        // _LogInfo($"  noCushionLocal = {noCushionLocal}, networkingManager.noCushionSynced = {networkingManager.noCushionSynced}");
        _LogInfo($"  specialPenaltyLocal = {specialPenaltyLocal}, networkingManager.specialPenaltySynced = {networkingManager.specialPenaltySynced}");
#endif
        if (0 < networkingManager.specialPenaltySynced)
        {
            if ((networkingManager.specialPenaltySynced & 0x1u) != 0 )
            {
                if (graphicsManager._SpawnPocketPoint(ballsP[0], false, -1))
                    aud_main.PlayOneShot(snd_PointMade, 1.0f);
            }
            if ((networkingManager.specialPenaltySynced & 0x2u) != 0 )
            {
                graphicsManager._SpawnSpecialFoulPenalty(ballsP[0], false);
                aud_main.PlayOneShot(snd_PointMade, 1.0f);
            }
            if ((networkingManager.specialPenaltySynced & 0x4u) != 0 )
            {
                graphicsManager._SpawnSpecialFoulPenalty(ballsP[0], true);
                aud_main.PlayOneShot(snd_PointMade, 1.0f);
            }
            networkingManager.specialPenaltySynced = 0;
        }

        breakBallIdLocal = networkingManager.breakBallIdSynced;
        onRemoteOpeningBreakStateChanged(networkingManager.isOpeningBreakSynced);

        // apply state transitions if needed
        onRemoteGameStateChanged(networkingManager.gameStateSynced);

        // now update game state
        onRemoteBallPositionsChanged(networkingManager.ballsPSynced);
        onRemoteTeamIdChanged(networkingManager.teamIdSynced);
        onRemoteFourBallCueBallChanged(networkingManager.fourBallCueBallSynced);
        onRemoteBallsPocketedChanged(networkingManager.ballsPocketedSynced, networkingManager.targetPocketedSynced, networkingManager.otherPocketedSynced);
        onRemoteFourBallScoresUpdated(networkingManager.fourBallScoresSynced);
        Array.Copy(networkingManager.chainedFoulsSynced, chainedFoulsLocal, chainedFoulsLocal.Length);
        onRemoteRepositionStateChanged(networkingManager.repositionStateSynced);
        onRemoteIsTableOpenChanged(networkingManager.isTableOpenSynced, networkingManager.teamColorSynced);
        onRemoteTurnStateChanged(networkingManager.turnStateSynced);
        onRemotePreviewWinningTeamChanged(networkingManager.previewWinningTeamSynced);

        bool stateIdChanged = (networkingManager.stateIdSynced != stateIdLocal);
        onRemotePointPocketsChanged(networkingManager.pointPocketsSynced, networkingManager.callShotLockSynced, stateIdChanged);
        onRemoteCalledBallsChanged(networkingManager.calledBallsSynced, stateIdChanged);
        onRemoteNextBallRepositionStateChanged(networkingManager.nextBallRepositionStateSynced);
        onRemoteDenyBallsChanged(networkingManager.denyBallsSynced);
        
        Array.Copy(networkingManager.totalPointsSynced, totalPointsLocal, totalPointsLocal.Length);
        Array.Copy(networkingManager.shotCountsSynced, shotCountsLocal, shotCountsLocal.Length);
        Array.Copy(networkingManager.shotSuccessCountsSynced, shotSuccessCountsLocal, shotSuccessCountsLocal.Length);
        Array.Copy(networkingManager.chainedPointsSynced, chainedPointsLocal, chainedPointsLocal.Length);
        inningCountLocal = networkingManager.inningCountSynced;
        Array.Copy(networkingManager.winRackCountSynced, winRackCountLocal, winRackCountLocal.Length);
#if TKCH_DEBUG_WINRACKCOUNT
        _LogInfo($"  networkingManager.winRackCountSynced = {networkingManager.winRackCountSynced[0]}-{networkingManager.winRackCountSynced[1]}");
#endif
        
        // finally, take a snapshot
        practiceManager._Record();

        stateIdLocal = networkingManager.stateIdSynced;

        redrawDebugger();
    }

    private void onRemoteGlobalSettingsUpdated(string tournamentRefereeSynced, byte physicsModeSynced, byte tableModelSynced, byte tableSkinSynced)
    {
        if (gameLive) return;

        if (
            tournamentRefereeLocal == tournamentRefereeSynced &&
            physicsModeLocal == physicsModeSynced &&
            tableModelLocal == tableModelSynced &&
            tableSkinLocal == tableSkinSynced
        )
        {
            return;
        }
        _LogInfo($"onRemoteGlobalSettingsUpdated tournamentReferee={tournamentRefereeSynced} physicsMode={physicsModeSynced} tableModel={tableModelSynced} tableSkin={tableSkinSynced}");

        if (tournamentRefereeLocal != tournamentRefereeSynced)
        {
            tournamentRefereeLocal = tournamentRefereeSynced;
        }

        if (physicsModeLocal != physicsModeSynced)
        {
            physicsModeLocal = physicsModeSynced;
            switch (physicsModeLocal)
            {
                case 0:
                    currentPhysicsManager = legacyPhysicsManager;
                    break;
                case 1:
                    currentPhysicsManager = standardPhysicsManager;
                    break;
                case 2:
                    currentPhysicsManager = betaPhysicsManager;
                    break;
            }
            currentPhysicsManager.SendCustomEvent("_InitConstants");
        }

        if (tableModelLocal != tableModelSynced)
        {
            setTableModel(tableModelSynced, true);
        }

        if (tableSkinLocal != tableSkinSynced && _CanUseTableSkin(networkingManager.playerNamesSynced[0], tableSkinSynced))
        {
            tableSkinLocal = tableSkinSynced;
            graphicsManager._UpdateTableColorScheme();
        }
    }

    private void onRemoteGameSettingsUpdated(uint gameModeSynced, int goalPointsSynced, uint timerSynced, 
        bool teamsSynced, bool noGuidelineSynced, bool noLockingSynced, uint rackConditionSynced,
        bool semiAutoCallBallSynced, bool semiAutoCallPocketSynced)
    {
        if (
            gameModeLocal == gameModeSynced &&
            goalPointsLocal == goalPointsSynced &&
            timerLocal == timerSynced &&
            teamsLocal == teamsSynced &&
            noGuidelineLocal == noGuidelineSynced &&
            noLockingLocal == noLockingSynced &&
            rackConditionLocal == rackConditionSynced &&
            semiAutoCallBallLocal == semiAutoCallBallSynced &&
            semiAutoCallPocketLocal == semiAutoCallPocketSynced
        )
        {
            return;
        }

        _LogInfo($"onRemoteGameSettingsUpdated gameMode={gameModeSynced} goalPoints={goalPointsSynced} timer={timerSynced} teams={teamsSynced} guideline={!noGuidelineSynced} locking={!noLockingSynced} rackCondition={rackConditionSynced} semiAutoCallBall={semiAutoCallBallSynced} semiAutoCallPocket={semiAutoCallPocketSynced}");

        if (gameModeLocal != gameModeSynced || goalPointsLocal != goalPointsSynced)
        {
            gameModeLocal = gameModeSynced;
            goalPointsLocal = goalPointsSynced;

            is8Ball = gameModeLocal == 0u;
            is9Ball = gameModeLocal == 1u;
            isJp4Ball = gameModeLocal == 2u;
            isKr4Ball = gameModeLocal == 3u;
            is4Ball = isJp4Ball || isKr4Ball;
            isStraight = gameModeLocal == 4u;

            menuManager._RefreshGameMode();
        }

        if (timerLocal != timerSynced)
        {
            timerLocal = timerSynced;

            menuManager._RefreshTimer();
        }

        bool refreshToggles = false;
        if (teamsLocal != teamsSynced)
        {
            teamsLocal = teamsSynced;
            refreshToggles = true;
        }

        if (noGuidelineLocal != noGuidelineSynced)
        {
            noGuidelineLocal = noGuidelineSynced;
            refreshToggles = true;
        }

        if (noLockingLocal != noLockingSynced)
        {
            noLockingLocal = noLockingSynced;
            refreshToggles = true;
        }

        if (rackConditionLocal != rackConditionSynced)
        {
            rackConditionLocal = rackConditionSynced;
            refreshToggles = true;
        }

        if (semiAutoCallBallLocal != semiAutoCallBallSynced)
        {
            semiAutoCallBallLocal = semiAutoCallBallSynced;
            refreshToggles = true;
        }

        if (semiAutoCallPocketLocal != semiAutoCallPocketSynced)
        {
            semiAutoCallPocketLocal = semiAutoCallPocketSynced;
            refreshToggles = true;
        }

        if (refreshToggles)
        {
            menuManager._RefreshToggleSettings();
        }
    }

    private void onRemotePlayersChanged(string[] playerNamesSynced)
    {
        if (stringArrayEquals(playerNamesCached, playerNamesSynced)) return;
        Array.Copy(playerNamesSynced, playerNamesCached, playerNamesCached.Length);

        string[] playerDetails = new string[4];
        for (int i = 0; i < 4; i++)
        {
            playerDetails[i] = playerNamesSynced[i] == "" ? "none" : playerNamesSynced[i];
        }
        _LogInfo($"onRemotePlayersChanged newPlayers={string.Join(",", playerDetails)}");
        
        Array.Copy(playerNamesSynced, playerNamesLocal, playerNamesLocal.Length);

        localPlayerId = Array.IndexOf(playerNamesLocal, Networking.LocalPlayer.displayName);
        if (localPlayerId != -1) localTeamId = (uint)(localPlayerId & 0x1u);

        cueControllers[0]._SetAuthorizedOwners(new string[] { playerNamesLocal[0], playerNamesLocal[2] });
        cueControllers[1]._SetAuthorizedOwners(new string[] { playerNamesLocal[1], playerNamesLocal[3] });

        menuManager._RefreshLobbyOpen();
        menuManager._RefreshPlayerList();
    }

    private void onRemoteGameStateChanged(byte gameStateSynced)
    {
        if (gameStateLocal == gameStateSynced) return;

        gameStateLocal = gameStateSynced;
        _LogInfo($"onRemoteGameStateChanged newState={gameStateSynced}");

        if (gameStateSynced == 1)
        {
            onRemoteLobbyOpened();
        }
        else if (gameStateSynced == 0)
        {
            onRemoteLobbyClosed();
        }
        else if (gameStateSynced == 2)
        {
            onRemoteGameStarted();
        }
        else if (gameStateSynced == 3)
        {
            onRemoteGameEnded(networkingManager.winningTeamSynced);
        }
        else if (gameStateSynced == 4) // continue next break
        {
            // gameStateLocal = networkingManager.gameStateSynced = 2;
            // isOpeningBreakLocal = false;
            onRemoteGameStarted();
        }
        // else if (gameStateSynced == 5) // request opening re-break
        // {
        //     gameStateLocal = networkingManager.gameStateSynced = 2;
        //     isOpeningBreakLocal = true;
        //     calledBallId = -2;
        //     calledPocketId = -2;
        //     semiAutoCalledBall = false;
        //     semiAutoCalledPocket = false;
        //     onRemoteGameStarted();
        // }
#if TKCH_DEBUG_OPENING_BREAK
        _LogInfo($"  gameStateLocal = {gameStateLocal}, isOpeningBreakLocal = {isOpeningBreakLocal}");
#endif
    }

    private void onRemoteLobbyOpened()
    {
        _LogInfo($"onRemoteLobbyOpened");

        lobbyOpen = true;
        graphicsManager._OnLobbyOpened();
        menuManager._RefreshLobbyOpen();
        menuManager._RefreshPlayerList();

        if (callbacks != null) callbacks.SendCustomEvent("_OnLobbyOpened");
    }

    private void onRemoteLobbyClosed()
    {
        _LogInfo($"onRemoteLobbyClosed");

        lobbyOpen = false;
        localPlayerId = -1;
        graphicsManager._OnLobbyClosed();
        menuManager._RefreshLobbyOpen();

        resetCachedData();

        if (callbacks != null) callbacks.SendCustomEvent("_OnLobbyClosed");
    }

    private void onRemoteGameStarted()
    {
#if TKCH_DEBUG_OPENING_BREAK
        _LogInfo($"TKCH BilliardsModule::onRemoteGameStarted()");
#endif
        _LogInfo($"onRemoteGameStarted");

        lobbyOpen = false;
        gameLive = true;

        Array.Clear(perfCounters, 0, PERF_MAX);
        Array.Clear(perfStart, 0, PERF_MAX);
        Array.Clear(perfTimings, 0, PERF_MAX);

        isPracticeMode = playerNamesLocal[1] == "" && playerNamesLocal[3] == "";

        menuManager._DisableMenu();

        if (isStraight)
        {
            graphicsManager._SetUSColors(true);
            // graphicsManager._UpdateTeamColor(teamIdLocal);
            initializeRack();
        }
        graphicsManager._OnGameStarted();
        desktopManager._OnGameStarted();
        applyCueAccess(false);
        practiceManager._Clear();
        repositionManager._OnGameStarted();
        if (isPracticeMode)
        {
            cueControllers[1].gameObject.SetActive(false);
        }

        Array.Clear(fbScoresLocal, 0, 2);
        auto_pocketblockers.SetActive(is4Ball);
        marker9ball.SetActive(is9Ball);

        afterBreak = false;
        // isOpeningBreakLocal = true;
        targetPocketedLocal = 0x0u;
        otherPocketedLocal = 0x0u;
        denyBallsLocal = 0x0u;
        graphicsManager._SetBallsDenyMark(denyBallsLocal);
        calledBallsLocal = 0;
        pointPocketsLocal = 0;
        // graphicsManager._UpdatePointPocketMarker(pointPocketsLocal, callShotLockLocal);
        if (isOpeningBreakLocal)
        {
            breakBallIdLocal = 0;
        }
        else
        {
            calledBallId = -2;
            calledPocketId = -2;
            semiAutoCalledPocket = false;
            semiAutoCalledTimeBall = 0;
        }
#if TKCH_DEBUG_OPENING_BREAK
        _LogInfo($"  isOpeningBreakLocal = {isOpeningBreakLocal}, breakBallIdLocal = {breakBallIdLocal}, calledBallId = {calledBallId}, calledPocketId = {calledPocketId}, semiAutoCalledPocket = {semiAutoCalledPocket}, semiAutoCalledTimeBall = {semiAutoCalledTimeBall}");
#endif

        graphicsManager._ShowBalls();

        // Reflect game state
        graphicsManager._UpdateScorecard();
        isReposition = false;
        markerObj.SetActive(false);
        markerHeadSpot.SetActive(false);
        markerCenterSpot.SetActive(false);
        markerFootSpot.SetActive(false);
        requestBreakOrange.SetActive(false);
        requestBreakBlue.SetActive(false);

        // Effects
        graphicsManager._PlayIntroAnimation(isStraight ? (~((0x1u << breakBallIdLocal) | (networkingManager.repositionStateSynced == 1 ? 0x1u : 0)) & straight_pocket_mask): 0xFFFFu);
        aud_main.PlayOneShot(snd_Intro, 1.0f);

        graphicsManager._SetScorecardPlayers(playerNamesLocal);

        timerRunning = false;

        reflection_main.RenderProbe();

        activeCue = cueControllers[isPracticeMode ? 0 : (int)networkingManager.teamIdSynced];
#if TKCH_DEBUG_OPENING_BREAK
        _LogInfo($"  activeCue isPrimary {activeCue == cueControllers[0]}, networkingManager.teamIdSynced = {networkingManager.teamIdSynced}");
#endif
    }

    private void onRemoteBallPositionsChanged(Vector3[] ballsPSynced)
    {
        if (vector3ArrayEquals(ballsP, ballsPSynced)) return;

        _LogInfo($"onRemoteBallPositionsChanged");

        Array.Copy(ballsPSynced, ballsP, ballsP.Length);
    }


    private void onRemotePreviewWinningTeamChanged(uint previewWinningTeamSynced)
    {
        if (!gameLive) return;
        if (string.IsNullOrEmpty(tournamentRefereeLocal)) return;

        if (previewWinningTeamLocal == previewWinningTeamSynced) return;

        _LogInfo($"onRemotePreviewWinningTeamChanged winningTeam={previewWinningTeamSynced}");
        previewWinningTeamLocal = previewWinningTeamSynced;

        if (previewWinningTeamSynced == 2)
        {
            graphicsManager._ResetWinners();
        }
        else
        {
            graphicsManager._SetWinners(isPracticeMode ? 0u : previewWinningTeamSynced, playerNamesLocal);
        }
    }

    private void onRemoteGameEnded(uint winningTeamSynced)
    {
        _LogInfo($"onRemoteGameEnded winningTeam={winningTeamSynced}");

        isLocalSimulationRunning = false;

        if (!string.IsNullOrEmpty(tournamentRefereeLocal))
        {
            // tournament mode has some special logic
            if (winningTeamSynced != 2u)
            {
                return;
            }

            winningTeamLocal = previewWinningTeamLocal;
        }
        else
        {
            winningTeamLocal = winningTeamSynced;
        }

        if (winningTeamLocal == 2)
        {
            winningTeamLocal = 0;

            isTableOpenLocal = true;
            _LogWarn("game reset");
            graphicsManager._OnGameReset();
        }
        else
        {
            _LogWarn("game over, team " + winningTeamLocal + " won (" + playerNamesLocal[winningTeamLocal] + " and " + playerNamesLocal[winningTeamLocal + 2] + ")");
            graphicsManager._SetWinners(isPracticeMode ? 0u : winningTeamLocal, playerNamesLocal);
        }

        gameLive = false;

        graphicsManager._UpdateTeamColor(winningTeamSynced);
        graphicsManager._UpdateScorecard();
        graphicsManager._RackBalls();

        disablePlayComponents();

        this.transform.Find("intl.controls/undo").gameObject.SetActive(false);
        this.transform.Find("intl.controls/redo").gameObject.SetActive(false);
        this.transform.Find("intl.controls/skipturn").gameObject.SetActive(false);
        this.transform.Find("intl.controls/callShotLock").gameObject.SetActive(false);

        // Remove any access rights
        localPlayerId = -1;
        localTeamId = 0;
        applyCueAccess(true);

        resetCachedData();

        cueControllers[1].gameObject.SetActive(true);

        menuManager._EnableMenu();

        infReset.text = "Reset";
    }

    private void onRemoteBallsPocketedChanged(uint ballsPocketedSynced, uint targetPocketedSynced, uint otherPocketedSynced)
    {
        if (!gameLive) return;

        // todo: actually use a separate variable to track local modifications to balls pocketed
        if (ballsPocketedLocal != ballsPocketedSynced) _LogInfo($"onRemoteBallsPocketedChanged ballsPocketed={ballsPocketedSynced:X}");

        ballsPocketedLocal = ballsPocketedSynced;
        targetPocketedLocal = targetPocketedSynced;
        otherPocketedLocal = otherPocketedSynced;

        graphicsManager._UpdateScorecard();
        graphicsManager._RackBalls();

        refreshBallPickups();
    }

    private void onRemoteFourBallScoresUpdated(int[] fbScoresSynced)
    {
        if (!gameLive) return;

        if (fbScoresLocal[0] == fbScoresSynced[0] && fbScoresLocal[1] == fbScoresSynced[1]) return;

        _LogInfo($"onRemoteFourBallScoresUpdated team1={fbScoresSynced[0]} team2={fbScoresSynced[1]}");

        Array.Copy(fbScoresSynced, fbScoresLocal, 2);
        graphicsManager._UpdateScorecard();
    }

    private void onRemoteTeamIdChanged(uint teamIdSynced)
    {
        if (!gameLive) return;

        if (teamIdLocal == teamIdSynced) return;

        _LogInfo($"onRemoteTeamIdChanged newTeam={teamIdSynced}");
        teamIdLocal = teamIdSynced;

        aud_main.PlayOneShot(snd_NewTurn, 1.0f);

        graphicsManager._UpdateTeamColor(teamIdLocal);

        // always use first cue if practice mode
        activeCue = cueControllers[isPracticeMode ? 0 : (int)teamIdLocal];
    }

    private void onRemoteFourBallCueBallChanged(uint fourBallCueBallSynced)
    {
        if (!gameLive) return;
        if (!is4Ball) return;

        if (fourBallCueBallLocal == fourBallCueBallSynced) return;

        _LogInfo($"onRemoteFourBallCueBallChanged cueBall={fourBallCueBallSynced}");
        fourBallCueBallLocal = fourBallCueBallSynced;

        graphicsManager._UpdateFourBallCueBallTextures(fourBallCueBallLocal);
    }

    private void onRemoteIsTableOpenChanged(bool isTableOpenSynced, uint teamColorSynced)
    {
        if (!gameLive) return;

        if (teamColorLocal == teamColorSynced && isTableOpenLocal == isTableOpenSynced) return;

        _LogInfo($"onRemoteIsTableOpenChanged isTableOpen={isTableOpenSynced} teamColor={teamColorSynced}");
        isTableOpenLocal = isTableOpenSynced;
        teamColorLocal = teamColorSynced;

        if (!isTableOpenLocal)
        {
            string color = (teamIdLocal ^ teamColorLocal) == 0 ? "blues" : "oranges";
            _LogInfo($"table closed, team {teamIdLocal} is {color}");
        }

        graphicsManager._UpdateTeamColor(teamIdLocal);
        graphicsManager._UpdateScorecard();
    }

    private void onRemoteRepositionStateChanged(uint repositionStateSynced)
    {
        if (!gameLive) return;

        if (repositionStateLocal == repositionStateSynced) return;

        _LogInfo($"onRemoteRepositionStateChanged repositionState={repositionStateSynced}");
        repositionStateLocal = repositionStateSynced;

        if (repositionStateLocal == 1 || repositionStateLocal == 3)
        {
            afterBreak = false;
        }

        if (!isOurTurn() || repositionStateLocal == 0 || repositionStateLocal == 3)
        {
            isReposition = false;
            setFoulPickupEnabled(false);
            return;
        }

        if (repositionStateLocal == 1 || repositionStateLocal == 2)
        {
            isReposition = true;
            if (repositionStateLocal == 1 || (isStraight /* && chainedFoulsLocal[teamIdLocal ^ 0x1u] < 3 */ ))
            {
#if TKCH_DEBUG_BREAKING_FOUL
                _LogInfo("  repoMaxX = -k_SPOT_POSITION_X");
#endif
                repoMaxX = -k_SPOT_POSITION_X;
            }
            else
            {
#if TKCH_DEBUG_BREAKING_FOUL
                _LogInfo("  repoMaxX = k_pR.x");
#endif
                Vector3 k_pR = (Vector3)currentPhysicsManager.GetProgramVariable("k_pR");
                repoMaxX = k_pR.x;
            }
            setFoulPickupEnabled(true);
        }
    }

    private void onRemoteNextBallRepositionStateChanged(uint nextBallRepositionStateSynced)
    {
        if (!gameLive) return;

        if (nextBallRepositionStateLocal == nextBallRepositionStateSynced) return;

        _LogInfo($"onRemoteNextBallRepositionStateChanged nextBallRepositionState={nextBallRepositionStateSynced}");
        nextBallRepositionStateLocal = nextBallRepositionStateSynced;

        _UpdateNextBallRepositionSpotMarker();
    }

    private void onRemoteTurnBegin(int timerStartSynced)
    {
        _LogInfo("onRemoteTurnBegin");
        canPlayLocal = true;
        timerStartLocal = timerStartSynced;

        enablePlayComponents();
        Array.Clear(ballsV, 0, ballsV.Length);
        Array.Clear(ballsW, 0, ballsW.Length);

        graphicsManager._UpdatePointPocketMarker(pointPocketsLocal, callShotLockLocal);
        graphicsManager._SpawnPocketPointMinusReset();
        // calledBallId = -2;
        // calledPocketId = -2;
        // semiAutoCalledBall = false;
        // semiAutoCalledPocket = false;
#if TKCH_DEBUG_NEXT_BREAK
        _LogInfo($"  afterBreak = {afterBreak}, breakBallIdLocal = {breakBallIdLocal}, calledBallId = {calledBallId}");
#endif
#if TKCH_DEBUG_SEMIAUTO_CALL || TKCH_DEBUG_SEMIAUTO_CALL_SIDE || TKCH_DEBUG_NEXT_BREAK
        debugLogFlg = true;
#endif
    }

    private void onRemoteTurnSimulate(Vector3 cueBallV, Vector3 cueBallW, string simulationOwner)
    {
        _LogInfo($"onRemoteTurnSimulate cueBallV={cueBallV.ToString("F4")} cueBallW={cueBallW.ToString("F4")} owner={simulationOwner}");

        balls[0].GetComponent<AudioSource>().PlayOneShot(snd_hitball, 1.0f);

        canPlayLocal = false;
        disablePlayComponents();

        if (!_IsPlayer(Networking.LocalPlayer) && !table.GetComponent<MeshRenderer>().isVisible)
        {
            // don't bother simulating if the table isn't even visible
            _LogWarn("skipping simulation");
            return;
        }

        if (afterBreak && 0 < denyBallsLocal)
        {
            currentPhysicsManager.SetProgramVariable("cueBallKichenLineOverCheck", true);
        }

        isLocalSimulationRunning = true;
        firstHit = 0;
        secondHit = 0;
        thirdHit = 0;
        cushionAfterFirstHit = 0;
        cushionObjectiveBallsOnBreak = 0x0u;
        // noCushionLocal = false;
        specialPenaltyLocal = 0;
        fbMadePoint = false;
        fbMadeFoul = false;
        ballsPocketedOrig = ballsPocketedLocal;
        targetPocketedOrig = targetPocketedLocal;
        otherPocketedOrig = otherPocketedLocal;
        if (Networking.LocalPlayer.displayName == simulationOwner)
        {
            isLocalSimulationOurs = true;
        }

        for (int i = 0; i < ballsV.Length; i++)
        {
            ballsV[i] = Vector3.zero;
            ballsW[i] = Vector3.zero;
        }
        ballsV[0] = cueBallV;
        ballsW[0] = cueBallW;

        auto_colliderBaseVFX.SetActive(true);
    }

    private void onRemoteTurnStateChanged(byte turnStateSynced)
    {
        if (!gameLive) return;

        if (turnStateSynced == turnStateLocal) return;

        _LogInfo($"onRemoteTurnStateChanged newState={turnStateSynced}");
        turnStateLocal = turnStateSynced;

        if (turnStateLocal == 0 || turnStateLocal == 2)
        {
            if (turnStateLocal == 2) turnStateLocal = 0; // synthetic state

            onRemoteTurnBegin(networkingManager.timerStartSynced);
            // practiceManager._Record();
        }
        else if (turnStateLocal == 1)
        {
            onRemoteTurnSimulate(networkingManager.cueBallVSynced, networkingManager.cueBallWSynced, networkingManager.simulationOwnerSynced);
            // practiceManager._Record();
        }
        else if (turnStateLocal == 3) // choice options after foul
        {
            turnStateLocal = 0; // synthetic state
            onRemoteTurnBegin(networkingManager.timerStartSynced);
        }
        else
        {
            canPlayLocal = false;
            disablePlayComponents();
        }
    }
    
    private void onRemoteDenyBallsChanged(uint denyBallsSynced)
    {
        if (!gameLive) return;

        if (denyBallsLocal == denyBallsSynced) return;

        _LogInfo($"onRemoteDenyBallsChanged denyBalls={denyBallsSynced:X4}");
        denyBallsLocal = denyBallsSynced;
        graphicsManager._SetBallsDenyMark(denyBallsLocal);
    }

    private void onRemotePointPocketsChanged(uint pointPocketsSynced, bool callShotLockSynced, bool stateIdChanged)
    {
#if TKCH_DEBUG_SPECIAL_PENALTY
        // _LogInfo($"TKCH BilliardsModule::onRemotePointPocketsChanged(pointPocketsSynced = {pointPocketsSynced})");
        // _LogInfo($"  pointPocketsLocal = {pointPocketsLocal}");
#endif
        if (!gameLive) return;

        if (pointPocketsLocal == pointPocketsSynced && callShotLockLocal == callShotLockSynced && 0 < stateIdLocal) return;

        _LogInfo($"onRemotePointPocketsChanged pointPockets={pointPocketsSynced:X2} callShotLock={callShotLockSynced}");
        pointPocketsLocal = pointPocketsSynced;
        callShotLockLocal = callShotLockSynced;
        graphicsManager._UpdatePointPocketMarker(pointPocketsLocal, callShotLockLocal);
        if (!stateIdChanged)
        {
            aud_main.PlayOneShot(snd_btn);
        }
        // else
        // {
        //     calledPocketId = -2;
        //     semiAutoCalledPocket = false;
        // }
        
        _UpdateNextBallRepositionSpotMarker();
    }

    private void onRemoteCalledBallsChanged(uint calledBallsSynced, bool stateIdChanged)
    {
        if (!gameLive) return;

        if (calledBallsLocal == calledBallsSynced && 0 < stateIdLocal) return;

        _LogInfo($"onRemoteCalledBallsChanged calledBalls={calledBallsSynced:X4}");
        calledBallsLocal = calledBallsSynced;
        if (!stateIdChanged)
        {
            aud_main.PlayOneShot(snd_btn);
        }
        // else
        // {
        //     calledBallId = -2;
        //     semiAutoCalledBall = false;
        // }
    }
    
    private void onRemoteOpeningBreakStateChanged(bool isOpeningBreakSynced)
    {
#if TKCH_DEBUG_OPENING_BREAK
        _LogInfo($"TKCH BilliardsModule::onRemoteOpeningBreakStateChanged(isOpeningBreakSynced = {isOpeningBreakSynced})");
#endif
#if TKCH_DEBUG_OPENING_BREAK
        _LogInfo($"  gameLive = {gameLive}, isOpeningBreakLocal = {isOpeningBreakLocal}");
#endif
        // if (!gameLive) return;

        if (isOpeningBreakLocal == isOpeningBreakSynced) return;

        _LogInfo($"onRemoteOpeningBreakStateChanged isOpeningBreak={isOpeningBreakSynced}");
        isOpeningBreakLocal = isOpeningBreakSynced;

        if (isOpeningBreakLocal)
        {
            calledBallId = 0;
            calledPocketId = 0;
            // semiAutoCalledBall = true;
            semiAutoCalledPocket = true;
            semiAutoCalledTimeBall = 0;
        }
#if TKCH_DEBUG_OPENING_BREAK
        _LogInfo($"  calledBallId = {calledBallId}, calledPocketId = {calledPocketId}, semiAutoCalledPocket = {semiAutoCalledPocket}, semiAutoCalledTimeBall = {semiAutoCalledTimeBall}");
#endif
    }
    #endregion

    #region PhysicsEngineCallbacks
    public void _TriggerCollision(int srcId, int dstId)
    {
        if (dstId < srcId)
        {
            int tmp = dstId;
            dstId = srcId;
            srcId = dstId;
        }
        if (srcId != 0) return;

        switch (gameModeLocal)
        {
            case 0:
            case 1:
                if (firstHit == 0) firstHit = dstId;
                break;
            case 2:
                if (firstHit == 0)
                {
                    firstHit = dstId;
                    break;
                }
                if (secondHit == 0)
                {
                    if (dstId != firstHit)
                    {
                        secondHit = dstId;
                        handle4BallHit(ballsP[dstId], true);
                    }
                    break;
                }
                if (thirdHit == 0)
                {
                    if (dstId != firstHit && dstId != secondHit)
                    {
                        thirdHit = dstId;
                        handle4BallHit(ballsP[dstId], true);
                    }
                    break;
                }
                break;
            case 3:
                if (dstId == 13)
                {
                    handle4BallHit(ballsP[dstId], false);
                    break;
                }
                if (firstHit == 0)
                {
                    firstHit = dstId;
                    break;
                }
                if (secondHit == 0)
                {
                    if (dstId != firstHit)
                    {
                        secondHit = dstId;
                        handle4BallHit(ballsP[dstId], true);
                    }
                    break;
                }
                break;
            case 4:
                if (firstHit == 0)
                {
                    firstHit = dstId;
                    if (0 < denyBallsLocal)
                    {
                        if ((denyBallsLocal & (0x1u << firstHit)) != 0x0u)
                        {
                            pointPocketsLocal = 0;
                            calledBallsLocal = 0;
                            graphicsManager._UpdatePointPocketMarker(pointPocketsLocal, callShotLockLocal);
                            markerCalledBall.SetActive(false);
                        
                            if (graphicsManager._SpawnPocketPoint(ballsP[0], false, -1))
                                aud_main.PlayOneShot(snd_PointMade, 1.0f);
                            
                            if (/* 3 */ 2 <= chainedFoulsLocal[teamIdLocal])
                            {
                                graphicsManager._SpawnSpecialFoulPenalty(ballsP[0], true);
                                // specialPenaltyLocal |= 0x4u;
                                // penaltyPoint += 15;
                            }
                        }
                        else
                        {
                            graphicsManager._SetBallsDenyMark(0);
                        }
                    }
                }
                break;
        }
    }

    public void _TriggerCushion(int id, Vector3 pos)
    {
        if (isOpeningBreakLocal && !afterBreak) //networkingManager.stateIdSynced == 2)
        {
            if (0 != firstHit) // if (0 < id)
            {
                uint ball_bit = 0x1u << id;
                if (0 == (cushionObjectiveBallsOnBreak & ball_bit))
                {
                    if (id == 0)
                    {
                        graphicsManager._SpawnCushionTouch(pos, 0);
                    }
                    else
                    {
                        int cushionBallCount = (int)SoftwareFallback(cushionObjectiveBallsOnBreak & 0xFFFFFFFE);
                        if (cushionBallCount < 2) //3) // 4)
                        {
                            graphicsManager._SpawnCushionTouch(pos, cushionBallCount + 1);
                        }
                    }
                    cushionObjectiveBallsOnBreak |= ball_bit;
                }
            }
        }
        else if (ballsPocketedLocal == ballsPocketedOrig)
        {
            if (0 != firstHit)
            {
                if (0 == cushionAfterFirstHit)
                {
                    graphicsManager._SpawnCushionTouch(pos, 0);
                }
                cushionAfterFirstHit++;
            }
        }
    }

    public void _TriggerPocketBall(int id, int pocketId)
    {
        uint total = 0U;

        // Get total for X positioning
        int count_extent = is9Ball ? 10 : 16;
        for (int i = 1; i < count_extent; i++)
        {
            total += (ballsPocketedLocal >> i) & 0x1U;
        }

        // place ball on the rack
        ballsP[id] = k_rack_position + (float)total * k_BALL_DIAMETRE * k_rack_direction;

        ballsPocketedLocal ^= 1U << id;

        uint bmask = 0x1FCU << ((int)(teamIdLocal ^ teamColorLocal) * 7);

        // Good pocket
        if (isStraight)
        {
            uint pointPockets = pointPocketsLocal & (0x1u << pocketId);
            if ((calledBallsLocal & (0x1u << id)) != 0 && 
                pointPockets != 0)
            {
                targetPocketedLocal |= 1U << id;
                graphicsManager._FlashTableLight();

                if (0 == (ballsPocketedLocal & 0x1u))
                {
                    if (graphicsManager._SpawnPocketPoint(pcketLocations[pocketId], true, (int)(teamIdLocal ^ teamColorLocal ^ 0x1U)))
                        aud_main.PlayOneShot(snd_PointMade, 1.0f);
                }

                calledBallsLocal = 0;
            }
            else
            {
                if (0 == id)
                {
                    if (graphicsManager._SpawnPocketPoint((pocketId < 0 ? balls[id].transform.localPosition : pcketLocations[pocketId]), false, -1))
                        aud_main.PlayOneShot(snd_PointMade, 1.0f);

                    if (isOpeningBreakLocal && !afterBreak)
                    {
                        graphicsManager._SpawnSpecialFoulPenalty((pocketId < 0 ? balls[id].transform.localPosition : pcketLocations[pocketId]), false);
                    }

                    if (/* 3 */ 2 <= chainedFoulsLocal[teamIdLocal])
                    {
                        graphicsManager._SpawnSpecialFoulPenalty((pocketId < 0 ? balls[id].transform.localPosition : pcketLocations[pocketId]), true);
                        // specialPenaltyLocal |= 0x4u;
                        // penaltyPoint += 15;
                    }
                }
                else
                {
                    otherPocketedLocal |= 1U << id;

                    if ((targetPocketedLocal & straight_pocket_mask) > (targetPocketedOrig & straight_pocket_mask))
                    {
                        if (graphicsManager._SpawnPocketPoint(pcketLocations[pocketId], true, (int)(teamIdLocal ^ teamColorLocal ^ 0x1U)))
                            aud_main.PlayOneShot(snd_PointMade, 1.0f);
                    }
                }
                graphicsManager._FlashTableError();
            }
            
            if ((calledBallsLocal & (0x1u << id)) != 0)
            {
                markerCalledBall.SetActive(false);
            }

            if (0 < id)
            {
                if (15 <= SoftwareFallback(ballsPocketedLocal & straight_pocket_mask))
                {
#if TKCH_DEBUG_BREAKBALL
                    _LogInfo($"  breakBallIdLocal = {breakBallIdLocal}");
#endif
                    breakBallIdLocal = id;
                }
                else
                {
                    breakBallIdLocal = -1;
                }
            }
        }
        else
        {
            if (((0x1U << id) & ((bmask) | (isTableOpenLocal ? 0xFFFCU : 0x0000U) | ((bmask & ballsPocketedLocal) == bmask ? 0x2U : 0x0U))) > 0)
            {
                graphicsManager._FlashTableLight();
            }
            else
            {
                graphicsManager._FlashTableError();
            }
        }
        aud_main.PlayOneShot(snd_Sink, 1.0f);

#if !HT_QUEST

        // VFX ( make ball move )
        Rigidbody body = balls[id].GetComponent<Rigidbody>();
        body.isKinematic = false;
        body.velocity = this.transform.TransformVector(new Vector3(
           ballsV[id].x,
           0.0f,
           ballsV[id].z
        ));

#else
        balls[id].transform.localPosition = ballsP[id];
#endif
    }
    
    public void _TriggerCueBallKichenLineOver()
    {
#if TKCH_DEBUG_CUEBALL_OVER_KICHENLINE
        _LogInfo("TKCH BilliardsModule::_TriggerCueBallKichenLineOver()");
#endif
        denyBallsLocal = 0;
        graphicsManager._SetBallsDenyMark(denyBallsLocal);
    }
    
    public void _TriggerSimulationEnded(bool forceScratch)
    {
        if (!isLocalSimulationRunning) return;
        isLocalSimulationRunning = false;

        _LogInfo("local simulation completed");
        cameraManager._OnLocalSimEnd();

        auto_colliderBaseVFX.SetActive(false);

        // Make sure we only run this from the client who initiated the move
        if (isLocalSimulationOurs)
        {
            isLocalSimulationOurs = false;

            uint bmask = 0xFFFCu;
            uint emask = 0x0u;

            // Quash down the mask if table has closed
            if (!isTableOpenLocal)
            {
                bmask = bmask & (0x1FCu << ((int)(teamIdLocal ^ teamColorLocal) * 7));
                emask = 0x1FCu << ((int)(teamIdLocal ^ teamColorLocal ^ 0x1U) * 7);
            }

            // Common informations
            bool isSetComplete = (ballsPocketedLocal & bmask) == bmask;
            bool isScratch = (ballsPocketedLocal & 0x1U) == 0x1U || forceScratch;

            ballsPocketedLocal = ballsPocketedLocal & ~(0x1U);
            if (isScratch) ballsP[0] = Vector3.zero;

            // Append black to mask if set is done
            if (isSetComplete)
            {
                bmask |= 0x2U;
            }

            // These are the resultant states we can set for each mode
            // then the rest is taken care of
            bool
               isObjectiveSink,
               isOpponentSink,
               winCondition,
               foulCondition,
               deferLossCondition
            ;

            bool repositionOnFoul = true;
            bool winnerBreak = true;
            bool reBreakAllowed = false;
            bool canChoiceFoot = false;

            if (is8Ball)
            {
                isObjectiveSink = (ballsPocketedLocal & bmask) > (ballsPocketedOrig & bmask);
                isOpponentSink = (ballsPocketedLocal & emask) > (ballsPocketedOrig & emask);

                // Calculate if objective was not hit first
                bool isWrongHit = ((0x1U << firstHit) & bmask) == 0;

                bool is8Sink = (ballsPocketedLocal & 0x2U) == 0x2U;

                if (is8Sink && isPracticeMode)
                {
                    is8Sink = false;

                    ballsPocketedLocal = ballsPocketedLocal & ~(0x2U);
                    ballsP[1] = Vector3.zero;
                }

                winCondition = isSetComplete && is8Sink;
                foulCondition = isScratch || isWrongHit;

                deferLossCondition = is8Sink;
            }
            else if (is9Ball)
            {
                // Rules are from: https://www.youtube.com/watch?v=U0SbHOXCtFw

                // Rule #1: Cueball must strike the lowest number ball, first
                bool isWrongHit = !(findLowestUnpocketedBall(ballsPocketedOrig) == firstHit);

                // Rule #2: Pocketing cueball, is a foul

                // Win condition: Pocket 9 ball ( at anytime )
                winCondition = (ballsPocketedLocal & 0x200u) == 0x200u;

                // this video is hard to follow so im just gonna guess this is right
                isObjectiveSink = (ballsPocketedLocal & 0x3FEu) > (ballsPocketedOrig & 0x3FEu);

                isOpponentSink = false;
                deferLossCondition = false;

                foulCondition = isWrongHit || isScratch;

                // TODO: Implement rail contact requirement
            }
            else if (isStraight)
            {
                // bool isWrongHit = !afterBreak && !isOpeningBreakLocal && (0 < breakBallIdLocal) && (breakBallIdLocal != firstHit); // !(findLowestUnpocketedBall(ballsPocketedOrig) == firstHit);
                bool isWrongHit = (denyBallsLocal & (0x1u << firstHit)) != 0x0u;
#if TKCH_DEBUG_BREAKBALL || TKCH_DEBUG_OPENING_BREAK
                _LogInfo($"  afterBreak = {afterBreak}, isOpeningBreakLocal = {isOpeningBreakLocal}, breakBallIdLocal = {breakBallIdLocal}, firstHit = {firstHit}");
                _LogInfo($"  isWrongHit = {isWrongHit}");
#endif
                bool isNoTouch = firstHit <= 0;

                bool isAnyPocketSink = (ballsPocketedLocal & straight_pocket_mask) > (ballsPocketedOrig & straight_pocket_mask);
                
                bool isNoCushon = !isAnyPocketSink && (
                                  // (!afterBreak && (SoftwareFallback(cushionObjectiveBallsOnBreak) < 4)) ||
                                  (afterBreak && cushionAfterFirstHit == 0));

                foulCondition = isScratch || isWrongHit || isNoTouch || isNoCushon;
                isObjectiveSink = (targetPocketedLocal & straight_pocket_mask) > (targetPocketedOrig & straight_pocket_mask);
                isOpponentSink = (otherPocketedLocal & straight_pocket_mask) > (otherPocketedOrig & straight_pocket_mask);
                /*
                if (isObjectiveSink || !afterBreak)
                {
                    isOpponentSink = false;
                }
                */

#if TKCH_DEBUG_OPENING_BREAK
                _LogInfo($"  cushionObjectiveBallsOnBreak = {cushionObjectiveBallsOnBreak:X4}");
                _LogInfo($"  isScratch = {isScratch}");
#endif
                bool isCueBallCushionOnBreak = isScratch || (cushionObjectiveBallsOnBreak & 0x1u) > 0;
                cushionObjectiveBallsOnBreak &= ~0x1u;

                reBreakAllowed = isOpeningBreakLocal && !isObjectiveSink && // !isAnyPocketSink &&
                    !afterBreak && (SoftwareFallback(cushionObjectiveBallsOnBreak) < 2 || !isCueBallCushionOnBreak); // 4);

#if TKCH_DEBUG_OPENING_BREAK
                _LogInfo($"  cushionObjectiveBallsOnBreak = {cushionObjectiveBallsOnBreak:X4}");
                _LogInfo($"  reBreakAllowed = {reBreakAllowed}, isCueBallCushionOnBreak = {isCueBallCushionOnBreak}");
#endif
#if TKCH_DEBUG_SPECIAL_PENALTY
                _LogInfo($"  isNoTouch = {isNoTouch}, isNoCushon = {isNoCushon}");
#endif

                if (isScratch)
                {
                    ballsP[0] = new Vector3(-k_SPOT_POSITION_X, 0.0f, 0.0f); // initialPositions[gameModeLocal][0];
                }
                else if (isNoTouch || isNoCushon)
                {
                    // noCushionLocal = true;
                    specialPenaltyLocal |= 0x1u;
                }
                
                deferLossCondition = false;
                
                uint ballsPocketedCurrent = ballsPocketedLocal & ~ballsPocketedOrig;

                int point = 0;
                if (isObjectiveSink && !foulCondition /* || !afterBreak */)
                {
                    isOpponentSink = false;
                    /*
                    for (int i = 1; i < balls.Length; i++)
                    {
                        if (0 < ((ballsPocketedCurrent >> i) & 0x1u))
                        {
                            int ballNumber = i == 1 ? 8 : (i < 9 ? i - 1 : i);
                            point += ballNumber;
                            point += 1;
                        }
                    }
                    */
                    point = (int)SoftwareFallback(ballsPocketedCurrent);
                    shotSuccessCountsLocal[teamIdLocal]++;
                }
                
                if (isOpponentSink || foulCondition)
                {
                    int uponBallsCount = pocketedballUponPool(ballsPocketedCurrent, k_SPOT_POSITION_X, break_order_straight.Length);
                    if (uponBallsCount <= 0)
                    {
                        pocketedballUponPool(ballsPocketedCurrent, 0, break_order_straight.Length);
                    }
                }
                else
                {
                    totalPointsLocal[teamIdLocal] += point;
                    if (127 < totalPointsLocal[teamIdLocal]) {totalPointsLocal[teamIdLocal] = 127;}
                    chainedPointsLocal[teamIdLocal] += point;
                    if (255 < chainedPointsLocal[teamIdLocal]) {chainedPointsLocal[teamIdLocal] = 255;}
                }
                
                winCondition = 14 <= (int)SoftwareFallback(ballsPocketedLocal & straight_pocket_mask);
                if (winCondition)
                {
                    // float kitchen_x = -k_SPOT_POSITION_X;
#if TKCH_DEBUG_IN_KITCHEN
                    _LogInfo($"  ballsP[0].x = {ballsP[0].x} < kitchen_x = {kitchen_x}");
#endif
                    // winnerBreak = ballsP[0].x < kitchen_x;
                    
                    winRackCountLocal[teamIdLocal]++;
#if TKCH_DEBUG_WINRACKCOUNT
                    _LogInfo($"  winRackCountLocal = {winRackCountLocal[0]}-{winRackCountLocal[1]}");
#endif
                }

                // if (foulCondition || deferLossCondition || !isObjectiveSink || isOpponentSink)
                if (point == 0 || foulCondition)
                {
                    chainedPointsLocal[teamIdLocal] = 0;
                }
                
#if TKCH_DEBUG_CLEAR_CHAINED_FOUL
                _LogInfo($"  chainedFoulsLocal[teamIdLocal = {teamIdLocal}] = {chainedFoulsLocal[teamIdLocal]}");
#endif
                int penaltyPoint = 0;
                if (foulCondition)
                {
                    penaltyPoint++;
                }
                else
                {
                    chainedFoulsLocal[teamIdLocal] = 0;
                }
                if (reBreakAllowed)
                {
                    specialPenaltyLocal |= 0x2u;
                    penaltyPoint += 2;
                }

                chainedFoulsLocal[teamIdLocal] += (foulCondition && !isOpeningBreakLocal) ? 1 : 0;
                if (3 <= chainedFoulsLocal[teamIdLocal])
                {
#if TKCH_DEBUG_SPECIAL_PENALTY || TKCH_DEBUG_CLEAR_CHAINED_FOUL
                    _LogInfo($"  chainedFoulsLocal[teamIdLocal = {teamIdLocal}] = {chainedFoulsLocal[teamIdLocal]}");
#endif
                    // if (isScratch) ballsP[0] = Vector3.zero;
                    if (!isScratch && !isWrongHit && (isNoTouch || isNoCushon))
                    {
                        specialPenaltyLocal |= 0x4u;
                    }
                    penaltyPoint += 15;
                    reBreakAllowed = true;
                }
                if (3 <= chainedFoulsLocal[teamIdLocal ^ 0x1u])
                {
                    chainedFoulsLocal[teamIdLocal ^ 0x1u] = 0;
                }
#if TKCH_DEBUG_CLEAR_CHAINED_FOUL
                _LogInfo($"  chainedFoulsLocal[teamIdLocal = {teamIdLocal}] = {chainedFoulsLocal[teamIdLocal]}");
#endif
                
                if (0 < penaltyPoint)
                {
                    totalPointsLocal[teamIdLocal] -= penaltyPoint;
                    if (totalPointsLocal[teamIdLocal] < -127) {totalPointsLocal[teamIdLocal] = -127;}
                }

                bool allowBallFound = false;
                denyBallsLocal = 0x0u;
                if (isScratch)
                {
                    uint ball_bit = 0x2u;
                    uint ballsInKitchen = 0x0u;
                    for (int k = 0; k < break_order_straight.Length; k++)
                    {
                        int i = k + 1;
                        if ((ballsPocketedLocal & ball_bit) == 0x0u)
                        {
                            if (ballsP[i].x <= -k_SPOT_POSITION_X)
                            {
                                ballsInKitchen |= 0x1u << i;
                            }
                            else
                            {
                                allowBallFound = true;
                            }
                        }
                        ball_bit <<= 1;
                    }
                    denyBallsLocal = ballsInKitchen;
                    canChoiceFoot = !allowBallFound;
                }
                graphicsManager._SetBallsDenyMark(denyBallsLocal);

                UpdateScoreSyncRowsByFlags(/* !foulCondition, foulCondition */);
                
                if (foulCondition && !isScratch && (isNoTouch || isNoCushon || isWrongHit))
                {
                    repositionOnFoul = false;
                }
                
                /*
                if (isScratch)
                {
                    // next ballがkitchen内の場合はセンターに移動させる
                    checkNextInKitchenThenMoveToCenter();
                }
                */
                
                // if (!afterBreak && 0 < point)
                // {
                //     isObjectiveSink = true;
                // }
            }
            else /*if (is4Ball)*/
            {
                isObjectiveSink = fbMadePoint;
                isOpponentSink = fbMadeFoul;
                foulCondition = false;
                deferLossCondition = false;

                winCondition = fbScoresLocal[teamIdLocal] >= 10;
            }
            
            afterBreak = true;
            isOpeningBreakLocal = false;
            calledBallId = -2;
            calledPocketId = -2;
            // semiAutoCalledBall = false;
            semiAutoCalledPocket = false;
            semiAutoCalledTimeBall = 0;

            networkingManager._OnSimulationEnded(ballsP, ballsPocketedLocal, targetPocketedLocal, otherPocketedLocal, denyBallsLocal,
                fbScoresLocal, totalPointsLocal, shotCountsLocal, shotSuccessCountsLocal, chainedPointsLocal, chainedFoulsLocal,
                specialPenaltyLocal, inningCountLocal, winRackCountLocal);

            if (isStraight && goalPointsLocal <= networkingManager.totalPointsSynced[teamIdLocal])
            {
                onMatchWin();
                return;
            }

            if (winCondition)
            {
                if (foulCondition)
                {
                    // Loss
                    onLocalTeamWin(teamIdLocal ^ 0x1U, winnerBreak);
                }
                else
                {
                    // Win
                    onLocalTeamWin(teamIdLocal, winnerBreak);
                }
            }
            else if (deferLossCondition)
            {
                // Loss
                onLocalTeamWin(teamIdLocal ^ 0x1U, winnerBreak);
            }
            else if (foulCondition)
            {
                // Foul
                onLocalTurnFoul(false, repositionOnFoul, reBreakAllowed, canChoiceFoot);
            }
            else if (isObjectiveSink && !isOpponentSink)
            {
                // Continue
                onLocalTurnContinue();
            }
            else
            {
                // Pass
                onLocalTurnPass(reBreakAllowed);
            }
        }
        else
        {
            afterBreak = true;
            isOpeningBreakLocal = false;
            calledBallId = -2;
            calledPocketId = -2;
            // semiAutoCalledBall = false;
            semiAutoCalledPocket = false;
            semiAutoCalledTimeBall = 0;
        }
    }
    #endregion

    #region GameLogic
    private void initializeRack()
    {
        for (int i = 0; i < initialPositions.Length; i++)
        {
            initialPositions[i] = new Vector3[16];
            for (int j = 0; j < 16; j++)
            {
                initialPositions[i][j] = Vector3.zero;
            }

            // cue ball always starts here (unless four ball, but we override below)
            initialPositions[i][0] = new Vector3(-k_SPOT_POSITION_X, 0.0f, 0.0f);
        }

        {
            // 8 ball
            initialBallsPocketed[0] = 0x00u;

            for (int i = 0, k = 0; i < 5; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    initialPositions[0][break_order_8ball[k++]] = new Vector3
                    (
                       k_SPOT_POSITION_X + i * k_BALL_PL_Y /*+ UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F)*/,
                       0.0f,
                       (-i + j * 2) * k_BALL_PL_X /*+ UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F)*/
                    );
                }
            }
        }

        {
            // 9 ball
            initialBallsPocketed[1] = 0xFC00u;

            for (int i = 0, k = 0; i < 5; i++)
            {
                int rown = break_rows_9ball[i];
                for (int j = 0; j <= rown; j++)
                {
                    initialPositions[1][break_order_9ball[k++]] = new Vector3
                    (
                       k_SPOT_POSITION_X + i * k_BALL_PL_Y + UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F),
                       0.0f,
                       (-rown + j * 2) * k_BALL_PL_X + UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F)
                    );
                }
            }
        }

        {
            // 4 ball (jp)
            initialBallsPocketed[2] = 0x1FFEu;
            initialPositions[2][0] = new Vector3(-k_SPOT_CAROM_X, 0.0f, 0.0f);
            initialPositions[2][13] = new Vector3(k_SPOT_CAROM_X, 0.0f, 0.0f);
            initialPositions[2][14] = new Vector3(k_SPOT_POSITION_X, 0.0f, 0.0f);
            initialPositions[2][15] = new Vector3(-k_SPOT_POSITION_X, 0.0f, 0.0f);
        }

        {
            // 4 ball (kr)
            initialBallsPocketed[3] = initialBallsPocketed[2];
            initialPositions[3] = initialPositions[2];
        }
        
        {
            // straight
            initialBallsPocketed[4] = 0x00u;

            for (int i = 0, k = 0; i < 5; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    initialPositions[4][break_order_straight[k++]] = new Vector3
                    (
                        k_SPOT_POSITION_X + i * k_BALL_PL_Y + (rackConditionLocal == 0 ? 0 : UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F)),
                        0.0f,
                        (-i + j * 2) * k_BALL_PL_X + (rackConditionLocal == 0 ? 0 : UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F))
                    );
                }
            }
        }
    }

    private int initializeStraightRack()
    {
#if TKCH_DEBUG_14RACK
        _LogInfo("TKCH BilliardsModule::initializeStraightRack()");
        _LogInfo($"  breakBallIdLocal = {breakBallIdLocal}, ballsPocketedLocal = {ballsPocketedLocal:X4}");
#endif

        int[] breakOrder = new int[break_order_straight.Length];
        // Array.Clear(breakOrder, 0, breakOrder.Length);

#if TKCH_DEBUG_14RACK
        String debugBreakOrder = "";
#endif
        int breakBallId = -1;
        int breakOrderIndex = 0; // break_order_straight.Length - (int)SoftwareFallback(ballsPocketedLocal & straight_pocket_mask);
        uint ball_bit = 0x2u;
        for (int i = 0; i < break_order_straight.Length; i++)
        {
            int ballId = i + 1;
            if (breakBallIdLocal == ballId || (ballsPocketedLocal & ball_bit) == 0x0u)
            {
                breakBallId = breakOrder[breakOrderIndex++] = ballId;
#if TKCH_DEBUG_14RACK
                debugBreakOrder += $"{ball_id_to_number[ballId]}, ";
#endif
            }            
            ball_bit <<= 1;
        }
#if TKCH_DEBUG_14RACK
        _LogInfo($"  breakOrderIndex = {breakOrderIndex}, breakOrder = {debugBreakOrder}");
#endif
        
        ball_bit = 0x2u;
        for (int i = 0; i < break_order_straight.Length; i++)
        {
            int ballId = i + 1;
            if (breakBallIdLocal != ballId && (ballsPocketedLocal & ball_bit) != 0x0u)
            {
                breakOrder[breakOrderIndex++] = ballId;
#if TKCH_DEBUG_14RACK
                debugBreakOrder += $"{ball_id_to_number[ballId]}, ";
#endif
            }            
            ball_bit <<= 1;
        }   
#if TKCH_DEBUG_14RACK
        _LogInfo($"  breakOrderIndex = {breakOrderIndex}, breakOrder = {debugBreakOrder}");
#endif
        
        for (int i = 0, k = 0; i < 5; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                int ballId = breakOrder[k++];
                ball_bit = 0x1u << ballId;
                if ((ballsPocketedLocal & ball_bit) == 0x0u)
                {
                    initialPositions[gameModeLocal][ballId] = ballsP[ballId];
                }
                else
                {
                    initialPositions[gameModeLocal][ballId] = ballsP[ballId] = new Vector3
                    (
                        k_SPOT_POSITION_X + i * k_BALL_PL_Y + (rackConditionLocal == 0 ? 0 : UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F)),
                        0.0f,
                        (-i + j * 2) * k_BALL_PL_X + (rackConditionLocal == 0 ? 0 : UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F))
                    );
                }
            }
        }

        return breakBallId;
    }

    private void resetCachedData()
    {
        for (int i = 0; i < 4; i++)
        {
            playerNamesLocal[i] = "";
        }
        repositionStateLocal = 0;
        gameModeLocal = uint.MaxValue;
        turnStateLocal = byte.MaxValue;
        previewWinningTeamLocal = 2;
    }

    private void setTransform(Transform src, Transform dest, float sf)
    {
        dest.position = src.position;
        dest.rotation = src.rotation;
        dest.localScale = src.localScale * sf;
    }

    private void setTableModel(int newTableModel, bool update)
    {
        tableModels[tableModelLocal].gameObject.SetActive(false);
        tableModels[newTableModel].gameObject.SetActive(true);

        tableModelLocal = newTableModel;

        ModelData data = tableModels[tableModelLocal];
        k_TABLE_WIDTH = data.tableWidth;
        k_TABLE_HEIGHT = data.tableHeight;
        k_CUSHION_RADIUS = data.cushionRadius;
        k_POCKET_RADIUS = data.pocketRadius;
        k_INNER_RADIUS = data.innerRadius;
        k_vE = data.cornerPocket;
        k_vF = data.sidePocket;
        pockets = data.pockets;

        Transform table_base = _GetTableBase().transform;
        auto_pocketblockers = table_base.Find(".4BALL_FILL").gameObject;
        auto_rackPosition = table_base.Find(".RACK").gameObject;
        auto_colliderBaseVFX = table_base.Find("collision.vfx").gameObject;

        pointPocketMarkers = new GameObject[6];
        pointPocketMarkerSphere = new GameObject[pointPocketMarkers.Length];
        for (int i = 0; i < pointPocketMarkers.Length; i++)
        {
            Transform pointPocketMarker = table_base.Find($"PointPocketMarker_{i}");
            pointPocketMarkers[i] = pointPocketMarker.gameObject;
            pointPocketMarkerSphere[i] = pointPocketMarker.Find("Sphere").gameObject;
        }

        Transform transformSurface = (Transform)currentPhysicsManager.GetProgramVariable("transform_Surface");
        k_rack_position = transformSurface.InverseTransformPoint(auto_rackPosition.transform.position);
        k_rack_direction = transformSurface.InverseTransformDirection(auto_rackPosition.transform.up);

        table = table_base.Find("table");
        if (table == null)
        {
            table = table_base.Find("glass");
        }
        if (update)
        {
            currentPhysicsManager.SendCustomEvent("_InitConstants");
            graphicsManager._InitializeTable();
        }

        Transform menu_transform = this.transform.Find("intl.menu");
        setTransform(table_base.Find(".MENU"), menu_transform, 1.0f);
        menu_transform.transform.position += menu_transform.right * 0.4f;

        Transform score_info_root = this.transform.Find("intl.scorecardinfo");
        setTransform(table_base.Find(".NAME_0"), score_info_root.Find("player0-name").gameObject.GetComponent<RectTransform>(), 1F / 200F);
        setTransform(table_base.Find(".NAME_1"), score_info_root.Find("player1-name").gameObject.GetComponent<RectTransform>(), 1F / 200F);

        // todo: reposition cues
    }

    public bool _IsLegacyPhysics()
    {
        return physicsModeLocal == 0;
    }

    public bool _IsNewPhysics()
    {
        return physicsModeLocal == 1;
    }

    public bool _IsBetaPhysics()
    {
        return physicsModeLocal == 2;
    }

    public GameObject _GetTableBase()
    {
        return tableModels[tableModelLocal].transform.Find("table_artwork").gameObject;
    }

    private void handle4BallHit(Vector3 loc, bool good)
    {
        if (good)
        {
            handle4BallHitGood(loc);
        }
        else
        {
            handle4BallHitBad(loc);
        }

        graphicsManager._SpawnFourBallPoint(loc, good);
        graphicsManager._UpdateScorecard();
    }

    private void handle4BallHitGood(Vector3 p)
    {
        fbMadePoint = true;
        aud_main.PlayOneShot(snd_PointMade, 1.0f);

        fbScoresLocal[teamIdLocal]++;
        if (fbScoresLocal[teamIdLocal] > 10) fbScoresLocal[teamIdLocal] = 10;
    }

    private void handle4BallHitBad(Vector3 p)
    {
        if (fbMadeFoul) return;
        fbMadeFoul = true;

        fbScoresLocal[teamIdLocal]--;
        if (fbScoresLocal[teamIdLocal] < 0) fbScoresLocal[teamIdLocal] = 0;
    }

    private void onMatchWin()
    {
        _LogInfo($"onMatchWin {(teamIdLocal)}");

        if (string.IsNullOrEmpty(tournamentRefereeLocal))
        {
            networkingManager._OnGameWin(teamIdLocal);
        }
        else
        {
            networkingManager._OnPreviewWinner(teamIdLocal);
        }
    }

    private void onLocalTeamWin(uint winner, bool winnerBreak)
    {
        _LogInfo($"onLocalTeamWin {(winner)}");

        if (isStraight)
        {
            if (teamIdLocal != 0 && teamIdLocal != winner)
            {
#if TKCH_DEBUG_SPECIAL_PENALTY
                _LogInfo("  inning count up");
#endif
                inningCountLocal++;
                networkingManager.inningCountSynced = inningCountLocal;
            }
            /*
            networkingManager.winRackCountSynced[winner]++;
            initializeRack();
            if (winnerBreak)
            {
                initialPositions[gameModeLocal][0] = ballsP[0];
            }
            */
            breakBallIdLocal = initializeStraightRack();
            
            float kitchen_x = -k_SPOT_POSITION_X;
            
            bool breakBallInRack = false;
            bool cueBallInRack = false;

            if (0 < breakBallIdLocal)
            {
                int breakBallTouchingId = ballTouching(breakBallIdLocal);
                if (0 < breakBallTouchingId)
                {
                    breakBallInRack = true;
                }
            }

            int cueBallTouchingId = ballTouching(0);
            if (0 < cueBallTouchingId)
            {
                cueBallInRack = true;
            }

#if TKCH_DEBUG_14RACK
            _LogInfo($"  breakBallInRack = {breakBallInRack}, cueBallInRack = {cueBallInRack}");
#endif
            
            bool cueBallFreeInKichen = false;
            if (breakBallInRack && cueBallInRack)
            {
#if TKCH_DEBUG_14RACK
                _LogInfo($"  breakball move to foot (normal rack)");
                _LogInfo($"  cueball move to head (in kitchen)");
#endif
                initialPositions[gameModeLocal][breakBallIdLocal] = ballsP[breakBallIdLocal] = new Vector3(k_SPOT_POSITION_X, 0.0f, 0.0f);
                initialPositions[gameModeLocal][0] = ballsP[0] = new Vector3(kitchen_x, 0.0f, 0.0f);
                cueBallFreeInKichen = true;
            }
            else
            {
                if (cueBallInRack)
                {
                    initialPositions[gameModeLocal][0] = ballsP[0] = new Vector3(kitchen_x, 0.0f, 0.0f);
                    int breakBallTouchingId = ballTouching(breakBallIdLocal);
                    if (0 == breakBallTouchingId)
                    {
#if TKCH_DEBUG_14RACK
                        _LogInfo($"  cueball move to center (confrict breakball)");
#endif
                        initialPositions[gameModeLocal][0] = ballsP[0] = new Vector3(0.0f, 0.0f, 0.0f);
                    }
                    else if (ballsP[breakBallIdLocal].x < kitchen_x)
                    {
#if TKCH_DEBUG_14RACK
                        _LogInfo($"  cueball move to head");
#endif
                    }
                    else
                    {
#if TKCH_DEBUG_14RACK
                        _LogInfo($"  cueball move to head (in kitchen)");
#endif
                        cueBallFreeInKichen = true;
                    }
                }
                else if (breakBallInRack)
                {
                    initialPositions[gameModeLocal][0] = ballsP[0];
                    initialPositions[gameModeLocal][breakBallIdLocal] = ballsP[breakBallIdLocal] = new Vector3(kitchen_x, 0.0f, 0.0f);
                    cueBallTouchingId = ballTouching(0);
                    if (breakBallIdLocal == cueBallTouchingId)
                    {
#if TKCH_DEBUG_14RACK
                        _LogInfo($"  breakball move to center (confrict cueball)");
#endif
                        initialPositions[gameModeLocal][breakBallIdLocal] = ballsP[breakBallIdLocal] = new Vector3(0.0f, 0.0f, 0.0f);
                    }
#if TKCH_DEBUG_14RACK
                    else
                    {
                        _LogInfo($"  breakball move to head");
                    }
#endif
                }
                else
                {
                    initialPositions[gameModeLocal][0] = ballsP[0];
                }
            }

#if TKCH_DEBUG_14RACK
            _LogInfo($"  cueBallFreeInKichen = {cueBallFreeInKichen}");
#endif
            
            denyBallsLocal = 0x0u;
            if (0 < breakBallIdLocal)
            {
                uint ballsWithoutBreakBall = (~(0x1u << breakBallIdLocal)) & straight_pocket_mask;
                denyBallsLocal = ballsWithoutBreakBall;
            }
            graphicsManager._SetBallsDenyMark(denyBallsLocal);
            networkingManager.denyBallsSynced = denyBallsLocal;

            networkingManager.breakBallIdSynced = (byte)breakBallIdLocal;
            networkingManager._OnGameNextBreak(
                initialBallsPocketed[gameModeLocal], initialPositions[gameModeLocal], 
                (winnerBreak ? winner : winner ^ 0x1U), 
                (!cueBallFreeInKichen /* winnerBreak */ ? 3 : 1), false, false);
            return;                
        }

        if (string.IsNullOrEmpty(tournamentRefereeLocal))
        {
            networkingManager._OnGameWin(winner);
        }
        else
        {
            networkingManager._OnPreviewWinner(winner);
        }
    }

    private void onLocalTurnPass(bool reBreakAllowed)
    {
        _LogInfo($"onLocalTurnPass");

        if (teamIdLocal != 0)
        {
#if TKCH_DEBUG_SPECIAL_PENALTY
            _LogInfo("  inning count up");
#endif
            inningCountLocal++;
            networkingManager.inningCountSynced = inningCountLocal;
        }

        networkingManager._OnTurnPass(teamIdLocal ^ 0x1u, reBreakAllowed);
    }

    private void onLocalTurnFoul(bool isTimeEnd, bool reposition, bool reBreakAllowed, bool canChoiceFoot)
    {
        _LogInfo($"onLocalTurnFoul");

        if (teamIdLocal != 0)
        {
#if TKCH_DEBUG_SPECIAL_PENALTY
            _LogInfo("  inning count up");
#endif
            inningCountLocal++;
            networkingManager.inningCountSynced = inningCountLocal;
        }

        if (isStraight)
        {
            if (isTimeEnd)
            {
                Array.Copy(ballsP, networkingManager.ballsPSynced, ballsP.Length);
                Array.Copy(totalPointsLocal, networkingManager.totalPointsSynced, totalPointsLocal.Length);
                Array.Copy(shotCountsLocal, networkingManager.shotCountsSynced, shotCountsLocal.Length);
                Array.Copy(shotSuccessCountsLocal, networkingManager.shotSuccessCountsSynced, shotSuccessCountsLocal.Length);
                Array.Copy(chainedPointsLocal, networkingManager.chainedPointsSynced, chainedPointsLocal.Length);
                Array.Copy(chainedFoulsLocal, networkingManager.chainedFoulsSynced, chainedFoulsLocal.Length);
                networkingManager.ballsPocketedSynced = ballsPocketedLocal;
                networkingManager.targetPocketedSynced = targetPocketedLocal;
                networkingManager.otherPocketedSynced = otherPocketedLocal;
                networkingManager.denyBallsSynced = denyBallsLocal;
                // networkingManager.noCushionSynced = noCushionLocal;
                networkingManager.specialPenaltySynced = (byte)specialPenaltyLocal;
            }
            
            networkingManager._OnTurnFoul(teamIdLocal ^ 0x1u, reposition, canChoiceFoot, reBreakAllowed);
            return;
        }

        networkingManager._OnTurnFoul(teamIdLocal ^ 0x1u, true, false, false);
    }

    private void onLocalTurnContinue()
    {
        _LogInfo($"onLocalTurnContinue");

        // try and close the table if possible
        if (is8Ball && isTableOpenLocal)
        {
            uint sink_orange = 0;
            uint sink_blue = 0;
            uint pmask = ballsPocketedLocal >> 2;

            for (int i = 0; i < 7; i++)
            {
                if ((pmask & 0x1u) == 0x1u)
                    sink_blue++;

                pmask >>= 1;
            }
            for (int i = 0; i < 7; i++)
            {
                if ((pmask & 0x1u) == 0x1u)
                    sink_orange++;

                pmask >>= 1;
            }

            if (sink_blue != sink_orange)
            {
                if (sink_blue > sink_orange)
                {
                    teamColorLocal = teamIdLocal;
                }
                else
                {
                    teamColorLocal = teamIdLocal ^ 0x1u;
                }

                networkingManager._OnTableClosed(teamColorLocal);
            }
        }

        networkingManager._OnTurnContinue();
    }

    private void onLocalTimerEnd()
    {
        timerRunning = false;

        _LogWarn("out of time!");

        graphicsManager._HideTimers();

        if (string.IsNullOrEmpty(tournamentRefereeLocal))
        {
            // no one is allowed to play
            canPlayLocal = false;

            if (isOurTurn())
            {
                bool reposition = true;
                bool reBreakAllowed = !afterBreak;
                if (isStraight)
                {
                    reposition = false;
                    // noCushionLocal = true;
                    specialPenaltyLocal |= 0x1u;
                    denyBallsLocal = 0x0u;
                    graphicsManager._SetBallsDenyMark(denyBallsLocal);
                    
                    int penaltyPoint = 1;
                    chainedFoulsLocal[teamIdLocal] += (!isOpeningBreakLocal) ? 1 : 0;
                    if (isOpeningBreakLocal)
                    {
                        specialPenaltyLocal |= 0x2u;
                        penaltyPoint += 2;
                    }
                    if (3 <= chainedFoulsLocal[teamIdLocal])
                    {
#if TKCH_DEBUG_SPECIAL_PENALTY
                        _LogInfo($"  chainedFoulsLocal[teamIdLocal = {teamIdLocal}] = {chainedFoulsLocal[teamIdLocal]}");
#endif
                        specialPenaltyLocal |= 0x4u;
                        penaltyPoint += 15;
                        reBreakAllowed = true;
                    }
                    if (3 <= chainedFoulsLocal[teamIdLocal ^ 0x1u])
                    {
                        chainedFoulsLocal[teamIdLocal ^ 0x1u] = 0;
                    }
                    
                    totalPointsLocal[teamIdLocal] -= penaltyPoint;
                    if (totalPointsLocal[teamIdLocal] < -127) {totalPointsLocal[teamIdLocal] = -127;}

                    UpdateScoreSyncRowsByFlags(/* false, true */);
                }

                // everyone on the current team propagates the change
                onLocalTurnFoul(true, reposition, reBreakAllowed, false);
            }
        }
    }

    private void applyCueAccess(bool gameOver)
    {
        if (gameOver)
        {
            if (_TeamPlayersOffline(0)) cueControllers[0]._SetAuthorizedOwners(Networking.IsMaster ? new[] { Networking.LocalPlayer.displayName } : new string[0]);
            if (_TeamPlayersOffline(1)) cueControllers[1]._SetAuthorizedOwners(Networking.IsMaster ? new[] { Networking.LocalPlayer.displayName } : new string[0]);
        }

        if (localPlayerId == -1)
        {
            cueControllers[0]._Disable(gameOver);
            cueControllers[1]._Disable(gameOver);
            return;
        }

        if (localTeamId == 0)
        {
            cueControllers[0]._Enable(gameOver);
            cueControllers[1]._Disable(gameOver);
        }
        else
        {
            cueControllers[1]._Enable(gameOver);
            cueControllers[0]._Disable(gameOver);
        }
    }

    // turn on any game elements that are enabled when someone is taking a shot
    private void enablePlayComponents()
    {
        bool isOurTurnVar = isOurTurn();

        if ((isOurTurnVar && isPracticeMode) || (!string.IsNullOrEmpty(tournamentRefereeLocal) && _IsLocalPlayerReferee()))
        {
            this.transform.Find("intl.controls/undo").gameObject.SetActive(true);
            this.transform.Find("intl.controls/redo").gameObject.SetActive(true);
            this.transform.Find("intl.controls/skipturn").gameObject.SetActive(true);
        }

        if (is9Ball)
        {
            marker9ball.SetActive(true);
            _Update9BallMarker();
        }

        if (isStraight && isOurTurnVar)
        {
            this.transform.Find("intl.controls/callShotLock").gameObject.SetActive(true);
        }
        else
        {
            this.transform.Find("intl.controls/callShotLock").gameObject.SetActive(false);
        }

        markerCalledBall.SetActive(false);

        refreshBallPickups();

        if (isOurTurnVar)
        {
            // Update for desktop
            desktopManager._AllowShoot();
        }
        else
        {
            desktopManager._DenyShoot();
        }

        if (timerLocal > 0)
        {
            timerRunning = true;
            graphicsManager._ShowTimers();
        }
    }

    public void _SkipTurn()
    {
        if (isPracticeMode || (!string.IsNullOrEmpty(tournamentRefereeLocal) && _IsLocalPlayerReferee()))
        {
            onLocalTurnFoul(false, false, false, false);
        }
    }

    public void _CallShotLock()
    {
        networkingManager._OnCallShotLockChanged(!callShotLockLocal);
        //callShotLockLocal = !callShotLockLocal;
        //graphicsManager._UpdatePointPocketMarker(pointPocketsLocal, callShotLockLocal);
    }
    
    public void _NextBallOnSpot(float x)
    {
#if TKCH_DEBUG_UPON_FOOT
        _LogInfo("TKCH BilliardsModule::_NextBallOnSpot()");
#endif

        int target = findNearestUnpocketedBallFromHeadSide(ballsPocketedLocal);
        if (0 < target)
        {
            pocketedballUponPool(0x1u << target, x, break_order_straight.Length);
            networkingManager.denyBallsSynced &= ~(0x1u << target);
        }
        
        networkingManager.nextBallRepositionStateSynced = (byte)(nextBallRepositionStateLocal & ~0x1u);
        networkingManager._OnRepositionBalls(ballsP, false);
    }

    public void _RequestBreak(uint teamId)
    {
#if TKCH_DEBUG_BREAKING_FOUL || TKCH_DEBUG_OPENING_BREAK || TKCH_DEBUG_SPECIAL_PENALTY
        _LogInfo($"TKCH BilliardsModule::_RequestBreak(teamId = {teamId})");
        _LogInfo($"  inningCountLocal = {inningCountLocal}, inningCountSynced = {networkingManager.inningCountSynced}");
#endif
        if (teamIdLocal == 0 /* && teamIdLocal == teamId */)
        {
#if TKCH_DEBUG_BREAKING_FOUL || TKCH_DEBUG_OPENING_BREAK || TKCH_DEBUG_SPECIAL_PENALTY
             _LogInfo("  inning count down");
#endif
            inningCountLocal--;
            networkingManager.inningCountSynced = inningCountLocal;
        }
         
#if TKCH_DEBUG_SPECIAL_PENALTY
        _LogInfo($"  chainedFoulsLocal[teamIdLocal ^ 0x1u = {teamIdLocal ^ 0x1u}] = {chainedFoulsLocal[teamIdLocal ^ 0x1u]}");
#endif
        if (3 <= chainedFoulsLocal[teamIdLocal ^ 0x1u])
        {
            chainedFoulsLocal[teamIdLocal ^ 0x1u] = 0;
        }
        Array.Copy(chainedFoulsLocal, networkingManager.chainedFoulsSynced, chainedFoulsLocal.Length);

        isOpeningBreakLocal = inningCountLocal <= 1;
        if (isOpeningBreakLocal)
        {
            initializeRack();
            networkingManager.breakBallIdSynced = 0;
        }
        else
        {
            initializeRack();
            breakBallIdLocal = break_order_straight[0];
            denyBallsLocal = (~(0x1u << breakBallIdLocal)) & straight_pocket_mask;
            graphicsManager._SetBallsDenyMark(denyBallsLocal);
            networkingManager.denyBallsSynced = denyBallsLocal;
            networkingManager.breakBallIdSynced = (byte)breakBallIdLocal;
        }

#if TKCH_DEBUG_SPECIAL_PENALTY
        _LogInfo($"  inningCountLocal = {inningCountLocal}");
        _LogInfo($"  chainedFoulsLocal[teamIdLocal ^ 0x1u = {teamIdLocal ^ 0x1u}] = {chainedFoulsLocal[teamIdLocal ^ 0x1u]}");
#endif
        
        UpdateScoreSyncRowsByFlags();

        networkingManager._OnGameNextBreak(
            initialBallsPocketed[gameModeLocal], initialPositions[gameModeLocal], 
            teamId, 1, isOpeningBreakLocal, true);
    }

    public void _CueBallInKitchen()
    {
        if (isStraight)
            return;

        // next ballがkitchen内の場合はセンターに移動させる
        // checkNextInKitchenThenMoveToCenter();
        
        ballsP[0] = initialPositions[gameModeLocal][0];
        networkingManager.repositionStateSynced = 2; // 1
        networkingManager.nextBallRepositionStateSynced = (byte)(nextBallRepositionStateLocal & ~0x2u);
        networkingManager._OnRepositionBalls(ballsP, false);
    }

    /*
    private void checkNextInKitchenThenMoveToCenter()
    {
#if TKCH_DEBUG_IN_KITCHEN
        _LogInfo("TKCH BilliardsModule::checkNextInKitchenThenMoveToCenter()");
#endif
        int target = findLowestUnpocketedBall(ballsPocketedLocal);
        float kitchen_x = -k_SPOT_POSITION_X;
#if TKCH_DEBUG_IN_KITCHEN
        _LogInfo($"  ballsP[{target}].x = {ballsP[target].x} < kitchen_x = {kitchen_x}");
#endif
        if (ballsP[target].x < kitchen_x)
        {
            // next ballがkitchen内の場合はセンターに移動させる
            //ballsP[target] = new Vector3(0.0f, 0.0f, 0.0f);
            pocketedballUponPool(0x1u << target, 0, break_order_straight.Length);
        }
    }
    */
    
    public void _Update9BallMarker()
    {
        if (marker9ball.activeSelf)
        {
            int target = findLowestUnpocketedBall(ballsPocketedLocal);
            marker9ball.transform.localPosition = ballsP[target];
        }
    }

    public void _UpdateCalledBallMarker(/* int lowestUnpocketedBallId */)
    {
        if (!gameLive)
        {
            return;
        }

        if (!isStraight)
        {
            return;
        }

        if (calledBallsLocal == 0)
        {
            markerCalledBall.SetActive(false);
        }
        
        int target = 0;
        uint ball_bit = 0x2u;
        for (int k = 0; k < break_order_straight.Length; k++)
        {
            int i = k + 1;
            if ((calledBallsLocal & ball_bit) != 0x0u)
            {
                target = i;
            }                
            ball_bit <<= 1;
        }

        if (0 < target)
        {
            bool callShotLock = callShotLockLocal && turnStateLocal != 1;
            markerCalledBall.GetComponent<MeshRenderer>().material = callShotLock ? calledBallMarkerWhite :
                ((teamIdLocal ^ teamColorLocal) == 0 ? calledBallMarkerBlue : calledBallMarkerOrange);
            markerCalledBall.transform.localPosition = ballsP[target];
            markerCalledBall.SetActive(true);
        }
        else
        {
            markerCalledBall.SetActive(false);
        }

        // if (!isLocalSimulationRunning)
        // {
        //     if (target == lowestUnpocketedBallId)
        //     {
        //         marker9ball.SetActive(false);
        //     }
        //     else
        //     {
        //         marker9ball.SetActive(true);
        //     }
        // }
    }

    public void _UpdateCalledPocketMarker()
    {
        graphicsManager._UpdatePointPocketMarker(pointPocketsLocal, false);
    }
    
    public void _UpdateNextBallRepositionSpotMarker()
    {
        if (!isOurTurn() || nextBallRepositionStateLocal == 0 || callShotLockLocal)
        {
            markerHeadSpot.SetActive(false);
            markerCenterSpot.SetActive(false);
            markerFootSpot.SetActive(false);
            requestBreakOrange.SetActive(false);
            requestBreakBlue.SetActive(false);
            return;
        }

        if ((nextBallRepositionStateLocal & 0x1u) > 0 )
        {
            // markerCenterSpot.SetActive(true);
            markerFootSpot.SetActive(true);
        }
        else
        {
            // markerCenterSpot.SetActive(false);
            markerFootSpot.SetActive(false);
        }
        
        /*
        if ((nextBallRepositionStateLocal & 0x2u) > 0 ) // || repositionStateLocal == 0 )
        {
            if (chainedFoulsLocal[teamIdLocal ^ 0x1u] < 3)
            {
                markerHeadSpot.SetActive(true);
            }
            else
            {
                markerHeadSpot.SetActive(false);
                isReposition = true;
                Vector3 k_pR = (Vector3)currentPhysicsManager.GetProgramVariable("k_pR");
                repoMaxX = k_pR.x;
                setFoulPickupEnabled(true);
                repositionStateLocal = 2;
            }
        }
        else
        {
            markerHeadSpot.SetActive(false);
        }
        */
        
        if ((nextBallRepositionStateLocal & 0x4u) > 0 )
        {
            // requestBreakOrange.SetActive(teamIdLocal != 0);
            // requestBreakBlue.SetActive(teamIdLocal == 0);
            requestBreakOrange.SetActive(true);
            requestBreakBlue.SetActive(true);
        }
        else
        {
            requestBreakOrange.SetActive(false);
            requestBreakBlue.SetActive(false);
        }
    }

    // turn off any game elements that are enabled when someone is taking a shot
    private void disablePlayComponents()
    {
        marker9ball.SetActive(false);
        setFoulPickupEnabled(false);
        refreshBallPickups();
        devhit.SetActive(false);
        guideline.SetActive(false);
        isGuidelineValid = false;
        isReposition = false;
        
        markerHeadSpot.SetActive(false);
        markerCenterSpot.SetActive(false);
        markerFootSpot.SetActive(false);
        requestBreakOrange.SetActive(false);
        requestBreakBlue.SetActive(false);

        desktopManager._DenyShoot();
        graphicsManager._HideTimers();
    }

    public int findLowestUnpocketedBall(uint field)
    {
        for (int i = 2; i <= 8; i++)
        {
            if (((field >> i) & 0x1U) == 0x00U)
                return i;
        }

        if (((field) & 0x2U) == 0x00U)
            return 1;

        for (int i = 9; i < 16; i++)
        {
            if (((field >> i) & 0x1U) == 0x00U)
                return i;
        }

        // ??
        return 0;
    }

    public int findNearestUnpocketedBallFromCueBall(uint field)
    {
        int nearestBallId = -1;
        float minSqrMagnitude = float.MaxValue;
        for (int i = 1; i < 16; i++)
        {
            if (((field >> i) & 0x1U) == 0x1U)
            {
                continue;
            }

            float sqrMagnitude = Vector3.SqrMagnitude(ballsP[0] - ballsP[i]);
            if (sqrMagnitude < minSqrMagnitude)
            {
                minSqrMagnitude = sqrMagnitude;
                nearestBallId = i;
            }
        }
        
        return nearestBallId;
    }

    public int findNearestUnpocketedBallFromHeadSide(uint field)
    {
        int nearestBallId = -1;
        float minX = float.MaxValue;
        for (int i = 1; i < 16; i++)
        {
            if (((field >> i) & 0x1U) == 0x1U)
            {
                continue;
            }

            if (ballsP[i].x < minX)
            {
                minX = ballsP[i].x;
                nearestBallId = i;
            }
        }
        
        return nearestBallId;
    }

    public int findNearestPocketFromBall(int ballId)
    {
        int pocketId = -1;
        Vector3 ballPos = ballsP[ballId];
        float abs_x = Mathf.Abs(ballPos.x);
        float abs_z_n = Mathf.Abs(ballPos.z) * findNearestPocket_n;
        bool nearSide = (abs_x + abs_z_n < findNearestPocket_x);
        if (ballPos.z < 0)
        {
            if (nearSide)
            {
                pocketId = 5;
            }
            else if (ballPos.x < 0)
            {
                pocketId = 3;
            }
            else
            {
                pocketId = 1;
            }
        }
        else
        {
            if (nearSide)
            {
                pocketId = 4;
            }
            else if (ballPos.x < 0)
            {
                pocketId = 2;
            }
            else
            {
                pocketId = 0;
            }
        }

        return pocketId;
    }

    public int findFrontsidePocketFromCueball(int targetBallId)
    {
#if TKCH_DEBUG_SEMIAUTO_CALL || TKCH_DEBUG_SEMIAUTO_CALL_SIDE || TKCH_DEBUG_NEXT_BREAK
        _LogInfo("TKCH BilliardsModule::findFrontsidePocketFromCueball()");
#endif
        int pocketId = -1;
        
        Vector3 cue2target = ballsP[targetBallId] - ballsP[0];
        float c2tRad = -Mathf.Atan2(cue2target.z, cue2target.x);
        float c2tDeg = c2tRad * Mathf.Rad2Deg;
#if TKCH_DEBUG_SEMIAUTO_CALL
        //_LogInfo($"  c2tDeg = {c2tDeg}, c2tRad = {c2tRad}, ballsP[{targetBallId}].y = {ballsP[targetBallId].y}");
#endif

        float minDegDiff = float.MaxValue;
        for (int i = 0; i < pcketLocations.Length; i++)
        {
            Vector3 target2pocket = pcketLocations[i] - ballsP[targetBallId];
#if TKCH_DEBUG_SEMIAUTO_CALL
            // _LogInfo($"  pockets[{j}].transform.localPosition = {pockets[j].transform.localPosition}, ballsP[0].y = {ballsP[0].y}");
#endif
            float t2pRad = -Mathf.Atan2(target2pocket.z, target2pocket.x);
            float t2pDeg = t2pRad * Mathf.Rad2Deg;

            float degDiff = c2tDeg - t2pDeg;
            if (degDiff < 0)
            {
                degDiff = -degDiff;
            }
            if (180 < degDiff)
            {
                degDiff = 360 - degDiff;
            }

#if TKCH_DEBUG_SEMIAUTO_CALL
            _LogInfo($"  c2tDeg = {c2tDeg}, t2pDeg = {t2pDeg}, degDiff = {degDiff}");
#endif
            if (degDiff < minDegDiff)
            {
                minDegDiff = degDiff;
                pocketId = i;
            }
        }
        
        return pocketId;
    }
    
    public uint findEasiestBallAndPocket(uint field)
    {
        Vector3[][] matrix = new Vector3[16][];
        for (int i = 0; i < matrix.Length; i++)
        {
            matrix[i] = new Vector3[pcketLocations.Length];
            for (int j = 0; j < matrix[i].Length; j++)
            {
                matrix[i][j] = Vector3.positiveInfinity;
            }
        }

        int[] matrixIndexPosListByCondition = new int[findEasiestBallAndPocketConditions.Length];
        int[][] matrixIndexListByCondition = new int[findEasiestBallAndPocketConditions.Length][];
        for (int i = 0; i < matrixIndexListByCondition.Length; i++)
        {
            matrixIndexPosListByCondition[i] = 0;
            matrixIndexListByCondition[i] = new int[matrix.Length * matrix[0].Length];
            for (int j = 0; j < matrixIndexListByCondition[i].Length; j++)
            {
                matrixIndexListByCondition[i][j] = -1;
            }
        }

        for (int i = 1; i < 16; i++)
        {
            if (((field >> i) & 0x1U) == 0x1U)
            {
                continue;
            }

            Vector3 cue2target = ballsP[i] - ballsP[0];
            float c2tRad = -Mathf.Atan2(cue2target.z, cue2target.x);
            float c2tDeg = c2tRad * Mathf.Rad2Deg;
            float c2tSqrMagnitude = Vector3.SqrMagnitude(cue2target);

            for (int j = 0; j < pcketLocations.Length; j++)
            {
                Vector3 target2pocket = pcketLocations[j] - ballsP[i];
                float t2pRad = -Mathf.Atan2(target2pocket.z, target2pocket.x);
                float t2pDeg = t2pRad * Mathf.Rad2Deg;
                float t2pSqrMagnitude = Vector3.SqrMagnitude(target2pocket);
#if TKCH_DEBUG_SEMIAUTO_CALL_SIDE
                if (debugLogFlg && 4 <= j) _LogInfo($"  t{i} to side{j} t2pDeg = {t2pDeg}");
#endif
                if (4 <= j && ((t2pDeg < 0 &&(t2pDeg < -135 || -45 < t2pDeg)) || (0 <= t2pDeg &&(t2pDeg < 45 || 135 < t2pDeg))))
                {
#if TKCH_DEBUG_SEMIAUTO_CALL_SIDE
                    if (debugLogFlg) _LogInfo($"  side pocket skip");
#endif
                    continue;
                }

                float degDiff = c2tDeg - t2pDeg;
                if (degDiff < 0)
                {
                    degDiff = -degDiff;
                }
                if (180 < degDiff)
                {
                    degDiff = 360 - degDiff;
                }
                
                // x:deg, y:t2p, z:c2t
                Vector3 p = matrix[i][j] = new Vector3(degDiff, t2pSqrMagnitude, c2tSqrMagnitude);
#if TKCH_DEBUG_SEMIAUTO_CALL
                // if (debugLogFlg) _LogInfo($"  matrix[{i}][{j}]] = {p.x}, {p.y}, {p.z}");
#endif

                 for (int k = 0; k < findEasiestBallAndPocketConditions.Length; k++)
                {
                    Vector3 c = findEasiestBallAndPocketConditions[k];
                    if (p.x < c.x && p.y < c.y && p.z < c.z)
                    {
                        matrixIndexListByCondition[k][matrixIndexPosListByCondition[k]++] = (i * 16) + j;
#if TKCH_DEBUG_SEMIAUTO_CALL
                        // if (debugLogFlg) _LogInfo($"    k = {k},  (i * 16) + j = {matrixIndexListByCondition[k][matrixIndexPosListByCondition[k]-1]}");
#endif
                        break;
                    }
                }
            }
        }

#if TKCH_DEBUG_SEMIAUTO_CALL
        if (debugLogFlg)
        {
            for (int i = 0; i < matrixIndexPosListByCondition.Length; i++)
            {
                _LogInfo($"  matrixIndexPosListByCondition[{i}] = {matrixIndexPosListByCondition[i]}");
                for (int j = 0; j < matrixIndexListByCondition[i].Length; j++)
                {
                    if (matrixIndexListByCondition[i][j] < 0) break;
                    _LogInfo($"  matrixIndexListByCondition[{i}][{j}] = {matrixIndexListByCondition[i][j]}");
                }
            }
        }
#endif

        int easiestBallId = -1;
        int easiestPocketId = -1;
        for (int i = 0; i < matrixIndexListByCondition.Length; i++)
        {
            int nearestBallId = -1;
            int nearestPocketId = -1;
            float minSqrMagnitude = float.MaxValue;
            for (int j = 0; j < matrixIndexListByCondition[i].Length; j++)
            {
                int materixIndex = matrixIndexListByCondition[i][j];
#if TKCH_DEBUG_SEMIAUTO_CALL
                // if (debugLogFlg) _LogInfo($"  materixIndex = {materixIndex}, matrixIndexListByCondition[{i}][{j}]] = {matrixIndexListByCondition[i][j]}");
#endif
                if (materixIndex < 0 || matrixIndexPosListByCondition[i] <= j)
                {
                    continue;
                }

                int ballId = materixIndex / 16;
                int pocketId = materixIndex % 16;
                Vector3 p = matrix[ballId][pocketId];
#if TKCH_DEBUG_SEMIAUTO_CALL
                if (debugLogFlg) _LogInfo($"  materixIndex = {materixIndex}, matrixIndexListByCondition[{i}][{j}]] = {matrixIndexListByCondition[i][j]}");
                if (debugLogFlg) _LogInfo($"  matrix[ballId = {ballId}][pocketId = {pocketId}]] = {p.x}, {p.y}, {p.z}");
#endif
                
                float sqrMagnitude = p.z;
                if (0 <= sqrMagnitude && sqrMagnitude < minSqrMagnitude)
                {
                    minSqrMagnitude = sqrMagnitude;
                    nearestBallId = ballId;
                    nearestPocketId = pocketId;
                }
            }

            if (0 <= nearestBallId && 0 <= nearestPocketId)
            {
                easiestBallId = nearestBallId;
                easiestPocketId = nearestPocketId;
                break;
            }
        }
        
#if TKCH_DEBUG_SEMIAUTO_CALL
        if (debugLogFlg) _LogInfo($"  easiestBallId = {easiestBallId}, easiestPocketId = {easiestPocketId}");
#endif

        return  ((easiestPocketId < 0 ? 0xFFFFu : (uint)easiestPocketId) << 16) | (easiestBallId < 0 ? 0xFFFFu : (uint)easiestBallId);
    }

    /*
    public uint findEasiestBallAndPocket__(uint field)
    {
        float[] group = new[] { 0, 15.0f, 30.0f, 45.0f, 60.0f };
        float[][][] matrix = new float[group.Length][][];
        for (int k = 0; k < matrix.Length; k++)
        {
            matrix[k] = new float[16][];
            for (int j = 0; j < matrix[k].Length; j++)
            {
                matrix[k][j] = new float[pcketLocations.Length];
                for (int i = 0; i < matrix[k][j].Length; i++)
                {
                    matrix[k][j][i] = -1;
                }
            }
        }

#if TKCH_DEBUG_SEMIAUTO_CALL
        // if (debugLogFlg)
        // {
        //     for (int k = 0; k < matrix.Length; k++)
        //     {
        //         for (int j = 0; j < matrix[k].Length; j++)
        //         {
        //             for (int i = 0; i < matrix[k][j].Length; i++)
        //             {
        //                 _LogInfo($"  matrix[{k}][{j}][{i}] = {matrix[k][j][i]}");
        //             }
        //         }
        //     }
        // }
#endif
        
        for (int i = 1; i < 16; i++)
        {
            if (((field >> i) & 0x1U) == 0x1U)
            {
                continue;
            }

            Vector3 cue2target = ballsP[i] - ballsP[0];
            float c2tRad = -Mathf.Atan2(cue2target.z, cue2target.x);
            float c2tDeg = c2tRad * Mathf.Rad2Deg;

            for (int j = 0; j < pointPocketMarkers.Length; j++)
            {
                Vector3 target2pocket = pcketLocations[j] - ballsP[i];
                float t2pRad = -Mathf.Atan2(target2pocket.z, target2pocket.x);
                float t2pDeg = t2pRad * Mathf.Rad2Deg;

                float degDiff = c2tDeg - t2pDeg;
                if (degDiff < 0)
                {
                    degDiff = -degDiff;
                }
                if (180 < degDiff)
                {
                    degDiff = 360 - degDiff;
                }
                
                for (int k = group.Length - 1; 0 <= k; k--)
                {
#if TKCH_DEBUG_SEMIAUTO_CALL
                    if (debugLogFlg) _LogInfo($"  ballId = {i}, pocketId[{j}] = {j}, group[{k}] = {group[k]}, degDiff = {degDiff}");
#endif
                    if (group[k] < degDiff)
                    {
                        float sqrMagnitude = Vector3.SqrMagnitude(ballsP[i] - pcketLocations[j]);
                        matrix[k][i][j] = sqrMagnitude;
#if TKCH_DEBUG_SEMIAUTO_CALL
                        if (debugLogFlg)  _LogInfo($"    matrix[{k}][{i}][{j}] = {sqrMagnitude}");
#endif
                        break;
                    }
                }
            }
        }

        int easiestBallId = -1;
        int easiestPocketId = -1;

        for (int k = 0; k < matrix.Length; k++)
        {
            int nearestBallId = -1;
            int nearestPocketId = -1;
            float minSqrMagnitude = float.MaxValue;
            for (int j = 0; j < matrix[k].Length; j++)
            {
                for (int i = 0; i < matrix[k][j].Length; i++)
                {
                    float sqrMagnitude = matrix[k][j][i];
                    if (0 <= sqrMagnitude && sqrMagnitude < minSqrMagnitude)
                    {
                        minSqrMagnitude = sqrMagnitude;
                        nearestBallId = j;
                        nearestPocketId = i;
                    }
                }
            }

            if (0 <= nearestBallId && 0 <= nearestPocketId)
            {
                easiestBallId = nearestBallId;
                easiestPocketId = nearestPocketId;
                break;
            }
        }
        
#if TKCH_DEBUG_SEMIAUTO_CALL
        if (debugLogFlg) _LogInfo($"  easiestBallId = {easiestBallId}, easiestPocketId = {easiestPocketId}");
#endif

        return  ((easiestPocketId < 0 ? 0xFFFFu : (uint)easiestPocketId) << 16) | (easiestBallId < 0 ? 0xFFFFu : (uint)easiestBallId);
    }
    */

    private void setBallPickupActive(int ballId, bool active)
    {
        Transform pickup = balls[ballId].transform.GetChild(0);

        pickup.gameObject.SetActive(active);
        pickup.GetComponent<SphereCollider>().enabled = active;
        ((VRC_Pickup)pickup.GetComponent(typeof(VRC_Pickup))).pickupable = active;
        if (!active) ((VRC_Pickup)pickup.GetComponent(typeof(VRC_Pickup))).Drop();
    }

    private void refreshBallPickups()
    {
        bool canUsePickup = (isOurTurn() && isPracticeMode) || (!string.IsNullOrEmpty(tournamentRefereeLocal) && _IsLocalPlayerReferee());

        uint ball_bit = 0x1u;
        for (int i = 0; i < balls.Length; i++)
        {
            if (gameLive && (canUsePickup || (i == 0 && isReposition)) && canPlayLocal && (ballsPocketedLocal & ball_bit) == 0x0u)
            {
                setBallPickupActive(i, true);
            }
            else
            {
                setBallPickupActive(i, false);
            }
            ball_bit <<= 1;
        }
    }

    private void setFoulPickupEnabled(bool enabled)
    {
        markerObj.SetActive(enabled);
        if (enabled)
        {
            setBallPickupActive(0, true);
        }
        else if (!isPracticeMode && !(!string.IsNullOrEmpty(tournamentRefereeLocal) && _IsLocalPlayerReferee()))
        {
            setBallPickupActive(0, false);
        }
    }

    private void tickTimer()
    {
        if (gameLive && timerRunning && canPlayLocal)
        {
            float timeRemaining = timerLocal - (Networking.GetServerTimeInMilliseconds() - timerStartLocal) / 1000.0f;
            float timePercentage = timeRemaining >= 0.0f ? 1.0f - (timeRemaining / timerLocal) : 0.0f;

            graphicsManager._SetTimerPercentage(timePercentage);

            if (timeRemaining < 0.0f)
            {
                onLocalTimerEnd();
            }
        }

#if TKCH_DEBUG_NEXT_BREAK
        // if (debugLogFlg) _LogInfo($"  isOpeningBreakLocal = {isOpeningBreakLocal}");
#endif
        if (isStraight && gameLive && canPlayLocal && !isOpeningBreakLocal /* afterBreak */)
        {
            // int target = findLowestUnpocketedBall(ballsPocketedLocal);
            //int target = (!afterBreak && 0 < breakBallIdLocal) ? breakBallIdLocal : findNearestUnpocketedBallFromCueBall(ballsPocketedLocal | denyBallsLocal);
            int target = -1;
            int pocketId = -1;

            if ((semiAutoCallBallLocal && semiAutoCalledTimeBall <= 0 && calledBallId < 0) ||
                (semiAutoCallPocketLocal && !semiAutoCalledPocket && calledPocketId < 0 && 0 < calledBallsLocal))
            {
                uint pocketAndBall = findEasiestBallAndPocket(ballsPocketedLocal | denyBallsLocal);
                if (pocketAndBall != 0xFFFFFFFF)
                {
                    target = (int)(pocketAndBall & 0xFFFFu);
                    pocketId = (int)((pocketAndBall >> 16) & 0xFFFFu);
                }
#if TKCH_DEBUG_NEXT_BREAK
                if (debugLogFlg) _LogInfo($"  afterBreak = {afterBreak}, breakBallIdLocal = {breakBallIdLocal}, target = {target}, pocketId = {pocketId}, calledBallId = {calledBallId}");
#endif
                if (!afterBreak && 0 < breakBallIdLocal)
                {
                    target = breakBallIdLocal;
                    pocketId = findFrontsidePocketFromCueball(target);
                }
            }
            
#if TKCH_DEBUG_NEXT_BREAK
            if (debugLogFlg) _LogInfo($"  target(final) = {target}");
#endif
#if TKCH_DEBUG_SEMIAUTO_CALL || TKCH_DEBUG_SEMIAUTO_CALL_SIDE || TKCH_DEBUG_NEXT_BREAK
            debugLogFlg = false;
#endif
            if (0 < target)
            {
                // bool isDesktopUser = ReferenceEquals(null, Networking.LocalPlayer) ? false : !Networking.LocalPlayer.IsUserInVR();
                float elapsedSeconds = (Networking.GetServerTimeInMilliseconds() - timerStartLocal) / 1000.0f;
                
#if TKCH_DEBUG_SEMIAUTO_CALL
                // if (0.1f < elapsedSeconds && elapsedSeconds < 0.12f)
                // {
                //     _LogInfo($"  semiAutoCalledBall = {semiAutoCalledBall}, calledBallId = {calledBallId}, target = {target}");
                // }
#endif

                // if (semiAutoCallBallLocal && !semiAutoCalledBall && calledBallId < 0)
                if (semiAutoCallBallLocal && semiAutoCalledTimeBall <= 0 && calledBallId < 0)
                {
                    if (0.4f < elapsedSeconds)
                    {
                        _TriggerOtherBallHit(target, true);
                        // semiAutoCalledBall = true;
                        semiAutoCalledTimeBall = elapsedSeconds;
                    }
                }
            
                // if (semiAutoCallPocketLocal && !semiAutoCalledPocket && calledPocketId < 0)
                // {
                //     if (0.8f < elapsedSeconds)
                //     {
                //         // int pocketId = findNearestPocketFromBall(target);
                //         int pocketId = findFrontsidePocketFromCueball(target);
                //         _TriggerPocketHit(pocketId, true);
                //         semiAutoCalledPocket = true;
                //     }
                // }
            }
            
            if (semiAutoCallPocketLocal && !semiAutoCalledPocket && calledPocketId < 0 && 0 < calledBallsLocal)
            {
                float elapsedSeconds = (Networking.GetServerTimeInMilliseconds() - timerStartLocal) / 1000.0f;
                if (0.4f + semiAutoCalledTimeBall < elapsedSeconds)
                {
                    // int pocketId = findNearestPocketFromBall(target);
                    // int pocketId = findFrontsidePocketFromCueball(target);
                    _TriggerPocketHit(pocketId, true);
                    semiAutoCalledPocket = true;
                }
            }
        }
    }

    private bool isOurTurn()
    {
        return localPlayerId >= 0 && (localTeamId == teamIdLocal || isPracticeMode);
    }

    public bool _AllPlayersOffline()
    {
        for (int i = 0; i < 4; i++)
        {
            if (playerNamesLocal[i] == "") continue;

            VRCPlayerApi player = _GetPlayerByName(playerNamesLocal[i]);
            if (Utilities.IsValid(player))
            {
                return false;
            }
        }

        return true;
    }

    public bool _TeamPlayersOffline(uint teamId)
    {
        for (int i = 0; i < 4; i++)
        {
            if (teamId != (uint)(i & 0x1u)) continue;
            if (playerNamesLocal[i] == "") continue;

            VRCPlayerApi player = _GetPlayerByName(playerNamesLocal[i]);
            if (Utilities.IsValid(player))
            {
                return false;
            }
        }

        return true;
    }

    public VRCPlayerApi _GetPlayerByName(string name)
    {
        VRCPlayerApi[] onlinePlayers = VRCPlayerApi.GetPlayers(new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()]);
        for (int playerId = 0; playerId < onlinePlayers.Length; playerId++)
        {
            if (onlinePlayers[playerId].displayName == name)
            {
                return onlinePlayers[playerId];
            }
        }
        return null;
    }

    public void _IndicateError()
    {
        graphicsManager._FlashTableColor(k_colour_foul);
    }

    public void _IndicateSuccess()
    {
        // nothing, for now
    }

    public string _SerializeGameState()
    {
        return networkingManager._EncodeGameState();
    }

    public void _LoadSerializedGameState(string gameState)
    {
        if (string.IsNullOrEmpty(tournamentRefereeLocal))
        {
            // no loading on top of other people's games
            if (!_IsPlayer(Networking.LocalPlayer)) return;

            // no loading outside of practice
            if (!isPracticeMode) return;
        }
        else
        {
            // only host can load on top of tournament
            if (!_IsLocalPlayerReferee()) return;
        }

        networkingManager._OnLoadGameState(gameState);
        // practiceManager._Record();
    }

    public object[] _SerializeInMemoryState()
    {
        Vector3[] positionClone = new Vector3[ballsP.Length];
        Array.Copy(ballsP, positionClone, ballsP.Length);
        int[] scoresClone = new int[fbScoresLocal.Length];
        Array.Copy(fbScoresLocal, scoresClone, fbScoresLocal.Length);
        int[] pointsClone = new int[totalPointsLocal.Length];
        Array.Copy(totalPointsLocal, pointsClone, totalPointsLocal.Length);
        int[] countsClone = new int[shotCountsLocal.Length];
        Array.Copy(shotCountsLocal, countsClone, shotCountsLocal.Length);
        int[] successCountsClone = new int[shotSuccessCountsLocal.Length];
        Array.Copy(shotSuccessCountsLocal, successCountsClone, shotSuccessCountsLocal.Length);
        return new object[22]
        {
            positionClone, ballsPocketedLocal, scoresClone, gameModeLocal, teamIdLocal, repositionStateLocal, isTableOpenLocal, teamColorLocal, fourBallCueBallLocal,
            turnStateLocal, networkingManager.cueBallVSynced, networkingManager.cueBallWSynced, networkingManager.previewWinningTeamSynced, networkingManager.nextBallRepositionStateSynced,
            targetPocketedLocal, otherPocketedLocal, denyBallsLocal, pointPocketsLocal, calledBallsLocal, pointsClone, countsClone, successCountsClone
        };
    }

    public void _LoadInMemoryState(object[] state, int stateIdLocal)
    {
        networkingManager._ForceLoadFromState(
            stateIdLocal,
            (Vector3[])state[0], (uint)state[1], (int[])state[2], (uint)state[3], (uint)state[4], (uint)state[5], (bool)state[6], (uint)state[7], (uint)state[8],
            (byte)state[9], (Vector3)state[10], (Vector3)state[11], (byte)state[12], (byte)state[13],
            (uint)state[14], (uint)state[15], (uint)state[16], (byte)state[17], (uint)state[18], (int[])state[19], (int[])state[20], (int[])state[21]
        );
    }

    public bool _AreInMemoryStatesEqual(object[] a, object[] b)
    {
        Vector3[] posA = (Vector3[])a[0];
        Vector3[] posB = (Vector3[])b[0];
        for (int i = 0; i < ballsP.Length; i++) if (posA[i] != posB[i]) return false;

        int[] scoresA = (int[])a[2];
        int[] scoresB = (int[])b[2];
        for (int i = 0; i < fbScoresLocal.Length; i++) if (scoresA[i] != scoresB[i]) return false;

        int[] pointsA = (int[])a[19];
        int[] pointsB = (int[])b[19];
        for (int i = 0; i < totalPointsLocal.Length; i++) if (pointsA[i] != pointsB[i]) return false;

        int[] countA = (int[])a[20];
        int[] countB = (int[])b[20];
        for (int i = 0; i < shotCountsLocal.Length; i++) if (countA[i] != countB[i]) return false;

        int[] succesCountA = (int[])a[21];
        int[] succesCountB = (int[])b[21];
        for (int i = 0; i < shotSuccessCountsLocal.Length; i++) if (succesCountA[i] != succesCountB[i]) return false;

        for (int i = 0; i < a.Length; i++) if (i != 0 && i != 2 && i < 19 && !a[i].Equals(b[i])) return false;

        return true;
    }

    public bool _IsLocalPlayerReferee()
    {
        return _IsReferee(Networking.LocalPlayer);
    }

    public bool _IsModerator(VRCPlayerApi player)
    {
        return Array.IndexOf(moderators, player.displayName) != -1;
    }

    public bool _IsReferee(VRCPlayerApi player)
    {
        if (player == null) return false;

        if (string.IsNullOrEmpty(tournamentRefereeLocal)) return false;

        return player.displayName == tournamentRefereeLocal || _IsModerator(player);
    }

    public bool _IsPlayer(VRCPlayerApi who)
    {
        if (who == null) return false;
        if (who.isLocal && localPlayerId >= 0) return true;

        for (int i = 0; i < 4; i++)
        {
            if (playerNamesLocal[i] == who.displayName)
            {
                return true;
            }
        }

        return false;
    }

    private bool stringArrayEquals(string[] a, string[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    private bool intArrayEquals(int[] a, int[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    private bool vector3ArrayEquals(Vector3[] a, Vector3[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    public static uint SoftwareFallback(uint value)
    {
        const uint c1 = 0x55555555u;
        const uint c2 = 0x33333333u;
        const uint c3 = 0x0F0F0F0Fu;
        const uint c4 = 0x01010101u;

        value -= (value >> 1) & c1;
        value = (value & c2) + ((value >> 2) & c2);
        value = (((value + (value >> 4)) & c3) * c4) >> 24;

        return value;
    }
    
    private int pocketedballUponPool(uint ballsPocketed, float posX, int safeLimitLoop)
    {
#if TKCH_DEBUG_UPON_FOOT
        _LogInfo("TKCH BilliardsModule::pocketedballUponPool()");
        _LogInfo($"  ballsPocketed = {ballsPocketed:X4}");
#endif
        if (ballsPocketed == 0x0u)
        {                                                       
            return -1; // 0x0u;
        }

        uint uponBalls = 0x0u;
        int uponBallsCount = 0;
        uint uponRacks = 0x0u;
        uint ball_bit = 0x2u;
        for (int i = 1; i < ballsP.Length; i++)
        {
            if ((ballsPocketed & ball_bit) != 0x0u)
            {
                Vector3 beforePos = ballsP[i];
#if TKCH_DEBUG_UPON_FOOT
                _LogInfo($"  ballsP[{i}] = [{ballsP[i].x},{ballsP[i].y},{ballsP[i].z}]");
#endif
                ballsP[i] = //Vector3.zero;
                new Vector3
                (
                    posX,
                    0.0f,
                    0.0f
                );
                int touching = ballTouching(i);
#if TKCH_DEBUG_UPON_FOOT
                _LogInfo($"  touching = {touching}, ballsP[{i}] = [{ballsP[i].x},{ballsP[i].y},{ballsP[i].z}]");
#endif
                int limit = safeLimitLoop;
                while (0 <= touching)
                {
#if TKCH_DEBUG_UPON_FOOT
                    //_LogInfo($"  k_BALL_DIAMETRE = {k_BALL_DIAMETRE}, ballsP[{i}].z = {ballsP[i].z}");
#endif
                    // ballsP[i] = new Vector3
                    // (
                    //     ballsP[touching].x + k_BALL_DIAMETRE,
                    //     ballsP[touching].y,
                    //     ballsP[touching].z
                    // );
                    float distanceZ = ballsP[i].z;
                    float adjustX = Mathf.Sqrt(Mathf.Pow(k_BALL_DIAMETRE, 2f) - Mathf.Pow(distanceZ, 2f));
#if TKCH_DEBUG_UPON_FOOT
                    _LogInfo($"  distanceZ = {distanceZ}, adjustX = {adjustX}");
#endif
                    ballsP[i] = new Vector3
                    (
                        ballsP[touching].x + adjustX,
                        ballsP[touching].y,
                        0
                    );
                    touching = ballTouching(i);
#if TKCH_DEBUG_UPON_FOOT
                    //_LogInfo($"  touching = {touching}, ballsP[{i}] = [{ballsP[i].x},{ballsP[i].y},{ballsP[i].z}]");
#endif
                    if (0 < limit)
                    {
                        limit--;
                    }
                    else
                    {
                        break;
                    }
                }

                if (footCushionTouching(ballsP[i]))
                {
                    ballsP[i] = beforePos;
                    continue;
                }
                
                uponBalls |= ball_bit;
                uponBallsCount++;

                int rackPosNum = (int)((beforePos.x - k_rack_position.x) / k_BALL_DIAMETRE);
                uponRacks |= 0x1u << rackPosNum;
            }
            ball_bit <<= 1;
        }

        //targetPocketedLocal[0] &= ~(uponRacks << 24);
        //pocketedRackLocal &= ~uponRacks;
#if TKCH_DEBUG_POCKETED_RACK || TKCH_DEBUG_UPON_FOOT
        //_LogInfo($"  uponRacks = {uponRacks:X4}, targetPocketedLocal[0] = {targetPocketedLocal[0]:X8}");
        //_LogInfo($"  uponRacks = {uponRacks:X4}, pocketedRackLocal = {pocketedRackLocal:X4}");
#endif

        targetPocketedLocal &= ~((uponBalls << 16) | (uponBalls & 0xFFFFu));
        ballsPocketedLocal &= ~uponBalls;
        otherPocketedLocal &= ~uponBalls;
        //onePocketNotYetUponLocal -= uponBallsCount;
        //return uponBalls;
        if (uponBallsCount <= 0)
        {
            _LogWarn("failed to upon " + (posX == 0 ? "center" : (posX == k_SPOT_POSITION_X ? "foot" : $"x = {posX}")) + ". no place to put.");
        }
        return uponBallsCount;
    }
    
    private int ballTouching(int n)
    {
        for (int i = 0; i < ballsP.Length; i++)
        {
            if (i == n)
            {
                continue;
            }
            if ((ballsP[n] - ballsP[i]).sqrMagnitude < 0.003598f) //k_BALL_DSQR)
            {
                return i;
            }
        }

        return -1;
    }

    private bool footCushionTouching(Vector3 ballPosition)
    {
#if TKCH_DEBUG_UPON_FOOT
        //_LogInfo("TKCH BilliardsModule::footCushionTouching()");
        //_LogInfo($"  check cushion {k_TABLE_WIDTH} < ball {ballPosition.x + k_BALL_RADIUS}");
#endif
        if (k_TABLE_WIDTH < ballPosition.x + k_BALL_RADIUS)
        {
            return true;
        }

        return false;
    }
    #endregion

    public void UpdateScoreSyncRowsByFlags(/* bool foulCountClear, bool foulCountIncrement */)
    {
#if TKCH_DEBUG_WINRACKCOUNT || TKCH_DEBUG_SCORE || TKCH_DEBUG_AVG
        _LogInfo("TKCH BilliardsModule::UpdateScoreSyncRowsByFlags()");
#endif
#if TKCH_DEBUG_WINRACKCOUNT
        _LogInfo($"  winRackCountLocal = {winRackCountLocal[0]}-{winRackCountLocal[1]}");
#endif
#if TKCH_DEBUG_AVG
        _LogInfo($"  shotCountsLocal = {shotCountsLocal[0]}-{shotCountsLocal[1]}");
        _LogInfo($"  shotSuccessCountsLocal = {shotSuccessCountsLocal[0]}-{shotSuccessCountsLocal[1]}");
#endif
        for (int teamId = 0; teamId < 2; teamId++)
        {
            uint[] encodeScoreSyncValues = null;
            if (teamId == teamIdLocal)
            {
                int maxChainedPoint = scoreScreen.GetTeamSafeNoPocketShotCount(teamId);
                if (maxChainedPoint < chainedPointsLocal[teamIdLocal]) {maxChainedPoint = chainedPointsLocal[teamId];}

#if TKCH_DEBUG_SCORE
                _LogInfo($"  update teamId = {teamId}");
#endif

                encodeScoreSyncValues = scoreScreen.EncodeScoreParams(
                    totalPointsLocal[teamId],
                    chainedFoulsLocal[teamId], // (foulCountClear ? 0 : scoreScreen.GetTeamScratchCount(teamId) + (foulCountIncrement ? 1 : 0)),
                    winRackCountLocal[teamId],
                    inningCountLocal + 1, // turn count
                    maxChainedPoint, // high run
                    shotCountsLocal[teamId] == 0 ? 0 : ((shotSuccessCountsLocal[teamId] * 100) / (shotCountsLocal[teamId])) // avg.
                );
            }
            else
            {
#if TKCH_DEBUG_SCORE
                _LogInfo($"  through teamId = {teamId}");
#endif
                encodeScoreSyncValues = scoreScreen.EncodeScoreParams(
                    scoreScreen.GetTeamPoint(teamId),
                    chainedFoulsLocal[teamId], // scoreScreen.GetTeamScratchCount(teamId),
                    scoreScreen.GetTeamPocketBallCount(teamId),
                    scoreScreen.GetTeamShotCount(teamId),
                    scoreScreen.GetTeamSafeNoPocketShotCount(teamId),
                    scoreScreen.GetTeamInvalidPocketBallCount(teamId) // avg.
                );
                /*
                encodeScoreSyncValues = scoreScreen.EncodeScoreSyncValues();
                */
            }
            Array.Copy(encodeScoreSyncValues, 0,
                networkingManager.scoreSyncRows, teamId * 2, 
                encodeScoreSyncValues.Length);
        }
#if TKCH_DEBUG_SCORE
        _LogInfo($"  networkingManager.scoreSyncRows = {networkingManager.scoreSyncRows[0]:X8}-{networkingManager.scoreSyncRows[1]:X8}");
        _LogInfo($"                                    {networkingManager.scoreSyncRows[2]:X8}-{networkingManager.scoreSyncRows[3]:X8}");
#endif
    }
    
    public void UpdateScoreSyncRowsByParams(uint teamId, int[] totalPoints, int[] chainedFouls, int[] winRackCount, int[] shotCounts, int[] shotSuccessCounts, int[] chainedPoints)
    {
#if TKCH_DEBUG_AVG
        _LogInfo("TKCH BilliardsModule::UpdateScoreSyncRowsByParams()");
        _LogInfo($"  shotCounts = {shotCounts[0]}-{shotCounts[1]}");
        _LogInfo($"  shotSuccessCounts = {shotSuccessCounts[0]}-{shotSuccessCounts[1]}");
#endif
        for (int i = 0; i < 2; i++)
        {
            uint[] encodeScoreSyncValues = null;
            if (teamId == i)
            {
                encodeScoreSyncValues = scoreScreen.EncodeScoreParams(
                    totalPoints[teamId],
                    chainedFouls[teamId],
                    winRackCount[teamId],
                    0, // turn count
                    chainedPoints[teamId], // high run
                    shotCounts[teamId] == 0 ? 0 : ((shotSuccessCounts[teamId] * 100) / (shotCounts[teamId])) // avg.
                );
            }
            else
            {
                encodeScoreSyncValues = scoreScreen.EncodeScoreParams(
                    totalPoints[teamId ^ 0x1u],
                    chainedFouls[teamId ^ 0x1u],
                    winRackCount[teamId ^ 0x1u],
                    0, // turn count
                    chainedPoints[teamId ^ 0x1u], // high run
                    shotCounts[teamId ^ 0x1u] == 0 ? 0 : ((shotSuccessCounts[teamId ^ 0x1u] * 100) / (shotCounts[teamId ^ 0x1u])) // avg.
                );
            }
            Array.Copy(encodeScoreSyncValues, 0,
                networkingManager.scoreSyncRows, i * 2,
                encodeScoreSyncValues.Length);
        }
    }

    #region Debugger
    const string LOG_LOW = "<color=\"#ADADAD\">";
    const string LOG_ERR = "<color=\"#B84139\">";
    const string LOG_WARN = "<color=\"#DEC521\">";
    const string LOG_YES = "<color=\"#69D128\">";
    const string LOG_END = "</color>";
#if HT8B_DEBUGGER
    public void _Log(string msg)
    {
        _log(LOG_WARN + msg + LOG_END);
    }
    public void _LogYes(string msg)
    {
        _log(LOG_YES + msg + LOG_END);
    }
    public void _LogWarn(string msg)
    {
        _log(LOG_WARN + msg + LOG_END);
    }
    public void _LogError(string msg)
    {
        _log(LOG_ERR + msg + LOG_END);
    }
    public void _LogInfo(string msg)
    {
        _log(LOG_LOW + msg + LOG_END);
    }
    public void _RedrawDebugger()
    {
        redrawDebugger();
    }
#else
public void _Log(string msg) { }
public void _LogYes(string msg) { }
public void _LogInfo(string msg) { }
public void _LogWarn(string msg) { }
public void _LogError(string msg) { }
public void _RedrawDebugger() { }
#endif

    public void _BeginPerf(int id)
    {
        perfStart[id] = Time.realtimeSinceStartup;
    }

    public void _EndPerf(int id)
    {
        perfTimings[id] += Time.realtimeSinceStartup - perfStart[id];
        perfCounters[id]++;
    }

    private void _log(string ln)
    {
        Debug.Log("[<color=\"#B5438F\">BilliardsModule</color>" + logLabel + "] " + ln);

        LOG_LINES[LOG_PTR++] = "[<color=\"#B5438F\">BilliardsModule" + logLabel + "</color>] " + ln + "\n";
        LOG_LEN++;

        if (LOG_PTR >= LOG_MAX)
        {
            LOG_PTR = 0;
        }

        if (LOG_LEN > LOG_MAX)
        {
            LOG_LEN = LOG_MAX;
        }

        redrawDebugger();
    }

    private void redrawDebugger()
    {
        string output = "BilliardsModule " + VERSION + " [" + logLabel + " ] ";

        // Add information about game state:
        output += Networking.IsOwner(Networking.LocalPlayer, networkingManager.gameObject) ?
           "<color=\"#95a2b8\">net(</color> <color=\"#4287F5\">OWNER</color> <color=\"#95a2b8\">)</color> " :
           "<color=\"#95a2b8\">net(</color> <color=\"#678AC2\">RECVR</color> <color=\"#95a2b8\">)</color> ";

        output += isLocalSimulationRunning ?
           "<color=\"#95a2b8\">sim(</color> <color=\"#4287F5\">ACTIVE</color> <color=\"#95a2b8\">)</color> " :
           "<color=\"#95a2b8\">sim(</color> <color=\"#678AC2\">PAUSED</color> <color=\"#95a2b8\">)</color> ";

        VRCPlayerApi currentOwner = Networking.GetOwner(networkingManager.gameObject);
        output += "<color=\"#95a2b8\">owner(</color> <color=\"#4287F5\">" + (currentOwner != null ? currentOwner.displayName + ":" + currentOwner.playerId : "[null]") + "/" + teamIdLocal + "</color> <color=\"#95a2b8\">)</color> ";

        output += physicsModeLocal == 0 ?
           "<color=\"#95a2b8\">phys(</color> <color=\"#4287F5\">LEGACY</color> <color=\"#95a2b8\">)</color> " :
           (physicsModeLocal == 1 ?
           "<color=\"#95a2b8\">phys(</color> <color=\"#678AC2\"> STND </color> <color=\"#95a2b8\">)</color> " :
           "<color=\"#95a2b8\">phys(</color> <color=\"#678AC2\"> BETA </color> <color=\"#95a2b8\">)</color> "
           );

        output += "\n---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------\n";

        for (int i = 0; i < PERF_MAX; i++)
        {
            output += "<color=\"#95a2b8\">" + perfNames[i] + "(</color> " + (perfCounters[i] > 0 ? perfTimings[i] * 1e6 / perfCounters[i] : 0).ToString("F2") + "µs <color=\"#95a2b8\">)</color> ";
        }

        output += "\n---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------\n";

        // Update display 
        for (int i = 0; i < LOG_LEN; i++)
        {
            output += LOG_LINES[(LOG_MAX + LOG_PTR - LOG_LEN + i) % LOG_MAX];
        }

        ltext.text = output;
    }
    #endregion
}
