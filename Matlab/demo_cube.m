clear all; clc;

% Connect to Unity TCP Server Bridge
fprintf('Connecting to Unity TCP Bridge on 127.0.0.1:55001...\n');
hUnity = tcpclient("127.0.0.1", 55001);
fprintf('Connected successfully!\n\n');

% High-frequency streaming parameters (>60 Hz requirement)
targetHz = 100;          % 100 Hz streaming rate
dt = 1 / targetHz;       % 0.01s frame interval
duration = 10;           % 10-second simulation loop
totalFrames = duration * targetHz;

t = linspace(0, 2*pi, totalFrames);

fprintf('Streaming 100 Hz 6-DOF Trajectory to Unity...\n');

tic;
for i = 1:totalFrames
    loopStart = toc;
    
    % 6-DOF circular trajectory kinematics
    x = 5 * cos(t(i));
    y = 5 * sin(t(i));
    z = 0;
    
    rx = 0;
    ry = 0;
    rz = t(i) * (180 / pi); % Continuous roll (degrees)
    
    % Command pose update
    movecube(hUnity, x, y, z, rx, ry, rz);
    
    % High-precision pacing loop
    elapsed = toc - loopStart;
    if dt > elapsed
        pause(dt - elapsed);
    end
end

fprintf('\nTrajectory stream completed successfully!\n');
clear hUnity; % Clean connection teardown
