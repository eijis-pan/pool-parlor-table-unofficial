#if UNITY_ANDROID
#define HT_QUEST
#endif

#if !HT_QUEST || true
#define HT8B_DEBUGGER
#endif

#define TKCH_ONEPOCKET_SCORE

//#define TKCH_DEBUG_ONEPOCKET
//#define TKCH_DEBUG_POCKETED_RACK
//#define TKCH_DEBUG_SCORE
//#define TKCH_DEBUG_UPON_FOOT
//#define TKCH_DEBUG_POINT_POCKET_MARKER
//#define TKCH_DEBUG_CUSHION
//#define TKCH_DEBUG_UNDO
//#define TKCH_DEBUG_TIMEOUT
//#define TKCH_DEBUG_REPOSITION_PICKUP

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using System;
using System.Runtime.Remoting.Messaging;
using Metaphira.Modules.CameraOverride;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class BilliardsModule : UdonSharpBehaviour
{
    [NonSerialized] public readonly string[] DEPENDENCIES = new string[] { nameof(CameraOverrideModule) };
    [NonSerialized] public readonly string VERSION = "6.0.0 one_pocket";

    [NonSerialized] public const uint GAME_MODE_ONEPOCKET_MASK = 0x3u;
    [NonSerialized] public const uint GAME_MODE_ONEPOCKET15 = 4;
    [NonSerialized] public const uint GAME_MODE_ONEPOCKET9 = 5;
    [NonSerialized] public const uint GAME_MODE_ONEPOCKET5 = 6;

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
    private Vector3[][] initialPositions = new Vector3[7][];
    private uint[] initialBallsPocketed = new uint[7];

    // constants
    private const float k_BALL_RADIUS = 0.03f;
    private const float k_BALL_DIAMETRE = 0.06f;
    private const float k_BALL_PL_X = 0.03f; // break placement X
    private const float k_BALL_PL_Y = 0.05196152422f; // sin(60) * 0.06
    private const float k_RANDOMIZE_F = 0.0001f;
    private const float k_SPOT_POSITION_X = 0.5334f; // First X position of the racked balls
    private const float k_SPOT_CAROM_X = 0.8001f; // Spot position for carom mode
    private readonly int[] break_order_8ball = { 9, 2, 10, 11, 1, 3, 4, 12, 5, 13, 14, 6, 15, 7, 8 };
    private readonly int[] break_order_9ball = { 2, 3, 4, 5, 9, 6, 7, 8, 1 };
    private readonly int[] break_rows_9ball = { 0, 1, 2, 1, 0 };
    private readonly int[] break_order_onepocket5 = { 2, 3, 4, 5, 6 };
    private readonly int[] break_rows_onepocket5 = { 1, 2 };
    private readonly int[] one_pocket_goals = { 8, 5, 3 };
    [NonSerialized] public readonly int[] one_pocket_balls = { 15, 9, 5 };
    private readonly uint[] one_pocket_masks = { 0xFFFEu, 0x3FEu, 0x7Cu };
    //private readonly uint one_pocket_pocketed_rack_mask = 0xFF000000u;
    //[NonSerialized] public readonly uint one_pocket_point_pocket_mask = 0xFF000000u;

    #region InspectorValues
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
    [NonSerialized] public uint timerLocal;
    [NonSerialized] public bool teamsLocal;
    [NonSerialized] public bool noGuidelineLocal;
    [NonSerialized] public bool noLockingLocal;
    [NonSerialized] public uint ballsPocketedLocal;
    [NonSerialized] public uint targetPocketedLocal;
    [NonSerialized] public uint otherPocketedLocal;
    [NonSerialized] public uint denyBallsLocal;
    [NonSerialized] public uint pocketedRackLocal;
    [NonSerialized] public uint pointPocketsLocal;
    [NonSerialized] public uint teamIdLocal;
    [NonSerialized] public uint fourBallCueBallLocal;
    [NonSerialized] public bool isTableOpenLocal;
    [NonSerialized] public uint teamColorLocal;
    [NonSerialized] public string[] playerNamesLocal = new string[4];
    [NonSerialized] public string tournamentRefereeLocal;
    [NonSerialized] public int[] fbScoresLocal = new int[2];
    [NonSerialized] public int onePocketNotYetUponLocal;
    [NonSerialized] public bool noCushionLocal;
    [NonSerialized] public uint winningTeamLocal;
    [NonSerialized] public uint previewWinningTeamLocal;
    [NonSerialized] public int activeCueSkin;
    [NonSerialized] public int tableSkinLocal;
    [NonSerialized] public int physicsModeLocal;
    private byte gameStateLocal = byte.MaxValue;
    private byte turnStateLocal = byte.MaxValue;
    private int timerStartLocal;
    private uint repositionStateLocal;
    private int tableModelLocal;

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
    private int cushionObjectiveBallOnBreak = 0;
#if TKCH_DEBUG_CUSHION
    private int debugCushionTunnelCount = 0;
#endif
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
    [NonSerialized] public bool isOnePocket = false;
    [NonSerialized] public bool isOnePocket15Ball = false;
    [NonSerialized] public bool isOnePocket9Ball = false;
    [NonSerialized] public bool isOnePocket5Ball = false;
    [NonSerialized] public bool is8Ball = false;
    [NonSerialized] public bool is9Ball = false;
    [NonSerialized] public bool is4Ball = false;
    [NonSerialized] public bool isJp4Ball = false;
    [NonSerialized] public bool isKr4Ball = false;
    [NonSerialized] public bool isPracticeMode = false;
    [NonSerialized] public CameraOverrideModule cameraOverrideModule;
    public string[] moderators = new string[0];

#if TKCH_ONEPOCKET_SCORE
    [Space(10)]
    [Header("ONE_POCKET")]
    [SerializeField] public BilliardsScoreScreen scoreScreen;
#endif
    
    private Vector3[] pcketLocations = new Vector3[6];

    private void OnEnable()
    {

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

        gameModeLocal = GAME_MODE_ONEPOCKET15;
        isOnePocket = true;
        isOnePocket15Ball = true;
        pointPocketsLocal = 0x03u;
        
        networkingManager._Init(this);
        networkingManager.gameModeSynced = (byte)gameModeLocal;
        networkingManager.pointPocketsSynced = pointPocketsLocal;
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
        pcketLocations[5] = new Vector3(k_vF.x, k_vF.y, -k_vF.z);;
        
#if TKCH_ONEPOCKET_SCORE
#if TKCH_DEBUG_SCORE
        _LogInfo($"  EmptyTextOnZero");
#endif
        scoreScreen.SetTeamSafeNoPocketShotCountEmptyTextOnZero(0, true);
        scoreScreen.SetTeamSafeNoPocketShotCountEmptyTextOnZero(1, true);
#endif
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

#if TKCH_ONEPOCKET_SCORE
        scoreScreen.Clear();
        Array.Copy(scoreScreen.EncodeScoreSyncValues(), networkingManager.scoreSyncRows, networkingManager.scoreSyncRows.Length);
#endif
        networkingManager._OnLobbyOpened();
    }

    public void _TriggerLobbyClosed()
    {
        networkingManager._OnLobbyClosed();
    }

    public bool _TriggerPocketChanged(bool pocketEnabled, uint pocket)
    {
        return networkingManager._OnPocketChanged(pocketEnabled, pocket);
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
        if (ReferenceEquals(null, Networking.LocalPlayer)) return; // キューを持った状態でVRChatを終了するとエラーになる
        if (!Networking.LocalPlayer.IsUserInVR()) desktopManager._OnDropCue();
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
        Debug.Log("[BilliardsModule] latest game state is " + networkingManager._EncodeGameState());

#if TKCH_DEBUG_ONEPOCKET || TKCH_DEBUG_SCORE || TKCH_DEBUG_TIMEOUT || TKCH_DEBUG_UNDO
        _LogInfo("TKCH BilliardsModule::_OnRemoteDeserialization()");
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
            networkingManager.pointPocketsSynced,
            networkingManager.timerSynced,
            networkingManager.teamsSynced,
            networkingManager.noGuidelineSynced,
            networkingManager.noLockingSynced
        );

        if (gameStateLocal != networkingManager.gameStateSynced && networkingManager.gameStateSynced == 1)
        {
            Array.Clear(playerNamesCached, 0, playerNamesCached.Length);
        }

        // propagate valid players second
        onRemotePlayersChanged(networkingManager.playerNamesSynced);

#if TKCH_ONEPOCKET_SCORE
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
                scoreScreen.SetTeamSafeNoPocketShotCountEmptyTextOnZero(0, false);
                scoreScreen.SetTeamSafeNoPocketShotCountEmptyTextOnZero(1, true);
            }
        }
        
