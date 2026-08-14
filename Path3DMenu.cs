#if TOOLS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using static Godot.EditorPlugin;

partial class Path3DMenu : MenuButton
{
	public override void _EnterTree()
	{
		this.Text = "BF Curve tools";

		//AddControlToContainer(CustomControlContainer.SpatialEditorMenu, this);

		this.GetPopup().AddItem("Convert to grind rail", 0);
		this.GetPopup().AddItem("Convert to autorun", 1);
		this.GetPopup().AddItem("Flatten curve", 2);
		this.GetPopup().AddItem("Make spring chain", 3);
		this.GetPopup().IdPressed += this.OnMenuSelect;

		EditorInterface.Singleton.GetSelection().SelectionChanged += OnSelectionChange;
		OnSelectionChange();    // To refresh visibility
	}
	public override void _ExitTree()
	{
		this.GetPopup().IdPressed -= OnMenuSelect;
		EditorInterface.Singleton.GetSelection().SelectionChanged -= OnSelectionChange;
	}


	public void OnMenuSelect(long id)
	{
		switch (id)
		{
			case 0:
				ConvertToGrindRail();
				break;
			case 1:
				ConvertToAutorun();
				break;
			case 2:
				FlattenCurve();
				break;
			case 3:
				MakeSpringChain();
				break;

		}
	}

	public void OnSelectionChange()
	{
		this.Visible = false;
		var selectedNodes = EditorInterface.Singleton.GetSelection().GetSelectedNodes();
		foreach (var selectedNode in selectedNodes)
		{
			if (selectedNode is Path3D path3D)
			{
				this.Visible = true;
				break;
			}
		}
	}

	private void ConvertToGrindRail()
	{
		var selectedNodes = EditorInterface.Singleton.GetSelection().GetSelectedNodes();
		foreach (var selectedNode in selectedNodes)
		{
			if (selectedNode is Path3D path3D)
			{
				var editorSettings = EditorInterface.Singleton.GetEditorSettings();
				bool generateVisual = editorSettings.Get(BadnikFrameworkExtras.SETTING_GENERATE_RAIL_MESH).AsBool();
				var csgPolygon3DVisual = new CsgPolygon3D();
				var existingVisual = selectedNode.GetNodeOrNull("./RailVisual");
				if (existingVisual != null)
				{
					existingVisual.Free();
				}
				if (generateVisual)
				{
					// Visual polygon
					csgPolygon3DVisual.Polygon = new Vector2[]
					{
					new Vector2(-0.1f, -0.1f),
					new Vector2(-0.1f, 0f),
					new Vector2(0.1f, 0f),
					new Vector2(0.1f, -0.1f),
					};
					csgPolygon3DVisual.Mode = CsgPolygon3D.ModeEnum.Path;
					csgPolygon3DVisual.PathNode = "..";
					csgPolygon3DVisual.Name = "RailVisual";
					csgPolygon3DVisual.PathInterval = 1f;
					csgPolygon3DVisual.PathSimplifyAngle = 1f;
					csgPolygon3DVisual.PathContinuousU = true;
					csgPolygon3DVisual.PathLocal = true;
					csgPolygon3DVisual.PathIntervalType = CsgPolygon3D.PathIntervalTypeEnum.Distance;

					StandardMaterial3D csgPolygon3DVisualMaterial = new StandardMaterial3D();
					csgPolygon3DVisualMaterial.AlbedoColor = editorSettings.Get(BadnikFrameworkExtras.SETTING_GENERATE_RAIL_MESH_COLOR).AsColor();
					csgPolygon3DVisual.Material = csgPolygon3DVisualMaterial;

					selectedNode.AddChild(csgPolygon3DVisual);
					csgPolygon3DVisual.Owner = EditorInterface.Singleton.GetEditedSceneRoot();
				}

				// Hitbox polygon
				var csgPolygon3DHitbox = new CsgPolygon3D();
				csgPolygon3DHitbox.Polygon = new Vector2[]
				{
					new Vector2(-1f, -0f),
					new Vector2(-1f, 1f),
					new Vector2(1f, 1f),
					new Vector2(1f, 0f),
				};
				csgPolygon3DHitbox.Mode = CsgPolygon3D.ModeEnum.Path;
				csgPolygon3DHitbox.PathNode = "..";
				csgPolygon3DHitbox.Name = "RailHitbox";
				csgPolygon3DHitbox.PathInterval = 1f;
				csgPolygon3DHitbox.PathSimplifyAngle = 5f;
				csgPolygon3DHitbox.PathContinuousU = true;
				csgPolygon3DHitbox.PathLocal = true;
				csgPolygon3DHitbox.PathIntervalType = CsgPolygon3D.PathIntervalTypeEnum.Distance;
				csgPolygon3DHitbox.Visible = false;

				var existingHitbox = selectedNode.GetNodeOrNull("./RailHitbox");
				if (existingHitbox != null)
				{
					existingHitbox.Free();
				}

				selectedNode.AddChild(csgPolygon3DHitbox);
				csgPolygon3DHitbox.Owner = EditorInterface.Singleton.GetEditedSceneRoot();

				// Add actual hitbox
				var area3D = new Area3D();
				area3D.Name = "Area3D";
				area3D.CollisionLayer = 256;    // TriggersForPlayer
				area3D.CollisionMask = 2;    // Player
				var existingArea = selectedNode.GetNodeOrNull("./Area3D");
				if (existingArea != null)
				{
					existingArea.Free();
				}
				selectedNode.AddChild(area3D);
				area3D.Owner = EditorInterface.Singleton.GetEditedSceneRoot();

				// Add shape
				var shape = new CollisionShape3D();
				shape.Shape = new ConcavePolygonShape3D();
				shape.Name = "CollisionShape3D";
				var existingShape = selectedNode.GetNodeOrNull("./CollisionShape3D");
				if (existingShape != null)
				{
					existingShape.Free();
				}
				area3D.AddChild(shape);
				shape.Owner = EditorInterface.Singleton.GetEditedSceneRoot();

				// Add grind rail script
				var path = EditorInterface.Singleton.GetEditedSceneRoot().GetPathTo(selectedNode);
				GrindRail rail = EditorInterface.Singleton.GetEditedSceneRoot().GetNodeOrNull<GrindRail>(path);
				if (rail == null)
				{
					selectedNode.SetScript(ResourceLoader.Load("res://Scripts/Interactables/GrindRail.cs"));
					rail = EditorInterface.Singleton.GetEditedSceneRoot().GetNode<GrindRail>(path);
					//rail = new GrindRail();
					//selectedNode.SetScript(rail);

				}
				rail.BoundingBox = shape;
				rail.RailHitboxMesh = csgPolygon3DHitbox;
				if (generateVisual)
				{
					rail.RailVisibleMesh = csgPolygon3DVisual;
				}
				rail.Hitbox = area3D;
				rail.CurveChanged += rail.OnCurveChanged;

			}
		}
	}

