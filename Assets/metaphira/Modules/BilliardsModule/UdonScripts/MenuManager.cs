// #define TKCH_DEBUG_TOGGLE

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
    [SerializeField] public UIButton button10Ball;
    [SerializeField] public UIButton button4Ball;
    [SerializeField] public UIButton button4BallJP;
    [SerializeField] public UIButton button4BallKR;
    [SerializeField] public UIButton buttonTimerLeft;
    [SerializeField] public UIButton buttonTimerRight;
    [SerializeField] public UIButton buttonTeamsToggle;
    [SerializeField] public UIButton buttonGuidelineToggle;
    [SerializeField] public UIButton buttonLockingToggle;
    [SerializeField] public UIButton buttonPushOutToggle;
    // [SerializeField] public UIButton buttonBreakingFoulToggle;
    [SerializeField] public UIButton buttonBreakRuleToggle;
    [SerializeField] public UIButton button4BallsInCushionToggle;
    [SerializeField] public UIButton button3PointBreakToggle;
    [SerializeField] public UIButton buttonRackSheet;
    [SerializeField] public UIButton buttonWoodFrame;
    [SerializeField] public UIButton buttonNoCushionFoulToggle;
    [SerializeField] public UIButton buttonCallShotToggle;
    [SerializeField] public UIButton buttonSemiAutoCallToggle;
    [SerializeField] public UIButton buttonCallOverwriteModeToggle;

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

        button8Ball._ResetPushButton();
        button9Ball._ResetPushButton();
        button10Ball._ResetPushButton();
        button4Ball._ResetPushButton();
        button4BallJP._ResetPushButton();
        button4BallKR._ResetPushButton();

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
                button10Ball._SetButtonPushed();
                button4BallJP.gameObject.SetActive(false);
                button4BallKR.gameObject.SetActive(false);
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
#if TKCH_SCORE_SCREEN
            table.scoreScreen.UpdateTeamName(i, teamName);
#endif
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
#if TKCH_DEBUG_TOGGLE
        table._LogInfo("EIJIS_DEBUG MenuManager::_RefreshToggleSettings()");
