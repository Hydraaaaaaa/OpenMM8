﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenMM8_HostilityResolver : MonoBehaviour
{
    public enum HostilityType { Friendly, Hostile, Undefined };
    public enum NPCRace
    {
        Undefined,
        Rock,
        Bazilisk,
        Dragon,
        DragonHatchling,
        DragonTurtle,
        Efrete,
        EtheralKnight,
        Phoenix,
        Gog,
        Gorgon,
        Lizardman,
        ThunderBird,
        Unicorn,
        LizardHabitant,
        Lizard,
        Centaur,
        Cleric,
        BoneDragon,
        Skeleton,
        Raven,
        Ratman,
        CrystalGuard,
        Rat,
        Cyclop,
        Snake,
        DragonHunter,
        Lycanthrope,
        Minotaur,
        Naga,
        Necromancer,
        Nightmare,
        FireElemental,
        WaterElemental,
        AirElemental,
        EarthElemental,
        Chaos,
        Pirate,
        Salamander,
        Wisp,
        DarkElf,
        DarkDwarf,
        Triton,
        Troll,
        Vampire,
        Vandal,
        Wolf,
        Bee,
        Ogre,
        BountyHunter,
        Player
    }

    public HostilityType m_HostilityType = HostilityType.Undefined;
    public NPCRace m_Race = NPCRace.Undefined;
    public List<NPCRace> m_SpecialHostileTo;
    public List<NPCRace> m_SpecialFriendlyTo;
    public bool m_IsHostileToPlayer = false;

    public bool IsHostileTo(GameObject what)
    {
        if (what.name == "Player")
        {
            return m_IsHostileToPlayer;
        }

        OpenMM8_HostilityResolver toWhatResolver = what.GetComponent<OpenMM8_HostilityResolver>();
        if (toWhatResolver == this)
        {
            return false;
        }

        if (toWhatResolver)
        {
            // TODO: Clarify if when hostile monsters attack eachother
            if (m_HostilityType == HostilityType.Hostile && 
                toWhatResolver.m_HostilityType == HostilityType.Hostile)
            {
                return false;
            }

            if (m_HostilityType == HostilityType.Friendly &&
                toWhatResolver.m_HostilityType == HostilityType.Hostile)
            {
                return true;
            }
            if (m_HostilityType == HostilityType.Hostile &&
                toWhatResolver.m_HostilityType == HostilityType.Friendly)
            {
                return true;
            }
        }

        return false;
    }

    public void AddHostileTarget(GameObject other)
    {

    }



	// Use this for initialization
	void Start ()
    {
		if (m_HostilityType == HostilityType.Hostile)
        {
            m_IsHostileToPlayer = true;
        }
	}
}
