using Godot;
using System;

public partial class InfiniteItemSpawner : Node3D
{
    [Export] public PackedScene ItemTemplate;
    [Export] public int MinCount = 0;
    [Export] public int MaxCount = 1;
    [Export] public float SpawnRate = 1f;
    private float spawnTimer;
    [Export] public Vector3 InitialRelativeVelocity = Vector3.Up;
    [Export] public Node3D Container;

    public override void _PhysicsProcess(double delta)
    {
        if (Container.GetChildCount() >= MaxCount)
        {
            spawnTimer = 0;
            return;
        }
        spawnTimer += (float)delta;
        if (Container.GetChildCount() < MinCount)
        {
            spawnTimer = SpawnRate;
        }
        if (spawnTimer < SpawnRate)
        {
            return;
        }
        var newItem = ItemTemplate.Instantiate<CollectableItem>();
        Container.AddChild(newItem);
        newItem.Position = Vector3.Zero;
        
    }
}