#endif
        
        buttonTeamsToggle._SetButtonToggle(table.teamsLocal);
        buttonGuidelineToggle._SetButtonToggle(!table.noGuidelineLocal);
        buttonLockingToggle._SetButtonToggle(!table.noLockingLocal);
        buttonPushOutToggle._SetButtonToggle(table.enablePushOutLocal);
        buttonNoCushionFoulToggle._SetButtonToggle(table.noCushionFoulEnableLocal);

        buttonRackSheet._ResetPushButton();
        buttonWoodFrame._ResetPushButton();
        if (table.rackConditionLocal == 0)
        {
            buttonRackSheet._SetButtonPushed();
        }
        else
        {
            buttonWoodFrame._SetButtonPushed();
        }

        // buttonBreakingFoulToggle._SetButtonToggle(table.breakTermsLocal != table.BREAK_TERMS_UNCONDITIONAL);
        buttonBreakRuleToggle._SetButtonToggle(table.breakTermsLocal != table.BREAK_TERMS_UNCONDITIONAL);
        button4BallsInCushionToggle.gameObject.SetActive(table.breakTermsLocal != table.BREAK_TERMS_UNCONDITIONAL);
        button3PointBreakToggle.gameObject.SetActive(table.breakTermsLocal != table.BREAK_TERMS_UNCONDITIONAL);
        button4BallsInCushionToggle._ResetPushButton();
        button3PointBreakToggle._ResetPushButton();
        if (table.breakTermsLocal == table.BREAK_TERMS_4_CUSHION)
        {
            button4BallsInCushionToggle._SetButtonPushed();
        }
        else if (table.breakTermsLocal == table.BREAK_TERMS_3_POINT)
        {
            button3PointBreakToggle._SetButtonPushed();
        }

        buttonCallShotToggle._SetButtonToggle(table.requireCallShotLocal);
        buttonSemiAutoCallToggle.gameObject.SetActive(table.requireCallShotLocal);
        buttonSemiAutoCallToggle._SetButtonToggle(table.requireCallShotLocal && table.semiAutoCallBallLocal && table.semiAutoCallPocketLocal);
        buttonCallOverwriteModeToggle.gameObject.SetActive(table.requireCallShotLocal);
        buttonCallOverwriteModeToggle._SetButtonToggle(table.requireCallShotLocal && table.callShotOprationOverwriteModeLocal);

        _RefreshPlayerList();
    }

    public void _RefreshLobbyOpen()
    {
        bool isNormalPlayer = table.localPlayerId != 0;
        button8Ball.disableInteractions = isNormalPlayer;
        button9Ball.disableInteractions = isNormalPlayer;
        button10Ball.disableInteractions = isNormalPlayer;
        button4Ball.disableInteractions = isNormalPlayer;
        button4BallJP.disableInteractions = isNormalPlayer;
        button4BallKR.disableInteractions = isNormalPlayer;
        buttonTeamsToggle.disableInteractions = isNormalPlayer;
        buttonGuidelineToggle.disableInteractions = isNormalPlayer;
        buttonLockingToggle.disableInteractions = isNormalPlayer;
        buttonTimerLeft.disableInteractions = isNormalPlayer;
        buttonTimerRight.disableInteractions = isNormalPlayer;
        buttonPushOutToggle.disableInteractions = isNormalPlayer;
        // buttonBreakingFoulToggle.disableInteractions = isNormalPlayer;
        buttonBreakRuleToggle.disableInteractions = isNormalPlayer;
        button4BallsInCushionToggle.disableInteractions = isNormalPlayer;
        button3PointBreakToggle.disableInteractions = isNormalPlayer;
        buttonRackSheet.disableInteractions = isNormalPlayer;
        buttonWoodFrame.disableInteractions = isNormalPlayer;
        buttonNoCushionFoulToggle.disableInteractions = isNormalPlayer;
        buttonCallShotToggle.disableInteractions = isNormalPlayer;
        buttonSemiAutoCallToggle.disableInteractions = isNormalPlayer;
        buttonCallOverwriteModeToggle.disableInteractions = isNormalPlayer;

        refreshJoinButtons();
        _RefreshToggleSettings();
    }

    [NonSerialized] public UIButton inButton;
    public void _OnButtonPressed() { onButtonPressed(inButton); }
    private void onButtonPressed(UIButton button)
    {
#if TKCH_DEBUG_TOGGLE
        table._LogInfo($"EIJIS_DEBUG MenuManager::onButtonPressed( button = {button.name})");
#endif
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
            else if (button.name == "10Ball")
            {
                table._TriggerGameModeChanged(4);
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
            else if (button.name == "NoCushionFoulToggle")
            {
                table._TriggerNoCushionFoulChanged(button.toggleState);
            }
            else if (button.name == "PushOutToggle")
            {
                table._TriggerEnablePushOutChanged(button.toggleState);
            }
            // else if (button.name == "BreakingFoulToggle")
            // {
            //     table._TriggerBreakTermsChanged(button.toggleState ? table.BREAK_TERMS_4_CUSHION : table.BREAK_TERMS_UNCONDITIONAL);
            // }
            else if (button.name == "BreakRuleToggle")
            {
                table._TriggerBreakTermsChanged(button.toggleState ? table.BREAK_TERMS_4_CUSHION : table.BREAK_TERMS_UNCONDITIONAL);
            }
            else if (button.name == "4BallsInCushionToggle")
            {
                table._TriggerBreakTermsChanged(table.BREAK_TERMS_4_CUSHION);
            }
            else if (button.name == "3PointBreakToggle")
            {
                table._TriggerBreakTermsChanged(table.BREAK_TERMS_3_POINT);
            }
            else if (button.name == "RackSheetToggle")
            {
#if TKCH_DEBUG_TOGGLE
                table._LogInfo($"  name = {button.name}, toggleState = {button.toggleState}");
#endif
                table._TriggerRackConditionChanged(0);
            }
            else if (button.name == "WoodFrameToggle")
            {
#if TKCH_DEBUG_TOGGLE
                table._LogInfo($"  name = {button.name}, toggleState = {button.toggleState}");
#endif
                table._TriggerRackConditionChanged(1);
            }
            else if (button.name == "CallShotToggle")
            {
                table._TriggerRequireCallShotChanged(button.toggleState);
            }
            else if (button.name == "SemiAutoCallToggle")
            {
                table._TriggerSemiAutoCallChanged(button.toggleState);
            }
            else if (button.name == "CallOverwriteModeToggle")
            {
                table._TriggerCallOperationOverwriteModeChanged(button.toggleState);
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
