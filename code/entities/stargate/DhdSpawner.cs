﻿namespace Sandbox.Tools
{
	[Library( "tool_shsspawner", Title = "DHD", Description = "Use to control stargate", Group = "construction" )]
	public partial class DhdSpawnerTool : BaseTool
	{
		PreviewEntity previewModel;

		private string Model => "models/markjaw/dhd_new/s2/dhd.vmdl";
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
					previewModel.PositionOffset = new Vector3( 0, 0, -15 );
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

				preview.RotationOffset = new Angles( 15, Owner.EyeRot.Angles().yaw + 180, 0 ).ToRotation();

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

					var dhd = new DhdSg1();
					dhd.Position = tr.EndPos + new Vector3(0, 0, -15);
					dhd.Rotation = new Angles( 15, Owner.EyeRot.Angles().yaw + 180, 0 ).ToRotation();
				}

			}
		}
	}
}