#if TKCH_DEBUG_SCORE
        _LogInfo($"  scoreUpdate = {scoreUpdate}");
#endif
        if (scoreUpdate)
        {
#if TKCH_DEBUG_SCORE
            _LogInfo($"  networkingManager.scoreSyncRows = {networkingManager.scoreSyncRows[0]:X8}-{networkingManager.scoreSyncRows[1]:X8}");
#endif
            scoreScreen.DecodeScoreSyncValues_Mini(networkingManager.scoreSyncRows);
        }
#endif

#if TKCH_DEBUG_TIMEOUT
        _LogInfo($"  noCushionLocal = {noCushionLocal}, networkingManager.noCushionSynced = {networkingManager.noCushionSynced}");
#endif
        if (/* noCushionLocal != networkingManager.noCushionSynced && */ networkingManager.noCushionSynced)
        {
            aud_main.PlayOneShot(snd_PointMade, 1.0f);
            graphicsManager._SpawnOnePocketPoint(ballsP[0], false, -1);
            //noCushionLocal = false;
            networkingManager.noCushionSynced = false;
        }

        onRemoteDenyBallsChanged(networkingManager.denyBallsSynced);
        onePocketNotYetUponLocal = networkingManager.onePocketNotYetUponSynced;

        // apply state transitions if needed
        onRemoteGameStateChanged(networkingManager.gameStateSynced);

        // now update game state
        onRemoteBallPositionsChanged(networkingManager.ballsPSynced);
        onRemoteTeamIdChanged(networkingManager.teamIdSynced);
        onRemoteFourBallCueBallChanged(networkingManager.fourBallCueBallSynced);
        onRemoteBallsPocketedChanged(networkingManager.ballsPocketedSynced, networkingManager.targetPocketedSynced, 
            networkingManager.otherPocketedSynced, networkingManager.pocketedRackSynced);
        onRemoteFourBallScoresUpdated(networkingManager.fourBallScoresSynced);
        onRemoteRepositionStateChanged(networkingManager.repositionStateSynced);
        onRemoteIsTableOpenChanged(networkingManager.isTableOpenSynced, networkingManager.teamColorSynced);
        onRemoteTurnStateChanged(networkingManager.turnStateSynced);
        onRemotePreviewWinningTeamChanged(networkingManager.previewWinningTeamSynced);

        // finally, take a snapshot
        practiceManager._Record();
        
#if TKCH_DEBUG_ONEPOCKET
        _LogInfo($"  ballsPocketedLocal = {ballsPocketedLocal:X4}");
        _LogInfo($"  targetPocketedLocal = {((targetPocketedLocal & 0xFFFF0000) >> 16):X4}-{(targetPocketedLocal & 0xFFFF):X4}, pocketedRackLocal = {pocketedRackLocal:X4}");
        _LogInfo($"  otherPocketedLocal = {otherPocketedLocal:X4}");
        _LogInfo($"  points orange = {fbScoresLocal[0]}, blue = {fbScoresLocal[1]}");
        _LogInfo($"  onePocketNotYetUponLocal = {onePocketNotYetUponLocal}");
