using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Carriage : Sprite
{
    [Export]
    public float TrainSpeed = 1;
    
    [Export] public Vector2 InitialWaypoint;
    public Carriage PulledBy;
    public Carriage Pulling;

    public Resources resources;
    
    public Queue<Vector2> waypointQueue;
    public Vector2 lastWaypoint;

    private float displacementEpsilon = 5f;
    protected GameManager gameManager;
    public Sprite CarriedResourceSprite;

    public override void _Ready()
    {
        gameManager = GetTree().Root.GetChild(0).GetNode<GameManager>("GameManager");
        CarriedResourceSprite = GetNode<Sprite>("CarriedResourceSprite");
        
        waypointQueue = new Queue<Vector2>();
        if (InitialWaypoint != new Vector2(0, 0))
        {
            this.AddWaypoint(InitialWaypoint);
        }
        
        lastWaypoint = this.Position;
    }

    public override void _Process(float delta)
    {
        MoveToWaypoint(delta);
    }

    // Figure out whether we're going straight or curvy and call the corresponding function
    private void MoveToWaypoint(float delta) 
    {
        if (waypointQueue.Count == 0 || gameManager.StopMovement) return; // No waypoints in queue, so do nothing. Also stops if GameManager stops movement
        
        Vector2 nextWaypoint = waypointQueue.Peek();
        Vector2 direction = this.GlobalPosition.DirectionTo(nextWaypoint);
        double angle = Math.Atan2(direction.y, direction.x);
        this.GlobalRotation = (float) (angle + Math.PI / 2);
        CarriedResourceSprite.GlobalRotation = 0f;

        this.Position = Lerp(lastWaypoint, nextWaypoint, delta); 
        if (IsDistanceSmallEnough(this.Position, nextWaypoint, displacementEpsilon))
        {
            this.Position = nextWaypoint;
            lastWaypoint = nextWaypoint;
            GD.Print("==========");
            foreach (Vector2 waypoint in waypointQueue)
            {
                GD.Print(waypoint);
            }
            GD.Print("==========");
            waypointQueue.Dequeue();
        }
    }

    public void AddWaypoint(Vector2 newPosition)
    { 
        waypointQueue.Enqueue(newPosition);
    }

    private Vector2 Lerp(Vector2 firstVector, Vector2 secondVector, float delta)
    {
        Vector2 direction = firstVector.DirectionTo(secondVector);
        Vector2 movement = direction * TrainSpeed * delta;
        return this.Position + movement;
    }

    private bool IsDistanceSmallEnough(Vector2 a, Vector2 b, float epsilon)
    {
        return a.DistanceTo(b) < epsilon;
    }

    private bool AreFloatsCloseEnough(float a, float b, float epsilon)
    {
        return Math.Abs(b - a) < epsilon;
    }

    public bool AddCarriage(Resources resources)
    {
        if (Pulling != null) return false;
        
        Carriage newCarriage = GD.Load<PackedScene>("res://scenes/Carriage.tscn").Instance<Carriage>();

        Vector2 direction = this.GlobalPosition.DirectionTo(waypointQueue.Peek());
        Vector2 backPosition = new Vector2(-180, -180) * direction;
        
        newCarriage.GlobalPosition = this.GlobalPosition + backPosition;
        newCarriage.InitialWaypoint = this.lastWaypoint;
        newCarriage.TrainSpeed = this.TrainSpeed;
        newCarriage.PulledBy = this;

        String textureName = ResourcesMethods.GetResourceNameForResource(resources);
        String texturePath = "res://assets/art/carriages/" + 
                                (textureName == null ? "Carriage Gold Gem Dynamite.PNG" : textureName);
        newCarriage.Texture = GD.Load<Texture>(texturePath);


        newCarriage.resources = resources;
        this.Pulling = newCarriage;
        GetTree().Root.AddChild(newCarriage);
        
        //Sets carriage resource icon based on resource type of carriage
        string resourceTextureName = ResourcesMethods.GetResourceIconName(resources);
        string resourceTexturePath = "res://assets/art/resources/" +
                                     (resourceTextureName == null ? "Gold.PNG" : resourceTextureName);
        newCarriage.CarriedResourceSprite.Texture = GD.Load<Texture>(resourceTexturePath);
        
        return true;
    }

    public bool ShiftCarriageUpOne()
    {
        if (this.Pulling != null) this.Pulling.ShiftCarriageUpOne();
        this.GlobalPosition = this.PulledBy.GlobalPosition;
        this.GlobalRotation = this.PulledBy.GlobalRotation;
        this.waypointQueue = this.PulledBy.waypointQueue;
        this.lastWaypoint = this.PulledBy.lastWaypoint;
        return true;
    }

    public bool RemoveCarriage()
    {
        if (this.Pulling != null)
        {
            this.Pulling.ShiftCarriageUpOne();
            this.Pulling.PulledBy = this.PulledBy;
        }
        this.PulledBy.Pulling = this.Pulling;
        this.QueueFree();
        return true;
    }

    public Carriage getNextCarriageWithResource(Resources resources)
    {
        if (this.resources == resources) return this;
        if (this.Pulling == null) return null;
        return this.Pulling.getNextCarriageWithResource(resources);
    }
    public void TrainBodyCollision(Area2D area)
    {
        if (area.Name != "CrashChecker") return;
        GD.Print("Body collision");
        gameManager.IsGameOver = true;
    }

}
