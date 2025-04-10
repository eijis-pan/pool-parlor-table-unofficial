//#define TKCH_DEBUG_TOGGLE
// #define TKCH_DEBUG_INITIAL_STATE

using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class MenuManager : UdonSharpBehaviour
{
    private readonly uint[] TIMER_VALUES = new uint[] { 0, 60, 30, 15 };

    [SerializeField] private GameObject menuBase;
    [SerializeField] public GameObject menuSettings;
    [SerializeField] private GameObject menuStart;
    [SerializeField] private Text[] lobbyNames;

    [SerializeField] private GameObject teamCover;
    [SerializeField] private GameObject timelimitDisplay;

    [SerializeField] public UIButton button8Ball;
    [SerializeField] public UIButton button9Ball;
    [SerializeField] public UIButton button4Ball;
    [SerializeField] public UIButton button4BallJP;
    [SerializeField] public UIButton button4BallKR;
    [SerializeField] public UIButton button40Win;
    [SerializeField] public UIButton[] button60Win;
    [SerializeField] public UIButton[] button90Win;
    [SerializeField] public UIButton[] button120Win;
    [SerializeField] public UIButton[] button180Win;
    [SerializeField] public UIButton button240Win;
    [SerializeField] public UIButton buttonTimerLeft;
    [SerializeField] public UIButton buttonTimerRight;
    [SerializeField] public UIButton buttonTeamsToggle;
    [SerializeField] public UIButton buttonGuidelineToggle;
    [SerializeField] public UIButton buttonLockingToggle;
    [SerializeField] public UIButton button6BallsToggle;
    [SerializeField] public UIButton button9BallsToggle;
    [SerializeField] public UIButton button10BallsToggle;
    [SerializeField] public UIButton button15BallsToggle;
    [SerializeField] public UIButton buttonRackSheetToggle;
    [SerializeField] public UIButton buttonWoodFrameToggle;
    [SerializeField] public UIButton buttonPushOutToggle;
    [SerializeField] public UIButton buttonCallShotToggle;
    // [SerializeField] public UIButton buttonSemiAutoCallBallToggle;
    // [SerializeField] public UIButton buttonSemiAutoCallPocketToggle;
    [SerializeField] public UIButton buttonSemiAutoCallToggle;
    [SerializeField] public UIButton buttonCallPassOptionToggle;

    [SerializeField] public UIButton buttonLeave;
    [SerializeField] public UIButton buttonPlay;
    [SerializeField] public UIButton buttonJoinOrange;
    [SerializeField] public UIButton buttonJoinBlue;

    [SerializeField] public GameObject rotation15ButtonsGroup;
    [SerializeField] public GameObject rotation9ButtonsGroup;
    [SerializeField] public GameObject rotation6ButtonsGroup;

    private BilliardsModule table;

    private uint selectedTimer;
    private uint selectedTimerPrev;
    private bool timerSpinPlaying;

    public void _Init(BilliardsModule table_)
    {
#if TKCH_DEBUG_INITIAL_STATE
        table_._LogInfo("TKCH MenuManager::_Init()");
#endif
        table = table_;
        
        for (int i = 0; i < button120Win.Length; i++) button120Win[i]._Init();
        
        _RefreshTimer();
        _RefreshToggleSettings();
        _RefreshGameMode();
        _RefreshLobbyOpen();
    }

    public void _Tick()
    {
        if (table.gameLive) return;
        
        // animate team cover
        teamCover.transform.localScale = Vector3.Lerp(teamCover.transform.localScale, table.teamsLocal ? new Vector3(0, 1, 1) : new Vector3(1, 1, 1), Time.deltaTime * 5.0f);

        // animate menu swap
        menuSettings.transform.localScale = Vector3.Lerp(menuSettings.transform.localScale, table.lobbyOpen ? Vector3.one : Vector3.zero, Time.deltaTime * 5.0f);
        menuStart.transform.localScale = Vector3.one - menuSettings.transform.localScale;

        // animate timer slider
        float targetPosition = -0.128f * selectedTimer;
        Vector3 position = timelimitDisplay.transform.localPosition;
        position.x = Mathf.Lerp(position.x, targetPosition, Time.deltaTime * 5.0f);
        timelimitDisplay.transform.localPosition = position;
        if (timerSpinPlaying && Mathf.Abs(targetPosition - position.x) < 0.01f)
        {
            timerSpinPlaying = false;
            table.aud_main.PlayOneShot(table.snd_spinstop);
        }
    }
    
    // View gamemode changes
    public void _RefreshGameMode()
    {
#if TKCH_DEBUG_INITIAL_STATE
        table._LogInfo($"TKCH MenuManager::_RefreshGameMode() gameModeLocal = {table.gameModeLocal}, goalPointsLocal = {table.goalPointsLocal}");
        table._LogInfo($"  button120Win.toggleState = {button120Win.toggleState}");
#endif
        uint menuGameMode = table.gameModeLocal;

        button8Ball._ResetPushButton();
        button9Ball._ResetPushButton();
        button4Ball._ResetPushButton();
        button4BallJP._ResetPushButton();
        button4BallKR._ResetPushButton();
        button40Win._ResetPushButton();
        for (int i = 0; i < button60Win.Length; i++) button60Win[i]._ResetPushButton();
        for (int i = 0; i < button90Win.Length; i++) button90Win[i]._ResetPushButton();
        for (int i = 0; i < button120Win.Length; i++) button120Win[i]._ResetPushButton();
        for (int i = 0; i < button180Win.Length; i++) button180Win[i]._ResetPushButton();
        button240Win._ResetPushButton();
        
        rotation15ButtonsGroup.SetActive(false);
        rotation9ButtonsGroup.SetActive(false);
        rotation6ButtonsGroup.SetActive(false);

        switch (menuGameMode)
        {
            case 0:
                button8Ball._SetButtonPushed();
                button4BallJP.gameObject.SetActive(false);
                button4BallKR.gameObject.SetActive(false);
                break;
            case 1:
                button9Ball._SetButtonPushed();
                button4BallJP.gameObject.SetActive(false);
                button4BallKR.gameObject.SetActive(false);
                break;
            case 2:
                button4Ball._SetButtonPushed();
                button4BallJP._SetButtonPushed();
                button4BallJP.gameObject.SetActive(true);
                button4BallKR.gameObject.SetActive(true);
                break;
            case 3:
                button4Ball._SetButtonPushed();
                button4BallKR._SetButtonPushed();
                button4BallJP.gameObject.SetActive(true);
                button4BallKR.gameObject.SetActive(true);
                break;
            default:
                switch (menuGameMode)
                {
                    case BilliardsModule.GAMEMODE_ROTATION_15:
                        rotation15ButtonsGroup.SetActive(true);
                        break;
                    case BilliardsModule.GAMEMODE_ROTATION_10:
                    case BilliardsModule.GAMEMODE_ROTATION_9:
                        rotation9ButtonsGroup.SetActive(true);
                        break;
                    case BilliardsModule.GAMEMODE_ROTATION_6:
                        rotation6ButtonsGroup.SetActive(true);
                        break;
                }
                refreshGoalPoint();
                button4BallJP.gameObject.SetActive(false);
                button4BallKR.gameObject.SetActive(false);
                break;
        }
    }

    public void refreshGoalPoint()
    {
        int goalPoints = table.goalPointsLocal;
        switch (goalPoints)
        {
            case 40:
                button40Win._SetButtonPushed();
                break;
            case 60:
                for (int i = 0; i < button60Win.Length; i++) button60Win[i]._SetButtonPushed();
                break;
            case 90:
                for (int i = 0; i < button90Win.Length; i++) button90Win[i]._SetButtonPushed();
                break;
            case 120:
                for (int i = 0; i < button120Win.Length; i++) button120Win[i]._SetButtonPushed();
                break;
            case 180:
                for (int i = 0; i < button180Win.Length; i++) button180Win[i]._SetButtonPushed();
                break;
            case 240:
                button240Win._SetButtonPushed();
                break;
        }
        table.scoreScreen.UpdateGameNameWithNumber("Rotation", goalPoints);
    }

    private void refreshJoinButtons()
    {
        buttonJoinOrange._ResetButton();
        buttonJoinBlue._ResetButton();
        buttonPlay._ResetButton();
        buttonLeave._ResetButton();

        if (table.lobbyOpen)
        {
            // If in the game
            if (table.localPlayerId >= 0)
            {
                buttonJoinOrange.gameObject.SetActive(false);
                buttonJoinBlue.gameObject.SetActive(false);

                // put the leave button where the join button for our team is
                if (table.localTeamId == 0)
                {
                    buttonLeave.transform.localPosition = buttonJoinOrange.transform.localPosition;
                }
                else
                {
                    buttonLeave.transform.localPosition = buttonJoinBlue.transform.localPosition;
                }
                buttonLeave._ResetPosition();
                buttonLeave.gameObject.SetActive(true);

                // host can also start the game
                buttonPlay.gameObject.SetActive(table.localPlayerId == 0);
            }
            else // Otherwise, its just join buttons
            {
                buttonPlay.gameObject.SetActive(false);
                buttonLeave.gameObject.SetActive(false);

                buttonJoinOrange.gameObject.SetActive(table.playerNamesLocal[0] == "" || (table.teamsLocal && table.playerNamesLocal[2] == ""));
                buttonJoinBlue.gameObject.SetActive(table.playerNamesLocal[1] == "" || (table.teamsLocal && table.playerNamesLocal[3] == ""));
            }
        }
        else
        {
            buttonJoinOrange.gameObject.SetActive(false);
            buttonJoinBlue.gameObject.SetActive(false);
            buttonPlay.gameObject.SetActive(false);
            buttonLeave.gameObject.SetActive(false);
        }
    }

    public void _RefreshPlayerList()
    {
        for (int i = 0; i < (table.teamsLocal ? 4 : 2); i++)
        {
            lobbyNames[i].text = table.graphicsManager._FormatName(table.playerNamesLocal[i]);
        }

        for (int i = 0; i < 2; i++)
        {
            string teamName = i == 0 ? "[Orange]" : "[Blue]";
            string name = table.playerNamesLocal[i];
            if (name != "")
            {
                teamName = name;
            }
            if (table.teamsLocal)
            {
                name = table.playerNamesLocal[i + 2];
                if (name != "")
                {
                    teamName += "\n" + name;
                }
            }
            table.scoreScreen.UpdateTeamName(i, teamName);
        }

        refreshJoinButtons();
    }

    public void _RefreshTimer()
    {
        int index = Array.IndexOf(TIMER_VALUES, table.timerLocal);
        selectedTimer = index == -1 ? 0 : (uint)index;

        if (selectedTimerPrev != selectedTimer)
        {
            selectedTimerPrev = selectedTimer;
            timerSpinPlaying = true;
            table.aud_main.PlayOneShot(table.snd_spin);
        }
    }

    public void _RefreshToggleSettings()
    {
        buttonTeamsToggle._SetButtonToggle(table.teamsLocal);
        buttonGuidelineToggle._SetButtonToggle(!table.noGuidelineLocal);
        buttonLockingToggle._SetButtonToggle(!table.noLockingLocal);
        button6BallsToggle._ResetPushButton();
        button9BallsToggle._ResetPushButton();
        button10BallsToggle._ResetPushButton();
        button15BallsToggle._ResetPushButton();
        buttonRackSheetToggle._ResetPushButton();
        buttonWoodFrameToggle._ResetPushButton();
        buttonPushOutToggle._SetButtonToggle(table.enablePushOutLocal);
        buttonCallShotToggle._SetButtonToggle(table.requireCallShotLocal);
        // buttonSemiAutoCallBallToggle._SetButtonToggle(table.semiAutoCallBallLocal);
        // buttonSemiAutoCallPocketToggle._SetButtonToggle(table.semiAutoCallPocketLocal);
        buttonSemiAutoCallToggle.gameObject.SetActive(table.requireCallShotLocal);
        buttonSemiAutoCallToggle._SetButtonToggle(table.requireCallShotLocal && table.semiAutoCallLocal);
        buttonCallPassOptionToggle.gameObject.SetActive(table.requireCallShotLocal);
        buttonCallPassOptionToggle._SetButtonToggle(table.requireCallShotLocal && table.callPassOptionLocal);

        if (table.gameModeLocal == BilliardsModule.GAMEMODE_ROTATION_15)
        {
            button15BallsToggle._SetButtonPushed();
        }
        else if (table.gameModeLocal == BilliardsModule.GAMEMODE_ROTATION_10)
        {
            button10BallsToggle._SetButtonPushed();
        }
        else if (table.gameModeLocal == BilliardsModule.GAMEMODE_ROTATION_9)
        {
            button9BallsToggle._SetButtonPushed();
        }
        else if (table.gameModeLocal == BilliardsModule.GAMEMODE_ROTATION_6)
        {
            button6BallsToggle._SetButtonPushed();
        }
        
        if (table.rackConditionLocal == 0)
        {
            buttonRackSheetToggle._SetButtonPushed();
        }
        else
        {
            buttonWoodFrameToggle._SetButtonPushed();
        }

        _RefreshPlayerList();
    }

    public void _RefreshLobbyOpen()
    {
        bool isNormalPlayer = table.localPlayerId != 0;
        button8Ball.disableInteractions = isNormalPlayer;
        button9Ball.disableInteractions = isNormalPlayer;
        button4Ball.disableInteractions = isNormalPlayer;
        button4BallJP.disableInteractions = isNormalPlayer;
        button4BallKR.disableInteractions = isNormalPlayer;
        button40Win.disableInteractions = isNormalPlayer;
        for (int i = 0; i < button60Win.Length; i++) button60Win[i].disableInteractions = isNormalPlayer;
        for (int i = 0; i < button90Win.Length; i++) button90Win[i].disableInteractions = isNormalPlayer;
        for (int i = 0; i < button120Win.Length; i++) button120Win[i].disableInteractions = isNormalPlayer;
        for (int i = 0; i < button180Win.Length; i++) button180Win[i].disableInteractions = isNormalPlayer;
        button240Win.disableInteractions = isNormalPlayer;
        buttonTeamsToggle.disableInteractions = isNormalPlayer;
        buttonGuidelineToggle.disableInteractions = isNormalPlayer;
        buttonLockingToggle.disableInteractions = isNormalPlayer;
        buttonTimerLeft.disableInteractions = isNormalPlayer;
        buttonTimerRight.disableInteractions = isNormalPlayer;
        button6BallsToggle.disableInteractions = isNormalPlayer;
        button9BallsToggle.disableInteractions = isNormalPlayer;
        button10BallsToggle.disableInteractions = isNormalPlayer;
        button15BallsToggle.disableInteractions = isNormalPlayer;
        buttonRackSheetToggle.disableInteractions = isNormalPlayer;
        buttonWoodFrameToggle.disableInteractions = isNormalPlayer;
        buttonPushOutToggle.disableInteractions = isNormalPlayer;
        buttonCallShotToggle.disableInteractions = isNormalPlayer;
        // buttonSemiAutoCallBallToggle.disableInteractions = isNormalPlayer;
        // buttonSemiAutoCallPocketToggle.disableInteractions = isNormalPlayer;
        buttonSemiAutoCallToggle.disableInteractions = isNormalPlayer;
        buttonCallPassOptionToggle.disableInteractions = isNormalPlayer;

        refreshJoinButtons();
        _RefreshToggleSettings();
    }

    [NonSerialized] public UIButton inButton;
    public void _OnButtonPressed() { onButtonPressed(inButton); }
    private void onButtonPressed(UIButton button)
    {
        if (button.name == "StartButton")
        {
            table._TriggerLobbyOpen();
            table._TriggerJoinTeam(0);
        }
        else if (button.name == "JoinOrange")
        {
            table._TriggerJoinTeam(0);
        }
        else if (button.name == "JoinBlue")
        {
            table._TriggerJoinTeam(1);
        }
        else if (button.name == "LeaveButton")
        {
            // Close lobby
            if (table.localPlayerId == 0)
            {
                table._TriggerLobbyClosed();
            }
            else
            {
                table._TriggerLeaveLobby();
            }
        }
        else if (table.localPlayerId == 0)
        {
            if (button.name == "PlayButton")
            {
                table._TriggerGameStart();
            }
            else if (button.name == "8Ball")
            {
                table._TriggerGameModeChanged(0);
            }
            else if (button.name == "9Ball")
            {
                table._TriggerGameModeChanged(1);
            }
            else if (button.name == "4Ball" || button.name == "4BallJP")
            {
                table._TriggerGameModeChanged(2);
            }
            else if (button.name == "4BallKR")
            {
                table._TriggerGameModeChanged(3);
            }
            else if (button.name == "40Win")
            {
                table._TriggerGoalPointsChanged(40);
            }
            else if (button.name == "60Win")
            {
                table._TriggerGoalPointsChanged(60);
            }
            else if (button.name == "90Win")
            {
                table._TriggerGoalPointsChanged(90);
            }
            else if (button.name == "120Win")
            {
                table._TriggerGoalPointsChanged(120);
            }
            else if (button.name == "180Win")
            {
                table._TriggerGoalPointsChanged(180);
            }
            else if (button.name == "240Win")
            {
                table._TriggerGoalPointsChanged(240);
            }
            else if (button.name == "TeamsToggle")
            {
                table._TriggerTeamsChanged(button.toggleState);
            }
            else if (button.name == "GuidelineToggle")
            {
                table._TriggerNoGuidelineChanged(!button.toggleState);
            }
            else if (button.name == "LockingToggle")
            {
                table._TriggerNoLockingChanged(!button.toggleState);
            }
            else if (button.name == "15Balls")
            {
                table._TriggerGameModeChanged(BilliardsModule.GAMEMODE_ROTATION_15);
            }
            else if (button.name == "10Balls")
            {
                table._TriggerGameModeChanged(BilliardsModule.GAMEMODE_ROTATION_10);
            }
            else if (button.name == "9Balls")
            {
                table._TriggerGameModeChanged(BilliardsModule.GAMEMODE_ROTATION_9);
            }
            else if (button.name == "6Balls")
            {
                table._TriggerGameModeChanged(BilliardsModule.GAMEMODE_ROTATION_6);
            }
            else if (button.name == "RackSheetToggle")
            {
#if TKCH_DEBUG_TOGGLE
                table._LogInfo($"  name = {button.name}, toggleState = {button.toggleState}");
#endif
                table._TriggerRackCondisionChanged(0);
            }
            else if (button.name == "WoodFrameToggle")
            {
#if TKCH_DEBUG_TOGGLE
                table._LogInfo($"  name = {button.name}, toggleState = {button.toggleState}");
#endif
                table._TriggerRackCondisionChanged(1);
            }
            else if (button.name == "PushOutToggle")
            {
                table._TriggerEnablePushOutChanged(button.toggleState);
            }
            else if (button.name == "CallShotToggle")
            {
                table._TriggerRequireCallShotChanged(button.toggleState);
            }
            // else if (button.name == "SemiAutoCallBallToggle")
            // {
            //     table._TriggerSemiAutoCallBallChanged(button.toggleState);
            // }
            // else if (button.name == "SemiAutoCallPocketToggle")
            // {
            //     table._TriggerSemiAutoCallPocketChanged(button.toggleState);
            // }
            else if (button.name == "SemiAutoCallToggle")
            {
                table._TriggerSemiAutoCallChanged(button.toggleState);
            }
            else if (button.name == "CallPassOptionToggle")
            {
                table._TriggerCallPassOptionChanged(button.toggleState);
            }
            else if (button.name == "TimeRight")
            {
                if (selectedTimer > 0)
                {
                    selectedTimer--;

                    table._TriggerTimerChanged(TIMER_VALUES[selectedTimer]);
                }
            }
            else if (button.name == "TimeLeft")
            {
                if (selectedTimer < 3)
                {
                    selectedTimer++;

                    table._TriggerTimerChanged(TIMER_VALUES[selectedTimer]);
                }
            }
        }
    }
    
    private void joinTeam(int id)
    {
        // Create new lobby
        if (!table.lobbyOpen)
        {
            table._TriggerLobbyOpen();
        }

        table._LogInfo("joining table on team " + id);

        if (table.localPlayerId == -1)
        {
            table._TriggerJoinTeam(id);
        }
    }

    public void _EnableMenu()
    {
        menuBase.SetActive(true);
    }

    public void _DisableMenu()
    {
        menuBase.SetActive(false);
    }
}
