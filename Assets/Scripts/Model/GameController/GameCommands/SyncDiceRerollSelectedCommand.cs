﻿using SubPhases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameCommands
{
    public class SyncDiceRerollSelectedCommand : GameCommand
    {
        public SyncDiceRerollSelectedCommand(GameCommandTypes type, Type subPhase, int subphaseId, string rawParameters) : base(type, subPhase, subphaseId, rawParameters)
        {

        }

        public override void Execute()
        {
            string diceToText = "";

            List<bool> selectedDice = new List<bool>();
            JSONObject jsonHolder1 = (JSONObject)GetParameter("dice");
            foreach (var dieInfo in jsonHolder1.list)
            {
                bool isSelected = bool.Parse(dieInfo["selected"].str);
                selectedDice.Add(isSelected);
                diceToText += isSelected + " ";
            }

            Console.Write($"Dice are selected for reroll: {diceToText}");

            DiceRerollManager.SyncDiceRerollSelected(selectedDice);
        }
    }

}
