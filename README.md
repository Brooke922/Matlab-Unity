# Matlab-Unity
# Real-Time MATLAB-to-Unity 6-DOF TCP/IP Kinematic Bridge

A low-latency, multithreaded inter-process communication (IPC) pipeline connecting MATLAB to Unity for high-frequency 6-DOF spatial motion control. Designed for neuromotor research, VR feedback, and kinematic perturbation experiments.

## Demonstration Video

- **Visual Polish:** The 3D Cube tracks a continuous 6-DOF circular trajectory around origin, rendering physical **Red ($X$), Green ($Y$), Blue ($Z$)** coordinate axes and a persistent visual motion trail.
- **High-Frequency Performance:** MATLAB streams pose commands at **100 Hz** ($dt = 0.01\text{s}$), with Unity logging live throughput (~100 Hz) and sub-millisecond network latency in the Console.
- **Robustness:** Handles hot client disconnects and reconnects seamlessly without freezing the engine or requiring a Unity scene restart.

---

## Architecture & Technical Decisions

- **Network Model:** Persistent TCP/IP server-client bridge running on `127.0.0.1:55001`. Unity hosts an asynchronous `TcpListener`.
- **Wire Protocol:** Delimited ASCII (CSV) payload `X, Y, Z, Rx, Ry, Rz, Timestamp\n` with newline framing to prevent buffer fragmentation.
- **Thread Safety:** Network reading occurs on a background C# thread using `ConcurrentQueue<PoseData>` to safely synchronize data with Unity's main rendering loop.
- **Coordinate Transformations:** Handled unit conversions from MATLAB radians to Unity degrees (`Quaternion.Euler`) and smoothed visual updates via `Vector3.Lerp` & `Quaternion.Slerp`.

---

## Repository Structure

```text
├── Matlab/
│   ├── movecube.m       # Reusable 6-DOF command sender function
│   └── demo_cube.m      # 100 Hz continuous trajectory test script
├── UnityAssets/
│   ├── MatlabTcpReceiver.cs  # Main multithreaded TCP receiver & benchmark logger
│   └── DrawAxes.cs           # Physical 3D RGB origin coordinate marker generator
├── .demo recording.mp4   # 30-second side-by-side execution screen capture
├── .gitignore
└── README.md