	private void ConvertToAutorun()
	{
		var selectedNodes = EditorInterface.Singleton.GetSelection().GetSelectedNodes();
		foreach (var selectedNode in selectedNodes)
		{
			if (selectedNode is Path3D path3D)
			{

				// Hitbox polygon
				var csgPolygon3DHitbox = new CsgPolygon3D();
				csgPolygon3DHitbox.Polygon = new Vector2[]
				{
					new Vector2(-5f, -5f),
					new Vector2(-5f, 5f),
					new Vector2(5f, 5f),
					new Vector2(5f, -5f),
				};
				csgPolygon3DHitbox.Mode = CsgPolygon3D.ModeEnum.Path;
				csgPolygon3DHitbox.PathNode = "..";
				csgPolygon3DHitbox.Name = "CSGPolygon3DHitbox";
				csgPolygon3DHitbox.PathInterval = 1f;
				csgPolygon3DHitbox.PathSimplifyAngle = 5f;
				csgPolygon3DHitbox.PathContinuousU = true;
				csgPolygon3DHitbox.PathLocal = true;
				csgPolygon3DHitbox.PathIntervalType = CsgPolygon3D.PathIntervalTypeEnum.Distance;
				csgPolygon3DHitbox.Visible = false;

				var existingHitbox = selectedNode.GetNodeOrNull("./CSGPolygon3DHitbox");
				if (existingHitbox != null)
				{
					existingHitbox.Free();
				}

				selectedNode.AddChild(csgPolygon3DHitbox);
				csgPolygon3DHitbox.Owner = EditorInterface.Singleton.GetEditedSceneRoot();

				// Add actual hitbox
				var spline2DTrigger = new Spline2DTrigger();
				spline2DTrigger.Name = "Area3D";
				spline2DTrigger.CollisionLayer = 256;    // TriggersForPlayer
				spline2DTrigger.CollisionMask = 2;    // Player
				var existingArea = selectedNode.GetNodeOrNull("./Area3D");
				if (existingArea != null)
				{
					existingArea.Free();
				}
				selectedNode.AddChild(spline2DTrigger);
				spline2DTrigger.Owner = EditorInterface.Singleton.GetEditedSceneRoot();

				spline2DTrigger.Path = path3D;
				spline2DTrigger.ReleaseOnDistanceFromPath = 20;

				// Add shape
				var shape = new CollisionShape3D();
				shape.Shape = new ConcavePolygonShape3D();
				shape.Name = "CollisionShape3D";
				var existingShape = selectedNode.GetNodeOrNull("./CollisionShape3D");
				if (existingShape != null)
				{
					existingShape.Free();
				}
				spline2DTrigger.AddChild(shape);
				shape.Owner = EditorInterface.Singleton.GetEditedSceneRoot();

				// Add shape copier. It has to be a separate node because CSGPolygon3D is... just weird to work with.
				var copyShape = new CollissionShapeFromCSGPolygon();
				copyShape.Name = "CopyShape";
				shape.AddChild(copyShape);
				copyShape.Owner = EditorInterface.Singleton.GetEditedSceneRoot();

				copyShape.Shape = shape;
				copyShape.CsgMesh = csgPolygon3DHitbox;
			}
		}
	}

