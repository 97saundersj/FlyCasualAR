﻿using Players;
using SquadBuilderNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameCommands
{
    public class SyncPlayerWithInitiativeCommand : GameCommand
    {
        public SyncPlayerWithInitiativeCommand(GameCommandTypes type, Type subPhase, int subphaseId, string rawParameters) : base(type, subPhase, subphaseId, rawParameters)
        {

        }

        public override void TryExecute()
        {
            GameInitializer.TryExecute(this);
        }

        public override void Execute()
        {
            Phases.PlayerWithInitiative = (PlayerNo)Enum.Parse(typeof(PlayerNo), GetString("player"));

            Console.Write($"Player with Initiative: Player {Tools.PlayerToInt(Phases.PlayerWithInitiative)}");

            Triggers.FinishTrigger();
        }
    }

}
