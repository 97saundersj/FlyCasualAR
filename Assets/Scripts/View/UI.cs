﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GameModes;
using UnityEngine.EventSystems;
using SquadBuilderNS;
using BoardTools;
using GameCommands;
using System;
using AR;
//Todo: Move to different scripts by menu names

public class UI : MonoBehaviour {

    private static float lastLogTextPosition = -5;
    private static float lastLogTextStep = -20;

    private int minimapSize = 256;
    private bool IsStatBlockVisible;

    public static bool ShowShipIds;

    public void Update()
    {
        UpdateShipIds();
        CheckSwarmManager();
    }

    private void UpdateShipIds()
    {
        ShowShipIds = Input.GetKey(KeyCode.LeftAlt);
    }

    private void CheckSwarmManager()
    {
        SwarmManager.CheckActivation();
    }

    //Move to context menu
    public static void CallContextMenu(Ship.GenericShip ship)
    {
        ShowContextMenu(ship, Input.mousePosition + new Vector3(0f, 0f, 0f));
    }

    private static void ShowContextMenu(Ship.GenericShip ship, Vector3 position)
    {
        HideDirectionMenu();
        HideContextMenuButtons();
        if (Phases.CurrentSubPhase.CountActiveButtons(ship) > 0)
        {
            GameObject.Find("UI").transform.Find("ContextMenuPanel").gameObject.SetActive(true);
            position = FixMenuPosition(GameObject.Find("UI").transform.Find("ContextMenuPanel").gameObject, position);
            GameObject.Find("UI").transform.Find("ContextMenuPanel").position = position;
        }
        else
        {
            GameObject.Find("UI").transform.Find("ContextMenuPanel").gameObject.SetActive(false);
        }
    }

    private static void HideContextMenuButtons()
    {
        foreach (Transform button in GameObject.Find("UI").transform.Find("ContextMenuPanel"))
        {
            button.gameObject.SetActive(false);
        }
    }

    public static void HideContextMenu()
    {
        GameObject.Find("UI").transform.Find("ContextMenuPanel").gameObject.SetActive(false);
    }

    public static void HideDirectionMenu()
    {
        DirectionsMenu.Hide();
    }

    public static void HideTemporaryMenus()
    {
        HideContextMenu();
    }

    //TODO: use in static generic UI class
    private static Vector3 FixMenuPosition(GameObject menuPanel, Vector3 position) {
        if (position.x + menuPanel.GetComponent<RectTransform>().rect.width > Screen.width) {
            position = new Vector3(Screen.width - menuPanel.GetComponent<RectTransform>().rect.width - 5, position.y, 0);
        }
        if (position.y - menuPanel.GetComponent<RectTransform>().rect.height < 0)
        {
            position = new Vector3(position.x, menuPanel.GetComponent<RectTransform>().rect.height + 5, 0);
        }
        return position;
    }

    public static void ShowGameResults(string results)
    {
        GameObject gameResultsPanel = GameObject.Find("UI").transform.Find("GameResultsPanel").gameObject;
        gameResultsPanel.transform.Find("Panel").transform.Find("Congratulations").GetComponent<Text>().text = results;
        gameResultsPanel.transform.Find("Panel").Find("Restart").gameObject.SetActive(!(GameMode.CurrentGameMode is NetworkGame));

        RectTransform rectTransform = gameResultsPanel.transform.Find("Panel").GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 350);

        gameResultsPanel.SetActive(true);

