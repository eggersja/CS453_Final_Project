Final project for CS453.

## Data specification for raw_boids

```
{timestamp}:{x_0},{y_0};{x_1},{y_1};...{x_(n-1)},{y_(n-1)};[cr][lf]
...[EOF]
```
Where each ordered pair is the coordinate of the boid at that moment in time.

# Execution Parameters

`datasets/raw_boids_base`:
Num Boids: 50
Boid Length: 10.0
Boid Width: 5.0
Trace Distance: 250.0
Num Traces: 10
Boid Speed: 200.0
Num Obstacles: 0
Obstacle Size: 30
Attracition Radius: 50.0
Repulsion Radius: 75.0
Orientation Radius: 75.0
Weight Attraction: 1.0
Weight Repulsion: 1.0
Weight Orientation: 1.0
Weight Goal: 3.0
Weight Current: 4.0
Weight Wall: 0.2
Weight Enemy: 10.0
Weight Obstacle: 1.0
Min X: -1000.0
Max X: 1000.0
Min Y: -1000.0
Max Y: 1000.0
Communication Model: Discreet_2D_FOV
Spawn Shape: Random
FOV: 240.0
Goal Radius: 300.0
Epochs: 10
Sim Timeout Seconds: 45
Share Goal Location: True
Log Time Step: 0.25
