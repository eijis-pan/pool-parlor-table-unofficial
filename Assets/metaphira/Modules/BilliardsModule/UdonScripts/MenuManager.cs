//#define TKCH_DEBUG_TOGGLE

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
    [SerializeField] public UIButton button60Win;
    [SerializeField] public UIButton button90Win;
    [SerializeField] public UIButton button120Win;
    [SerializeField] public UIButton button180Win;
    [SerializeField] public UIButton button240Win;
    [SerializeField] public UIButton buttonTimerLeft;
    [SerializeField] public UIButton buttonTimerRight;
    [SerializeField] public UIButton buttonTeamsToggle;
    [SerializeField] public UIButton buttonGuidelineToggle;
    [SerializeField] public UIButton buttonLockingToggle;
    [SerializeField] public UIButton buttonRackSheet;
    [SerializeField] public UIButton buttonWoodFrame;
    // [SerializeField] public UIButton buttonSemiAutoCallBallToggle;
    // [SerializeField] public UIButton buttonSemiAutoCallPocketToggle;
    [SerializeField] public UIButton buttonSemiAutoCallToggle;
    [SerializeField] public UIButton buttonCallPassOptionToggle;

    [SerializeField] public UIButton buttonLeave;
    [SerializeField] public UIButton buttonPlay;
    [SerializeField] public UIButton buttonJoinOrange;
    [SerializeField] public UIButton buttonJoinBlue;

    private BilliardsModule table;

    private uint selectedTimer;
    private uint selectedTimerPrev;
    private bool timerSpinPlaying;

    public void _Init(BilliardsModule table_)
    {
        table = table_;
        
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
        uint menuGameMode = table.gameModeLocal;
        int goalPoints = table.goalPointsLocal;

        button8Ball._ResetPushButton();
        button9Ball._ResetPushButton();
        button4Ball._ResetPushButton();
        button4BallJP._ResetPushButton();
        button4BallKR._ResetPushButton();
        button60Win._ResetPushButton();
        button90Win._ResetPushButton();
        button120Win._ResetPushButton();
        button180Win._ResetPushButton();
        button240Win._ResetPushButton();

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
            case 4:
                switch (goalPoints)
                {
                    case 60:
                        button60Win._SetButtonPushed();
                        break;
                    case 90:
                        button90Win._SetButtonPushed();
                        break;
                    case 120:
                        button120Win._SetButtonPushed();
                        break;
                    case 180:
                        button180Win._SetButtonPushed();
                        break;
                    case 240:
                        button240Win._SetButtonPushed();
                        break;
                }
                button4BallJP.gameObject.SetActive(false);
                button4BallKR.gameObject.SetActive(false);
                table.scoreScreen.UpdateGameNameWithNumber("Rotation", goalPoints);
                break;
        }
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
        buttonRackSheet._ResetPushButton();
        buttonWoodFrame._ResetPushButton();
        // buttonSemiAutoCallBallToggle._SetButtonToggle(table.semiAutoCallBallLocal);
        // buttonSemiAutoCallPocketToggle._SetButtonToggle(table.semiAutoCallPocketLocal);
        buttonSemiAutoCallToggle._SetButtonToggle(table.semiAutoCallLocal);
        buttonCallPassOptionToggle._SetButtonToggle(table.callPassOptionLocal);
        if (table.rackConditionLocal == 0)
        {
            buttonRackSheet._SetButtonPushed();
        }
        else
        {
            buttonWoodFrame._SetButtonPushed();
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
        button60Win.disableInteractions = isNormalPlayer;
        button90Win.disableInteractions = isNormalPlayer;
        button120Win.disableInteractions = isNormalPlayer;
        button180Win.disableInteractions = isNormalPlayer;
        button240Win.disableInteractions = isNormalPlayer;
        buttonTeamsToggle.disableInteractions = isNormalPlayer;
        buttonGuidelineToggle.disableInteractions = isNormalPlayer;
        buttonLockingToggle.disableInteractions = isNormalPlayer;
        buttonTimerLeft.disableInteractions = isNormalPlayer;
        buttonTimerRight.disableInteractions = isNormalPlayer;
        buttonRackSheet.disableInteractions = isNormalPlayer;
        buttonWoodFrame.disableInteractions = isNormalPlayer;
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
            else if (button.name == "RackSheet")
            {
#if TKCH_DEBUG_TOGGLE
                table._LogInfo($"  name = {button.name}, toggleState = {button.toggleState}");
#endif
                table._TriggerRackCondisionChanged(0);
            }
            else if (button.name == "WoodFrame")
            {
#if TKCH_DEBUG_TOGGLE
                table._LogInfo($"  name = {button.name}, toggleState = {button.toggleState}");
#endif
                table._TriggerRackCondisionChanged(1);
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
