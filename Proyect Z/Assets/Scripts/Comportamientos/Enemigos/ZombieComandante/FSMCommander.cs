using System;
using System.Collections.Generic;
using UnityEngine;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.UnityToolkit;
using BehaviourAPI.StateMachines;
using BehaviourAPI.UtilitySystems;
using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;


public class FSMCommander : BehaviourRunner
{
	[SerializeField] private ZombieActions m_ZombieActions;
	[SerializeField] private CommanderActions m_CommanderActions;
    [SerializeField] BSRuntimeDebugger _debugger;

    protected override void Init()
	{
		m_ZombieActions = GetComponent<ZombieActions>();
		m_CommanderActions = GetComponent<CommanderActions>();
		
		base.Init();
	}
	
	protected override BehaviourGraph CreateGraph()
	{
		FSM FSMCommander = new FSM();
		UtilitySystem USCommander = new UtilitySystem(1.3f);
		
		FunctionalAction Roaming_action = new FunctionalAction();
		Roaming_action.onStarted = m_ZombieActions.EnterRoaming;
		Roaming_action.onUpdated = m_ZombieActions.TickRoaming;
		State Roaming = FSMCommander.CreateState(Roaming_action);
		
		FunctionalAction Flee_action = new FunctionalAction();
		Flee_action.onStarted = m_CommanderActions.TickHuir;
		Flee_action.onUpdated = () => Status.Running;
		State Flee = FSMCommander.CreateState(Flee_action);
		
		SubsystemAction Action_action = new SubsystemAction(USCommander);
		State Action = FSMCommander.CreateState(Action_action);
		
		ConditionPerception JugadorLejos_perception = new ConditionPerception();
		JugadorLejos_perception.onCheck = m_CommanderActions.JugadorEstaLejos;
		StateTransition JugadorLejos = FSMCommander.CreateTransition(Action, Roaming, JugadorLejos_perception);
		
		ConditionPerception JugadorVisto_perception = new ConditionPerception();
		JugadorVisto_perception.onCheck = m_CommanderActions.VeAlJugador;
		StateTransition JugadorVisto = FSMCommander.CreateTransition(Roaming, Flee, JugadorVisto_perception, statusFlags: StatusFlags.Finished);
		
		ConditionPerception JugadorLejos_1_perception = new ConditionPerception();
		JugadorLejos_1_perception.onCheck = m_CommanderActions.JugadorEstaLejos;
		StateTransition JugadorLejos_1 = FSMCommander.CreateTransition(Flee, Action, JugadorLejos_1_perception);
		
		VariableFactor VidaNorm = USCommander.CreateVariable(m_CommanderActions.GetVidaNorm, 0f, 1f);
		
		VariableFactor DistNorm = USCommander.CreateVariable(m_CommanderActions.GetDistNorm, 0f, 1f);
		
		VariableFactor InvVidaNorm = USCommander.CreateVariable(m_CommanderActions.GetInvVidaNorm, 0f, 1f);
		
		WeightedFusionFactor InvVidaDist = USCommander.CreateFusion<WeightedFusionFactor>(DistNorm, InvVidaNorm);
		
		SimpleAction Cure_action = new SimpleAction();
		Cure_action.action = m_CommanderActions.Cure;
		UtilityAction Cure = USCommander.CreateAction(InvVidaDist, Cure_action);
		
		VariableFactor InvDistNorm = USCommander.CreateVariable(m_CommanderActions.GetInvDistNorm, 0f, 1f);
		
		WeightedFusionFactor InvTodo = USCommander.CreateFusion<WeightedFusionFactor>(InvDistNorm, InvVidaNorm);
		
		SimpleAction Accelerate_action = new SimpleAction();
		Accelerate_action.action = m_CommanderActions.Accelerate;
		UtilityAction Accelerate = USCommander.CreateAction(InvTodo, Accelerate_action);
		
		WeightedFusionFactor VidaDist = USCommander.CreateFusion<WeightedFusionFactor>(DistNorm, VidaNorm);
		
		SimpleAction Summon_action = new SimpleAction();
		Summon_action.action = m_CommanderActions.Summon;
		UtilityAction Summon = USCommander.CreateAction(VidaDist, Summon_action);
		
		WeightedFusionFactor VidaInvDist = USCommander.CreateFusion<WeightedFusionFactor>(VidaNorm, InvDistNorm);
		
		SimpleAction Fortify_action = new SimpleAction();
		Fortify_action.action = m_CommanderActions.Fortify;
		UtilityAction Fortify = USCommander.CreateAction(VidaInvDist, Fortify_action);

        _debugger.RegisterGraph(FSMCommander, "FSM Commander");
        _debugger.RegisterGraph(USCommander, "FSM Commander");

        return FSMCommander;
	}
}
