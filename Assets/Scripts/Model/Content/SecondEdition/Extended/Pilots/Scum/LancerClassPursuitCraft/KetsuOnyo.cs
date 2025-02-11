﻿using Arcs;
using BoardTools;
using Content;
using Ship;
using SubPhases;
using System.Collections.Generic;
using Upgrade;

namespace Ship
{
    namespace SecondEdition.LancerClassPursuitCraft
    {
        public class KetsuOnyo : LancerClassPursuitCraft
        {
            public KetsuOnyo() : base()
            {
                PilotInfo = new PilotCardInfo25
                (
                    "Ketsu Onyo",
                    "Black Sun Contractor",
                    Faction.Scum,
                    5,
                    7,
                    15,
                    isLimited: true,
                    abilityType: typeof(Abilities.SecondEdition.KetsuOnyoPilotAbility),
                    tags: new List<Tags>
                    {
                        Tags.BountyHunter,
                        Tags.Mandalorian
                    },
                    extraUpgradeIcons: new List<UpgradeType>()
                    {
                        UpgradeType.Talent,
                        UpgradeType.Crew,
                        UpgradeType.Illicit,
                        UpgradeType.Illicit,
                        UpgradeType.Modification,
                        UpgradeType.Title
                    },
                    seImageNumber: 218,
                    legality: new List<Legality>() { Legality.ExtendedLegal }
                );
            }
        }
    }
}

namespace Abilities.SecondEdition
{
    public class KetsuOnyoPilotAbility : GenericAbility
    {

        public override void ActivateAbility()
        {
            Phases.Events.OnCombatPhaseStart_Triggers += TryRegisterKetsuOnyoPilotAbility;
        }

        public override void DeactivateAbility()
        {
            Phases.Events.OnCombatPhaseStart_Triggers -= TryRegisterKetsuOnyoPilotAbility;
        }

        private void TryRegisterKetsuOnyoPilotAbility()
        {
            if (TargetsForAbilityExist(FilterTargetsOfAbility))
            {
                RegisterAbilityTrigger(TriggerTypes.OnCombatPhaseStart, AskSelectShip);
            }
        }

        private void AskSelectShip(object sender, System.EventArgs e)
        {
            Selection.ChangeActiveShip(HostShip);

            SelectTargetForAbility(
                CheckAssignTractorBeam,
                FilterTargetsOfAbility,
                GetAiPriorityOfTarget,
                HostShip.Owner.PlayerNo,
                HostShip.PilotInfo.PilotName,
                "Choose a ship inside your primary and mobile firing arcs to assign 1 Tractor Beam token to it",
                HostShip
            );
        }

        private bool FilterTargetsOfAbility(GenericShip ship)
        {
            ShotInfo shotInfo = new ShotInfo(HostShip, ship, HostShip.PrimaryWeapons);

            return FilterByTargetType(ship, new List<TargetTypes>() { TargetTypes.Enemy, TargetTypes.OtherFriendly })
                && FilterTargetsByRange(ship, 0, 1)
                && shotInfo.InArcByType(ArcType.SingleTurret)
                && shotInfo.InPrimaryArc;
        }

        private int GetAiPriorityOfTarget(GenericShip ship)
        {
            return 50;
        }

        private void CheckAssignTractorBeam()
        {
            SelectShipSubPhase.FinishSelectionNoCallback();

            ShotInfo shotInfo = new ShotInfo(HostShip, TargetShip, HostShip.PrimaryWeapons);
            if (shotInfo.InArcByType(ArcType.SingleTurret) && shotInfo.InPrimaryArc && shotInfo.Range <= 1)
            {
                Messages.ShowInfo(HostShip.PilotInfo.PilotName + " assigns a Tractor Beam Token\nto " + TargetShip.PilotInfo.PilotName);
                Tokens.TractorBeamToken token = new Tokens.TractorBeamToken(TargetShip, HostShip.Owner);
                TargetShip.Tokens.AssignToken(token, Triggers.FinishTrigger);
            }
            else
            {
                if (!shotInfo.InArcByType(ArcType.SingleTurret) || !shotInfo.InPrimaryArc)
                {
                    Messages.ShowError("The target is not inside both your mobile arc and your primary arc");
                }
                else if (shotInfo.Range > 1)
                {
                    Messages.ShowError("The target is outside of range 1");
                }
            }
        }

    }
}