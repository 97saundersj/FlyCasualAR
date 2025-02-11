﻿using System.Collections.Generic;
using UnityEngine;

static partial class DamageNumbers
{
    private struct HpInfo
    {
        public int Hull;
        public int Shields;

        public HpInfo(int hull, int shields)
        {
            Hull = hull;
            Shields = shields;
        }
    }

    private static Dictionary<string, HpInfo> SavedHP;

    public static void UpdateSavedHP()
    {
        SavedHP = new Dictionary<string, HpInfo>();

        foreach (var shipHolder in Roster.AllUnits)
        {
            SavedHP.Add(shipHolder.Key, new HpInfo(shipHolder.Value.State.HullCurrent, shipHolder.Value.State.ShieldsCurrent));
        }
    }

    public static void ShowChangedHP()
    {
        if (DebugManager.BatchAiSquadTestingModeActive) return;

        foreach (var shipHolder in Roster.AllUnits)
        {
            if (!SavedHP.ContainsKey(shipHolder.Key)) continue;

            int hullChanged = SavedHP[shipHolder.Key].Hull - shipHolder.Value.State.HullCurrent;
            int shieldsChanged = SavedHP[shipHolder.Key].Shields - shipHolder.Value.State.ShieldsCurrent;

            if ((hullChanged != 0) || (shieldsChanged != 0))
            {
                CreateDamageNumbersPanel(shipHolder.Value, hullChanged, shieldsChanged);
            }
        }
    }
}
