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


public class BTSpawnerCompleto : BehaviourRunner
{
    [SerializeField] private ObjectSpawner m_ObjectSpawner;
    [SerializeField] BSRuntimeDebugger _debugger;
	
	protected override BehaviourGraph CreateGraph()
	{
		BehaviourTree BT_CompleteSpawner = new BehaviourTree();
		UtilitySystem US_Armas = CrearSubUS();


        // Acciones
        SimpleAction GenerarBotiquin_action = new SimpleAction(m_ObjectSpawner.SpawnBotiquin);
        SimpleAction GenerarZombies_action = new SimpleAction(m_ObjectSpawner.PenalizacionTiempo);
        DelayAction Pulso_Chequeo_action = new DelayAction(5);
        SubsystemAction GenerarArma_action = new SubsystemAction(US_Armas);


        // Nodos Hoja
        LeafNode GenerarBotiquin = BT_CompleteSpawner.CreateLeafNode(GenerarBotiquin_action);
        LeafNode GenerarArma = BT_CompleteSpawner.CreateLeafNode(GenerarArma_action);
        LeafNode GenerarZombies = BT_CompleteSpawner.CreateLeafNode(GenerarZombies_action);
        LeafNode Pulso_Chequeo = BT_CompleteSpawner.CreateLeafNode(Pulso_Chequeo_action);


        // Condiciones
        ConditionNode vidaGenerada = BT_CompleteSpawner.CreateDecorator<ConditionNode>(GenerarBotiquin);
        vidaGenerada.Perception = new ConditionPerception(null, m_ObjectSpawner.GetVidaGenerada, null);
        ConditionNode vidaBaja = BT_CompleteSpawner.CreateDecorator<ConditionNode>(vidaGenerada);
        vidaBaja.Perception = new ConditionPerception(null, m_ObjectSpawner.VidaJugadorBaja, null);
        ConditionNode armaGenerada = BT_CompleteSpawner.CreateDecorator<ConditionNode>(GenerarArma);
        armaGenerada.Perception = new ConditionPerception(null, m_ObjectSpawner.GetArmaGenerada, null);
        ConditionNode muchosZombies = BT_CompleteSpawner.CreateDecorator<ConditionNode>(armaGenerada);
        muchosZombies.Perception = new ConditionPerception(null, m_ObjectSpawner.MuchosZombies, null);
        ConditionNode pocosZombies = BT_CompleteSpawner.CreateDecorator<ConditionNode>(GenerarZombies);
        pocosZombies.Perception = new ConditionPerception(null, m_ObjectSpawner.PocosZombies, null);
        ConditionNode muchoSinMatar = BT_CompleteSpawner.CreateDecorator<ConditionNode>(pocosZombies);
        muchoSinMatar.Perception = new ConditionPerception(null, m_ObjectSpawner.MuchoTiempoSinMatar, null);


        // Succeders
        SuccederNode vidaSucceder = BT_CompleteSpawner.CreateDecorator<SuccederNode>(vidaBaja);
        SuccederNode armaSucceder = BT_CompleteSpawner.CreateDecorator<SuccederNode>(muchosZombies);
        SuccederNode zombiesSucceder = BT_CompleteSpawner.CreateDecorator<SuccederNode>(muchoSinMatar);


        // Secuencia Principal
        SequencerNode Secuencia_Principal = BT_CompleteSpawner.CreateComposite<SequencerNode>(false, vidaSucceder, armaSucceder, zombiesSucceder, Pulso_Chequeo);
        Secuencia_Principal.IsRandomized = false;


        // Loop Principal
        LoopNode Loop_Principal = BT_CompleteSpawner.CreateDecorator<LoopNode>(Secuencia_Principal);
        Loop_Principal.Iterations = -1;

        BT_CompleteSpawner.SetRootNode(Loop_Principal);

        _debugger.RegisterGraph(BT_CompleteSpawner, "Main BT");
        _debugger.RegisterGraph(US_Armas, "Sub US");

        return BT_CompleteSpawner;
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
