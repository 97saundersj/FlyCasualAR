﻿using ActionsList;
using GameCommands;
using GameModes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class DiceModificationsManager
{
    public static bool IsLocked { get; set; }

    public void ShowDiceModificationButtons(List<GenericAction> diceModifications)
    {
        ShowDiceModificationsUiEmpty();
        CreateDiceModificationButtons(diceModifications);
        CreateOkButton();

        DiceModificationsManager.IsLocked = false;
    }

    private void CreateDiceModificationButtons(List<GenericAction> diceModifications)
    {
        float offset = 0;
        Vector3 position = Vector3.zero;
        AvailableDiceModifications = new Dictionary<string, GenericAction>();

        foreach (var actionEffect in diceModifications)
        {
            AvailableDiceModifications.Add(actionEffect.Name, actionEffect);

            position += new Vector3(0, -offset, 0);
            CreateDiceModificationsButton(actionEffect, position);

            offset = 65;
        }
    }

    public static void CreateDiceModificationsButton(GenericAction actionEffect, Vector3 position)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/GenericButton", typeof(GameObject));
        GameObject newButton = MonoBehaviour.Instantiate(prefab, GameObject.Find("UI").transform.Find("CombatDiceResultsPanel").Find("DiceModificationsPanel"));
        newButton.GetComponent<RectTransform>().localPosition = position;

        newButton.name = "Button" + actionEffect.DiceModificationName;
        newButton.transform.GetComponentInChildren<Text>().text = actionEffect.DiceModificationName;
        
        newButton.GetComponent<Button>().onClick.AddListener(
            delegate {
                if (DiceModificationsManager.IsLocked) return;
                DiceModificationsManager.IsLocked = true;

                GameCommand command = DiceModificationsManager.GenerateDiceModificationCommand(actionEffect.DiceModificationName);
                GameMode.CurrentGameMode.ExecuteCommand(command);
            }
        );
        Tooltips.AddTooltip(newButton, actionEffect.ImageUrl);

        newButton.GetComponent<Button>().interactable = true;
        newButton.SetActive(ShowOnlyForHuman());
    }

    private void CreateOkButton()
    {
        Button closeButton = GameObject.Find("UI").transform.Find("CombatDiceResultsPanel").Find("DiceModificationsPanel").Find("Confirm").GetComponent<Button>();
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(
            delegate {
                if (DiceModificationsManager.IsLocked) return;
                DiceModificationsManager.IsLocked = true;

                GameCommand command = DiceModificationsManager.GenerateDiceModificationCommand("OK");
                GameMode.CurrentGameMode.ExecuteCommand(command);
            }
        );
        closeButton.gameObject.SetActive(ShowOnlyForHuman());
    }

    // Subs

    public void ShowDiceModificationsUiEmpty()
    {
        if (!DebugManager.BatchAiSquadTestingModeActive) GameObject.Find("UI").transform.Find("CombatDiceResultsPanel").gameObject.SetActive(true);
        GameObject.Find("UI").transform.Find("CombatDiceResultsPanel").Find("DiceModificationsPanel").gameObject.SetActive(true);
        HideAllButtons();
    }

    public void HideDiceModificationsUi()
    {
        GameObject.Find("UI").transform.Find("CombatDiceResultsPanel").gameObject.SetActive(false);
        GameObject.Find("UI").transform.Find("CombatDiceResultsPanel").Find("DiceModificationsPanel").gameObject.SetActive(false);
    }

    public void HideAllButtons()
    {
        HideDiceModificationsButtonsList();
        HideOkButton();
    }

    public void HideDiceModificationsButtonsList()
    {
        foreach (Transform transform in GameObject.Find("UI").transform.Find("CombatDiceResultsPanel").Find("DiceModificationsPanel").transform)
        {
            if (transform.name != "Confirm") GameObject.Destroy(transform.gameObject);
        }
    }

    public void HideOkButton()
    {
        Button closeButton = GameObject.Find("UI").transform.Find("CombatDiceResultsPanel").Find("DiceModificationsPanel").Find("Confirm").GetComponent<Button>();
        closeButton.gameObject.SetActive(false);
    }

    private static bool ShowOnlyForHuman()
    {
        return Selection.ActiveShip.Owner.GetType() == typeof(Players.HumanPlayer);
    }
}