        CheckBatchMode(results);
    }

    private static void CheckBatchMode(string results)
    {
        if (DebugManager.BatchAiSquadTestingModeActive)
        {
            RestartMatchStatic();
        }
    }

    public static void ToggleInGameMenu()
    {
        GameObject gameResultsPanel = GameObject.Find("UI").transform.Find("GameResultsPanel").gameObject;
        gameResultsPanel.transform.Find("Panel").Find("Restart").gameObject.SetActive(!(GameMode.CurrentGameMode is NetworkGame));

        RectTransform rectTransform = gameResultsPanel.transform.Find("Panel").GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 170);

        gameResultsPanel.SetActive(!gameResultsPanel.activeSelf);
    }

    public void CallChangeMiniMapSize()
    {
        switch (minimapSize)
        {
            case 128: minimapSize = 256;
                break;
            case 256: minimapSize = 512;
                break;
            case 512: minimapSize = 128;
                break;
            default:
                break;
        }
        GameObject.Find("UI").transform.Find("MiniMapHolder").GetComponent<RectTransform>().sizeDelta = new Vector2(minimapSize, minimapSize);
    }

    public void ToggleMinimap()
    {
        GameObject.Find("UI").transform.Find("GameLogHolder").gameObject.SetActive(false);
        GameObject.Find("UI").transform.Find("MiniMapHolder").gameObject.SetActive(!GameObject.Find("UI").transform.Find("MiniMapHolder").gameObject.activeSelf);
    }

    public void ToggleControls()
    {
        if (CameraScript.InputTouchIsEnabled)
        {
            CameraScript.InputMouseIsEnabled = true;
        }
        else
        {
            CameraScript.InputTouchIsEnabled = true;
        }
        UpdateControlsButtonName();
    }

    public static void UpdateControlsButtonName()
    {
        GameObject.Find("UI/ToolsPanel/ControlsButton").GetComponentInChildren<Text>().text = "Controls: " + ((CameraScript.InputTouchIsEnabled) ? "Touch" : "Mouse");
    }

    public void ToggleGameLog()
    {
        GameObject.Find("UI").transform.Find("MiniMapHolder").gameObject.SetActive(false);
        GameObject.Find("UI").transform.Find("GameLogHolder").gameObject.SetActive(!GameObject.Find("UI").transform.Find("GameLogHolder").gameObject.activeSelf);
    }

    public void ToggleViewMode()
    {
        CameraScript.ToggleMode();
    }

    public void ToggleStatsView()
    {
        RectTransform statsGoRect = GameObject.Find("UI").transform.Find("StatsHolder").gameObject.GetComponent<RectTransform>();
        int modifier = (IsStatBlockVisible) ? -1 : +1;
        IsStatBlockVisible = !IsStatBlockVisible;
        statsGoRect.localPosition += new Vector3(0, modifier * 525, 0);
    }

    public static void AddTestLogEntry(string text)
    {
        if (GameObject.Find("UI").transform.Find("GameLogHolder") != null)
        {
            GameObject area = GameObject.Find("UI").transform.Find("GameLogHolder").Find("Scroll").Find("Viewport").Find("Content").gameObject;
            GameObject logText = (GameObject)Resources.Load("Prefabs/LogText", typeof(GameObject));
            GameObject newLogEntry = Instantiate(logText, area.transform);
            newLogEntry.transform.localPosition = new Vector3(5, lastLogTextPosition, 0);
            lastLogTextPosition += lastLogTextStep;
            if (area.GetComponent<RectTransform>().sizeDelta.y < Mathf.Abs(lastLogTextPosition)) area.GetComponent<RectTransform>().sizeDelta = new Vector2(area.GetComponent<RectTransform>().sizeDelta.x, Mathf.Abs(lastLogTextPosition));
            GameObject.Find("UI").transform.Find("GameLogHolder").Find("Scroll").GetComponent<ScrollRect>().verticalNormalizedPosition = 0;
            newLogEntry.GetComponent<Text>().text = text;
        }
    }

    public void CompleteARPlacement()
    {
        GameObject.Find("UI/ARPanel").SetActive(false);
        GameObject.Find("AR Session").GetComponent<XRBoardPlacer>().SetPlacementConfirmed(true);

        GameObject.Find("SceneHolder").transform.Find("Board").Find("ObstaclesZone").Find("ObstaclesZoneHighlight").gameObject.SetActive(true);
        GameObject.Find("UI/DecisionPanelHolder").transform.Find("DecisionsPanel").gameObject.SetActive(true);
    }

    public void ShowDecisionsPanel()
    {
        //start subphase

        GameObject.Find("UI").transform.Find("DecisionsPanel").gameObject.SetActive(true);
    }

    public void HideDecisionsPanel()
    {
        GameObject.Find("UI").transform.Find("DecisionsPanel").gameObject.SetActive(false);
        //end subphase
    }

    public static void CallClickNextPhase()
    {
        HideNextButton();
        Roster.AllShipsHighlightOff();

        GameMode.CurrentGameMode.ExecuteCommand(GenerateNextButtonCommand());
    }

    public void ClickNextPhase()
    {
        Phases.CurrentSubPhase.NextButtonLocal();
        CallClickNextPhase();
    }

    public static GameCommand GenerateNextButtonCommand()
    {
        return GameController.GenerateGameCommand(
            GameCommandTypes.PressNext,
            Phases.CurrentSubPhase.GetType(),
            Phases.CurrentSubPhase.ID
        );
    }

    public static void NextButtonEffect()
    {
        Phases.CurrentSubPhase.NextButton();
    }

    public void ClickSkipPhase()
    {
        SkipButtonPressedEffect();
    }

    public static void SkipButtonPressedEffect()
    {
        HideSkipButton();
        Roster.AllShipsHighlightOff();

        GameMode.CurrentGameMode.ExecuteCommand(GenerateSkipButtonCommand());
    }

    public static GameCommand GenerateSkipButtonCommand()
    {
        return GameController.GenerateGameCommand(
            GameCommandTypes.PressSkip,
            Phases.CurrentSubPhase.GetType(),
            Phases.CurrentSubPhase.ID
        );
    }

    public static void ClickDeclareTarget()
    {
        GameCommand command = Combat.GenerateIntentToAttackCommand(Selection.ThisShip.ShipId, Selection.AnotherShip.ShipId);
        if (command != null) GameMode.CurrentGameMode.ExecuteServerCommand(command);
    }

    public static void CheckFiringRangeAndShow()
    {
        ActionsHolder.GetRangeAndShow(Selection.ThisShip, Selection.AnotherShip);
    }

    public static void ShowNextButton()
    {
        if (Roster.GetPlayer(Phases.CurrentPhasePlayer).PlayerType == Players.PlayerType.Human || Phases.CurrentSubPhase.AllowsMultiplayerSelection)
        {
            GameObject.Find("UI").transform.Find("NextPanel").gameObject.SetActive(true);
            GameObject.Find("UI/NextPanel").transform.Find("NextButton").GetComponent<Animator>().enabled = false;
        }
    }

    public static void HideNextButton()
    {
        GameObject.Find("UI").transform.Find("NextPanel").gameObject.SetActive(false);
        GameObject.Find("UI").transform.Find("NextPanel").Find("NextButton").GetComponent<Animator>().enabled = false;

        ColorBlock colors = GameObject.Find("UI").transform.Find("NextPanel").Find("NextButton").GetComponent<Button>().colors;
        colors.normalColor = new Color32(0, 0, 0, 200);
        GameObject.Find("UI").transform.Find("NextPanel").Find("NextButton").GetComponent<Button>().colors = colors;
    }

    public static void ShowSkipButton(string text = null)
    {
        if (Roster.GetPlayer(Phases.CurrentPhasePlayer).PlayerType == Players.PlayerType.Human)
        {
            GameObject.Find("UI").transform.Find("SkipPanel").GetComponentInChildren<Text>().text = text ?? "Skip";
            GameObject.Find("UI").transform.Find("SkipPanel").gameObject.SetActive(true);
        }
    }

    public static void HideSkipButton()
    {
        GameObject.Find("UI").transform.Find("SkipPanel").gameObject.SetActive(false);
    }

    public static void HighlightNextButton()
    {
        GameObject.Find("UI").transform.Find("NextPanel").Find("NextButton").GetComponent<Animator>().enabled = true;
    }

    public static void HighlightSkipButton()
    {
        GameObject.Find("UI").transform.Find("SkipPanel").Find("SkipButton").GetComponent<Animator>().enabled = true;
    }

    public static void CallHideTooltip()
    {
        Tooltips.EndTooltip();
    }

    public void HideInformCritPanel()
    {
        InformCrit.ButtonConfirm();
    }

    public void ReturnToMainMenu()
    {
        GameMode.CurrentGameMode.ReturnToMainMenu();
    }

    public void QuitGame()
    {
        GameMode.CurrentGameMode.QuitToDesktop();
    }

    public void GoNextShortcut()
    {
        bool pressNext = false;
        bool pressCancel = false;

        if (GameObject.Find("UI").transform.Find("NextPanel").gameObject.activeSelf) pressNext = true;
        else if (GameObject.Find("UI").transform.Find("SkipPanel").gameObject.activeSelf) pressCancel = true;

        if (pressNext) ClickNextPhase();
        else if (pressCancel) ClickSkipPhase();
    }

    public static void AssignManeuverButtonPressed(string buttonName)
    {
        string maneuverCode = buttonName;
        //OLD: EventSystem.current.currentSelectedGameObject.name;

        if (maneuverCode.Split('.').Length == 3)
        {
            HideDirectionMenu();

            DirectionsMenu.Callback(maneuverCode);
        }
        else
        {
            Messages.ShowError($"Error: maneuver button is required, but name of pressed UI element: \"{maneuverCode}\"");
        }
    }

    public void RestartMatch()
    {
        RestartMatchStatic();
    }

    public static void RestartMatchStatic()
    {
        Rules.FinishGame();
        if (ReplaysManager.Mode == ReplaysMode.Write)
        {
            Global.SquadBuilder.GenerateDatabase();
            Global.SquadBuilder.ReGenerateSquads();
            Global.SwitchToBattleScene();
        }
        else if (ReplaysManager.Mode == ReplaysMode.Read)
        {
            GameController.StartBattle(ReplaysMode.Read);
        }
    }

    public static void ToggleDebugMenu()
    {
        bool isActive = GameObject.Find("UI").transform.Find("DebugMenuPanel").gameObject.activeSelf;
        GameObject.Find("UI").transform.Find("DebugMenuPanel").gameObject.SetActive(!isActive);
    }
}
