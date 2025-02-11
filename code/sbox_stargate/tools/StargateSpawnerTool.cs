﻿using System;

namespace Sandbox.Tools
{
	[Library( "tool_stargate_spawner", Title = "Stargate", Description = "Use wormholes to transport matter\n\nMOUSE1 - Spawn Milky Way gate\nE + MOUSE1 - Spawn Movie gate\nR - copy gate address\n\nMOUSE2 - Close gate/Stop dialling/Fast dial copied address\nSHIFT + MOUSE2 - Slow dial copied address\nCTRL + MOUSE2 - Instant dial copied address\n", Group = "construction" )]
	public partial class StargateSpawnerTool : BaseTool
	{
		PreviewEntity previewModel;

		static Stargate CopiedGate = null;

		private string Model => "models/sbox_stargate/gate_sg1/gate_sg1.vmdl";

		protected override bool IsPreviewTraceValid( TraceResult tr )
		{
			if ( !base.IsPreviewTraceValid( tr ) )
				return false;

			if ( tr.Entity is Stargate )
				return false;

			return true;
		}

		public override void CreatePreviews()
		{
			if ( TryCreatePreview( ref previewModel, Model ) )
			{
				if (Owner.IsValid())
				{
					previewModel.RelativeToNormal = false;
					previewModel.OffsetBounds = false;
					previewModel.PositionOffset = new Vector3( 0, 0, 90 );
					previewModel.RotationOffset = new Angles( 0, Owner.EyeRot.Angles().yaw + 180, 0 ).ToRotation();
				}

			}
		}

		public override void OnFrame()
		{
			base.OnFrame();

			if ( Owner.IsValid() && Owner.Health > 0)
			{
				RefreshPreviewAngles();
			}
		}

		public void RefreshPreviewAngles()
		{
			foreach ( var preview in Previews )
			{
				if ( !preview.IsValid() || !Owner.IsValid() )
					continue;

				preview.RotationOffset = new Angles( 0, Owner.EyeRot.Angles().yaw + 180, 0 ).ToRotation();

			}
		}

		public override void Simulate()
		{
			if ( !Host.IsServer )
				return;

			using ( Prediction.Off() )
			{
				if ( Input.Pressed( InputButton.Attack1 ) )
				{
					var startPos = Owner.EyePos;
					var dir = Owner.EyeRot.Forward;

					var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
						.Ignore( Owner )
						.Run();

					if ( !tr.Hit || !tr.Entity.IsValid() )
						return;

					CreateHitEffects( tr.EndPos );

					if ( tr.Entity is Stargate )
					{
						// TODO: Set properties

						return;
					}

					var gate = (Input.Down( InputButton.Use )) ? new StargateMovie() : new StargateMilkyWay();

					gate.Position = tr.EndPos + gate.SpawnOffset;
					gate.Rotation = new Angles( 0, Owner.EyeRot.Angles().yaw + 180, 0 ).ToRotation();
					gate.Owner = Owner;

					if ( tr.Entity is IStargateRamp ramp ) Stargate.PutGateOnRamp( gate, ramp );
				}

				if ( Input.Pressed( InputButton.Reload ) )
				{					
					var startPos = Owner.EyePos;
					var dir = Owner.EyeRot.Forward;

					var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
						.Ignore( Owner )
						.Run();

					if ( !tr.Hit || !tr.Entity.IsValid() )
						return;

					CreateHitEffects( tr.EndPos );

					if ( tr.Entity is Stargate gate)
					{
						CopiedGate = gate;
						return;
					}

				}


				if ( Input.Pressed( InputButton.Attack2 ) )
				{
					var startPos = Owner.EyePos;
					var dir = Owner.EyeRot.Forward;

					var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
						.Ignore( Owner )
						.Run();

					if ( !tr.Hit || !tr.Entity.IsValid() )
						return;

					CreateHitEffects( tr.EndPos );


					if ( tr.Entity is Stargate gate )
					{
						if ( gate.Busy ) return;

						if (gate.Open)
						{
							gate.DoStargateClose(true);
						}
						else
						{
							if ( !gate.Dialing )
							{
								var finalAddress = Stargate.GetOtherGateAddressForMenu( gate, CopiedGate );
								Log.Info( $"Dialing {finalAddress}" );

								if (Input.Down(InputButton.Run))
								{
									gate.BeginDialSlow( finalAddress );
								}
								else if (Input.Down(InputButton.Duck))
								{
									gate.BeginDialInstant( finalAddress );
								}
								else
								{
									gate.BeginDialFast( finalAddress );
								}
							}
							else
							{
								gate.StopDialing();
							}
						}


					}

				}


			}
		}
	}
}
