using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.UnityToolkit;
using BehaviourAPI.BehaviourTrees;
using BehaviourAPI.UtilitySystems;
using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;


public class BTSpawnerObjetos : BehaviourRunner
{
	[SerializeField] private ObjectSpawner m_ObjectSpawner;
    [SerializeField] BSRuntimeDebugger _debugger;
	
	protected override BehaviourGraph CreateGraph()
	{
		BehaviourTree BT_ObjectSpawner = new BehaviourTree();
		UtilitySystem US_Armas = CrearSubUS();

        // Acciones
        SimpleAction GenerarBotiquin_action = new SimpleAction(m_ObjectSpawner.SpawnBotiquin);
        SubsystemAction GenerarArma_action = new SubsystemAction(US_Armas);


        // Nodos Hoja
        LeafNode GenerarBotiquin = BT_ObjectSpawner.CreateLeafNode(GenerarBotiquin_action);
        LeafNode GenerarArma = BT_ObjectSpawner.CreateLeafNode(GenerarArma_action);


        // Condiciones
        ConditionNode Vida_Generada = BT_ObjectSpawner.CreateDecorator<ConditionNode>(GenerarBotiquin);
        Vida_Generada.Perception = new ConditionPerception(null, m_ObjectSpawner.GetVidaGenerada, null);
        ConditionNode Vida_baja = BT_ObjectSpawner.CreateDecorator<ConditionNode>(Vida_Generada);
        Vida_baja.Perception = new ConditionPerception(null, m_ObjectSpawner.VidaJugadorBaja, null);
        ConditionNode Arma_Generada = BT_ObjectSpawner.CreateDecorator<ConditionNode>(GenerarArma);
        Arma_Generada.Perception = new ConditionPerception(null, m_ObjectSpawner.GetArmaGenerada, null);
        ConditionNode Muchos_Zombies = BT_ObjectSpawner.CreateDecorator<ConditionNode>(Arma_Generada);
        Muchos_Zombies.Perception = new ConditionPerception(null, m_ObjectSpawner.MuchosZombies, null);




        // Decoradores de Delay
        TimerDecoratorNode retardoVida = BT_ObjectSpawner.CreateDecorator<TimerDecoratorNode>(Vida_baja);
        TimerDecoratorNode retardoArma = BT_ObjectSpawner.CreateDecorator<TimerDecoratorNode>(Muchos_Zombies);
        retardoVida.Time = 5f;
        retardoArma.Time = 5f;


        // Selector Principal
        SelectorNode Seleccion_principal = BT_ObjectSpawner.CreateComposite<SelectorNode>(false, retardoVida, retardoArma);
        Seleccion_principal.IsRandomized = false;


        // Loop Principal
        LoopNode Loop_Principal = BT_ObjectSpawner.CreateDecorator<LoopNode>(Seleccion_principal);
        Loop_Principal.Iterations = -1;

        BT_ObjectSpawner.SetRootNode(Loop_Principal);

        _debugger.RegisterGraph(BT_ObjectSpawner, "Main BT");
        _debugger.RegisterGraph(US_Armas, "Sub US");

        return BT_ObjectSpawner;
	}

	UtilitySystem CrearSubUS()
	{
        UtilitySystem us = new UtilitySystem();

        // Factores
        VariableFactor NumeroSpawnEscopeta = us.CreateVariable(m_ObjectSpawner.GetNumEscopeta, 0f, 1f);
        VariableFactor NumeroSpawnRifle = us.CreateVariable(m_ObjectSpawner.GetNumRifle, 0f, 1f);
        VariableFactor NumeroSpawnFrancotirador = us.CreateVariable(m_ObjectSpawner.GetNumFranco, 0f, 1f);


        // Acciones
        SimpleAction SpawnEscopeta_action = new SimpleAction(m_ObjectSpawner.SpawnEscopeta);
        SimpleAction SpawnRifle_action = new SimpleAction(m_ObjectSpawner.SpawnFusil);
        SimpleAction SpawnFranco_action = new SimpleAction(m_ObjectSpawner.SpawnFrancotirador);


        // Curvas
        LinearCurveFactor CurvaEscopeta = us.CreateCurve<LinearCurveFactor>(NumeroSpawnEscopeta);
        CurvaEscopeta.Slope = -1f;
        CurvaEscopeta.YIntercept = 1f;

        var pointList1 = new List<CurvePoint>();
        pointList1.Add(new CurvePoint(0.0f, 1f));
        pointList1.Add(new CurvePoint(0.4f, 0.5f));
        pointList1.Add(new CurvePoint(0.6f, 0.5f));
        pointList1.Add(new CurvePoint(1.0f, 0.0f));
        PointedCurveFactor CurvaRifle = us.CreateCurve<PointedCurveFactor>(NumeroSpawnRifle).SetPoints(pointList1);

        var pointList2 = new List<CurvePoint>();
        pointList2.Add(new CurvePoint(0.0f, 0.7f));
        pointList2.Add(new CurvePoint(0.4f, 0.5f));
        pointList2.Add(new CurvePoint(1.0f, 0.0f));
        PointedCurveFactor CurvaFrancotirador = us.CreateCurve<PointedCurveFactor>(NumeroSpawnFrancotirador).SetPoints(pointList2);


        // Utility Actions 
        UtilityAction SpawnEscopeta = us.CreateAction(CurvaEscopeta, SpawnEscopeta_action, true);
        UtilityAction SpawnRifle = us.CreateAction(CurvaRifle, SpawnRifle_action, true);
        UtilityAction SpawnFranco = us.CreateAction(CurvaFrancotirador, SpawnFranco_action, true);

        return us;
    }
}
