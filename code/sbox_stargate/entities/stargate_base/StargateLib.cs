﻿using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public partial class Stargate : Prop, IUse
{
	public enum DialType
	{
		SLOW,
		FAST,
		INSTANT,
		NOX,
		DHD
	}

	public enum GateState
	{
		IDLE,
		ACTIVE,
		DIALING,
		OPENING,
		OPEN,
		CLOSING
	}

	public enum GlyphType
	{
		MILKYWAY,
		PEGASUS,
		UNIVERSE
	}

	public enum TimedTaskCategory
	{
		GENERIC,
		DIALING,
		SYMBOL_ROLL_PEGASUS_DHD,
		SET_BUSY
	}

	// Timed Tasks
	public struct TimedTask
	{
		public TimedTask( float time, Action action, TimedTaskCategory category )
		{
			TaskTime = time;
			TaskAction = action;
			TaskCategory = category;
			TaskFinished = false;
		}

		public void Execute()
		{
			TaskAction();
			TaskFinished = true;
		}

		public TimedTaskCategory TaskCategory { get; }
		public float TaskTime { get; }
		private Action TaskAction { get; }
		public bool TaskFinished { get; private set; }
	}

	private List<TimedTask> StargateActions = new();


	public const string Symbols = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#@"; // we wont use * and ? for now, since they arent on the DHD
	public const string SymbolsForAddress = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
	public const string SymbolsForGroup = "@";
	public const char PointOfOrigin = '#';

	public readonly int[] ChevronAngles = { 40, 80, 120, 240, 280, 320, 0, 160, 200 };

	public const int AutoCloseTimerDuration = 5;


	// WIP tasks testing

	public void AddTask(float time, Action task, TimedTaskCategory category)
	{
		if ( !IsServer ) return;
		StargateActions.Add( new TimedTask(time, task, category) );
	}

	public void ClearTasks()
	{
		if ( !IsServer ) return;
		StargateActions.Clear();
	}

	public void ClearTasksByCategory(TimedTaskCategory category)
	{
		if ( !IsServer ) return;
		var rem = StargateActions.RemoveAll( task => task.TaskCategory == category );
	}

	[Event.Tick.Server]
	private void TaskThink() // dont mind the retarded checks, it prevents ArgumentOutOfRangeException if actions get deleted while the loop runs, probably thread related stuff
	{
		if (StargateActions.Count > 0)
		{
			for ( var i = StargateActions.Count - 1; i >= 0; i-- )
			{
				if ( StargateActions.Count > i )
				{
					var task = StargateActions[i];
					if ( Time.Now >= task.TaskTime )
					{
						if ( !task.TaskFinished )
						{
							task.Execute();
							if ( StargateActions.Count > i ) StargateActions.RemoveAt( i );
						}
					}
				}
			}
		}
	}

	// Utility funcs

	/// <summary>
	/// Generates a random 2 symbol Gate Group.
	/// </summary>
	/// <returns>Gate Group.</returns>
	public static string GenerateGateGroup()
	{
		StringBuilder symbolsCopy = new( SymbolsForAddress + SymbolsForGroup );

		string generatedGroup = "";
		for ( int i = 0; i < 2; i++ ) // pick random symbols without repeating
		{
			var randomIndex = new Random().Int( 0, symbolsCopy.Length - 1 );
			generatedGroup += symbolsCopy[randomIndex];

			symbolsCopy = symbolsCopy.Remove( randomIndex, 1 );
		}

		return generatedGroup;
	}

	/// <summary>
	/// Generates a random 6 symbol Gate Address, excluding characters from a Gate Group.
	/// </summary>
	/// <returns>Gate Adress.</returns>
	public static string GenerateGateAddress( string excludeGroup )
	{
		StringBuilder symbolsCopy = new( SymbolsForAddress );

		foreach ( var c in excludeGroup )  // remove group chars from symbols
		{
			if ( symbolsCopy.ToString().Contains( c ) )	symbolsCopy = symbolsCopy.Remove( symbolsCopy.ToString().IndexOf( c ), 1 );
		}

		string generatedAddress = "";
		for ( int i = 0; i < 6; i++ ) // pick random symbols without repeating
		{
			var randomIndex = new Random().Int( 0, symbolsCopy.Length - 1 );
			generatedAddress += symbolsCopy[randomIndex];

			symbolsCopy = symbolsCopy.Remove( randomIndex, 1 );
		}

		return generatedAddress;
	}

	public static string GetFullGateAddress(Stargate gate, bool only8chev = false)
	{
		if ( !only8chev ) return gate.GateAddress + gate.GateGroup + PointOfOrigin;

		return gate.GateAddress + gate.GateGroup[0] + PointOfOrigin;
	}

	/// <summary>
	/// Checks if the format of the input string is that of a valid full Stargate Address (Address + Group + Point of Origin).
	/// </summary>
	/// <param name="address">The gate address represented in the string.</param>
	/// <returns>True or False</returns>
	public static bool IsValidFullAddress( string address ) // a valid address has an address, a group and a point of origin, with no repeating symbols
	{
		if ( address.Length < 7 || address.Length > 9 ) return false; // only 7, 8 or 9 symbol addresses

		foreach ( char sym in address )
		{
			if ( !Symbols.Contains( sym ) ) return false; // only valid symbols
			if ( address.Count( c => c == sym ) > 1 ) return false; // only one occurence
		}

		if ( !address.EndsWith( PointOfOrigin ) ) return false; // must end with point of origin

		return true;
	}

	/// <summary>
	/// Checks if the format of the input string is that of a valid Stargate Address (6 non-repeating valid symbols).
	/// </summary>
	/// <param name="address">The gate address represented in the string.</param>
	/// <returns>True or False</returns>
	public static bool IsValidAddressOnly( string address )
	{
		if ( address.Length != 6 ) return false; // only 7, 8 or 9 symbol addresses

		foreach ( char sym in address )
		{
			if ( !SymbolsForAddress.Contains( sym ) ) return false; // only valid symbols
			if ( address.Count( c => c == sym ) > 1 ) return false; // only one occurence
		}

		return true;
	}

	/// <summary>
	/// Checks if the format of the input string is that of a valid Stargate Group.
	/// </summary>
	/// <param name="group">The gate group represented in the string.</param>
	/// <returns>True or False</returns>
	public static bool IsValidGroup( string group ) // a valid address has an address, a group and a point of origin, with no repeating symbols
	{
		if ( group.Length != 2 ) return false; // only 2 symbol groups

		var validSyms = SymbolsForAddress + SymbolsForGroup;

		foreach ( char sym in group )
		{
			if ( !validSyms.Contains( sym ) ) return false; // only valid symbols
			if ( group.Count( c => c == sym ) > 1 ) return false; // only one occurence
		}
		return true;
	}

	public static bool IsUniverseGate(Stargate gate)
	{
		if ( !gate.IsValid() ) return false;
		return gate is StargateUniverse;
	}

	public static Stargate FindDestinationGateByDialingAddress(Stargate gate, string address)
	{
		var addrLen = address.Length;
		var otherAddress = address.Substring( 0, 6 );
		var otherGroup = (addrLen == 9) ? address.Substring( 6, 2 ) : (addrLen == 8) ? address.Substring( 6, 1 ) : "";

		Stargate target = null;

		if ( addrLen == 9 ) // 9 chevron connection - universe connection
		{
			target = FindByFullAddress( address );
			if ( target.IsValid() )
			{
				// cant have 9 chevron connection between 2 universe or 2 non-universe gates
				if ( !IsUniverseGate( gate ) && !IsUniverseGate( target ) ) target = null;
				if ( IsUniverseGate( gate ) && IsUniverseGate( target ) ) target = null;

				if ( gate.GateLocal || target.GateLocal ) target = null;
			}
		}
		else if ( addrLen == 8 ) // 8 chevron connection - different group
		{
			if ( otherGroup[0] != gate.GateGroup[0] ) target = FindByAddress8Chev( address );
			if ( target.IsValid() )
			{
				if ( gate.GateLocal || target.GateLocal ) target = null;
				if ( IsUniverseGate( gate ) || IsUniverseGate( target ) ) target = null; // make it invalid if for some reason we got a universe gate
			}
		}
		else // classic 7 chevron connection - must have same group, unless both are universe, they always use 7 symbols
		{
			target = FindByAddressOnly( address );
			if ( !IsUniverseGate( gate ) && !IsUniverseGate( target ) ) // both arent universe gates
			{
				if ( target.IsValid() && target.GateGroup != gate.GateGroup ) target = null; // if found gate does not have same group, its not valid
			}
			else if ( (IsUniverseGate( gate ) != IsUniverseGate( target )) ) // only one of them is universe gate and the other is not
			{
				target = null;
			}
		}

		return target;
	}

	/// <summary>
	/// Returns the gate if it finds it by a specified full address.
	/// </summary>
	/// <param name="address">The full gate address represented in the string.</param>
	/// <returns>A gate that matches the parameter.</returns>
	public static Stargate FindByFullAddress( string address )
	{
		foreach ( Stargate gate in Entity.All.OfType<Stargate>() )
		{
			if ( GetFullGateAddress(gate) == address ) return gate;
		}
		return null;
	}

	/// <summary>
	/// Returns the gate if it finds it by a specified address.
	/// </summary>
	/// <param name="address">The gate address represented in the string.</param>
	/// <returns>A gate that matches the parameter.</returns>
	public static Stargate FindByAddressOnly( string address )
	{
		foreach ( Stargate gate in Entity.All.OfType<Stargate>() )
		{
			if ( gate.GateAddress + PointOfOrigin == address ) return gate;
		}
		return null;
	}

	/// <summary>
	/// Returns the gate if it finds it by a specified address.
	/// </summary>
	/// <param name="address">The gate address represented in the string.</param>
	/// <returns>A gate that matches the parameter.</returns>
	public static Stargate FindByAddress8Chev( string address )
	{
		foreach ( Stargate gate in Entity.All.OfType<Stargate>() )
		{
			if ( gate.GateAddress + gate.GateGroup[0] + PointOfOrigin == address ) return gate;
		}
		return null;
	}

	public static string GetSelfAddressBasedOnOtherAddress(Stargate gate, string otherAddress)
	{
		if (otherAddress.Length == 7)
		{
			return gate.GateAddress + PointOfOrigin;
		}
		else if (otherAddress.Length == 8)
		{
			return gate.GateAddress + gate.GateGroup[0] + PointOfOrigin;
		}
		else if (otherAddress.Length == 9)
		{
			return GetFullGateAddress( gate );
		}

		return "";
	}

	public static string GetOtherGateAddressForMenu( Stargate gate, Stargate otherGate )
	{
		var finalAddress = "";

		if ( !IsUniverseGate(gate) && !IsUniverseGate(otherGate) ) // none of them are universe gates
		{
			if ( gate.GateGroup == otherGate.GateGroup ) // if groups are equal, return address
			{
				finalAddress = otherGate.GateAddress + PointOfOrigin;
			}
			else // if groups arent equal, return addres with first group symbol
			{
				finalAddress = otherGate.GateAddress + otherGate.GateGroup[0] + PointOfOrigin;
			}
		}
		else // one or both are universe gates
		{

			if ( IsUniverseGate( gate ) && IsUniverseGate( otherGate ) ) // both are universe gates
			{
				if ( gate.GateGroup == otherGate.GateGroup ) // they have same gate group
				{
					finalAddress = otherGate.GateAddress + PointOfOrigin;
				}
				else
				{
					finalAddress = otherGate.GateAddress + PointOfOrigin;
				}
			}
			else // only one is universe gate
			{
				finalAddress = GetFullGateAddress( otherGate );
			}
		}

		return finalAddress;
	}

	/// <summary>
	/// Return the random gate.
	/// </summary>
	/// <returns>A gate that matches the parameter.</returns>
	public static Stargate FindRandomGate()
	{
		var allGates = All.OfType<Stargate>().ToList();

		return allGates.Count is 0 ? null : (new Random().FromList( allGates ));
	}

	/// <summary>
	/// Return the random gate, this gate will never be the gate given in the argument
	/// </summary>
	/// <param name="ent">A gate that is eliminated with a random outcome.</param>
	/// <returns>A gate that matches the parameter.</returns>
	public static Stargate FindRandomGate( Stargate ent )
	{
		var allGates = All.OfType<Stargate>().ToList();
		allGates.Remove( ent ); // it will always be in the list, since it is a stargate

		return allGates.Count is 0 ? null : (new Random().FromList( allGates ));
	}

	/// <summary>
	/// It finds the nearest gate from the entity. It returns that gate.
	/// </summary>
	/// <param name="ent">The entity that will be the first point of remoteness.</param>
	/// <returns>A gate that matches the parameter.</returns>
	public static Stargate FindNearestGate( Entity ent )
	{
		var allGates = All.OfType<Stargate>().ToList();
		if ( allGates.Count() is 0 ) return null;

		var distances = new float[allGates.Count()];
		for ( int i = 0; i < allGates.Count(); i++ ) distances[i] = ent.Position.Distance( allGates[i].Position );

		return allGates[distances.ToList().IndexOf( distances.Min() )];
	}

	/// <summary>
	/// It finds the furthest gate from the entity that is in the argument. It returns that gate.
	/// </summary>
	/// <param name="ent">The entity that will be the first point of remoteness.</param>
	/// <returns>A gate that matches the parameter.</returns>
	public static Stargate FindFarthestGate( Entity ent )
	{
		var allGates = Entity.All.OfType<Stargate>().ToList();
		if ( allGates.Count() is 0 ) return null;

		var distanceAllGates = new float[allGates.Count()];
		for ( int i = 0; i < allGates.Count(); i++ ) distanceAllGates[i] = ent.Position.Distance( allGates[i].Position );

		return allGates[distanceAllGates.ToList().IndexOf( distanceAllGates.Max() )];
	}

	/// <summary>
	/// Adds an Iris on the target Stargate if it does not have one yet.
	/// </summary>
	/// <returns>The just created, or already existing Iris.</returns>
	public static StargateIris AddIris(Stargate gate, Entity owner = null)
	{
		if ( !gate.HasIris() )
		{
			var iris = new StargateIris();
			iris.Position = gate.Position;
			iris.Rotation = gate.Rotation;
			iris.Scale = gate.Scale;
			iris.SetParent( gate );
			iris.Gate = gate;
			//iris.Owner = owner; -- why the fuck does this break iris anims // its a sbox issue, ofcourse
			gate.Iris = iris;
		}
		return gate.Iris;
	}

	/// <summary>
	/// Attempts to remove the Iris from the target Stargate.
	/// </summary>
	/// <returns>Whether or not the Iris was removed succesfully.</returns>
	public static bool RemoveIris(Stargate gate)
	{
		if ( gate.HasIris() )
		{
			gate.Iris.Delete();
			return true;
		}
		return false;
	}

	public static async void PlaySound( Entity ent, string name, float delay = 0 )
	{
		if ( !ent.IsValid() ) return;
		if ( delay > 0 ) await ent.Task.DelaySeconds( delay );
		if ( !ent.IsValid() ) return;
		Sound.FromEntity( name, ent );
	}

	/// <summary>
	/// Attempts to position a Stargate onto a Ramp.
	/// </summary>
	/// <returns>Whether or not the Gate was positioned on the Ramp succesfully.</returns>
	public static bool PutGateOnRamp(Stargate gate, IStargateRamp ramp)
	{
		var rampEnt = (Entity) ramp;
		if ( gate.IsValid() && rampEnt.IsValid() ) // gate ramps
		{
			if ( ramp.Gate.Count < ramp.AmountOfGates )
			{
				int offsetIndex = ramp.Gate.Count;
				gate.Position = rampEnt.Transform.PointToWorld( ramp.StargatePositionOffset[offsetIndex] );
				gate.Rotation = rampEnt.Transform.RotationToWorld( ramp.StargateRotationOffset[offsetIndex].ToRotation() );
				gate.SetParent( rampEnt );
				gate.Ramp = ramp;

				ramp.Gate.Add( gate );

				return true;
			}
		}

		return false;
	}

}