#endif

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
                    currentPhysicsManager = standardPhysicsManager;
                    break;
                case 1:
                    currentPhysicsManager = legacyPhysicsManager;
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

    private void onRemoteGameSettingsUpdated(uint gameModeSynced, uint pointPocketsSynced, uint timerSynced, bool teamsSynced, bool noGuidelineSynced, bool noLockingSynced)
    {
#if TKCH_DEBUG_POINT_POCKET_MARKER
        _LogInfo("TKCH BilliardsModule::onRemoteGameSettingsUpdated()");
#endif
        if (
            gameModeLocal == gameModeSynced &&
            //(targetPocketedLocal[1] & one_pocket_point_pocket_mask) == (targetPocketedSynced[1] & one_pocket_point_pocket_mask) &&
            pointPocketsLocal == pointPocketsSynced &&
            timerLocal == timerSynced &&
            teamsLocal == teamsSynced &&
            noGuidelineLocal == noGuidelineSynced &&
            noLockingLocal == noLockingSynced
        )
        {
            return;
        }

        _LogInfo($"onRemoteGameSettingsUpdated gameMode={gameModeSynced} pointPockets={pointPocketsSynced:X2} timer={timerSynced} teams={teamsSynced} guideline={!noGuidelineSynced} locking={!noLockingSynced}");

        if (gameModeLocal != gameModeSynced)
        {
            gameModeLocal = gameModeSynced;

            isOnePocket15Ball = gameModeLocal == GAME_MODE_ONEPOCKET15;
            isOnePocket9Ball = gameModeLocal == GAME_MODE_ONEPOCKET9;
            isOnePocket5Ball = gameModeLocal == GAME_MODE_ONEPOCKET5;
            isOnePocket = isOnePocket15Ball || isOnePocket9Ball || isOnePocket5Ball;
            is8Ball = gameModeLocal == 0u;
            is9Ball = gameModeLocal == 1u;
            isJp4Ball = gameModeLocal == 2u;
            isKr4Ball = gameModeLocal == 3u;
            is4Ball = isJp4Ball || isKr4Ball;

            menuManager._RefreshGameMode();
        }

        if (timerLocal != timerSynced)
        {
            timerLocal = timerSynced;

            menuManager._RefreshTimer();
        }

        bool refreshToggles = false;
        //if ((targetPocketedLocal[1] & one_pocket_point_pocket_mask) != (targetPocketedSynced[1] & one_pocket_point_pocket_mask))
        if (pointPocketsLocal != pointPocketsSynced)
        {
            //Array.Copy(targetPocketedSynced, targetPocketedLocal, targetPocketedLocal.Length);
            pointPocketsLocal = pointPocketsSynced;
            refreshToggles = true;
        }

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
        
#if TKCH_ONEPOCKET_SCORE
        if (gameStateSynced != 2)
        {
#if TKCH_DEBUG_SCORE
            _LogInfo($"  EmptyTextOnZero");
#endif
            scoreScreen.SetTeamSafeNoPocketShotCountEmptyTextOnZero(0, true);
            scoreScreen.SetTeamSafeNoPocketShotCountEmptyTextOnZero(1, true);
        }
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
        _LogInfo($"onRemoteGameStarted");

        lobbyOpen = false;
        gameLive = true;

        Array.Clear(perfCounters, 0, PERF_MAX);
        Array.Clear(perfStart, 0, PERF_MAX);
        Array.Clear(perfTimings, 0, PERF_MAX);

        isPracticeMode = playerNamesLocal[1] == "" && playerNamesLocal[3] == "";

        menuManager._DisableMenu();

        if (isOnePocket)
        {
            graphicsManager._SetUSColors(true);
            graphicsManager._UpdateTeamColor(teamIdLocal);
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
        onePocketNotYetUponLocal = 0;
        auto_pocketblockers.SetActive(is4Ball);
        marker9ball.SetActive(is9Ball);

        afterBreak = false;
        targetPocketedLocal = 0x0u;
        otherPocketedLocal = 0x0u;
        pocketedRackLocal = 0x0u;
        denyBallsLocal = 0x0u;
        graphicsManager._SetBallsDenyMark(denyBallsLocal);
#if false // TKCH_DEBUG_ONEPOCKET
        _LogInfo($"  ballsPocketedLocal = {ballsPocketedLocal:X4}");
        _LogInfo($"  targetPocketedLocal = {((targetPocketedLocal & 0xFFFF0000) >> 16):X4}-{(targetPocketedLocal & 0xFFFF):X4}, pocketedRackLocal = {pocketedRackLocal:X4}");
        _LogInfo($"  points orange = {fbScoresLocal[0]}, blue = {fbScoresLocal[1]}");
        _LogInfo($"  onePocketNotYetUponLocal = {onePocketNotYetUponLocal}");
#endif

        graphicsManager._ShowBalls();

        // Reflect game state
        graphicsManager._UpdateScorecard();
        isReposition = false;
        markerObj.SetActive(false);

        // Effects
        graphicsManager._PlayIntroAnimation();
        aud_main.PlayOneShot(snd_Intro, 1.0f);

        graphicsManager._SetScorecardPlayers(playerNamesLocal);

        timerRunning = false;

        reflection_main.RenderProbe();

        activeCue = cueControllers[0];
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

        // Remove any access rights
        localPlayerId = -1;
        localTeamId = 0;
        applyCueAccess(true);

        resetCachedData();

        cueControllers[1].gameObject.SetActive(true);

        menuManager._EnableMenu();

        infReset.text = "Reset";
    }

    private void onRemoteBallsPocketedChanged(uint ballsPocketedSynced, uint targetPocketedSynced, uint otherPocketedSynced, uint pocketedRackSynced)
    {
        if (!gameLive) return;

        // todo: actually use a separate variable to track local modifications to balls pocketed
        if (ballsPocketedLocal != ballsPocketedSynced) _LogInfo($"onRemoteBallsPocketedChanged ballsPocketed={ballsPocketedSynced:X}");

        ballsPocketedLocal = ballsPocketedSynced;
        //Array.Copy(targetPocketedSynced, targetPocketedLocal, targetPocketedLocal.Length);
        targetPocketedLocal = targetPocketedSynced;
        otherPocketedLocal = otherPocketedSynced;
        pocketedRackLocal = pocketedRackSynced;

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
        
#if TKCH_ONEPOCKET_SCORE
#if TKCH_DEBUG_SCORE
        _LogInfo($"  EmptyTextOnZero teamIdLocal = {teamIdLocal}, teamColorLocal = {teamColorLocal}");
#endif
        scoreScreen.SetTeamSafeNoPocketShotCountEmptyTextOnZero((int)teamIdLocal, false);
        scoreScreen.SetTeamSafeNoPocketShotCountEmptyTextOnZero((int)(teamIdLocal ^ 0x1u), true);
#endif
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
#if TKCH_DEBUG_REPOSITION_PICKUP || TKCH_DEBUG_CUSHION
        _LogInfo($"TKCH BilliardsModule::onRemoteRepositionStateChangeD(repositionStateSynced = {repositionStateSynced})");
        _LogInfo($"  repositionStateLocal = {repositionStateLocal}");
#endif

        if (!gameLive) return;

        if (repositionStateLocal == repositionStateSynced) return;

        _LogInfo($"onRemoteRepositionStateChanged repositionState={repositionStateSynced}");
        repositionStateLocal = repositionStateSynced;

        if (repositionStateLocal == 1)
        {
            afterBreak = false;
#if TKCH_DEBUG_CUSHION
            _LogInfo($"  afterBreak = {afterBreak}");
#endif
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
            if (repositionStateLocal == 1 || isOnePocket)
            {
                repoMaxX = -k_SPOT_POSITION_X;
            }
            else
            {
                Vector3 k_pR = (Vector3)currentPhysicsManager.GetProgramVariable("k_pR");
                repoMaxX = k_pR.x;
            }
            setFoulPickupEnabled(true);
        }
    }

    private void onRemoteTurnBegin(int timerStartSynced)
    {
        _LogInfo("onRemoteTurnBegin");
        canPlayLocal = true;
        timerStartLocal = timerStartSynced;

        enablePlayComponents();
        Array.Clear(ballsV, 0, ballsV.Length);
        Array.Clear(ballsW, 0, ballsW.Length);
    }

    private void onRemoteTurnSimulate(Vector3 cueBallV, Vector3 cueBallW, string simulationOwner)
    {
#if TKCH_DEBUG_CUSHION
        _LogInfo("TKCH BilliardsModule::onRemoteTurnSimulate()");
#endif
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

        isLocalSimulationRunning = true;
        firstHit = 0;
        secondHit = 0;
        thirdHit = 0;
        cushionAfterFirstHit = 0;
        cushionObjectiveBallOnBreak = 0;
#if TKCH_DEBUG_CUSHION
        debugCushionTunnelCount = 0;
#endif
        noCushionLocal = false;
        fbMadePoint = false;
        fbMadeFoul = false;
        ballsPocketedOrig = ballsPocketedLocal;
        //Array.Copy(targetPocketedLocal, targetPocketedOrig, targetPocketedLocal.Length);
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
            
            if (isOnePocket)
            {
                graphicsManager._SetBallsDenyMark(0x0u);
            }
        }
        else
        {
            canPlayLocal = false;
            disablePlayComponents();
        }
    }

    private void onRemoteDenyBallsChanged(uint denyBallsLocalSynced)
    {
        if (!gameLive) return;

        if (denyBallsLocal == denyBallsLocalSynced) return;

        _LogInfo($"onRemoteDenyBallsChanged denyBalls={denyBallsLocalSynced}");
        denyBallsLocal = denyBallsLocalSynced;
        graphicsManager._SetBallsDenyMark(denyBallsLocal);
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
            case GAME_MODE_ONEPOCKET15:
            case GAME_MODE_ONEPOCKET9:
            case GAME_MODE_ONEPOCKET5:
                if (firstHit == 0)
                {
                    firstHit = dstId;
                    if ((denyBallsLocal & (0x1u << firstHit)) != 0x0u)
                    {
                        aud_main.PlayOneShot(snd_PointMade, 1.0f);
                        graphicsManager._SpawnOnePocketPoint(ballsP[firstHit], false, -1);
                    }
                }
                break;
        }
    }

