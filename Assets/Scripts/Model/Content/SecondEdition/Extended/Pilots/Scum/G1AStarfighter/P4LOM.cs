﻿using Actions;
using ActionsList;
using Content;
using Movement;
using Ship;
using SubPhases;
using System;
using System.Collections.Generic;
using System.Linq;
using Tokens;
using Upgrade;

namespace Ship
{
    namespace SecondEdition.G1AStarfighter
    {
        public class P4LOM : G1AStarfighter
        {
            public P4LOM() : base()
            {
                PilotInfo = new PilotCardInfo25
                (
                    "4-LOM",
                    "Reprogrammed Protocol Droid",
                    Faction.Scum,
                    3,
                    5,
                    14,
                    isLimited: true,
                    abilityType: typeof(Abilities.SecondEdition.P4LOMAbility),
                    tags: new List<Tags>
                    {
                        Tags.BountyHunter,
                        Tags.Droid
                    },
                    extraUpgradeIcons: new List<UpgradeType>()
                    {
                        UpgradeType.Talent,
                        UpgradeType.Sensor,
                        UpgradeType.Crew,
                        UpgradeType.Illicit,
                        UpgradeType.Modification,
                        UpgradeType.Title
                    },
                    seImageNumber: 201,
                    legality: new List<Legality>() { Legality.ExtendedLegal }
                );

                ShipInfo.ActionIcons.SwitchToDroidActions();
            }
        }
    }
}

namespace Abilities.SecondEdition
{
    public class P4LOMAbility : GenericAbility
    {
        public override void ActivateAbility()
        {
            HostShip.OnMovementFinishSuccessfully += Check4LOMMoveAbility;
            Phases.Events.OnEndPhaseStart_Triggers += RegisterEndOfRoundAbility;
        }

        public override void DeactivateAbility()
        {
            HostShip.OnMovementFinishSuccessfully -= Check4LOMMoveAbility;
            Phases.Events.OnEndPhaseStart_Triggers -= RegisterEndOfRoundAbility;
        }

        // Movement ability begins here
        public void Check4LOMMoveAbility(GenericShip ship)
        {
            if (ship.AssignedManeuver.ColorComplexity == MovementComplexity.Complex)
            {
                RegisterAbilityTrigger(TriggerTypes.OnMovementFinish, AssignCalculateToken);
            }
        }

        public void AssignCalculateToken(object sender, EventArgs e)
        {
            HostShip.Tokens.AssignToken(typeof(CalculateToken), Triggers.FinishTrigger);
        }

        // End of round stress pass begins here
        public void RegisterEndOfRoundAbility()
        {
            if (HostShip.Tokens.HasToken(typeof(StressToken)))
            {
                RegisterAbilityTrigger(TriggerTypes.OnCombatPhaseStart, EndOfRoundAbility);
            }
        }

        private void EndOfRoundAbility(object sender, EventArgs e)
        {
            if (TargetsForAbilityExist(FilterAbilityTarget))
            {
                Selection.ChangeActiveShip(HostShip);

                SelectTargetForAbility
                (
                    SelectAbilityTarget,
                    FilterAbilityTarget,
                    GetAIAbilityPriority,
                    HostShip.Owner.PlayerNo,
                    HostShip.PilotInfo.PilotName,
                    "Choose a target to transfer stress to",
                    HostShip
                );
            }
            else
            {
                Triggers.FinishTrigger();
            }
        }

        private void SelectAbilityTarget()
        {
            HostShip.Tokens.RemoveToken(typeof(StressToken), delegate
            {
                TargetShip.Tokens.AssignToken(typeof(StressToken), delegate
                {
                    SelectShipSubPhase.FinishSelection();
                });
            });
        }

        protected virtual bool FilterAbilityTarget(GenericShip ship)
        {
            return FilterTargetsByRange(ship, 0, 1);
        }

        private int GetAIAbilityPriority(GenericShip ship)
        {
            // AI should stress the most expensive ship enemy ship in range or the least expensive friendly.
            int cost = ship.PilotInfo.Cost + ship.UpgradeBar.GetUpgradesOnlyFaceup().Sum(n => n.UpgradeInfo.Cost);

            if (HostShip.Owner.PlayerNo == ship.Owner.PlayerNo)
            {
                // we're on the same team!
                return 100 - cost;
            }
            else
            {
                // stress the highest costing enemy first
                return 100 + cost;
            }

        }

    }
}