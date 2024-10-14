#if UNITY_ANDROID
#define HT_QUEST
#endif

#if !HT_QUEST || true
#define HT8B_DEBUGGER
#endif

//#define TKCH_SCORE_SCREEN

//#define TKCH_DEBUG_IN_KITCHEN
//#define TKCH_DEBUG_UPON_FOOT
//#define TKCH_DEBUG_BREAKING_FOUL
//#define TKCH_DEBUG_CALLSHOT_BALL
//#define TKCH_DEBUG_CALLSHOT_DELAY
//#define TKCH_DEBUG_CALLSHOT_POCKETEDBALL
//#define TKCH_DEBUG_SCORE
//#define TKCH_DEBUG_SEMIAUTO_CALL
//#define TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC
//#define TKCH_DEBUG_9BALL_TABLECOLOR
//#define TKCH_DEBUG_PUSHOUT

#define TKCH_CALLSHOT_CALLEDPBALL_DELAY
#define TKCH_CALLSHOT_CALLEDPOCKET_DELAY

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using System;
using Metaphira.Modules.CameraOverride;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class BilliardsModule : UdonSharpBehaviour
{
    [NonSerialized] public readonly string[] DEPENDENCIES = new string[] { nameof(CameraOverrideModule) };
    [NonSerialized] public readonly string VERSION = "6.0.0 10Ball";

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
    private readonly int[] break_order_9ball = { 2, 8, 4, 5, 9, 6, 7, 1, 3 };
    private readonly int[] break_rows_9ball = { 0, 1, 2, 1, 0 };
    private readonly int[] break_order_10ball = { 2, 1, 9, 8, 10, 5, 3, 6, 7, 4 };
    private readonly uint pocket_mask_8ball = 0xFFFEu;
    private readonly uint pocket_mask_9ball = 0x03FEu;
    private readonly uint pocket_mask_10ball = 0x07FEu;
    private uint pocketMask = 0x0;
    [NonSerialized] public int ballsLengthByPocketGame = 15;
    [NonSerialized] public readonly uint BREAK_TERMS_UNCONDITIONAL = 0;
    [NonSerialized] public readonly uint BREAK_TERMS_4_CUSHION = 1;
    [NonSerialized] public readonly uint BREAK_TERMS_3_POINT = 2;
#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC || TKCH_DEBUG_SEMIAUTO_CALL_SIDE ||TKCH_DEBUG_NEXT_BREAK
    private bool debugLogFlg = false;
#endif
#if TKCH_DEBUG_SEMIAUTO_CALL
    private bool debugFlg = false;
#endif

    #region InspectorValues
    [Header("Debug")]
    [SerializeField] public string logLabel;

    [Header("Pocket Billiard Additional")]
    [SerializeField]
    public GameObject markerCalledBall;

    [SerializeField] Material calledBallMarkerBlue;
    [SerializeField] Material calledBallMarkerOrange;
    [SerializeField] Material calledBallMarkerWhite;
    [SerializeField] Material calledBallMarkerYellow;
    [SerializeField] public GameObject requestBreakOrange;
    [SerializeField] public GameObject requestBreakBlue;

#if TKCH_SCORE_SCREEN
    [Header("Score Screen")]
    [SerializeField] public BilliardsScoreScreen scoreScreen;
#endif

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
    [NonSerialized] public uint rackConditionLocal = 1;
    [NonSerialized] public bool semiAutoCallBallLocal;
    [NonSerialized] public bool semiAutoCallPocketLocal;
    [NonSerialized] public uint timerLocal;
    [NonSerialized] public bool teamsLocal;
    [NonSerialized] public bool noGuidelineLocal;
    [NonSerialized] public bool noLockingLocal;
    [NonSerialized] public bool noCushionFoulEnableLocal;
    [NonSerialized] public bool enablePushOutLocal;
    [NonSerialized] public uint breakTermsLocal;
    [NonSerialized] public bool enableRequestBreakLocal = false;
    [NonSerialized] public bool requireCallShotLocal;
    [NonSerialized] public bool callShotOprationOverwriteModeLocal;
    [NonSerialized] public uint ballsPocketedLocal;
    [NonSerialized] public uint targetPocketedLocal;
    [NonSerialized] public uint otherPocketedLocal;
    [NonSerialized] public uint pointPocketsLocal;
    [NonSerialized] public uint calledBallsLocal;
    [NonSerialized] public uint teamIdLocal;
    [NonSerialized] public uint fourBallCueBallLocal;
    [NonSerialized] public bool isTableOpenLocal;
    [NonSerialized] public uint teamColorLocal;
    [NonSerialized] public string[] playerNamesLocal = new string[4];
    [NonSerialized] public string tournamentRefereeLocal;
    [NonSerialized] public int[] fbScoresLocal = new int[2];
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
    private bool requestBreakStateLocal; // uint nextBallRepositionStateLocal;
    private int tableModelLocal;
    private bool callShotLockLocal;
    private bool calledBallOff = false;
    private bool calledPocketOff = false;
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
    private bool semiAutoCalledBall;
    private bool semiAutoCalledPocket;
    private float semiAutoCalledTimeBall;
    private bool semiAutoCallTick;
    [NonSerialized] public byte pushOutStateLocal;
    [NonSerialized] public readonly byte PUSHOUT_BEFORE_BREAK = 0;
    [NonSerialized] public readonly byte PUSHOUT_ILLEGAL_REACTIONING = 1;
    [NonSerialized] public readonly byte PUSHOUT_DONT = 2;
    [NonSerialized] public readonly byte PUSHOUT_DOING = 3;
    [NonSerialized] public readonly byte PUSHOUT_REACTIONING = 4;
    [NonSerialized] public readonly byte PUSHOUT_ENDED = 5;
#if TKCH_DEBUG_PUSHOUT
    private string[] PushOutState = new string[] {
        "BEFORE_BREAK",
        "ILLEGAL_REACTIONING",
        "DONT",
        "DOING",
        "REACTIONING",
        "ENDED"
    }; 
#endif

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
    private uint kichenLineOverBalls3PointBreak = 0x0u;
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
    [NonSerialized] public bool is10Ball = false;
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

        gameModeLocal = 0; is8Ball = true;
        // gameModeLocal = 4; is10Ball = true;
        pocketMask = is10Ball ? pocket_mask_10ball : (is9Ball ? pocket_mask_9ball : pocket_mask_8ball);
        ballsLengthByPocketGame = (is9Ball
            ? break_order_9ball.Length
            : (is10Ball ? break_order_10ball.Length : break_order_8ball.Length));
        noCushionFoulEnableLocal = true;
        enablePushOutLocal = true;
        breakTermsLocal = BREAK_TERMS_4_CUSHION;
        rackConditionLocal = 1;
        requireCallShotLocal = true;
        semiAutoCallBallLocal = true;
        semiAutoCallPocketLocal = true;
        callShotOprationOverwriteModeLocal = true;
        pointPocketsLocal = 0;
        calledBallsLocal = 0;
        
        networkingManager._Init(this);
        networkingManager.gameModeSynced = (byte)gameModeLocal;
        networkingManager.noCushionFoulEnableSynced = noCushionFoulEnableLocal;
        networkingManager.enablePushOutSynced = enablePushOutLocal;
        networkingManager.breakTermsSynced = (byte)breakTermsLocal;
        networkingManager.rackConditionSynced = (byte)rackConditionLocal;
        networkingManager.requireCallShotSynced = requireCallShotLocal;
        networkingManager.semiAutoCallBallSynced = semiAutoCallBallLocal;
        networkingManager.semiAutoCallPocketSynced = semiAutoCallPocketLocal;
        networkingManager.callShotOperationOverwriteModeSynced = callShotOprationOverwriteModeLocal;
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
        pcketLocations[4] = k_vF;
        pcketLocations[5] = new Vector3(k_vF.x, k_vF.y, -k_vF.z);
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
        if (semiAutoCallTick) tickSemiAutoCall();

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

    public void _TriggerNoCushionFoulChanged(bool noCushionFoulEnabled)
    {
        networkingManager._OnNoCushionFoulChanged(noCushionFoulEnabled);
    }

    public void _TriggerEnablePushOutChanged(bool pushOutEnabled)
    {
        networkingManager._OnEnablePushOutChanged(pushOutEnabled);
    }

    public void _TriggerBreakTermsChanged(uint breakTerms)
    {
        networkingManager._OnBreakTermsChanged(breakTerms);
    }
    
    public void _TriggerRackConditionChanged(uint rackCondition)
    {
        networkingManager._OnRackConditionChanged(rackCondition);
    }

    public void _TriggerRequireCallShotChanged(bool callShotEnabled)
    {
        networkingManager._OnRequireCallShotChanged(callShotEnabled);
    }

    public void _TriggerSemiAutoCallChanged(bool semiAutoCallEnabled)
    {
        networkingManager._OnSemiAutoCallChanged(semiAutoCallEnabled);
    }

    public void _TriggerSemiAutoCallBallChanged(bool semiAutoCallBallEnabled)
    {
        networkingManager._OnSemiAutoCallBallChanged(semiAutoCallBallEnabled);
    }

    public void _TriggerSemiAutoCallPocketChanged(bool semiAutoCallPocketEnabled)
    {
        networkingManager._OnSemiAutoCallPocketChanged(semiAutoCallPocketEnabled);
    }

    public void _TriggerCallOperationOverwriteModeChanged(bool callShotOperationOverwriteModeEnabled)
    {
        networkingManager._OnCallOperationOverwriteModeChanged(callShotOperationOverwriteModeEnabled);
    }

    public void _TriggerTimerChanged(uint timerSelected)
    {
        networkingManager._OnTimerChanged(timerSelected);
    }

    public void _TriggerGameModeChanged(uint newGameMode)
    {
        networkingManager._OnGameModeChanged(newGameMode);
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

        networkingManager._OnHitBall(ballsV[0], ballsW[0]);
    }

    public void _TriggerOtherBallHit(int ballId, bool desktop)
    {
#if TKCH_DEBUG_SEMIAUTO_CALL
        // _LogInfo($"TKCH BilliardsModule::_TriggerOtherBallHit(ballId = {ballId}, desktop = {desktop})");
#endif
        if (localTeamId != teamIdLocal && !isPracticeMode) return; // is there a better way to do this?
         
        if (callShotLockLocal)
        {
#if TKCH_DEBUG_SEMIAUTO_CALL
            _LogInfo("TKCH   callshot locked (return)");
#endif
            return;
        }
        
#if TKCH_CALLSHOT_CALLEDPBALL_DELAY
        if (!desktop && calledBallId == ballId)
        {
#if TKCH_DEBUG_SEMIAUTO_CALL
            // _LogInfo("TKCH   ignore nochange (return)");
#endif
            return;
        }

        if (!desktop && Time.time < calledBallIdDelayTimestamp + callShotDelay)
        {
#if TKCH_DEBUG_SEMIAUTO_CALL
            // _LogInfo("TKCH   delaying (return)");
#endif
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
#if TKCH_DEBUG_SEMIAUTO_CALL
        _LogInfo($"TKCH   proccess ball id = {id}");
#endif

#if TKCH_DEBUG_CALLSHOT_POCKETEDBALL
        _LogInfo($"  id = {id}, ballsPocketedLocal = {ballsPocketedLocal:X4}, (0x1 << id) = {(0x1 << id):X4}");
#endif
        if (0 < id && 0 != (ballsPocketedLocal & (0x1 << id)))
        {
#if TKCH_DEBUG_CALLSHOT_POCKETEDBALL
            _LogInfo("  return");
#endif
#if TKCH_DEBUG_SEMIAUTO_CALL
            _LogInfo("TKCH   pocketed (return)");
#endif
            return;
        }

        if (id < 0)
        {
#if TKCH_DEBUG_SEMIAUTO_CALL
            _LogInfo("TKCH   initial or incalid value (called off)(return)");
#endif
            calledBallOff = true;
            return;
        }

        if (!calledBallOff && !desktop)
        {
#if TKCH_DEBUG_SEMIAUTO_CALL
            _LogInfo("TKCH   NOT called off (return)");
#endif
            return;
        }

        uint calledBalls = calledBallsLocal;
        calledBalls |= 0x1u << id;
        if (calledBalls == calledBallsLocal)
        {
            calledBalls ^= 0x1u << id;
        }

        if (!callShotOprationOverwriteModeLocal){
            if (calledBallsLocal != 0 && calledBalls != 0 && !desktop)
            {
#if TKCH_DEBUG_SEMIAUTO_CALL
                // _LogInfo("TKCH   other chiced exists (return)");
#endif
                return;
            }
        }

        if (Networking.LocalPlayer == null || Networking.GetOwner(activeCue.gameObject) != Networking.LocalPlayer)
        {
#if TKCH_DEBUG_SEMIAUTO_CALL
            _LogInfo($"TKCH   not cue owner {Networking.GetOwner(activeCue.gameObject)}");
#endif
            if (localPlayerId != teamIdLocal)
            {
#if TKCH_DEBUG_SEMIAUTO_CALL
                _LogInfo($"TKCH   not team leader localPlayerId = {localPlayerId}, teamIdLocal = {teamIdLocal} (return)");
#endif
                return;
            }
        }

        bool enable = (calledBallsLocal < calledBalls);
        if (enable) semiAutoCalledTimeBall = (Networking.GetServerTimeInMilliseconds() - timerStartLocal) / 1000.0f;
        
#if TKCH_DEBUG_SEMIAUTO_CALL
        _LogInfo($"TKCH   call _OnCalledBallChanged(enable = {enable}, id = {id})");
#endif
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

        if (!callShotOprationOverwriteModeLocal){
            if (pointPocketsLocal != 0 && pointPockets != 0 && !desktop)
            {
                return;
            }
        }

        if (Networking.LocalPlayer == null || Networking.GetOwner(activeCue.gameObject) != Networking.LocalPlayer)
        {
            if (localPlayerId != teamIdLocal)
            {
                return;
            }
        }

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

        if (is8Ball || is9Ball || is10Ball)
        {
            initializeRack();
        }
        
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
#if TKCH_DEBUG_SEMIAUTO_CALL
        debugFlg = false;
#endif
        _LogInfo("processing latest remote state (packet=" + networkingManager.packetIdSynced + ", state=" + networkingManager.stateIdSynced + ")");
        Debug.Log("[BilliardsModule" + logLabel + "] latest game state is " + networkingManager._EncodeGameState());
#if TKCH_DEBUG_9BALL_TABLECOLOR
        _LogInfo($" isTableOpen={isTableOpenLocal} isTableOpenSynced={networkingManager.isTableOpenSynced}");
#endif
#if TKCH_DEBUG_PUSHOUT
        _LogInfo($"  pushOutState local = {PushOutState[pushOutStateLocal]}({pushOutStateLocal}), synced = {PushOutState[networkingManager.pushOutStateSynced]}({networkingManager.pushOutStateSynced})");
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
            networkingManager.timerSynced,
            networkingManager.teamsSynced,
            networkingManager.noGuidelineSynced,
            networkingManager.noLockingSynced,
            networkingManager.noCushionFoulEnableSynced,
            networkingManager.enablePushOutSynced,
            networkingManager.breakTermsSynced,
            networkingManager.rackConditionSynced,
            networkingManager.requireCallShotSynced,
            networkingManager.semiAutoCallBallSynced,
            networkingManager.semiAutoCallPocketSynced,
            networkingManager.callShotOperationOverwriteModeSynced
        );

        if (gameStateLocal != networkingManager.gameStateSynced && networkingManager.gameStateSynced == 1)
        {
            Array.Clear(playerNamesCached, 0, playerNamesCached.Length);
        }

        // propagate valid players second
        onRemotePlayersChanged(networkingManager.playerNamesSynced);

        bool stateIdChanged = (networkingManager.stateIdSynced != stateIdLocal);
        onRemotePushOutStateChanged(networkingManager.pushOutStateSynced, stateIdChanged);
        
        // apply state transitions if needed
        onRemoteGameStateChanged(networkingManager.gameStateSynced);

        // now update game state
        onRemoteBallPositionsChanged(networkingManager.ballsPSynced);
        onRemoteTeamIdChanged(networkingManager.teamIdSynced);
        onRemoteFourBallCueBallChanged(networkingManager.fourBallCueBallSynced);
        onRemoteBallsPocketedChanged(networkingManager.ballsPocketedSynced, networkingManager.targetPocketedSynced, networkingManager.otherPocketedSynced);
        onRemoteFourBallScoresUpdated(networkingManager.fourBallScoresSynced);
        onRemoteRepositionStateChanged(networkingManager.repositionStateSynced);
        onRemoteIsTableOpenChanged(networkingManager.isTableOpenSynced, networkingManager.teamColorSynced);
        onRemoteTurnStateChanged(networkingManager.turnStateSynced);
        onRemotePreviewWinningTeamChanged(networkingManager.previewWinningTeamSynced);

        // bool stateIdChanged = (networkingManager.stateIdSynced != stateIdLocal);
        onRemotePointPocketsChanged(networkingManager.pointPocketsSynced, networkingManager.callShotLockSynced, 
            stateIdChanged);
        onRemoteCalledBallsChanged(networkingManager.calledBallsSynced, stateIdChanged);
        // onRemotePushOutStateChanged(networkingManager.pushOutStateSynced, stateIdChanged);
        onRemoteRequestBreakStateChanged(networkingManager.requestBreakStateSynced); // onRemoteNextBallRepositionStateChanged(networkingManager.nextBallRepositionStateSynced);
        
        // finally, take a snapshot
        practiceManager._Record();

        stateIdLocal = networkingManager.stateIdSynced;

        redrawDebugger();
        semiAutoCallTick = (requireCallShotLocal && (semiAutoCallBallLocal || semiAutoCallPocketLocal));
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

    private void onRemoteGameSettingsUpdated(uint gameModeSynced, uint timerSynced, bool teamsSynced, 
        bool noGuidelineSynced, bool noLockingSynced, bool noCushionFoulEnableSynced, 
        bool enablePushOutSynced, uint breakTermsSynced, uint rackConditionSynced, 
        bool requireCallShotSynced, bool semiAutoCallBallSynced, bool semiAutoCallPocketSynced, 
        bool callShotOperationOverwriteModeSynced)
    {
        if (
            gameModeLocal == gameModeSynced &&
            timerLocal == timerSynced &&
            teamsLocal == teamsSynced &&
            noGuidelineLocal == noGuidelineSynced &&
            noLockingLocal == noLockingSynced &&
            noCushionFoulEnableLocal == noCushionFoulEnableSynced &&
            enablePushOutLocal == enablePushOutSynced &&
            breakTermsLocal == breakTermsSynced &&
            rackConditionLocal == rackConditionSynced &&
            requireCallShotLocal == requireCallShotSynced &&
            semiAutoCallBallLocal == semiAutoCallBallSynced &&
            semiAutoCallPocketLocal == semiAutoCallPocketSynced &&
            callShotOprationOverwriteModeLocal == callShotOperationOverwriteModeSynced
        )
        {
            return;
        }

        _LogInfo($"onRemoteGameSettingsUpdated gameMode={gameModeSynced} timer={timerSynced} teams={teamsSynced} guideline={!noGuidelineSynced} locking={!noLockingSynced}");
        _LogInfo($"                            noCushionFoul={noCushionFoulEnableSynced} breakTerms={breakTermsSynced} rackCondition={rackConditionSynced}");
        _LogInfo($"                            callShot={requireCallShotSynced} semiAutoCallBall={semiAutoCallBallSynced} semiAutoCallPocket={semiAutoCallPocketSynced} callOverwrite={callShotOperationOverwriteModeSynced}");

        if (gameModeLocal != gameModeSynced)
        {
            gameModeLocal = gameModeSynced;

            is8Ball = gameModeLocal == 0u;
            is9Ball = gameModeLocal == 1u;
            isJp4Ball = gameModeLocal == 2u;
            isKr4Ball = gameModeLocal == 3u;
            is4Ball = isJp4Ball || isKr4Ball;
            is10Ball = gameModeLocal == 4u;
            pocketMask = is10Ball ? pocket_mask_10ball : (is9Ball ? pocket_mask_9ball : pocket_mask_8ball);
            ballsLengthByPocketGame = (is9Ball
                ? break_order_9ball.Length
                : (is10Ball ? break_order_10ball.Length : break_order_8ball.Length));

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

        if (noCushionFoulEnableLocal != noCushionFoulEnableSynced)
        {
            noCushionFoulEnableLocal = noCushionFoulEnableSynced;
            refreshToggles = true;
        }

        if (enablePushOutLocal != enablePushOutSynced)
        {
            enablePushOutLocal = enablePushOutSynced;
            refreshToggles = true;
        }

        if (breakTermsLocal != breakTermsSynced)
        {
            breakTermsLocal = breakTermsSynced;
            refreshToggles = true;
        }

        if (rackConditionLocal != rackConditionSynced)
        {
            rackConditionLocal = rackConditionSynced;
            refreshToggles = true;
        }

        if (requireCallShotLocal != requireCallShotSynced)
        {
            requireCallShotLocal = requireCallShotSynced;
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

        if (callShotOprationOverwriteModeLocal != callShotOperationOverwriteModeSynced)
        {
            callShotOprationOverwriteModeLocal = callShotOperationOverwriteModeSynced;
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
            onRemoteGameStarted();
        }
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
        _LogInfo($"onRemoteGameStarted");

        lobbyOpen = false;
        gameLive = true;

        Array.Clear(perfCounters, 0, PERF_MAX);
        Array.Clear(perfStart, 0, PERF_MAX);
        Array.Clear(perfTimings, 0, PERF_MAX);

        isPracticeMode = playerNamesLocal[1] == "" && playerNamesLocal[3] == "";

        menuManager._DisableMenu();

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
        marker9ball.SetActive(is9Ball | is10Ball);

        afterBreak = false;
        pushOutStateLocal = PUSHOUT_BEFORE_BREAK;
        targetPocketedLocal = 0x0u;
        otherPocketedLocal = 0x0u;
        calledBallsLocal = 0;
        pointPocketsLocal = 0;
        calledBallId = -2;
        calledPocketId = -2;
        semiAutoCalledPocket = false;
        semiAutoCalledTimeBall = 0;
        kichenLineOverBalls3PointBreak = 0;

        graphicsManager._ShowBalls();

        // Reflect game state
        graphicsManager._UpdateScorecard();
        isReposition = false;
        markerObj.SetActive(false);
        graphicsManager._UpdatePushOut(pushOutStateLocal);
        requestBreakOrange.SetActive(false);
        requestBreakBlue.SetActive(false);

        // Effects
        graphicsManager._PlayIntroAnimation();
        aud_main.PlayOneShot(snd_Intro, 1.0f);

        graphicsManager._SetScorecardPlayers(playerNamesLocal);

        timerRunning = false;

        reflection_main.RenderProbe();

        activeCue = cueControllers[isPracticeMode ? 0 : (int)networkingManager.teamIdSynced];
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
        this.transform.Find("intl.controls/pushOut").gameObject.SetActive(false);

        markerCalledBall.SetActive(false);
        graphicsManager._UpdatePointPocketMarker(0, callShotLockLocal);

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
        repositionStateLocal = repositionStateSynced == 4 ? 2 : repositionStateSynced;

        if (repositionStateLocal == 1)
        {
            afterBreak = false;
        }

        if (!isOurTurn() || repositionStateLocal == 0)
        {
            isReposition = false;
            setFoulPickupEnabled(false);
            return;
        }

        if (repositionStateLocal == 1 || repositionStateLocal == 2)
        {
            isReposition = true;
            if (repositionStateLocal == 1)
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

    private void onRemoteRequestBreakStateChanged(bool requestBreakStateSynced)
    {
        if (!gameLive) return;

        if (requestBreakStateLocal == requestBreakStateSynced) return;

        _LogInfo($"onRemoteRequestBreakStateChanged requestBreakState={requestBreakStateSynced}");
        requestBreakStateLocal = requestBreakStateSynced;

        if (enableRequestBreakLocal)
        {
            _UpdateRequestBreakMarker();
        }
        else
        {
            bool isOurTurnVar = isOurTurn();
            bool practiceEnable = (isOurTurnVar && isPracticeMode) ||
                                  (!string.IsNullOrEmpty(tournamentRefereeLocal) && _IsLocalPlayerReferee());
            if ((is8Ball || is9Ball || is10Ball) && isOurTurnVar)
            {
                this.transform.Find("intl.controls/skipturn").gameObject.SetActive(practiceEnable || (enablePushOutLocal && (pushOutStateLocal == PUSHOUT_REACTIONING)) || (pushOutStateLocal == PUSHOUT_ILLEGAL_REACTIONING));
            }
        }
    }

    /*
    private void onRemoteNextBallRepositionStateChanged(uint nextBallRepositionStateSynced)
    {
        if (!gameLive) return;

        if (nextBallRepositionStateLocal == nextBallRepositionStateSynced) return;

        _LogInfo($"onRemoteNextBallRepositionStateChanged nextBallRepositionState={nextBallRepositionStateSynced}");
        nextBallRepositionStateLocal = nextBallRepositionStateSynced;

        if (enableRequestBreakLocal)
        {
            _UpdateNextBallRepositionSpotMarker();
        }
        else
        {
            bool isOurTurnVar = isOurTurn();
            bool practiceEnable = (isOurTurnVar && isPracticeMode) ||
                                  (!string.IsNullOrEmpty(tournamentRefereeLocal) && _IsLocalPlayerReferee());
            if ((is8Ball || is9Ball || is10Ball) && isOurTurnVar)
            {
                this.transform.Find("intl.controls/skipturn").gameObject.SetActive(practiceEnable || (pushOutStateLocal == PUSHOUT_REACTIONING || pushOutStateLocal == PUSHOUT_ILLEGAL_REACTIONING));
            }
        }
    }
    */

    private void onRemoteTurnBegin(int timerStartSynced)
    {
        _LogInfo("onRemoteTurnBegin");
        canPlayLocal = true;
        timerStartLocal = timerStartSynced;
#if TKCH_DEBUG_SEMIAUTO_CALL
        _LogInfo($"TKCH   timerStart = {timerStartLocal}, elapsedSeconds = {(Networking.GetServerTimeInMilliseconds() - timerStartLocal) / 1000.0f}");
        _LogInfo($"TKCH   semiAutoCalledTimeBall = {semiAutoCalledTimeBall}");
        _LogInfo($"TKCH   calledBallId = {calledBallId}, calledPocketId = {calledPocketId}");
        debugFlg = true;
#endif

        enablePlayComponents();
        Array.Clear(ballsV, 0, ballsV.Length);
        Array.Clear(ballsW, 0, ballsW.Length);

        graphicsManager._UpdatePointPocketMarker(pointPocketsLocal, callShotLockLocal);
#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC || TKCH_DEBUG_SEMIAUTO_CALL_SIDE || TKCH_DEBUG_NEXT_BREAK
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

        if (breakTermsLocal == BREAK_TERMS_3_POINT && !afterBreak)
        {
            currentPhysicsManager.SetProgramVariable("ballKichenLineOverCheck", true);
        }

        isLocalSimulationRunning = true;
        firstHit = 0;
        secondHit = 0;
        thirdHit = 0;
        cushionAfterFirstHit = 0;
        cushionObjectiveBallsOnBreak = 0x0u;
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
    
    private void onRemotePointPocketsChanged(uint pointPocketsSynced, bool callShotLockSynced, bool stateIdChanged)
    {
        if (!gameLive) return;

        if (pointPocketsLocal == pointPocketsSynced && callShotLockLocal == callShotLockSynced && 0 < stateIdLocal ) return;

        _LogInfo($"onRemotePointPocketsChanged pointPockets={pointPocketsSynced:X2}, callShotLock={callShotLockSynced}");
        pointPocketsLocal = pointPocketsSynced;
        callShotLockLocal = callShotLockSynced;
        graphicsManager._UpdatePointPocketMarker(pointPocketsLocal, callShotLockLocal);
        if (!stateIdChanged)
        {
            aud_main.PlayOneShot(snd_btn);
        }
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
    }
    
    private void onRemotePushOutStateChanged(byte pushOutStateSynced, bool stateIdChanged)
    {
        if (!gameLive) return;

        if (pushOutStateLocal == pushOutStateSynced /* && 0 < stateIdLocal */) return;

        _LogInfo($"onRemotePushOutStateChanged pushOutState={pushOutStateSynced}");
        pushOutStateLocal = pushOutStateSynced;
        graphicsManager._UpdatePushOut(pushOutStateLocal);
        if (!stateIdChanged)
        {
            aud_main.PlayOneShot(snd_btn);
        }
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
                }
                break;
        }
    }

    public void _TriggerCushion(int id, Vector3 pos)
    {
        if (ballsPocketedLocal == ballsPocketedOrig)
        {
            if (breakTermsLocal == BREAK_TERMS_4_CUSHION && !afterBreak)
            {
                if (0 < id)
                {
                    uint ball_bit = 0x1u << id;
                    if (0 == (cushionObjectiveBallsOnBreak & ball_bit))
                    {
                        int cushionBallCount = (int)SoftwareFallback(cushionObjectiveBallsOnBreak);
                        if (cushionBallCount < 4)
                        {
                            graphicsManager._SpawnCushionTouch(pos, cushionBallCount);
                        }
                        cushionObjectiveBallsOnBreak |= ball_bit;
                    }
                }
            }
            
            if (0 != firstHit)
            {
                if (noCushionFoulEnableLocal && 0 == cushionAfterFirstHit && (afterBreak || breakTermsLocal == BREAK_TERMS_UNCONDITIONAL))
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
        int count_extent = is9Ball ? 10 : (is10Ball ? 11 : 16);
        for (int i = 1; i < count_extent; i++)
        {
            total += (ballsPocketedLocal >> i) & 0x1U;
        }

        // place ball on the rack
        ballsP[id] = k_rack_position + (float)total * k_BALL_DIAMETRE * k_rack_direction;

        ballsPocketedLocal ^= 1U << id;

        uint bmask = 0x1FCU << ((int)(teamIdLocal ^ teamColorLocal) * 7);

        bool callSuccess = ((calledBallsLocal & (0x1u << id)) != 0) && ((pointPocketsLocal & (0x1u << pocketId)) != 0);
        
        // Good pocket
        if (is9Ball || is10Ball)
        {
            if (!requireCallShotLocal || callSuccess)
            {
                targetPocketedLocal |= 1U << id;
                graphicsManager._FlashTableLight();

                // if (0 == (ballsPocketedLocal & 0x1u))
                // {
                //     aud_main.PlayOneShot(snd_PointMade, 1.0f);
                //     graphicsManager._SpawnOnePocketPoint(pcketLocations[pocketId], true, (int)(teamIdLocal ^ teamColorLocal ^ 0x1U));
                // }

                calledBallsLocal = 0;
            }
            else
            {
                if (0 == id)
                {
                    // aud_main.PlayOneShot(snd_PointMade, 1.0f);
                    // graphicsManager._SpawnOnePocketPoint((pocketId < 0 ? balls[id].transform.localPosition : pcketLocations[pocketId]), false, -1);
                }
                else
                {
                    otherPocketedLocal |= 1U << id;
                }
                graphicsManager._FlashTableError();
            }
        }
        else
        {
            if ((!requireCallShotLocal || callSuccess) &&
                (((0x1U << id) & ((bmask) | (isTableOpenLocal ? 0xFFFCU : 0x0000U) | ((bmask & ballsPocketedLocal) == bmask ? 0x2U : 0x0U))) > 0))
            {
                targetPocketedLocal |= 1U << id;
                graphicsManager._FlashTableLight();
            }
            else
            {
                if (0 != id)
                {
                    otherPocketedLocal |= 1U << id;
                }
                graphicsManager._FlashTableError();
            }
        }
        if (breakTermsLocal == BREAK_TERMS_3_POINT && !afterBreak)
        {
            int point = (int)SoftwareFallback((kichenLineOverBalls3PointBreak | ballsPocketedLocal) & pocketMask);
            if (point < 3)
            {
                graphicsManager._SpawnCushionTouch(pcketLocations[pocketId], point);
            }
            else
            {
                currentPhysicsManager.SetProgramVariable("ballKichenLineOverCheck", false);
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

    public void _TriggerBallKichenLineOver(int id, Vector3 pos)
    {
#if TKCH_DEBUG_BALL_OVER_KICHENLINE
        _LogInfo("TKCH BilliardsModule::_TriggerBallKichenLineOver()");
#endif

        if (breakTermsLocal == BREAK_TERMS_3_POINT && !afterBreak)
        {
            if (0 < id)
            {
                uint ball_bit = 0x1u << id;
                if (0 == (kichenLineOverBalls3PointBreak & ball_bit))
                {
                    int point = (int)SoftwareFallback((kichenLineOverBalls3PointBreak | ballsPocketedLocal) & pocketMask);
                    if (point < 3)
                    {
                        graphicsManager._SpawnCushionTouch(pos, point);
                        kichenLineOverBalls3PointBreak |= ball_bit;
                    }
                    else
                    {
                        currentPhysicsManager.SetProgramVariable("ballKichenLineOverCheck", false);
                    }
                }
            }
        }
    }

    public void _TriggerSimulationEnded(bool forceScratch)
    {
        if (!isLocalSimulationRunning) return;
        isLocalSimulationRunning = false;

        _LogInfo("local simulation completed");
        cameraManager._OnLocalSimEnd();

        auto_colliderBaseVFX.SetActive(false);

        bool pushOut = false;
        if (!afterBreak)
        {
            if (breakTermsLocal == BREAK_TERMS_3_POINT)
            {
                currentPhysicsManager.SetProgramVariable("ballKichenLineOverCheck", false);
            }
            
            if (pushOutStateLocal == PUSHOUT_BEFORE_BREAK)
            {
                pushOutStateLocal = PUSHOUT_DONT;
            }
        }
        else
        {
            if (pushOutStateLocal == PUSHOUT_DOING)
            {
                pushOut = true;
                pushOutStateLocal = PUSHOUT_REACTIONING;
            }
            else if (pushOutStateLocal == PUSHOUT_DONT || pushOutStateLocal == PUSHOUT_REACTIONING|| pushOutStateLocal == PUSHOUT_ILLEGAL_REACTIONING)
            {
#if TKCH_DEBUG_PUSHOUT
                _LogInfo($"  set ENDED pushOutState {PushOutState[pushOutStateLocal]}({pushOutStateLocal})");
#endif
                pushOutStateLocal = PUSHOUT_ENDED;
            }
        }

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

            bool isAnyPocketSink = (ballsPocketedLocal & pocketMask) > (ballsPocketedOrig & pocketMask);
            bool reBreakAllowed = !afterBreak && (
                (breakTermsLocal == BREAK_TERMS_4_CUSHION && !isAnyPocketSink && (SoftwareFallback(cushionObjectiveBallsOnBreak) < 4)) ||
                (breakTermsLocal == BREAK_TERMS_3_POINT && ((int)SoftwareFallback((kichenLineOverBalls3PointBreak | ballsPocketedLocal) & pocketMask) < 3))
            );
            bool isNoCushon = noCushionFoulEnableLocal && !isAnyPocketSink && afterBreak && cushionAfterFirstHit == 0;
            bool isNoTouch = firstHit <= 0;
            uint ballsPocketedCurrent = ballsPocketedLocal & ~ballsPocketedOrig;
            uint gameBallMask = is8Ball ? 0x2u : (is9Ball ? 0x200u : (is10Ball ? 0x400u : 0)); 
            
            if (reBreakAllowed)
            {
                if (!enableRequestBreakLocal)
                {
                    if (breakTermsLocal == BREAK_TERMS_4_CUSHION)
                    {
                        reBreakAllowed = false;
                        isNoTouch = true;
                    }
                }
                
                if (breakTermsLocal == BREAK_TERMS_3_POINT)
                {
                    pushOutStateLocal = PUSHOUT_ILLEGAL_REACTIONING;
                }
            }

            if (pushOut && isScratch)
            {
                pushOutStateLocal = PUSHOUT_ENDED;
            }
            
#if TKCH_DEBUG_BREAKING_FOUL
            if (!afterBreak)
            {
                if (breakTermsLocal == BREAK_TERMS_4_CUSHION)
                {
                    _LogInfo($"  BREAK_TERMS_4_CUSHION isAnyPocketSink = {isAnyPocketSink}, cushionObjectiveBallsOnBreak = {cushionObjectiveBallsOnBreak:x4} (count {SoftwareFallback(cushionObjectiveBallsOnBreak)})");
                }
                else if (breakTermsLocal == BREAK_TERMS_3_POINT)
                {
                    _LogInfo($"  BREAK_TERMS_3_POINT kichenLineOverBalls3PointBreak = {kichenLineOverBalls3PointBreak:X4}, ballsPocketedLocal = {ballsPocketedLocal:x4}");
                    _LogInfo($"               masked kichenLineOverBalls3PointBreak = {(kichenLineOverBalls3PointBreak & pocketMask):X4}, ballsPocketedLocal = {(ballsPocketedLocal & pocketMask):x4}");
                    _LogInfo($"               calced kichenLineOverBalls3PointBreak = {SoftwareFallback(kichenLineOverBalls3PointBreak & pocketMask)}, ballsPocketedLocal = {SoftwareFallback(ballsPocketedLocal & pocketMask)}");
                }
                _LogInfo($"  breakTermsLocal = {breakTermsLocal}, reBreakAllowed = {reBreakAllowed}");
            }
#endif
#if TKCH_DEBUG_PUSHOUT
            _LogInfo($"  pushOutState {PushOutState[pushOutStateLocal]}({pushOutStateLocal})");
#endif

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

                // additional rules
                {
                    foulCondition = foulCondition || isNoCushon || isNoTouch;

#if TKCH_DEBUG_CALLSHOT_BALL
                    _LogInfo($"  isObjectiveSink = {isObjectiveSink}, isOpponentSink = {isOpponentSink}");
#endif
                    if (requireCallShotLocal)
                    {
#if TKCH_DEBUG_CALLSHOT_BALL
                        _LogInfo($"  requireCallShot targetPocketedLocal = {targetPocketedLocal:X4}, targetPocketedOrig = {targetPocketedOrig:x4}");
                        _LogInfo($"           masked targetPocketedLocal = {(targetPocketedLocal & bmask):X4}, targetPocketedOrig = {(targetPocketedOrig & bmask):x4}");
                        _LogInfo($"                  otherPocketedLocal = {otherPocketedLocal:X4}, otherPocketedLocal = {otherPocketedLocal:x4}");
                        _LogInfo($"           masked otherPocketedLocal = {(otherPocketedLocal & pocketMask):X4}, otherPocketedLocal = {(otherPocketedOrig & pocketMask):x4}");
#endif
                        isObjectiveSink = (targetPocketedLocal & bmask) > (targetPocketedOrig & bmask);
                        isOpponentSink = isOpponentSink | (otherPocketedLocal & pocketMask) > (otherPocketedOrig & pocketMask);
#if TKCH_DEBUG_CALLSHOT_BALL
                        _LogInfo($"  isObjectiveSink = {isObjectiveSink}, isOpponentSink = {isOpponentSink}");
#endif
                    }
                    
                    // if (!isObjectiveSink && isOpponentSink)
                    // {
                    //     int uponBallsCount = pocketedballUponPool(ballsPocketedCurrent, k_SPOT_POSITION_X, ballsLengthByPocketGame);
                    //     if (uponBallsCount <= 0)
                    //     {
                    //         pocketedballUponPool(ballsPocketedCurrent, 0, ballsLengthByPocketGame);
                    //     }
                    // }

                    if (isObjectiveSink && isOpponentSink)
                    {
                        isOpponentSink = false;
                    }

                    if (is8Sink && (pushOut || (!afterBreak && !winCondition && deferLossCondition) || (reBreakAllowed && (breakTermsLocal == BREAK_TERMS_3_POINT))))
                    {
                        int uponBallsCount = pocketedballUponPool(gameBallMask, k_SPOT_POSITION_X, ballsLengthByPocketGame);
                        if (uponBallsCount <= 0)
                        {
                            pocketedballUponPool(gameBallMask, 0, ballsLengthByPocketGame);
                        }
                        if (uponBallsCount > 0)
                        {
                            deferLossCondition = false;
                        }
                    }
                    
                    if (!afterBreak && isAnyPocketSink)
                    {
                        isObjectiveSink = true;
                        isOpponentSink = false;
                    }
                }                
            }
            /*
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
            */
            else if (is9Ball || is10Ball)
            {
                bool isWrongHit = !(findLowestUnpocketedBall(ballsPocketedOrig) == firstHit);
                
                foulCondition = isScratch || isWrongHit || isNoTouch || isNoCushon;
                isObjectiveSink = (targetPocketedLocal & pocketMask) > (targetPocketedOrig & pocketMask);
                isOpponentSink = (otherPocketedLocal & pocketMask) > (otherPocketedOrig & pocketMask);
                if (isObjectiveSink || !afterBreak)
                {
                    isOpponentSink = false;
                }
                
                deferLossCondition = false;
                
                // if (isOpponentSink || foulCondition)
                // {
                //     int uponBallsCount = pocketedballUponPool(ballsPocketedCurrent, k_SPOT_POSITION_X, ballsLengthByPocketGame);
                //     if (uponBallsCount <= 0)
                //     {
                //         pocketedballUponPool(ballsPocketedCurrent, 0, ballsLengthByPocketGame);
                //     }
                // }

                bool isGameBallSink = (ballsPocketedLocal & gameBallMask) != 0;
                isObjectiveSink = isObjectiveSink | (is9Ball && !afterBreak && isGameBallSink);
                if (isGameBallSink && (!isObjectiveSink || pushOut || foulCondition || (is10Ball && !afterBreak) || (reBreakAllowed && (breakTermsLocal == BREAK_TERMS_3_POINT))))
                {
                    int uponBallsCount = pocketedballUponPool(gameBallMask, k_SPOT_POSITION_X, ballsLengthByPocketGame);
                    if (uponBallsCount <= 0)
                    {
                        pocketedballUponPool(gameBallMask, 0, ballsLengthByPocketGame);
                    }
                }

                winCondition = isGameBallSink && isObjectiveSink && !foulCondition;
                
                if (!afterBreak && isAnyPocketSink)
                {
                    isObjectiveSink = true;
                }
            }
            else /*if (is4Ball)*/
            {
                isObjectiveSink = fbMadePoint;
                isOpponentSink = fbMadeFoul;
                foulCondition = false;
                deferLossCondition = false;

                winCondition = fbScoresLocal[teamIdLocal] >= 10;
            }

            if (reBreakAllowed && (breakTermsLocal == BREAK_TERMS_3_POINT))
            {
                winCondition = false;
                deferLossCondition = false;
                isObjectiveSink = false;
            }
            
            if (pushOut && !isScratch)
            {
                foulCondition = false;
                winCondition = false;
                deferLossCondition = false;
                isObjectiveSink = false;
            }
            
            afterBreak = true;
            calledBallId = -2;
            calledPocketId = -2;
            // semiAutoCalledBall = false;
            semiAutoCalledPocket = false;
            semiAutoCalledTimeBall = 0;

            networkingManager._OnSimulationEnded(ballsP, ballsPocketedLocal, targetPocketedLocal, otherPocketedLocal, 
                fbScoresLocal, pushOutStateLocal
                );
            
            if (winCondition)
            {
                if (foulCondition)
                {
                    // Loss
                    onLocalTeamWin(teamIdLocal ^ 0x1U);
                }
                else
                {
                    // Win
                    onLocalTeamWin(teamIdLocal);
                }
            }
            else if (deferLossCondition)
            {
                // Loss
                onLocalTeamWin(teamIdLocal ^ 0x1U);
            }
            else if (foulCondition)
            {
                // Foul
                onLocalTurnFoul(false, true, reBreakAllowed);
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
                       k_SPOT_POSITION_X + i * k_BALL_PL_Y + (rackConditionLocal == 0 ? 0 : UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F)),
                       0.0f,
                       (-i + j * 2) * k_BALL_PL_X + (rackConditionLocal == 0 ? 0 : UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F))
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
                       k_SPOT_POSITION_X + i * k_BALL_PL_Y + (rackConditionLocal == 0 ? 0 : UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F)),
                       0.0f,
                       (-rown + j * 2) * k_BALL_PL_X + (rackConditionLocal == 0 ? 0 : UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F))
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
            // 10 ball
            initialBallsPocketed[4] = 0xF800u;

            for (int i = 0, k = 0; i < 4; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    initialPositions[4][break_order_10ball[k++]] = new Vector3
                    (
                        k_SPOT_POSITION_X + i * k_BALL_PL_Y + (rackConditionLocal == 0 ? 0 : UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F)),
                        0.0f,
                        (-i + j * 2) * k_BALL_PL_X + (rackConditionLocal == 0 ? 0 : UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F))
                    );
                }
            }
        }
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

    private void onLocalTeamWin(uint winner)
    {
        _LogInfo($"onLocalTeamWin {(winner)}");

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

        networkingManager._OnTurnPass(teamIdLocal ^ 0x1u, reBreakAllowed);
    }

    private void onLocalTurnFoul(bool isTimeEnd, bool reposition, bool reBreakAllowed)
    {
        _LogInfo($"onLocalTurnFoul");

        if (isTimeEnd && (is8Ball || is9Ball || is10Ball))
        {
            Array.Copy(ballsP, networkingManager.ballsPSynced, ballsP.Length);
            networkingManager.ballsPocketedSynced = ballsPocketedLocal;
            networkingManager.targetPocketedSynced = targetPocketedLocal;
            networkingManager.otherPocketedSynced = otherPocketedLocal;
        }
            
        networkingManager._OnTurnFoul(teamIdLocal ^ 0x1u, reposition, reBreakAllowed);
#if TKCH_DEBUG_SEMIAUTO_CALL
        _LogInfo("onLocalTurnFoul end");
        debugFlg = true;
#endif
    }

    private void onLocalTurnContinue()
    {
        _LogInfo($"onLocalTurnContinue");

        // try and close the table if possible
        if (is8Ball && isTableOpenLocal)
        {
            uint sink_orange = 0;
            uint sink_blue = 0;
            uint pmask = (requireCallShotLocal ?(targetPocketedLocal & ~targetPocketedOrig): ballsPocketedLocal) >> 2;

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
        semiAutoCallTick = false;

        _LogWarn("out of time!");

        graphicsManager._HideTimers();

        if (string.IsNullOrEmpty(tournamentRefereeLocal))
        {
            // no one is allowed to play
            canPlayLocal = false;

            if (isOurTurn())
            {
                calledBallId = -2;
                calledPocketId = -2;
                semiAutoCalledPocket = false;
                semiAutoCalledTimeBall = 0;
                
                // everyone on the current team propagates the change
                onLocalTurnFoul(true, true, !afterBreak);
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
#if TKCH_DEBUG_PUSHOUT
        _LogInfo("TKCH BilliardsModule::enablePlayComponents()");
#endif

        bool isOurTurnVar = isOurTurn();

        bool practiceEnable = (isOurTurnVar && isPracticeMode) ||
                        (!string.IsNullOrEmpty(tournamentRefereeLocal) && _IsLocalPlayerReferee());

        this.transform.Find("intl.controls/undo").gameObject.SetActive(practiceEnable);
        this.transform.Find("intl.controls/redo").gameObject.SetActive(practiceEnable);

        if (is9Ball || is10Ball)
        {
            marker9ball.SetActive(pushOutStateLocal != PUSHOUT_DOING);
            _Update9BallMarker();
        }

        if ((is8Ball || is9Ball || is10Ball) && isOurTurnVar)
        {
#if TKCH_DEBUG_PUSHOUT
            _LogInfo($"  enablePushOut {enablePushOutLocal}, pushOutState {PushOutState[pushOutStateLocal]}({pushOutStateLocal})");
#endif
            this.transform.Find("intl.controls/callShotLock").gameObject.SetActive(requireCallShotLocal);
            this.transform.Find("intl.controls/pushOut").gameObject.SetActive(enablePushOutLocal && (pushOutStateLocal == PUSHOUT_DONT || pushOutStateLocal == PUSHOUT_DOING));
            this.transform.Find("intl.controls/skipturn").gameObject.SetActive(practiceEnable || (enablePushOutLocal && (pushOutStateLocal == PUSHOUT_REACTIONING)) || (pushOutStateLocal == PUSHOUT_ILLEGAL_REACTIONING));
        }
        else
        {
            this.transform.Find("intl.controls/callShotLock").gameObject.SetActive(false);
            this.transform.Find("intl.controls/pushOut").gameObject.SetActive(false);
            this.transform.Find("intl.controls/skipturn").gameObject.SetActive(practiceEnable);
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

        if (is8Ball || is9Ball || is10Ball)
        {
            if (requireCallShotLocal && !enablePushOutLocal)
            {
                desktopManager._ChangeCallShotPushOut(true);
            }
            else
            {
                bool canPushOut = (pushOutStateLocal == PUSHOUT_DONT || pushOutStateLocal == PUSHOUT_DOING);
                desktopManager._ChangeCallShotPushOut(!canPushOut);
            }
        }
        else
        {
            desktopManager._CallShotSetActive(false);
            desktopManager._PushOutSetActive(false);
        }
        
        if (timerLocal > 0)
        {
            timerRunning = true;
            graphicsManager._ShowTimers();
        }
    }

    public void _SkipTurn()
    {
        semiAutoCallTick = false;
#if TKCH_DEBUG_SEMIAUTO_CALL
        _LogInfo($"TKCH BilliardsModule::_SkipTurn()");
        _LogInfo($"TKCH   semiAutoCalledTimeBall = {semiAutoCalledTimeBall}");
#endif
        calledBallId = -2;
        calledPocketId = -2;
        semiAutoCalledPocket = false;
        semiAutoCalledTimeBall = 0;
#if TKCH_DEBUG_SEMIAUTO_CALL
        _LogInfo($"TKCH   semiAutoCalledTimeBall = {semiAutoCalledTimeBall}");
#endif

        if (isPracticeMode || (!string.IsNullOrEmpty(tournamentRefereeLocal) && _IsLocalPlayerReferee()))
        {
            onLocalTurnFoul(false, true, false);
        }
        else if (pushOutStateLocal == PUSHOUT_REACTIONING || pushOutStateLocal == PUSHOUT_ILLEGAL_REACTIONING)
        {
#if TKCH_DEBUG_PUSHOUT
            _LogInfo($"  set {((pushOutStateLocal == PUSHOUT_REACTIONING)? "ENDED" : "DONT")} pushOutState {PushOutState[pushOutStateLocal]}({pushOutStateLocal})");
#endif
            networkingManager.pushOutStateSynced = (pushOutStateLocal == PUSHOUT_REACTIONING)? PUSHOUT_ENDED : PUSHOUT_DONT;
            onLocalTurnFoul(false, isReposition, false);
        }
    }

    public void _CallShotLock()
    {
        networkingManager._OnCallShotLockChanged(!callShotLockLocal);
    }
    
    public void _PushOut()
    {
        networkingManager._OnPushOutChanged(pushOutStateLocal);
    }

    public void _RequestBreak(uint teamId)
    {
#if TKCH_DEBUG_BREAKING_FOUL || TKCH_DEBUG_OPENING_BREAK || TKCH_DEBUG_SPECIAL_PENALTY
        _LogInfo($"TKCH BilliardsModule::_RequestBreak(teamId = {teamId})");
        // _LogInfo($"  inningCountLocal = {inningCountLocal}, inningCountSynced = {networkingManager.inningCountSynced}");
#endif
         
#if TKCH_DEBUG_SPECIAL_PENALTY
        _LogInfo($"  chainedFoulsLocal[teamIdLocal ^ 0x1u = {teamIdLocal ^ 0x1u}] = {chainedFoulsLocal[teamIdLocal ^ 0x1u]}");
#endif

        initializeRack();

        networkingManager._OnGameNextBreak(
            initialBallsPocketed[gameModeLocal], initialPositions[gameModeLocal], 
            teamId);
    }

    public void _Update9BallMarker()
    {
        if (marker9ball.activeSelf)
        {
            int target = findLowestUnpocketedBall(ballsPocketedLocal);
            marker9ball.transform.localPosition = ballsP[target];
        }
    }

    public void _UpdateCalledBallMarker()
    {
        if (!gameLive)
        {
            return;
        }

        if (!is8Ball && !is9Ball && !is10Ball)
        {
            return;
        }

        if (calledBallsLocal == 0)
        {
            markerCalledBall.SetActive(false);
        }
        
        int target = 0;
        uint ball_bit = 0x2u;
        for (int k = 0; k < (is9Ball ? break_order_9ball.Length : (is10Ball ? break_order_10ball.Length : break_order_8ball.Length)); k++)
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
            markerCalledBall.GetComponent<MeshRenderer>().material = callShotLock ? calledBallMarkerYellow :
                (isTableOpenLocal ? calledBallMarkerWhite :
                    ((teamIdLocal ^ teamColorLocal) == 0 ? calledBallMarkerBlue : calledBallMarkerOrange));
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
    
    public void _UpdateRequestBreakMarker()
    {
        if (!isOurTurn() || !requestBreakStateLocal || callShotLockLocal)
        {
            requestBreakOrange.SetActive(false);
            requestBreakBlue.SetActive(false);
            return;
        }

        if (enableRequestBreakLocal && requestBreakStateLocal)
        {
            requestBreakOrange.SetActive(true);
            requestBreakBlue.SetActive(true);
        }
        else
        {
            requestBreakOrange.SetActive(false);
            requestBreakBlue.SetActive(false);
        }
    }

    /*
    public void _UpdateNextBallRepositionSpotMarker()
    {
        if (!isOurTurn() || nextBallRepositionStateLocal == 0 || callShotLockLocal)
        {
            requestBreakOrange.SetActive(false);
            requestBreakBlue.SetActive(false);
            return;
        }

        if (enableRequestBreakLocal && (nextBallRepositionStateLocal & 0x4u) > 0 )
        {
            requestBreakOrange.SetActive(true);
            requestBreakBlue.SetActive(true);
        }
        else
        {
            requestBreakOrange.SetActive(false);
            requestBreakBlue.SetActive(false);
        }
    }
    */

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
#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC
                // if (debugLogFlg) _LogInfo($"  matrix[{i}][{j}]] = {p.x}, {p.y}, {p.z}");
#endif

                 for (int k = 0; k < findEasiestBallAndPocketConditions.Length; k++)
                {
                    Vector3 c = findEasiestBallAndPocketConditions[k];
                    if (p.x < c.x && p.y < c.y && p.z < c.z)
                    {
                        matrixIndexListByCondition[k][matrixIndexPosListByCondition[k]++] = (i * 16) + j;
#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC
                        // if (debugLogFlg) _LogInfo($"    k = {k},  (i * 16) + j = {matrixIndexListByCondition[k][matrixIndexPosListByCondition[k]-1]}");
#endif
                        break;
                    }
                }
            }
        }

#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC
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
#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC
                // if (debugLogFlg) _LogInfo($"  materixIndex = {materixIndex}, matrixIndexListByCondition[{i}][{j}]] = {matrixIndexListByCondition[i][j]}");
#endif
                if (materixIndex < 0 || matrixIndexPosListByCondition[i] <= j)
                {
                    continue;
                }

                int ballId = materixIndex / 16;
                int pocketId = materixIndex % 16;
                Vector3 p = matrix[ballId][pocketId];
#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC
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
        
#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC
        if (debugLogFlg) _LogInfo($"  easiestBallId = {easiestBallId}, easiestPocketId = {easiestPocketId}");
#endif

        return  ((easiestPocketId < 0 ? 0xFFFFu : (uint)easiestPocketId) << 16) | (easiestBallId < 0 ? 0xFFFFu : (uint)easiestBallId);
    }

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
    }

    private void tickSemiAutoCall()
    {
#if TKCH_DEBUG_SEMIAUTO_CALL
        // if (debugFlg) return;
#endif
        if (!isOurTurn() && 0 < semiAutoCalledTimeBall)
        {
            semiAutoCalledTimeBall = 0;
            return;
        }
        
        if ((is8Ball || is9Ball || is10Ball) && gameLive && canPlayLocal && isOurTurn() && afterBreak && requireCallShotLocal)
        {
#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC
            if (debugLogFlg) _LogInfo($"TKCH SEMIAUTO_CALL semiAutoCallBall = {semiAutoCallBallLocal}, semiAutoCalledTimeBall = {semiAutoCalledTimeBall}, calledBallId = {calledBallId}");
            if (debugLogFlg) _LogInfo($"                   semiAutoCallPocket = {semiAutoCallPocketLocal}, semiAutoCalledPocket = {semiAutoCalledPocket}, calledPocketId = {calledPocketId}, calledBalls = {calledBallsLocal:X4}");
#endif
            int target = -1;
            int pocketId = -1;

            if ((semiAutoCallBallLocal && semiAutoCalledTimeBall <= 0 && calledBallId < 0) ||
                (semiAutoCallPocketLocal && !semiAutoCalledPocket && calledPocketId < 0 && 0 < calledBallsLocal))
            {
                uint findBalls = ballsPocketedLocal;
                if (is9Ball || is10Ball)
                {
                    findBalls = ~(0x1u << findLowestUnpocketedBall(ballsPocketedLocal));
                }
                else if (is8Ball)
                {
                    if (isTableOpenLocal)
                    {
                        findBalls = ballsPocketedLocal | ~0xFFFFFFFCu;
                    }
                    else
                    {
                        uint bmask = (0x1FCu << ((int)(teamIdLocal ^ teamColorLocal) * 7));
                        bool isSetComplete = (ballsPocketedLocal & bmask) == bmask;
#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC
                        if (debugLogFlg) _LogInfo($"  teamIdLocal = {teamIdLocal}, teamColorLocal = {teamColorLocal}, ^ = {teamIdLocal ^ teamColorLocal}");
                        if (debugLogFlg) _LogInfo($"  findBalls = {ballsPocketedLocal:X4}");
#endif
                        findBalls = isSetComplete ? ~0x2u : ballsPocketedLocal | ~(0xFFFF0000 | bmask);
#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC
                        if (debugLogFlg) _LogInfo($"  findBalls = {findBalls:X4}");
#endif
                    }
                }
                uint pocketAndBall = findEasiestBallAndPocket(findBalls);
                if (pocketAndBall != 0xFFFFFFFF)
                {
                    target = (int)(pocketAndBall & 0xFFFFu);
                    pocketId = (int)((pocketAndBall >> 16) & 0xFFFFu);
                }
            }
            
#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC || TKCH_DEBUG_NEXT_BREAK
            if (debugLogFlg) _LogInfo($"  target(final) = {target}");
#endif
#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC || TKCH_DEBUG_SEMIAUTO_CALL_SIDE || TKCH_DEBUG_NEXT_BREAK
            // debugLogFlg = false;
#endif
            
            if (0 < target)
            {
                float elapsedSeconds = (Networking.GetServerTimeInMilliseconds() - timerStartLocal) / 1000.0f;
                
#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC
                if (0.1f < elapsedSeconds && elapsedSeconds < 0.12f)
                {
                    _LogInfo($"  semiAutoCalledBall = {semiAutoCalledBall}, calledBallId = {calledBallId}, target = {target}");
                }
#endif

                if (semiAutoCallBallLocal && semiAutoCalledTimeBall <= 0 && calledBallId < 0)
                {
#if TKCH_DEBUG_SEMIAUTO_CALL
                    _LogInfo($"  elapsedSeconds = {elapsedSeconds}");
#endif
                    if (0.4f < elapsedSeconds)
                    {
#if TKCH_DEBUG_SEMIAUTO_CALL
                        _LogInfo($"  elapsedSeconds = {elapsedSeconds}, call _TriggerOtherBallHit(target = {target})");
#endif
                        _TriggerOtherBallHit(target, true);
                        // semiAutoCalledTimeBall = elapsedSeconds;
                        semiAutoCalledTimeBall = 0.4f;
                    }
                }
            }
                
            if (semiAutoCallPocketLocal && !semiAutoCalledPocket && calledPocketId < 0 && 0 < calledBallsLocal)
            {
                float elapsedSeconds = (Networking.GetServerTimeInMilliseconds() - timerStartLocal) / 1000.0f;
#if TKCH_DEBUG_SEMIAUTO_CALL
                _LogInfo($"  elapsedSeconds = {elapsedSeconds}");
#endif
                if (0.4f + semiAutoCalledTimeBall < elapsedSeconds)
                {
#if TKCH_DEBUG_SEMIAUTO_CALL
                    _LogInfo($"  elapsedSeconds = {elapsedSeconds}, call _TriggerPocketHit(pocketId = {pocketId}, TRUE)");
#endif
                    _TriggerPocketHit(pocketId, true);
                    semiAutoCalledPocket = true;
#if TKCH_DEBUG_SEMIAUTO_CALL
                    debugFlg = false;
#endif
                }
            }
            
#if TKCH_DEBUG_SEMIAUTO_CALL_FINDLOGIC || TKCH_DEBUG_SEMIAUTO_CALL_SIDE || TKCH_DEBUG_NEXT_BREAK
            debugLogFlg = false;
#endif
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
        return new object[15]
        {
            positionClone, ballsPocketedLocal, scoresClone, gameModeLocal, teamIdLocal, repositionStateLocal, isTableOpenLocal, teamColorLocal, fourBallCueBallLocal,
            turnStateLocal, networkingManager.cueBallVSynced, networkingManager.cueBallWSynced, networkingManager.previewWinningTeamSynced,  requestBreakStateLocal,
            pushOutStateLocal
        };
    }

    public void _LoadInMemoryState(object[] state, int stateIdLocal)
    {
        networkingManager._ForceLoadFromState(
            stateIdLocal,
            (Vector3[])state[0], (uint)state[1], (int[])state[2], (uint)state[3], (uint)state[4], (uint)state[5], (bool)state[6], (uint)state[7], (uint)state[8],
            (byte)state[9], (Vector3)state[10], (Vector3)state[11], (byte)state[12], (bool)state[13],
            (byte)state[14]
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

        for (int i = 0; i < a.Length; i++) if (i != 0 && i != 2 && !a[i].Equals(b[i])) return false;

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
        //_LogInfo($"  check cusion {k_TABLE_WIDTH} < ball {ballPosition.x + k_BALL_RADIUS}");
#endif
        if (k_TABLE_WIDTH < ballPosition.x + k_BALL_RADIUS)
        {
            return true;
        }

        return false;
    }
    #endregion

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
