#define TKCH_ONEPOCKET_SCORE
//#define TKCH_TEAMS_OFF

//#define TKCH_DEBUG_GAMEMODE
//#define TKCH_DEBUG_POINT_POCKET_MARKER

using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class MenuManager : UdonSharpBehaviour
{
    private readonly uint[] TIMER_VALUES = new uint[] { 0, 300, 150, 60 };

    [SerializeField] private GameObject menuBase;
    [SerializeField] public GameObject menuSettings;
    [SerializeField] private GameObject menuStart;
    [SerializeField] private Text[] lobbyNames;

    [SerializeField] private GameObject teamCover;
    [SerializeField] private GameObject timelimitDisplay;

    [SerializeField] public UIButton button8Win;
    [SerializeField] public UIButton button5Win;
    [SerializeField] public UIButton button3Win;
    [SerializeField] public UIButton button3WinVForm;
    [SerializeField] public UIButton button3WinPentagon;
    [SerializeField] public UIButton button3WinPlusDia;
    [SerializeField] public UIButton button8Ball;
    [SerializeField] public UIButton button9Ball;
    [SerializeField] public UIButton button4Ball;
    [SerializeField] public UIButton button4BallJP;
    [SerializeField] public UIButton button4BallKR;
    [SerializeField] public UIButton[] buttonPocketToggles;
    [SerializeField] public UIButton buttonTimerLeft;
    [SerializeField] public UIButton buttonTimerRight;
    [SerializeField] public UIButton buttonTeamsToggle;
    [SerializeField] public UIButton buttonGuidelineToggle;
    [SerializeField] public UIButton buttonLockingToggle;
    [SerializeField] public UIButton buttonBankToggle;

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
        button8Ball.gameObject.SetActive(false);
        button9Ball.gameObject.SetActive(false);
        button4Ball.gameObject.SetActive(false);
        button4BallJP.gameObject.SetActive(false);
        button4BallKR.gameObject.SetActive(false);
        
        table = table_;
        
        _RefreshTimer();
        _RefreshToggleSettings();
        _RefreshGameMode();
        _RefreshLobbyOpen();
        
#if TKCH_TEAMS_OFF
        buttonTeamsToggle.gameObject.SetActive(false);
#endif
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
#if TKCH_DEBUG_GAMEMODE
        table._LogInfo("TKCH MenuManager::_RefreshGameMode()");
#endif
        uint menuGameMode = table.gameModeLocal;

        button8Win._ResetPushButton();
        button5Win._ResetPushButton();
        button3Win._ResetPushButton();
        button8Ball._ResetPushButton();
        button9Ball._ResetPushButton();
        button4Ball._ResetPushButton();
        button4BallJP._ResetPushButton();
        button4BallKR._ResetPushButton();
        button3WinVForm._ResetPushButton();
        button3WinPentagon._ResetPushButton();
        button3WinPlusDia._ResetPushButton();

        switch (menuGameMode)
        {
            case 0:
                button8Ball._SetButtonPushed();
                button4BallJP.gameObject.SetActive(false);
                button4BallKR.gameObject.SetActive(false);
                button3WinVForm.gameObject.SetActive(false);
                button3WinPentagon.gameObject.SetActive(false);
                button3WinPlusDia.gameObject.SetActive(false);
                break;
            case 1:
                button9Ball._SetButtonPushed();
                button4BallJP.gameObject.SetActive(false);
                button4BallKR.gameObject.SetActive(false);
                button3WinVForm.gameObject.SetActive(false);
                button3WinPentagon.gameObject.SetActive(false);
                button3WinPlusDia.gameObject.SetActive(false);
                break;
            case 2:
                button4Ball._SetButtonPushed();
                button4BallJP._SetButtonPushed();
                button4BallJP.gameObject.SetActive(true);
                button4BallKR.gameObject.SetActive(true);
                button3WinVForm.gameObject.SetActive(false);
                button3WinPentagon.gameObject.SetActive(false);
                button3WinPlusDia.gameObject.SetActive(false);
                break;
            case 3:
                button4Ball._SetButtonPushed();
                button4BallKR._SetButtonPushed();
                button4BallJP.gameObject.SetActive(true);
                button4BallKR.gameObject.SetActive(true);
                button3WinVForm.gameObject.SetActive(false);
                button3WinPentagon.gameObject.SetActive(false);
                button3WinPlusDia.gameObject.SetActive(false);
                break;
            case BilliardsModule.GAME_MODE_ONEPOCKET15:
                button8Win._SetButtonPushed();
                button4BallJP.gameObject.SetActive(false);
                button4BallKR.gameObject.SetActive(false);
                button3WinVForm.gameObject.SetActive(false);
                button3WinPentagon.gameObject.SetActive(false);
                button3WinPlusDia.gameObject.SetActive(false);
                break;
            case BilliardsModule.GAME_MODE_ONEPOCKET9:
                button5Win._SetButtonPushed();
                button4BallJP.gameObject.SetActive(false);
                button4BallKR.gameObject.SetActive(false);
                button3WinVForm.gameObject.SetActive(false);
                button3WinPentagon.gameObject.SetActive(false);
                button3WinPlusDia.gameObject.SetActive(false);
                break;
            case BilliardsModule.GAME_MODE_ONEPOCKET5:
                button3Win._SetButtonPushed();
                button4BallJP.gameObject.SetActive(false);
                button4BallKR.gameObject.SetActive(false);
                button3WinVForm.gameObject.SetActive(true);
                button3WinPentagon.gameObject.SetActive(true);
                button3WinPlusDia.gameObject.SetActive(true);
                if (table.rackFormLocal == BilliardsModule.RACK_MODE_5BALL_VFORM)
                {
                    button3WinVForm._SetButtonPushed();
                }
                else if (table.rackFormLocal == BilliardsModule.RACK_MODE_5BALL_PENTAGON)
                {
                    button3WinPentagon._SetButtonPushed();
                }
                else if (table.rackFormLocal == BilliardsModule.RACK_MODE_5BALL_PLUSDIA)
                {
                    button3WinPlusDia._SetButtonPushed();
                }
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

#if TKCH_ONEPOCKET_SCORE
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
#endif

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
#if TKCH_DEBUG_POINT_POCKET_MARKER
        table._LogInfo("TKCH MenuManager::_RefreshToggleSettings()");
#endif
        buttonTeamsToggle._SetButtonToggle(table.teamsLocal);
        buttonGuidelineToggle._SetButtonToggle(!table.noGuidelineLocal);
        buttonLockingToggle._SetButtonToggle(!table.noLockingLocal);
        buttonBankToggle._SetButtonToggle(!table.noBankLocal);

        _RefreshPointPockets();

        _RefreshPlayerList();
    }

    public void _RefreshPointPockets()
    {
#if TKCH_DEBUG_POINT_POCKET_MARKER
        table._LogInfo("TKCH MenuManager::_RefreshPointPockets()");
#endif

        //uint pockets = (table.targetPocketedLocal[1] & table.one_pocket_point_pocket_mask) >> 24;
        uint pockets = table.pointPocketsLocal;
#if TKCH_DEBUG_POINT_POCKET_MARKER
        table._LogInfo($"  pockets = {pockets}");
#endif
        for (int i = 0; i < buttonPocketToggles.Length; i++)
        {
            bool toggle = ((pockets >> i) & 0x1u) != 0;
#if TKCH_DEBUG_POINT_POCKET_MARKER
            table._LogInfo($"  i = {i}, toggle = {toggle}");
#endif
            buttonPocketToggles[i]._SetButtonToggle(toggle);
        }
        table.graphicsManager._UpdatePointPocketMarker(pockets, false);
    }

    public void _RefreshLobbyOpen()
    {
        bool isNormalPlayer = table.localPlayerId != 0;
        button8Win.disableInteractions = isNormalPlayer;
        button5Win.disableInteractions = isNormalPlayer;
        button3Win.disableInteractions = isNormalPlayer;
        button3WinVForm.disableInteractions = isNormalPlayer;
        button3WinPentagon.disableInteractions = isNormalPlayer;
        button3WinPlusDia.disableInteractions = isNormalPlayer;
        button8Ball.disableInteractions = isNormalPlayer;
        button9Ball.disableInteractions = isNormalPlayer;
        button4Ball.disableInteractions = isNormalPlayer;
        button4BallJP.disableInteractions = isNormalPlayer;
        button4BallKR.disableInteractions = isNormalPlayer;
        //buttonPocketToggles[0].disableInteractions = true;
        //buttonPocketToggles[1].disableInteractions = true;
        for (int i = 0; i < buttonPocketToggles.Length; i++)
        {
            buttonPocketToggles[i].disableInteractions = isNormalPlayer;
        }
        buttonTeamsToggle.disableInteractions = isNormalPlayer;
        buttonGuidelineToggle.disableInteractions = isNormalPlayer;
        buttonLockingToggle.disableInteractions = isNormalPlayer;
        buttonBankToggle.disableInteractions = isNormalPlayer;
        buttonTimerLeft.disableInteractions = isNormalPlayer;
        buttonTimerRight.disableInteractions = isNormalPlayer;

        refreshJoinButtons();
        _RefreshToggleSettings();
    }

    [NonSerialized] public UIButton inButton;
    public void _OnButtonPressed() { onButtonPressed(inButton); }
    private void onButtonPressed(UIButton button)
    {
#if TKCH_DEBUG_GAMEMODE || TKCH_DEBUG_POINT_POCKET_MARKER
        table._LogInfo($"TKCH MenuManager::onButtonPressed() gameMode = {table.gameModeLocal}");
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
            else if (button.name == "8Win")
            {
                table._TriggerGameModeChanged(BilliardsModule.GAME_MODE_ONEPOCKET15);
            }
            else if (button.name == "5Win")
            {
                table._TriggerGameModeChanged(BilliardsModule.GAME_MODE_ONEPOCKET9);
            }
            else if (button.name == "3Win" | button.name == "3WinVForm")
            {
                table._TriggerGameModeChanged(BilliardsModule.GAME_MODE_ONEPOCKET5);
                table._TriggerRackFromChanged(BilliardsModule.RACK_MODE_5BALL_VFORM);
            }
            else if (button.name == "3WinPentagon")
            {
                table._TriggerRackFromChanged(BilliardsModule.RACK_MODE_5BALL_PENTAGON);
            }
            else if (button.name == "3WinPlusDia")
            {
                table._TriggerRackFromChanged(BilliardsModule.RACK_MODE_5BALL_PLUSDIA);
            }
            else if (button.name.StartsWith("Pocket") && button.name.EndsWith("Toggle"))
            {
                int pocket = Array.IndexOf(buttonPocketToggles, button);
#if TKCH_DEBUG_POINT_POCKET_MARKER
                table._LogInfo($"  pocket = {pocket}");
#endif
                if (0 <= pocket)
                {
                    if (!table._TriggerPocketChanged(button.toggleState, (uint)pocket))
                    {
                        //button.toggleState = !button.toggleState;
                        _RefreshToggleSettings();
                    }
                }
            }
            else if (button.name == "BankToggle")
            {
                table._TriggerNoBankChanged(!button.toggleState);
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