#if TKCH_DEBUG_CUSHION
    public void _TriggerCushion(int id, Vector3 pos, bool isTunnel)
#else
    public void _TriggerCushion(int id, Vector3 pos)
#endif
    {
        if (isOnePocket)
        {
#if TKCH_DEBUG_CUSHION
            if (isTunnel)
            {
                debugCushionTunnelCount++;
            }
#endif
            if (ballsPocketedLocal == ballsPocketedOrig)
            {
                if (!afterBreak) //networkingManager.stateIdSynced == 2)
                {
                    if (0 < id)
                    {
                        if (0 == cushionObjectiveBallOnBreak)
                        {
                            graphicsManager._SpawnCushionTouch(pos, false);
                        }
                        cushionObjectiveBallOnBreak++;
                    }
                }
                else if (0 != firstHit)
                {
                    if (0 == cushionAfterFirstHit)
                    {
                        graphicsManager._SpawnCushionTouch(pos, true);
                    }
                    cushionAfterFirstHit++;
                }
            }
        }
    }

    public void _TriggerPocketBall(int id, int pocketId)
    {
#if TKCH_DEBUG_POCKETED_RACK
        _LogInfo("TKCH BilliardsModule::_TriggerPocketBall()");
#endif
        uint total = 0U;

        uint pocketedRack = pocketedRackLocal; // (targetPocketedLocal[0] & one_pocket_pocketed_rack_mask) >> 24;
        // Get total for X positioning
        int count_extent = is9Ball || isOnePocket9Ball ? 10 : (isOnePocket15Ball ? 17 :  16);
        for (int i = 1; i < count_extent; i++)
        {
            if (isOnePocket)
            {
                if (0 == ((pocketedRack >> (i - 1)) & 0x1U))
                {
                    total = (uint)(i - 1);
                    break;
                }
                continue;
            }
            total += (ballsPocketedLocal >> i) & 0x1U;
        }

        // place ball on the rack
        ballsP[id] = k_rack_position + (float)total * k_BALL_DIAMETRE * k_rack_direction;

        ballsPocketedLocal ^= 1U << id;

        uint bmask = 0x1FCU << ((int)(teamIdLocal ^ teamColorLocal) * 7);

        // Good pocket
        if (isOnePocket)
        {
            if (0 < id)
            {
                pocketedRackLocal |= 0x1u << (int)total;
            }
            //targetPocketedLocal[0] |= pocketedRack << 24;
#if TKCH_DEBUG_POCKETED_RACK || TKCH_DEBUG_POINT_POCKET_MARKER
            _LogInfo($"  total = {total}, pocketedRackLocal = {pocketedRackLocal:X2}");
#endif

            //uint pointPockets = ((targetPocketedLocal[1] & one_pocket_point_pocket_mask) >> 24) & (0x1u << pocketId);
            uint pointPockets = pointPocketsLocal & (0x1u << pocketId);
#if TKCH_DEBUG_POINT_POCKET_MARKER
            //_LogInfo($"  pointPockets = {pointPockets:X2}, targetPocketedLocal[ pocketId % 2 = {pocketId % 2} ] = {targetPocketedLocal[pocketId % 2]:X4}");
            _LogInfo($"  pointPockets = {pointPockets:X2}, targetPocketedLocal = {((targetPocketedLocal & 0xFFFF0000) >> 16):X4}-{(targetPocketedLocal & 0xFFFF):X4}, pocketedRackLocal = {pocketedRackLocal:X4}");
#endif
            if (0 < id && pointPockets != 0)
            {
                //targetPocketedLocal[pocketId % 2] |= 1U << id;
                targetPocketedLocal |= 1U << (((pocketId % 2) * 16) + id);
                if ((teamIdLocal ^ ((pointPockets & 0x2Au) != 0 ? 0 : 0x1u)) == 0)
                {
#if TKCH_DEBUG_POINT_POCKET_MARKER
                    _LogInfo("  my point");
#endif
                    graphicsManager._FlashTableLight();
                }
                else
                {
#if TKCH_DEBUG_POINT_POCKET_MARKER
                    _LogInfo("  vs point");
#endif
                    graphicsManager._FlashTableError();
                }

                if (0 == (ballsPocketedLocal & 0x1u))
                {
                    aud_main.PlayOneShot(snd_PointMade, 1.0f);
                    graphicsManager._SpawnOnePocketPoint(pcketLocations[pocketId], true, pocketId % 2);
                }
            }
            else
            {
                if (0 == id)
                {
                    aud_main.PlayOneShot(snd_PointMade, 1.0f);
                    graphicsManager._SpawnOnePocketPoint(pcketLocations[pocketId], false, -1);
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

    public void _TriggerSimulationEnded(bool forceScratch)
    {
#if TKCH_DEBUG_ONEPOCKET || TKCH_DEBUG_CUSHION
        _LogInfo("TKCH BilliardsModule::_TriggerSimulationEnded()");
#endif
        if (!isLocalSimulationRunning) return;
        isLocalSimulationRunning = false;

        _LogInfo("local simulation completed");
        cameraManager._OnLocalSimEnd();

        auto_colliderBaseVFX.SetActive(false);

#if TKCH_DEBUG_ONEPOCKET
        _LogInfo($"  isLocalSimulationOurs = {isLocalSimulationOurs}");
#endif
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
            else if (isOnePocket)
            {
                int onePocketKind = (int)(gameModeLocal & GAME_MODE_ONEPOCKET_MASK);
                uint onePocketMask = one_pocket_masks[onePocketKind];
                bool isWrongHit = (denyBallsLocal & (0x1u << firstHit)) != 0x0u;
                bool isNoTouch = firstHit <= 0;
                
                bool isAnyPocketSink = (ballsPocketedLocal & onePocketMask) > (ballsPocketedOrig & onePocketMask);
                
                bool isNoCushon = !isAnyPocketSink &&
                                  ((!afterBreak && cushionObjectiveBallOnBreak == 0) ||
                                   (afterBreak && cushionAfterFirstHit == 0));
                
#if TKCH_DEBUG_CUSHION
                _LogInfo($"  debugCushionTunnelCount = {debugCushionTunnelCount}");
                _LogInfo($"  cushionAfterFirstHit = {cushionAfterFirstHit}, cushionObjectiveBallOnBreak = {cushionObjectiveBallOnBreak}");
                _LogInfo($"  isAnyPocketSink = {isAnyPocketSink}, isNoCushon = {isNoCushon}, afterBreak = {afterBreak}");
#endif
#if TKCH_DEBUG_ONEPOCKET
                _LogInfo($"  ballsPocketedLocal = {ballsPocketedLocal:X4}");
                _LogInfo($"  targetPocketedLocal = {((targetPocketedLocal & 0xFFFF0000) >> 16):X4}-{(targetPocketedLocal & 0xFFFF):X4}");
                _LogInfo($"  firstHit = {firstHit}, isWrongHit = {isWrongHit}");
                _LogInfo($"  teamIdLocal = {teamIdLocal}, onePocketKind = {onePocketKind}");
                _LogInfo($"  isNoTouch = {isNoTouch}");
#endif
                foulCondition = isScratch || isWrongHit || isNoTouch || isNoCushon;
                isObjectiveSink = ((targetPocketedLocal >> ((int)teamIdLocal * 16)) & onePocketMask) > ((targetPocketedOrig >> ((int)teamIdLocal * 16)) & onePocketMask);
                isOpponentSink = false;
                
                if (isScratch)
                {
                    ballsP[0] = initialPositions[gameModeLocal][0];
                }
                else if (isNoTouch || isNoCushon)
                {
                    noCushionLocal = true;
                }

                //int upon = 0;
                uint targetPocketed = ((targetPocketedLocal & ~targetPocketedOrig) >> ((int)teamIdLocal * 16)) & onePocketMask;
                int point = (int)SoftwareFallback(targetPocketed);
                if (foulCondition)
                {
                    fbScoresLocal[teamIdLocal]--; // foul penalty
                    if (fbScoresLocal[teamIdLocal] < -127)
                    {
                        fbScoresLocal[teamIdLocal] = -127;
                    }

                    onePocketNotYetUponLocal += point;
                    onePocketNotYetUponLocal++; // foul penalty
                    pocketedballUponPool(targetPocketed, k_SPOT_POSITION_X, one_pocket_balls[onePocketKind]);
                }
                else
                {
                    fbScoresLocal[teamIdLocal] += point;
                    if (127 < fbScoresLocal[teamIdLocal])
                    {
                        fbScoresLocal[teamIdLocal] = 127;
                    }
                }

                uint vsTargetPocketed = ((targetPocketedLocal & ~targetPocketedOrig) >> ((int)(teamIdLocal ^ 0x1u) * 16)) & onePocketMask;
                point = (int)SoftwareFallback(vsTargetPocketed);
                if (foulCondition)
                {
                    onePocketNotYetUponLocal += point;
                    pocketedballUponPool(vsTargetPocketed, k_SPOT_POSITION_X, one_pocket_balls[onePocketKind]);
                }
                else
                {
                    fbScoresLocal[teamIdLocal ^ 0x1u] += point;
                    if (127 < fbScoresLocal[teamIdLocal ^ 0x1u])
                    {
                        fbScoresLocal[teamIdLocal ^ 0x1u] = 127;
                    }
                }

                deferLossCondition = one_pocket_goals[onePocketKind] <= fbScoresLocal[teamIdLocal ^ 0x1u];
                winCondition = one_pocket_goals[onePocketKind] <= fbScoresLocal[teamIdLocal];
                
                bool isOtherPocketSink = (otherPocketedLocal & onePocketMask) > (otherPocketedOrig & onePocketMask);
                uint otherPocketed = otherPocketedLocal & ~otherPocketedOrig & onePocketMask;
#if TKCH_DEBUG_ONEPOCKET
                //_LogInfo($"  bothTargetPocketed = {bothTargetPocketed:X4}, bothTargetPocketedOrig = {bothTargetPocketedOrig:X4}");
                _LogInfo($"  isOtherPocketSink = {isOtherPocketSink}, otherPocketed = {otherPocketed:X4}");
#endif
                if (isOtherPocketSink)
                {
                    int notYetUpon = (int)SoftwareFallback(otherPocketed);
                    onePocketNotYetUponLocal += notYetUpon;
                    if (!isObjectiveSink || foulCondition)
                    {
                        pocketedballUponPool(otherPocketed, k_SPOT_POSITION_X, one_pocket_balls[onePocketKind]);
                    }
                }

                if (!isObjectiveSink || foulCondition)
                {
                    if (0 < onePocketNotYetUponLocal)
                    {
                        uint uponBallsPocketed = 0x0u;
                        int uponBallsPocketedCount = 0;
                        uint ball_bit = 0x2u;
                        for (int i = 1; i <= one_pocket_balls[onePocketKind] && uponBallsPocketedCount < onePocketNotYetUponLocal; i++)
                        {
                            if ((ballsPocketedLocal & ball_bit) != 0x0u)
                            {
                                uponBallsPocketed |= ball_bit;
                                uponBallsPocketedCount++;
                            }

                            ball_bit <<= 1;
                        }

#if TKCH_DEBUG_UPON_FOOT
                        //_LogInfo($"  ballsPocketedLocal = {ballsPocketedLocal:X4}, uponBallsPocketed = {uponBallsPocketed:X4}");
#endif
                        pocketedballUponPool(uponBallsPocketed, k_SPOT_POSITION_X, one_pocket_balls[onePocketKind]);
                    }
                }
                else
                {
                    if ((ballsPocketedLocal & onePocketMask) == onePocketMask)
                    {
                        pocketedballUponPool(0x2u, k_SPOT_POSITION_X, one_pocket_balls[onePocketKind]);
                    }
                }

                if (255 < onePocketNotYetUponLocal)
                {
                    onePocketNotYetUponLocal = 255;
                }
                if (onePocketNotYetUponLocal < 0)
                {
#if TKCH_DEBUG_UPON_FOOT
                    _LogInfo($"  onePocketNotYetUponLocal = {onePocketNotYetUponLocal}");
#endif
                    onePocketNotYetUponLocal = 0;
                }

                denyBallsLocal = 0x0u;
                if (isScratch)
                {
                    uint ball_bit = 0x2u;
                    uint ballsInKitchen = 0x0u;
                    for (int i = 1; i <= one_pocket_balls[onePocketKind]; i++)
                    {
                        if ((ballsPocketedLocal & ball_bit) == 0x0u)
                        {
                            if (ballsP[i].x <= -k_SPOT_POSITION_X)
                            {
                                ballsInKitchen |= 0x1u << i;
                            }
                        }
                        ball_bit <<= 1;
                    }

                    graphicsManager._SetBallsDenyMark(ballsInKitchen);
                    denyBallsLocal = ballsInKitchen;
                }
#if false //TKCH_DEBUG_ONEPOCKET
                _LogInfo($"  ballsPocketedLocal = {ballsPocketedLocal:X4}");
                _LogInfo($"  points orange = {fbScoresLocal[0]}, blue = {fbScoresLocal[1]}");
                _LogInfo($"  onePocketNotYetUponLocal = {onePocketNotYetUponLocal}");
#endif
#if TKCH_ONEPOCKET_SCORE
                UpdateScoreSyncRowsByFlags(
                    (!foulCondition && isObjectiveSink), !foulCondition, 
                    foulCondition, true);
                /*
                uint[] encodeScoreSyncValues = new uint[2];
                encodeScoreSyncValues[teamIdLocal] = scoreScreen.EncodeScoreParams_Mini(
                    fbScoresLocal[teamIdLocal],
                    scoreScreen.GetTeamShotCount((int)teamIdLocal) + 1,
                     (foulCondition ? (scoreScreen.GetTeamScratchCount((int)teamIdLocal) + 1) : 0),
                    //scoreScreen.GetTeamSafeNoPocketShotCount((int)teamIdLocal) + upon
                    ((!foulCondition && isObjectiveSink) ? onePocketNotYetUponLocal : 0)
                );
                encodeScoreSyncValues[teamIdLocal ^ 0x1u] = scoreScreen.EncodeScoreParams_Mini(
                    fbScoresLocal[teamIdLocal ^ 0x1u],
                    scoreScreen.GetTeamShotCount((int)(teamIdLocal ^ 0x1u)),
                    scoreScreen.GetTeamScratchCount((int)(teamIdLocal ^ 0x1u)),
                    //scoreScreen.GetTeamSafeNoPocketShotCount((int)(teamIdLocal ^ 0x1u))
                    ((!foulCondition && isObjectiveSink) ? 0 : onePocketNotYetUponLocal)
                );
#if TKCH_DEBUG_SCORE
                _LogInfo($"  encodeScoreSyncValues = {encodeScoreSyncValues[0]:X8}-{encodeScoreSyncValues[1]:X8}");
#endif
                Array.Copy(encodeScoreSyncValues, networkingManager.scoreSyncRows, networkingManager.scoreSyncRows.Length);
                */
#endif
                if (foulCondition && !isScratch && (isNoTouch || isNoCushon || isWrongHit))
                {
                    repositionOnFoul = false;
                    //foulCondition = false;
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
#if TKCH_DEBUG_ONEPOCKET
            _LogInfo($"  isScratch = {isScratch}, foulCondition = {foulCondition}");
            _LogInfo($"  isObjectiveSink = {isObjectiveSink}, isOpponentSink = {isOpponentSink}");
            _LogInfo($"  winCondition = {winCondition}");
#endif

            afterBreak = true;
#if TKCH_DEBUG_CUSHION
            _LogInfo($"  afterBreak = {afterBreak}");
#endif

            //networkingManager._OnSimulationEnded(ballsP, ballsPocketedLocal, targetPocketedLocal, fbScoresLocal, onePocketNotYetUponLocal);
            networkingManager._OnSimulationEnded(ballsP, ballsPocketedLocal, targetPocketedLocal, otherPocketedLocal, 
                denyBallsLocal, pocketedRackLocal, fbScoresLocal, onePocketNotYetUponLocal, noCushionLocal);

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
                onLocalTurnFoul(false, repositionOnFoul);
            }
            else if (isObjectiveSink && !isOpponentSink)
            {
                // Continue
                onLocalTurnContinue();
            }
            else
            {
                // Pass
                onLocalTurnPass();
            }
        }
        else
        {
            afterBreak = true;
#if TKCH_DEBUG_CUSHION
            _LogInfo($"  afterBreak = {afterBreak}");
#endif
        }
    }
    #endregion

    #region GameLogic
    private void initializeRack()
    {
        for (int i = 0; i < 7; i++)
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
            // one pocket
            initialBallsPocketed[GAME_MODE_ONEPOCKET15] = 0x00u;
            initialBallsPocketed[GAME_MODE_ONEPOCKET9] = 0xFC00u;
            initialBallsPocketed[GAME_MODE_ONEPOCKET5] = 0xFF82;

            initialPositions[GAME_MODE_ONEPOCKET15] = initialPositions[0];
            initialPositions[GAME_MODE_ONEPOCKET9] = initialPositions[1];

            for (int i = 0, k = 0; i < break_rows_onepocket5.Length; i++)
            {
                int rown = break_rows_onepocket5[i];
                for (int j = 0; j <= rown; j++)
                {
                    initialPositions[GAME_MODE_ONEPOCKET5][break_order_onepocket5[k++]] = new Vector3
                    (
                        k_SPOT_POSITION_X + i * k_BALL_PL_Y + UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F),
                        0.0f,
                        (-rown + j * 2) * k_BALL_PL_X + UnityEngine.Random.Range(-k_RANDOMIZE_F, k_RANDOMIZE_F)
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
        for (int i = 0; i < pointPocketMarkers.Length; i++)
        {
            pointPocketMarkers[i] = table_base.Find($"PointPocketMarker_{i}").gameObject;
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
        return physicsModeLocal == 1;
    }

    public bool _IsNewPhysics()
    {
        return physicsModeLocal == 0;
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

    private void onLocalTurnPass()
    {
        _LogInfo($"onLocalTurnPass");

        networkingManager._OnTurnPass(teamIdLocal ^ 0x1u);
    }

    private void onLocalTurnFoul(bool isTimeEnd, bool reposition)
    {
        _LogInfo($"onLocalTurnFoul {reposition}");

        if (isOnePocket)
        {
            if (isTimeEnd)
            {
                Array.Copy(ballsP, networkingManager.ballsPSynced, ballsP.Length);
                Array.Copy(fbScoresLocal, networkingManager.fourBallScoresSynced, fbScoresLocal.Length);
                networkingManager.ballsPocketedSynced = ballsPocketedLocal;
                networkingManager.targetPocketedSynced = targetPocketedLocal;
                networkingManager.otherPocketedSynced = otherPocketedLocal;
                networkingManager.denyBallsSynced = denyBallsLocal;
                networkingManager.pocketedRackSynced = pocketedRackLocal;
                networkingManager.onePocketNotYetUponSynced = onePocketNotYetUponLocal;
                networkingManager.noCushionSynced = noCushionLocal;
                networkingManager._OnTurnFoul(teamIdLocal ^ 0x1u, false);
                return;
            }
        }
        
        networkingManager._OnTurnFoul(teamIdLocal ^ 0x1u, reposition);
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
#if TKCH_DEBUG_TIMEOUT
        _LogInfo("TKCH BilliardsModule::onLocalTimerEnd()");
#endif

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
                if (isOnePocket)
                {
                    reposition = false;
                    noCushionLocal = true;
                    //aud_main.PlayOneShot(snd_PointMade, 1.0f);
                    //graphicsManager._SpawnOnePocketPoint(ballsP[0], false, -1);

                    denyBallsLocal = 0x0u;
                    graphicsManager._SetBallsDenyMark(denyBallsLocal);
                    
                    fbScoresLocal[teamIdLocal]--; // foul penalty
                    if (fbScoresLocal[teamIdLocal] < -127)
                    {
                        fbScoresLocal[teamIdLocal] = -127;
                    }

                    onePocketNotYetUponLocal++;
                    if (255 < onePocketNotYetUponLocal)
                    {
                        onePocketNotYetUponLocal = 255;
                    }
                    
#if TKCH_DEBUG_TIMEOUT || TKCH_DEBUG_UPON_FOOT
                    _LogInfo($"  onePocketNotYetUponLocal = {onePocketNotYetUponLocal}");
#endif

                    int onePocketKind = (int)(gameModeLocal & GAME_MODE_ONEPOCKET_MASK);
                    uint uponBallsPocketed = 0x0u;
                    int uponBallsPocketedCount = 0;
                    uint ball_bit = 0x2u;
                    for (int i = 1; i <= one_pocket_balls[onePocketKind] && uponBallsPocketedCount < onePocketNotYetUponLocal; i++)
                    {
                        if ((ballsPocketedLocal & ball_bit) != 0x0u)
                        {
                            uponBallsPocketed |= ball_bit;
                            uponBallsPocketedCount++;
                        }

                        ball_bit <<= 1;
                    }

#if TKCH_DEBUG_TIMEOUT || TKCH_DEBUG_UPON_FOOT
                    _LogInfo($"  uponBallsPocketed = {uponBallsPocketed:X4}");
#endif

                    pocketedballUponPool(uponBallsPocketed, k_SPOT_POSITION_X, one_pocket_balls[onePocketKind]);
                }
                
#if TKCH_ONEPOCKET_SCORE
                UpdateScoreSyncRowsByFlags(false, false, true, false);
                /*
                uint[] encodeScoreSyncValues = new uint[2];
                encodeScoreSyncValues[teamIdLocal] = scoreScreen.EncodeScoreParams_Mini(
                    fbScoresLocal[teamIdLocal],
                    scoreScreen.GetTeamShotCount((int)teamIdLocal),
                    scoreScreen.GetTeamScratchCount((int)teamIdLocal) + 1,
                    0
                );
                encodeScoreSyncValues[teamIdLocal ^ 0x1u] = scoreScreen.EncodeScoreParams_Mini(
                    fbScoresLocal[teamIdLocal ^ 0x1u],
                    scoreScreen.GetTeamShotCount((int)(teamIdLocal ^ 0x1u)),
                    scoreScreen.GetTeamScratchCount((int)(teamIdLocal ^ 0x1u)),
                    onePocketNotYetUponLocal
                );
                Array.Copy(encodeScoreSyncValues, networkingManager.scoreSyncRows, networkingManager.scoreSyncRows.Length);
                */
#endif
                
                // everyone on the current team propagates the change
                onLocalTurnFoul(true, reposition);
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
            onLocalTurnFoul(false, true);
        }
    }

    public void _Update9BallMarker()
    {
        if (marker9ball.activeSelf)
        {
            int target = findLowestUnpocketedBall(ballsPocketedLocal);
            marker9ball.transform.localPosition = ballsP[target];
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
#if TKCH_DEBUG_REPOSITION_PICKUP
        _LogInfo($"TKCH BilliardsModule::setFoulPickupEnabled(enabled = {enabled})");
#endif
        
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
#if TKCH_DEBUG_UNDO
        _LogInfo("TKCH BilliardsModule::_SerializeInMemoryState()");
        _LogInfo($"  onePocketNotYetUponLocal = {onePocketNotYetUponLocal}, networkingManager.onePocketNotYetUponSynced = {networkingManager.onePocketNotYetUponSynced}");
#endif

        Vector3[] positionClone = new Vector3[ballsP.Length];
        Array.Copy(ballsP, positionClone, ballsP.Length);
        int[] scoresClone = new int[fbScoresLocal.Length];
        Array.Copy(fbScoresLocal, scoresClone, fbScoresLocal.Length);
        //uint[] scoreSyncRowsClone = new uint[networkingManager.scoreSyncRows.Length];
        //Array.Copy(networkingManager.scoreSyncRows, scoreSyncRowsClone, networkingManager.scoreSyncRows.Length);
        return new object[18]
        {
            positionClone, ballsPocketedLocal, scoresClone, gameModeLocal, teamIdLocal, repositionStateLocal, isTableOpenLocal, teamColorLocal, fourBallCueBallLocal,
            turnStateLocal, networkingManager.cueBallVSynced, networkingManager.cueBallWSynced, networkingManager.previewWinningTeamSynced
            , targetPocketedLocal, otherPocketedLocal, denyBallsLocal, pocketedRackLocal, onePocketNotYetUponLocal //, scoreSyncRowsClone
        };
    }

    public void _LoadInMemoryState(object[] state, int stateIdLocal)
    {
        networkingManager._ForceLoadFromState(
            stateIdLocal,
            (Vector3[])state[0], (uint)state[1], (int[])state[2], (uint)state[3], (uint)state[4], (uint)state[5], (bool)state[6], (uint)state[7], (uint)state[8],
            (byte)state[9], (Vector3)state[10], (Vector3)state[11], (byte)state[12]
            , (uint)state[13], (uint)state[14], (uint)state[15], 
            (uint)state[16], (byte)state[17] //, (uint[])state[18]
        );
    }

    public bool _AreInMemoryStatesEqual(object[] a, object[] b)
    {
#if TKCH_DEBUG_UNDO
        _LogInfo("TKCH BilliardsModule::_AreInMemoryStatesEqual()");
#endif
        
        Vector3[] posA = (Vector3[])a[0];
        Vector3[] posB = (Vector3[])b[0];
        for (int i = 0; i < ballsP.Length; i++) if (posA[i] != posB[i]) return false;

#if TKCH_DEBUG_UNDO
        _LogInfo("  state[0] a equal b");
#endif

        int[] scoresA = (int[])a[2];
        int[] scoresB = (int[])b[2];
        for (int i = 0; i < fbScoresLocal.Length; i++) if (scoresA[i] != scoresB[i]) return false;

#if TKCH_DEBUG_UNDO
        _LogInfo("  state[2] a equal b");
#endif

        /*
        uint[] targetPocketedA = (uint[])a[13];
        uint[] targetPocketedB = (uint[])b[13];
        for (int i = 0; i < targetPocketedLocal.Length; i++) if (targetPocketedA[i] != targetPocketedB[i]) return false;
        */

        /*
        uint[] scoreSyncRowsA = (uint[])a[18];
        uint[] scoreSyncRowsB = (uint[])b[18];
        for (int i = 0; i < scoreSyncRowsA.Length; i++) if (scoreSyncRowsA[i] != scoreSyncRowsB[i]) return false;

#if TKCH_DEBUG_UNDO
        _LogInfo("  state[18] a equal b");
#endif
        */
        
#if TKCH_DEBUG_UNDO
        for (int i = 0; i < a.Length; i++)
        {
            if (i != 0 && i != 2 /* && i != 18 */)
            {
                if (!a[i].Equals(b[i]))
                {
                    if (i != 6 && i != 10 && i != 11)
                    {
                        _LogInfo($"  state[i = {i}] a {a[i]:x8} != b {b[i]:x8}");
                    }
                    else
                    {
                        _LogInfo($"  state[i = {i}] a {a[i]} != b {b[i]}");
                    }
                    return false;
                }
                else
                {
                    //_LogInfo($"  state[i = {i}] a equal b");
                }
            }
        }
#else
        for (int i = 0; i < a.Length; i++) if (i != 0 && i != 2 /* && i != 18 */ && !a[i].Equals(b[i])) return false;
#endif

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

    private void pocketedballUponPool(uint ballsPocketed, float posX, int safeLimitLoop)
    {
#if TKCH_DEBUG_UPON_FOOT
        _LogInfo("TKCH BilliardsModule::pocketedballUponPool()");
        _LogInfo($"  ballsPocketed = {ballsPocketed:X4}");
#endif
        if (ballsPocketed == 0x0u)
        {
            return; // 0x0u;
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
                //_LogInfo($"  ballsP[{i}] = [{ballsP[i].x},{ballsP[i].y},{ballsP[i].z}]");
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
                //_LogInfo($"  touching = {touching}, ballsP[{i}] = [{ballsP[i].x},{ballsP[i].y},{ballsP[i].z}]");
#endif
                int limit = safeLimitLoop;
                while (0 <= touching)
                {
                    ballsP[i] = new Vector3
                    (
                        ballsP[touching].x + k_BALL_DIAMETRE,
                        ballsP[touching].y,
                        ballsP[touching].z
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
        pocketedRackLocal &= ~uponRacks;
#if TKCH_DEBUG_POCKETED_RACK || TKCH_DEBUG_UPON_FOOT
        //_LogInfo($"  uponRacks = {uponRacks:X4}, targetPocketedLocal[0] = {targetPocketedLocal[0]:X8}");
        _LogInfo($"  uponRacks = {uponRacks:X4}, pocketedRackLocal = {pocketedRackLocal:X4}");
#endif

        targetPocketedLocal &= ~((uponBalls << 16) | (uponBalls & 0xFFFFu));
        ballsPocketedLocal &= ~uponBalls;
        otherPocketedLocal &= ~uponBalls;
        onePocketNotYetUponLocal -= uponBallsCount;
        //return uponBalls;
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
    
#if TKCH_ONEPOCKET_SCORE
    public uint EncodeScoreSyncValue(int point, int shotCount, int foulCount, int uponWaitingCount)
    {
        uint scoreSyncValue = 0x0u;
        scoreSyncValue |= (uint)((shotCount & 0xFFu) << 24);
        scoreSyncValue |= (uint)((foulCount & 0xFFu) << 16);
        scoreSyncValue |= (uint)((uponWaitingCount & 0xFFu) << 8);
        //scoreSyncValue |= (uint)(((point /* + 127 */ ) & 0xFFu) << 0);
        scoreSyncValue |= (uint)(
            point & 0x7Fu | (point < 0 ? 0x80u : 0x0u)
        ) << 0;
        
        return scoreSyncValue;
    }
    
    /*
    public uint[] TransScoreSyncValue(uint value)
    {
#if TKCH_DEBUG_SCORE
        _LogInfo("TKCH BilliardsModule::TransScoreSyncValue()");
#endif
        int shotCount = (int)((value >> 24) & 0xFFu);
        int foulCount = (int)((value >> 16) & 0xFFu);
        int uponWaitingCount = (int)((value >> 8) & 0xFFu);
        int point = (int)((value >> 0) & 0xFFu);
        //if (scoreSigned[Array.IndexOf(scoreTexts, pointText)])
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
        
#if TKCH_DEBUG_SCORE
        _LogInfo($"  uponWaitingCount = {uponWaitingCount}, point = {point}");
#endif

        uint[] scoreSyncValues = new uint[2];

        uint scoreSyncValue = 0x0u;
        //scoreSyncValue |= (uint)((point & 0xFFFFu) << 16);
        scoreSyncValue |= (uint)(
            point & 0x7FFFu | (point < 0 ? 0x8000u : 0x0u)
        ) << 16;

        scoreSyncValue |= (uint)((foulCount & 0xFFu) << 8);
        //scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, pocketBallCountText)] & 0xFFu) << 0);
        scoreSyncValues[0] = scoreSyncValue;

        scoreSyncValue = 0x0u;
        scoreSyncValue |= (uint)((shotCount & 0xFFFu) << 20);
        scoreSyncValue |= (uint)((uponWaitingCount & 0x3FFu) << 10);
        //scoreSyncValue |= (uint)((scores[Array.IndexOf(scoreTexts, invalidPocketBallCountText)] & 0x3FFu) << 0);
        scoreSyncValues[1] = scoreSyncValue;
        
        return scoreSyncValues;
    }
    */

    public void UpdateScoreSyncRowsByFlags(bool turnContinue, bool foulCountClear, bool foulCountIncrement, bool shotCountIncrement)
    {
#if TKCH_DEBUG_SCORE || TKCH_DEBUG_UNDO
        _LogInfo("TKCH BilliardsModule::UpdateScoreSyncRowsByFlags()");
#endif
        uint[] encodeScoreSyncValues = new uint[2];
        encodeScoreSyncValues[teamIdLocal] = scoreScreen.EncodeScoreParams_Mini(
            fbScoresLocal[teamIdLocal],
            scoreScreen.GetTeamShotCount((int)teamIdLocal) + (shotCountIncrement ? 1 : 0),
            ((foulCountClear ? 0 : scoreScreen.GetTeamScratchCount((int)teamIdLocal)) + (foulCountIncrement ? 1 : 0)),
            (turnContinue ? onePocketNotYetUponLocal : 0)
        );
        encodeScoreSyncValues[teamIdLocal ^ 0x1u] = scoreScreen.EncodeScoreParams_Mini(
            fbScoresLocal[teamIdLocal ^ 0x1u],
            scoreScreen.GetTeamShotCount((int)(teamIdLocal ^ 0x1u)),
            scoreScreen.GetTeamScratchCount((int)(teamIdLocal ^ 0x1u)),
            (turnContinue ? 0 : onePocketNotYetUponLocal)
        );
#if TKCH_DEBUG_SCORE
        _LogInfo($"  encodeScoreSyncValues = {encodeScoreSyncValues[0]:X8}-{encodeScoreSyncValues[1]:X8}");
#endif
        Array.Copy(encodeScoreSyncValues, networkingManager.scoreSyncRows, networkingManager.scoreSyncRows.Length);
    }
#endif
    
    public void UpdateScoreSyncRowsByParams(uint teamId, int[] fbScores, int onePocketNotYetUpon)
    {
#if TKCH_ONEPOCKET_SCORE
#if TKCH_DEBUG_SCORE || TKCH_DEBUG_UNDO
        _LogInfo("TKCH BilliardsModule::UpdateScoreSyncRowsByParams()");
#endif
        uint[] encodeScoreSyncValues = new uint[2];
        encodeScoreSyncValues[teamId] = scoreScreen.EncodeScoreParams_Mini(
            fbScores[teamId], 0, 0, onePocketNotYetUpon);
        encodeScoreSyncValues[(int)teamId ^ 0x1u] = scoreScreen.EncodeScoreParams_Mini(
            fbScores[teamId ^ 0x1u], 0, 0, 0);
#if TKCH_DEBUG_SCORE
        _LogInfo($"  encodeScoreSyncValues = {encodeScoreSyncValues[0]:X8}-{encodeScoreSyncValues[1]:X8}");
#endif
        Array.Copy(encodeScoreSyncValues, networkingManager.scoreSyncRows, networkingManager.scoreSyncRows.Length);
#endif
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
        Debug.Log("[<color=\"#B5438F\">BilliardsModule</color>] " + ln);

        LOG_LINES[LOG_PTR++] = "[<color=\"#B5438F\">BilliardsModule</color>] " + ln + "\n";
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
        string output = "BilliardsModule ";

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
