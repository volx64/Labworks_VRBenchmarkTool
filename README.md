# Labworks_VRBenchmarkTool
An open-source Unity package for creating camera fly-through benchmarks, recording runtime data and in-engine graph analysis

Credit to:
Mert Öztürk for the "PlottingGraph" class - unity-graph-editor:
https://github.com/mertcanozturk/unity-graph-editor

Easiest way to install this package is via Unity's Package Manager:
  Open Package manager
  Press the + button in the top left corner
  Select "add package from git URL"
  Paste `https://github.com/volx64/Labworks_VRBenchmarkTool.git`

  <img width="349" height="274" alt="image" src="https://github.com/user-attachments/assets/9fdb9bc9-41ee-471f-b3ae-9c61aa2b612f" />


To get started create an new Empty game object and add a `VR Benchmark Player` component.
Open Benchmark editor and create new benchmark data
Add points using camera's position and tweak
Populate the "Benchmark Name" field 
Populate the Player object field with a Seperate camera object with no XR tracking
Enable Force run benchmark, and enter play-mode.
