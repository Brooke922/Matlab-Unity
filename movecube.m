function movecube(hUnity, x, y, z, rx, ry, rz)
    % 1. Attach POSIX UTC timestamp for real-time latency benchmarking in Unity
    timestamp = posixtime(datetime('now', 'TimeZone', 'UTC'));
    
    % 2. Format 6-DOF payload + timestamp (CSV ASCII protocol)
    msg = sprintf('%.4f,%.4f,%.4f,%.4f,%.4f,%.4f,%.6f\n', x, y, z, rx, ry, rz, timestamp);
    
    % 3. Write and flush stream
    writeline(hUnity, msg);
    flush(hUnity);
end