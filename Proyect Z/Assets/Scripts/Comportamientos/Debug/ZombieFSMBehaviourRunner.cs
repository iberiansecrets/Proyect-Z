using System;
using System.Collections.Generic;
using UnityEngine;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.UnityToolkit;
using BehaviourAPI.StateMachines;
using static UnityEngine.GraphicsBuffer;

public class ZombieFSMBehaviourRunner : BehaviourRunner
{
	[SerializeField] private ZombieActions m_ZombieActions;
	[SerializeField] private Transform target;

	[SerializeField] private bool debugLogs;

	private VisionSensor visionSensor;
	private HearingSensor hearingSensor;
	
	protected override void Init()
	{
        if (m_ZombieActions == null) m_ZombieActions = GetComponent<ZombieActions>();
        if (visionSensor == null) visionSensor = GetComponent<VisionSensor>();
        if (hearingSensor == null) hearingSensor = GetComponent<HearingSensor>();

        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        if (debugLogs)
        {
            Debug.Log($"[ZombieFSMBehaviourRunner.Init] actions={(m_ZombieActions != null)}, vision={(visionSensor != null)}, hearing={(hearingSensor != null)}, target={(target != null ? target.name : "NULL")}");
        }

        base.Init();
    }
	
	protected override BehaviourGraph CreateGraph()
	{
		FSM ZombieFSM = new FSM();
		
		FunctionalAction Roaming_action = new FunctionalAction();
		Roaming_action.onStarted = m_ZombieActions.EnterRoaming;
		Roaming_action.onUpdated = m_ZombieActions.TickRoaming;
		State Roaming = ZombieFSM.CreateState(Roaming_action);
		
		FunctionalAction Chasing_action = new FunctionalAction();
		Chasing_action.onUpdated = m_ZombieActions.TickChasing;
		State Chasing = ZombieFSM.CreateState(Chasing_action);
		
		FunctionalAction InvestigateSound_action = new FunctionalAction();
		InvestigateSound_action.onStarted = m_ZombieActions.EnterInvestigateSound;
		InvestigateSound_action.onUpdated = m_ZombieActions.TickInvestigateSound;
		State InvestigateSound = ZombieFSM.CreateState(InvestigateSound_action);
		
		ConditionPerception RoamToChase_perception = new ConditionPerception();
		RoamToChase_perception.onCheck = CheckCanSeePlayer;
		//StateTransition RoamToChase = ZombieFSM.CreateTransition(Roaming, Chasing, RoamToChase_perception, statusFlags: StatusFlags.Running, StatusFlags.Finished);
		ZombieFSM.CreateTransition("RoamToChase", Roaming, Chasing, RoamToChase_perception);
		
		ConditionPerception SoundToChase_perception = new ConditionPerception();
		SoundToChase_perception.onCheck = CheckCanSeePlayer;
        //StateTransition SoundToChase = ZombieFSM.CreateTransition(InvestigateSound, Chasing, SoundToChase_perception, statusFlags: StatusFlags.Running, Finished);
        ZombieFSM.CreateTransition("SoundToChase", InvestigateSound, Chasing, SoundToChase_perception);

        StateTransition FromChasing = ZombieFSM.CreateTransition(Chasing, Roaming, statusFlags: StatusFlags.Failure);
		
		StateTransition FromInvestigate = ZombieFSM.CreateTransition(InvestigateSound, Roaming, statusFlags: StatusFlags.Success);
		
		ConditionPerception RoamToInvestigate_perception = new ConditionPerception();
		RoamToInvestigate_perception.onCheck = CheckHasHeardSound;
        //StateTransition RoamToInvestigate = ZombieFSM.CreateTransition(Roaming, InvestigateSound, RoamToInvestigate_perception, statusFlags: StatusFlags.Running, Finished);
        ZombieFSM.CreateTransition("RoamToInvestigate", Roaming, InvestigateSound, RoamToInvestigate_perception);

        return ZombieFSM;
	}
	
	private Boolean CheckCanSeePlayer()
	{
        if (visionSensor == null) visionSensor = GetComponent<VisionSensor>();
        if (visionSensor == null)
        {
            if (debugLogs) Debug.Log("[ZombieRunner] Runner_CanSeePlayer = false (no visionSensor)");
            return false;
        }

        if (target == null)
        {
            if (debugLogs) Debug.Log("[ZombieRunner] Runner_CanSeePlayer = false (no target)");
            return false;
        }

        bool sees = visionSensor.CanSeePlayerSimple(target);
        if (debugLogs) Debug.Log($"[ZombieRunner] Runner_CanSeePlayer = {sees}");
        return sees;
    }
	
	private Boolean CheckHasHeardSound()
	{
        if (hearingSensor == null) hearingSensor = GetComponent<HearingSensor>();
        if (hearingSensor == null)
        {
            if (debugLogs) Debug.Log("[ZombieRunner] Runner_HasHeardSound = false (no hearingSensor)");
            return false;
        }

        bool h = hearingSensor.HasHeardSound();
        if (debugLogs) Debug.Log($"[ZombieRunner] Runner_HasHeardSound = {h}");
        return h;
    }
}