	private void FlattenCurve()
	{
		var selectedNodes = EditorInterface.Singleton.GetSelection().GetSelectedNodes();
		foreach (var selectedNode in selectedNodes)
		{
			if (selectedNode is Path3D path3D)
			{
				if (path3D.Curve.PointCount == 0) continue;
				float averageY = 0;
				for (int i = 0; i < path3D.Curve.PointCount; i++)
				{
					averageY += path3D.Curve.GetPointPosition(i).Y;
				}
				averageY /= path3D.Curve.PointCount;
				for (int i = 0; i < path3D.Curve.PointCount; i++)
				{
					var pos = path3D.Curve.GetPointPosition(i);
					pos.Y = averageY;
					path3D.Curve.SetPointPosition(i, pos);

					var pointIn = path3D.Curve.GetPointIn(i);
					pointIn.Y = 0;
					path3D.Curve.SetPointIn(i, pointIn);

					var pointOut = path3D.Curve.GetPointOut(i);
					pointOut.Y = 0;
					path3D.Curve.SetPointOut(i, pointOut);

				}
			}
		}
	}
	private void MakeSpringChain()
	{
		var selectedNodes = EditorInterface.Singleton.GetSelection().GetSelectedNodes();
		const string SPRING_PREFAB_PATH = "res://SonicAssets/Prefabs/Interactables/[interactable]_spring_red.tscn";
		foreach (var selectedNode in selectedNodes)
		{
			if (selectedNode is Path3D path3D)
			{
				if (path3D.Curve.PointCount <= 1) return;
				var children = path3D.GetChildren();
				foreach (var child in children)
				{
					child.Free();
				}

				for (int i = 0; i < path3D.Curve.PointCount - 1; i++)
				{
					var thisPoint = path3D.GlobalTransform * path3D.Curve.GetPointPosition(i);
					var nextPoint = path3D.GlobalTransform * path3D.Curve.GetPointPosition(i + 1);

					var launcherPrefab = ResourceLoader.Load<PackedScene>(SPRING_PREFAB_PATH);
					var spring = launcherPrefab.Instantiate<DirectionalLauncher>();
					path3D.AddChild(spring);
					spring.Owner = EditorInterface.Singleton.GetEditedSceneRoot();
					spring.GlobalPosition = thisPoint;
					spring.LookAt(nextPoint);
					spring.Snap = true;
					spring.MinVelocity = 100;
					spring.MaxVelocity = 100;
					spring.KeepVelocityDistance = thisPoint.DistanceTo(nextPoint);

					var editorSettings = EditorInterface.Singleton.GetEditorSettings();
					spring.Scale = editorSettings.Get(BadnikFrameworkExtras.SETTING_GENERATE_SPRING_CHAIN_SPRING_SCALE).AsVector3();
				}
			}
		}
	}

}

#endif
