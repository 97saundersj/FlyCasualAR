﻿using ActionsList;
using Ship;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Players
{

    public partial class NetworkOpponentPlayer : GenericPlayer
    {

        public NetworkOpponentPlayer() : base()
        {
            PlayerType = PlayerType.Network;
            Name = "Network";
        }

        public override void ConfirmDiceCheck()
        {
            (Phases.CurrentSubPhase as SubPhases.DiceRollCheckSubPhase).ShowConfirmButton();
        }

        public override void PerformTractorBeamReposition(GenericShip ship)
        {
            RulesList.TractorBeamRule.PerfromManualTractorBeamReposition(ship, this);
        }

        public override void InformAboutCrit()
        {
            base.InformAboutCrit();

            InformCrit.ShowConfirmButton();
        }

        public override void ChangeManeuver(Action<string> doWithManeuverString, Action callback, Func<string, bool> filter = null)
        {
            base.ChangeManeuver(doWithManeuverString, callback, filter);

            DirectionsMenu.Show(doWithManeuverString, callback, filter);
        }

        public override void SelectManeuver(Action<string> doWithManeuverString, Action callback, Func<string, bool> filter = null)
        {
            DirectionsMenu.Show(doWithManeuverString, callback, filter);

            base.SelectManeuver(doWithManeuverString, callback, filter);
        }
    }

}
