﻿using Ship;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SubPhases
{

    public class EndStartSubPhase : GenericSubPhase
    {

        public override void Start()
        {
            Name = "End start";
        }

        public override void Initialize()
        {
            Console.Write($"\nEnd Phase (Round:{Phases.RoundCounter})", isBold: true, color: "orange");

            Phases.Events.CallEndPhaseTrigger(EndRound);
        }

        private void EndRound()
        {
            Phases.Events.CallRoundEndTrigger(delegate {
                if (!Phases.GameIsEnded) Next();
            });
            
        }

        public override void Next()
        {
            if (!DebugManager.BatchAiSquadTestingModeActive)
            {
                GameManagerScript.Wait(1, Phases.CurrentPhase.NextPhase);
            }
            else
            {
                Phases.CurrentPhase.NextPhase();
            }
        }

        public override bool ThisShipCanBeSelected(GenericShip ship, int mouseKeyIsPressed)
        {
            return false;
        }

        public override bool AnotherShipCanBeSelected(GenericShip targetShip, int mouseKeyIsPressed)
        {
            return false;
        }

    }

}
