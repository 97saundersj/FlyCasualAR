﻿using Abilities.Parameters;
using Ship;
using SubPhases;
using System;
using System.Collections.Generic;

namespace Abilities
{
    public class EachShipCanDoAction : AbilityPart
    {
        private GenericAbility Ability;

        private Action<GenericShip, Action> EachShipAction;
        private Action OnFinish;
        private Action OnSkip;
        private ConditionsBlock Conditions;
        private AbilityDescription AbilityDescription;

        private List<GenericShip> ShipsThatCanBeActivated = new List<GenericShip>();

        public EachShipCanDoAction(
            Action<GenericShip, Action> eachShipAction,
            Action onFinish = null,
            Action onSkip = null,
            ConditionsBlock conditions = null,
            AbilityDescription description = null)
        {
            EachShipAction = eachShipAction;
            OnFinish = onFinish ?? Triggers.FinishTrigger;
            OnSkip = onSkip;
            Conditions = conditions;
            AbilityDescription = description;
        }

        public override void DoAction(GenericAbility ability)
        {
            Ability = ability;
            ShipsThatCanBeActivated = Ability.GetTargetsForAbility(FilterTargets);

            StartSelection();
        }

        private bool FilterTargets(GenericShip ship)
        {
            ConditionArgs args = new ConditionArgs()
            {
                ShipToCheck = ship,
                ShipAbilityHost = Ability.HostShip
            };

            return Conditions.Passed(args);
        }

        private void StartSelection()
        {
            if (ShipsThatCanBeActivated.Count > 0)
            {
                Ability.SelectTargetForAbility(
                    WhenShipIsSelected,
                    GetAlreadyFilteredTargets,
                    GetAiPriority,
                    Ability.HostShip.Owner.PlayerNo,
                    AbilityDescription.Name,
                    AbilityDescription.Description,
                    AbilityDescription.ImageSource,
                    showSkipButton: true,
                    callback: AfterShipIsSelected,
                    onSkip: OnSkip
                );
            }
            else
            {
                OnFinish();
            }
        }

        private bool GetAlreadyFilteredTargets(GenericShip ship)
        {
            return ShipsThatCanBeActivated.Contains(ship);
        }

        private void WhenShipIsSelected()
        {
            DecisionSubPhase.ConfirmDecisionNoCallback();
            Ability.RegisterAbilityTrigger
            (
                TriggerTypes.OnAbilityDirect,
                DoEachShipActon
            );
            Triggers.ResolveTriggers(TriggerTypes.OnAbilityDirect, StartSelection);
        }

        private int GetAiPriority(GenericShip ship)
        {
            return 0;
        }

        private void DoEachShipActon(object sender, EventArgs e)
        {
            ShipsThatCanBeActivated.Remove(Ability.TargetShip);
            EachShipAction(Ability.TargetShip, Triggers.FinishTrigger);
        }

        private void AfterShipIsSelected()
        {
            Triggers.FinishTrigger();
        }
    }
}
