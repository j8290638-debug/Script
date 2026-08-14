using Godot;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace BadnikFramework.Tools.HsonImport {
	[Tool]
	public partial class HsonImport : Node
	{
		[Export] bool LoadOnStart = false;
		[Export] bool HedgehogEngineCorrections = true;

		public override void _Ready()
		{
			if (!LoadOnStart || Engine.IsEditorHint()) return;
			LoadFromJson();
		}

		[Export(PropertyHint.MultilineText)] public string SourceJson;
		[Export] public Node3D Output;

		[Export]
		public bool Clear
		{
			get
			{
				return false;
			}
			set
			{
				ClearOutput();
			}
		}

		[Export]
		public bool Load
		{
			get
			{
				return false;
			}
			set
			{
				ClearOutput();
				LoadFromJson();
			}
		}

		[Export] public Godot.Collections.Dictionary Mapping { get; set; }  // Key: type, Value: prefab path
		[Export] public Godot.Collections.Dictionary PathMapping { get; set; }  // Key: path type, Value: prefab path
		[Export] public bool ImportPaths = true;
		[Export] public bool ImportObjects = true;

		private libHSON.Project LibHSONProject;

		void ClearOutput()
		{
			if (Output == null) return;
			var children = Output.GetChildren();
			foreach (var child in children)
			{
				if (Engine.IsEditorHint())
				{
					child.Free();
				}
				else
				{
					child.QueueFree();
				}
			}
		}

		void LoadFromJson()
		{
			if (SourceJson == null || SourceJson.Length == 0) return;
			GD.Print("Loading HSON.");
			var input = System.Text.Encoding.UTF8.GetBytes(SourceJson);
			LibHSONProject = libHSON.Project.FromData(input);
			SpawnObjects();
		}

		void SpawnObjects()
		{
			foreach (var newObject in LibHSONProject.Objects)
			{
				Node3D newGameObject;
				if (ImportPaths && newObject.Type.ToLower() == "path" && PathMapping.ContainsKey(newObject.GetParameter("setParameter/pathType").ValueString.ToLower()))
				{
					PackedScene res = ResourceLoader.Load(PathMapping[newObject.GetParameter("setParameter/pathType").ValueString.ToLower()].AsString()) as PackedScene;
					newGameObject = res.Instantiate<Node3D>();
				}
				else if (!newObject.HasSpecifiedType || !Mapping.ContainsKey(newObject.Type.ToLower()))
				{
					// TODO: Some types are supported but aren't a separate objects, like PathNode. Need to silence that.
					//GD.Print("Unsupported type ", newObject.Type.ToString(), " for object ", newObject.Name, " ; skipping");
					continue;
					//newGameObject = new Node3D();
				}
				else if (ImportObjects)
				{
					PackedScene res = ResourceLoader.Load(Mapping[newObject.Type.ToLower()].AsString()) as PackedScene;
					newGameObject = res.Instantiate<Node3D>();
				}
				else
				{
					continue;
				}
				Output.AddChild(newGameObject);
				if (Engine.IsEditorHint())
				{
					newGameObject.Owner = GetTree().EditedSceneRoot;
				}

				if (newObject.HasSpecifiedName)
				{
					newGameObject.Name = newObject.Name;
				}

				GD.Print("Added ", newGameObject.GetPath());

				if (newObject.HasSpecifiedPosition)
				{
					var pos = newObject.LocalPosition;
					newGameObject.Position = new Vector3(pos.X, pos.Y, pos.Z);
				}

				if (newObject.HasSpecifiedRotation)
				{
					var rot = newObject.LocalRotation;
					newGameObject.Basis = new Basis(new Quaternion(rot.X, rot.Y, rot.Z, rot.W));
				}

				if (newObject.HasSpecifiedIsEditorVisible)
				{
					newGameObject.Visible = newObject.IsEditorVisible;
				}


				switch (newObject.Type.ToLower())
				{
					case "path":
						SetPathParams(newGameObject, newObject);
						break;
					case "ring":
						newGameObject.Translate(newGameObject.Basis.Y * 0.5f);      // To spawn slightly above ground instead of in.
						break;
					case "superring":
						newGameObject.Translate(newGameObject.Basis.Y * 0.5f);      // To spawn slightly above ground instead of in.
						break;
					case "spring":
						newGameObject.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2);  // Rotate because BF prefab is different from HE
						SetSpringParams(newGameObject, newObject);
						break;
					case "widespring":
						if (HedgehogEngineCorrections)
						{
							//newGameObject.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2 + Mathf.Pi / 6);  // Rotate because BF prefab is different from HE
							newGameObject.RotateObjectLocal(Vector3.Right, -newGameObject.Rotation.X + Mathf.Pi / 2);   // In HE they always launch them up, but they are positioned diagonally for visuals. Here we rotate them to point where they launch.
						}
						SetSpringParams(newGameObject, newObject);
						break;
					case "jumpboard":
						if (HedgehogEngineCorrections)
						{
							newGameObject.RotateObjectLocal(Vector3.Up, Mathf.Pi);  // Rotate because BF prefab is different from HE
						}
						SetJumpboardParams(newGameObject, newObject);
						break;
					case "dashpanel":
						if (HedgehogEngineCorrections)
						{
							newGameObject.RotateObjectLocal(Vector3.Up, Mathf.Pi);  // Rotate because BF prefab is different from HE
						}
						SetDashPanelParams(newGameObject, newObject);
						break;
					case "dashroller":
						if (HedgehogEngineCorrections)
						{
							newGameObject.RotateObjectLocal(Vector3.Up, -Mathf.Pi);  // Rotate because BF prefab is different from HE
						}
						SetDashPanelParams(newGameObject, newObject);
						break;
					case "dashring":
						SetSpringParams(newGameObject, newObject);
						break;
					case "upreel":
						if (HedgehogEngineCorrections)
						{
							newGameObject.RotateObjectLocal(Vector3.Up, Mathf.Pi);  // Rotate because BF prefab is different from HE
							newGameObject.Position += newGameObject.Basis.Z; // HE and BF prefab difference too
						}
						SetUpreelParams(newGameObject, newObject);
						break;
					case "balloon":
						SetBalloonParams(newGameObject, newObject);
						break;
					case "grindbooster":
						if (HedgehogEngineCorrections)
						{
							newGameObject.RotateObjectLocal(Vector3.Up, Mathf.Pi);  // Rotate because BF prefab is different from HE
						}
						SetSpringParams(newGameObject, newObject);
						break;
					case "redring":
						newGameObject.Translate(newGameObject.Basis.Y * 0.5f);      // To spawn slightly above ground instead of in.
						SetRedRingParams(newGameObject, newObject);
						break;
					case "bouncepad":	// NOT IN HEDGEHOG ENGINE GAMES! It's the giant propeller spring from Sonic Dream Team.
						SetSpringParams(newGameObject, newObject);
						break;
				}

			}
		}

		void SetSpringParams(Node3D dirLauncher, libHSON.Object hsonObject)
		{
			// TODO: I think accessing DirectionalLauncher script would be better. Otherwise it will be a mess to maintain.

			var firstSpeed = hsonObject.GetParameterFromName("firstSpeed");
			if (firstSpeed != null)
			{
				dirLauncher.Set("MinVelocity", firstSpeed.ValueFloatingPoint);
				dirLauncher.Set("MaxVelocity", firstSpeed.ValueFloatingPoint);
			}

			var speed = hsonObject.GetParameterFromName("Speed");
			if (speed != null)
			{
				dirLauncher.Set("MinVelocity", speed.ValueFloatingPoint);
				dirLauncher.Set("MaxVelocity", speed.ValueFloatingPoint);
			}

			var ooc = hsonObject.GetParameterFromName("outOfControl");
			if (ooc != null)
			{
				dirLauncher.Set("LockInputTime", ooc.ValueFloatingPoint);
			}

			var keepVelocityDistance = hsonObject.GetParameter("keepVelocityDistance");
			if (keepVelocityDistance != null)
			{
				dirLauncher.Set("KeepVelocityDistance", keepVelocityDistance.ValueFloatingPoint);
				dirLauncher.Set("Snap", true);
			}

			var keepVelocityTime = hsonObject.GetParameter("KeepVelocity");
			if (keepVelocityTime != null)
			{
				dirLauncher.Set("KeepVelocityTime", keepVelocityTime.ValueFloatingPoint);
			}
		}

		void SetJumpboardParams(Node3D dirLauncherNode, libHSON.Object hsonObject)
		{
			// Note: It works very differently from Hedgehog Engine (at least in Frontiers). HE makes movement go on path
			// according to height and distance. BF doesn't launch using set paths, but is more physics based instead.
			// These values are approximations to achieve similar movement, but don't guarantee identical movement.

			var dirLauncher = dirLauncherNode.GetNode<DirectionalLauncher>(".");

			var firstSpeed = hsonObject.GetParameterFromName("impulseSpeedOn");
			if (firstSpeed != null)
			{
				dirLauncher.MinVelocity = (float)firstSpeed.ValueFloatingPoint;
				dirLauncher.MaxVelocity = (float)firstSpeed.ValueFloatingPoint;
			}

			var ooc = hsonObject.GetParameterFromName("outOfControl");
			var motionTime = hsonObject.GetParameterFromName("motionTime");
			if (ooc != null && motionTime != null)
			{
				dirLauncher.LockInputTime = ooc.ValueFloatingPoint;
			}

			var distanceX = hsonObject.GetParameterFromName("distanceX");
			var distanceY = hsonObject.GetParameterFromName("height");
			var distanceZ = hsonObject.GetParameterFromName("distance");
			if (distanceX != null && distanceY != null && distanceZ != null && motionTime != null)
			{
				//var dir = Vector3.Forward.Rotated(new Vector3(1,0,0), (float) Mathf.DegToRad(inAngle.ValueFloatingPoint));
				//var dir = new Vector3((float)distanceX.ValueFloatingPoint, (float)distanceY.ValueFloatingPoint * 3, (float)-distanceZ.ValueFloatingPoint).Normalized();
				//dirLauncher.RelativeVelocityDir = dir;

				var gravity = 60f;  // BF defaults
				var velocityZ =  (distanceZ.ValueFloatingPoint * 1.2f) / (motionTime.ValueFloatingPoint);	// Adding constant because it kept undershooting. It's guestimate because I don't know what's the factor. Maybe global air drag?
				//	var velocityY = ((distanceY.ValueFloatingPoint) + (0.25f * gravity * motionTime.ValueFloatingPoint * motionTime.ValueFloatingPoint)) / (motionTime.ValueFloatingPoint);
				var velocityY = ((distanceY.ValueFloatingPoint + gravity * motionTime.ValueFloatingPoint) / (motionTime.ValueFloatingPoint));

				// (m + (m/s * s)) = m/s
				// m/s

				var velocity = (float)Mathf.Sqrt(velocityZ * velocityZ + velocityY * velocityY);
				var dir = new Vector3(0, (float)velocityY, -(float)velocityZ).Normalized();

				dirLauncher.RelativeVelocityDir = dir;
				dirLauncher.MinVelocity = velocity;
				dirLauncher.MaxVelocity = velocity;
				
				

				//GD.Print("Velocity = ", velocity, " ; dir = ", dir);
			}

			var size = hsonObject.GetParameterFromName("size");
			if (size != null)
			{
				switch(size.ValueString)	// NOTE: These are just guestimates
				{
					case "SIZE_S":
						dirLauncherNode.Scale = new Vector3(1, 1, 1);
						break;
					case "SIZE_M":
						dirLauncherNode.Scale = new Vector3(2, 2, 2);
						break;
					case "SIZE_L":
						dirLauncherNode.Scale = new Vector3(5, 5, 5);
						break;
				}
			}

			dirLauncher.KeepVelocityTime = 0.3f;


		}


		void SetDashPanelParams(Node3D dirLauncher, libHSON.Object hsonObject)
		{
			var firstSpeed = hsonObject.GetParameterFromName("speed");
			if (firstSpeed != null)
			{
				dirLauncher.Set("MinVelocity", firstSpeed.ValueFloatingPoint);
				dirLauncher.Set("MaxVelocity", firstSpeed.ValueFloatingPoint);
			}

			var ooc = hsonObject.GetParameterFromName("ocTime");
			if (ooc != null)
			{
				dirLauncher.Set("LockInputTime", ooc.ValueFloatingPoint);
			}
		}

		void SetPathParams(Node3D splineObject, libHSON.Object hsonObject)
		{
			splineObject.Position = Vector3.Zero;

			Path3D path = splineObject.GetNodeOrNull<Path3D>(".");
			path.Curve = new Curve3D();
			path.Curve.ClearPoints();
			var childrenUuids = hsonObject.GetParameter("setParameter/nodeList").ValueArray;
			for (int i = 0; i < childrenUuids.Count; i++)
			{
				var uuid = childrenUuids[i];
				var hsonNode = LibHSONProject.Objects[Guid.Parse(uuid.ValueString)];

				var newPosition = hsonNode.LocalPosition.ToVector3();
				var newRotation = hsonNode.LocalRotation.ToQuaternion();

				path.Curve.AddPoint(newPosition);

			}

			bool isLoop = false;
			if (hsonObject.GetParameter("setParameter/isLoopPath").ValueBoolean)
			{
				var uuid = childrenUuids[0];
				var hsonNode = LibHSONProject.Objects[Guid.Parse(uuid.ValueString)];

				var newPosition = hsonNode.LocalPosition.ToVector3();
				var newRotation = hsonNode.LocalRotation.ToQuaternion();
				path.Curve.AddPoint(newPosition);




				isLoop = true;
			}

			if (hsonObject.GetParameter("setParameter/pathType").ValueString == "GR_PATH")  // TODO: Think whenever it should be grind path BEFORE spawning it
			{
				var grindRail = path.GetNodeOrNull<GrindRail>(".");
				if (grindRail != null)
				{
					grindRail.Loop = hsonObject.GetParameter("setParameter/isLoopPath").ValueBoolean;
				}
			}

			for (int i = 0; i < childrenUuids.Count; i++)
			{
				var uuid = childrenUuids[i];
				var hsonNode = LibHSONProject.Objects[Guid.Parse(uuid.ValueString)];
				if (hsonNode.GetParameter("lineType").ValueString == "LINETYPE_SNS")
				{
					//if (i > 0 || isLoop)
					//{
					//	var prevIdx = i - 1;
					//	if (i==0)
					//	{
					//		prevIdx = childrenUuids.Count - 1;
					//	}
					//	var prevUuid = childrenUuids[prevIdx];
					//	var prevHsonNode = LibHSONProject.Objects[Guid.Parse(prevUuid.ValueString)];
					//	var prevPosition = new Vector3(prevHsonNode.LocalPosition.X, prevHsonNode.LocalPosition.Y, prevHsonNode.LocalPosition.Z);
					//	path.Curve.SetPointIn(i, prevPosition);
					//}
					if (i < childrenUuids.Count - 1 || isLoop)
					{
						var nextIdx = i + 1;
						if (i == childrenUuids.Count - 1)
						{
							nextIdx = 0;
						}
						var nextUuid = childrenUuids[nextIdx];
						var nextHsonNode = LibHSONProject.Objects[Guid.Parse(nextUuid.ValueString)];
						var nextPosition = nextHsonNode.LocalPosition.ToVector3();
						//path.Curve.SetPointOut(i, nextPosition);

					}
				}
			}

			// Set tilts
			for (int i = 0; i < childrenUuids.Count; i++)
			{
				var offset = path.Curve.GetClosestOffset(path.Curve.GetPointPosition(i));
				var pwr = path.Curve.SampleBakedWithRotation(offset, false, true);
				Vector3 pointUp;
				pointUp = pwr.Basis.Y;
				//pointUp = path.Curve.SampleBakedUpVector(offset, true);

				var uuid = childrenUuids[i];
				var hsonNode = LibHSONProject.Objects[Guid.Parse(uuid.ValueString)];
				var hsonNodeRotation = hsonNode.LocalRotation.ToQuaternion();


				var rotationOffset = new Quaternion((hsonNodeRotation * Vector3.Up).ProjectOnPlane(pwr.Basis.Z).Normalized() , pointUp.ProjectOnPlane(hsonNodeRotation * Vector3.Forward).Normalized() ).Normalized();

				//var rotationOffset = new Quaternion(pointUp.ProjectOnPlane(hsonNodeRotation.Normalized() * Vector3.Forward).Normalized(), (hsonNodeRotation.Normalized() * Vector3.Up).Normalized()).Normalized();
				var angle = -rotationOffset.GetAngle();

				path.Curve.SetPointTilt(i, angle);

				// 360 degree problem correction
				// Angle is within range from -180 degrees to 180. That means, for example, instead of going from 179 degrees it will jump to -179 instead of 181, reversing the direction. 
				if (i == 0) continue;
				var prevTilt = path.Curve.GetPointTilt(i - 1);
				int fullRotations = (int)(prevTilt / (Mathf.Pi * 2));
				angle += fullRotations * Mathf.Pi;
				var angleDiff = Mathf.Abs(angle - prevTilt);
				if (angleDiff > Mathf.Abs(angle-360 - prevTilt)) {
					angle = angle - 360;
				} else if (angleDiff > Mathf.Abs(angle - 360 - prevTilt)) {
					angle = angle + 360;
				}
				path.Curve.SetPointTilt(i, angle);

			}


		}

		void SetUpreelParams(Node3D upreel, libHSON.Object hsonObject)
		{
			var length = hsonObject.GetParameter("length");
			if (length != null)
			{
				Path3D path = upreel.GetNodeOrNull<Path3D>(".");
				path.Curve = new Curve3D();
				path.Curve.ClearPoints();


				path.Curve.AddPoint(-upreel.Basis.Y * (float)length.ValueFloatingPoint);
				path.Curve.AddPoint(Vector3.Zero);
			}
		}

		void SetBalloonParams(Node3D balloonNode, libHSON.Object hsonObject)
		{
			Balloon balloon = balloonNode.GetNodeOrNull<Balloon>(".");
			if (balloon == null) return;
			var upSpeed = hsonObject.GetParameter("upSpeed");
			if (upSpeed != null)
			{
				balloon.RelativeEjectVelocity = new Vector3(0, (float)upSpeed.ValueFloatingPoint, 0);
			}
		}

		void SetRedRingParams(Node3D redRingNode, libHSON.Object hsonObject)
		{
			CollectableItem item = redRingNode.GetNodeOrNull<CollectableItem>(".");
			if (item == null) return;
			var itemId = hsonObject.GetParameter("ItemId");
			if (itemId != null)
			{
				item.ItemName = "RedRing" + (itemId.ValueSignedInteger+1).ToString();
			}
		}
	}
}

public static class SystemNumericsConvert
{
	public static Vector3 ToVector3(this System.Numerics.Vector3 p)
	{
		return new Vector3(p.X, p.Y, p.Z);
	}

	public static Quaternion ToQuaternion(this System.Numerics.Quaternion p)
	{
		return new Quaternion(p.X, p.Y, p.Z, p.W).Normalized();
	}
}
