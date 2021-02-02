﻿using Ship;
using Upgrade;
using UnityEngine;
using Bombs;
using System.Collections.Generic;
using BoardTools;
using System.Linq;
using Movement;

namespace UpgradesList.FirstEdition
{
    public class Bombardier : GenericUpgrade
    {
        public Bombardier() : base()
        {
            UpgradeInfo = new UpgradeCardInfo(
                "Bombardier",
                UpgradeType.Crew,
                cost: 1,
                abilityType: typeof(Abilities.FirstEdition.BombardierAbility)
            );
        }        
    }
}

namespace Abilities.FirstEdition
{
    public class BombardierAbility : GenericAbility
    {
        public override void ActivateAbility()
        {
            HostShip.OnGetAvailableBombDropTemplatesNoConditions += BombardierTemplate;
        }

        public override void DeactivateAbility()
        {
            HostShip.OnGetAvailableBombDropTemplatesNoConditions -= BombardierTemplate;
        }

        private void BombardierTemplate(List<ManeuverTemplate> availableTemplates, GenericUpgrade upgrade)
        {
            ManeuverTemplate newTemplate = new ManeuverTemplate(ManeuverBearing.Straight, ManeuverDirection.Forward, ManeuverSpeed.Speed2, isBombTemplate: true);
            if (!availableTemplates.Any(t => t.Name == newTemplate.Name)) availableTemplates.Add(newTemplate);
        }
    }
}