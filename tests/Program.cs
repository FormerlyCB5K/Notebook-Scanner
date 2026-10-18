using System.Buffers.Binary;
using System.Security.Cryptography;
using NotebookScanner;

int passed = 0;
// Minimal SOF fixture tests the protocol validator, not native JPEG decoding.
byte[] jpeg = [255, 216, 255, 192, 0, 11, 8, 0, 10, 0, 20, 1, 1, 17, 0, 255, 217];
byte[] Frame(byte[] image) {
    var result = new byte[36 + image.Length];
    BinaryPrimitives.WriteUInt32BigEndian(result, (uint)image.Length);
    SHA256.HashData(image).CopyTo(result, 4); image.CopyTo(result, 36); return result;
}
async Task Reject(string name, byte[] data) {
    try { await Wire.Receive(new MemoryStream(data), default); throw new Exception("Accepted " + name); }
    catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException) { passed++; }
}
var frame = Frame(jpeg);
var valid = await Wire.Receive(new FragmentedStream(frame), default);
if (!valid.SequenceEqual(jpeg)) throw new Exception("Fragmented frame mismatch"); passed++;
var both = new MemoryStream(frame.Concat(frame).ToArray());
await Wire.Receive(both, default); await Wire.Receive(both, default);
if (both.Position != both.Length) throw new Exception("Frame boundary mismatch"); passed++;
for (int i = 0; i < frame.Length; i++) await Reject("truncated at " + i, frame[..i]);
await Reject("zero length", new byte[4]);
await Reject("oversized", new byte[] { 0, 128, 0, 1 });
var corrupt = frame.ToArray(); corrupt[^1] ^= 1; await Reject("corruption", corrupt);
await Reject("non-JPEG", Frame(new byte[] { 1, 2, 3 }));
var enormous = jpeg.ToArray(); enormous[7] = 127; await Reject("huge dimensions", Frame(enormous));
var progressive = jpeg.ToArray(); progressive[3] = 194; await Reject("unsupported coding", Frame(progressive));
var malformed = jpeg.ToArray(); malformed[5] = 255; await Reject("bad segment", Frame(malformed));
using (var cancelled = new CancellationTokenSource()) {
    cancelled.Cancel();
    try { await Wire.Receive(new MemoryStream(frame), cancelled.Token); throw new Exception("Cancellation ignored"); }
    catch (OperationCanceledException) { passed++; }
}
Console.WriteLine($"PASS: {passed} protocol checks (fragmentation, boundaries, every truncation, corruption, limits, cancellation).");

// Exercise actual retry supervision with simulated disconnections and cancellation.
int connectionChecks = 0;
using (var stop = new CancellationTokenSource()) {
    int attempts = 0, active = 0, maximumActive = 0, failures = 0;
    var enteredSecond = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    Task loop = ConnectionRetry.Run(async token => {
        maximumActive = Math.Max(maximumActive, ++active);
        try {
            if (++attempts == 1) throw new IOException("simulated disconnect");
            enteredSecond.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        } finally { active--; }
    }, (_, _) => failures++, stop.Token, TimeSpan.FromMilliseconds(5));
    await enteredSecond.Task.WaitAsync(TimeSpan.FromSeconds(5));
    stop.Cancel();
    try { await loop; throw new Exception("Connection cancellation ignored"); }
    catch (OperationCanceledException) { }
    if (attempts != 2 || failures != 1 || maximumActive != 1 || active != 0) throw new Exception("Retry attempts overlapped or failed to clean up");
    connectionChecks++;
}
using (var stop = new CancellationTokenSource()) {
    int attempts = 0;
    var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    Task loop = ConnectionRetry.Run(_ => { attempts++; throw new IOException("phone not ready"); },
        (_, _) => waiting.SetResult(), stop.Token, TimeSpan.FromSeconds(30));
    await waiting.Task.WaitAsync(TimeSpan.FromSeconds(5)); stop.Cancel();
    try { await loop.WaitAsync(TimeSpan.FromSeconds(2)); throw new Exception("Backoff cancellation ignored"); }
    catch (OperationCanceledException) { }
    if (attempts != 1) throw new Exception("Retried after Pause");
    connectionChecks++;
}
int deniedAttempts = 0;
try {
    await ConnectionRetry.Run(_ => { deniedAttempts++; throw new UnauthorizedAccessException(); },
        (_, _) => throw new Exception("Repeated access denial"), default);
    throw new Exception("Access denial ignored");
} catch (UnauthorizedAccessException) { if (deniedAttempts != 1) throw; connectionChecks++; }
using (var stop = new CancellationTokenSource()) {
    stop.Cancel(); int attempts = 0;
    try {
        await ConnectionRetry.Run(_ => { attempts++; return Task.CompletedTask; }, (_, _) => { }, stop.Token);
        throw new Exception("Started after pre-cancellation");
    } catch (OperationCanceledException) { if (attempts != 0) throw; connectionChecks++; }
}
string memoryDirectory = Path.Combine(Environment.CurrentDirectory, ".tools", "memory-tests", Guid.NewGuid().ToString("N"));
string memoryFile = Path.Combine(memoryDirectory, "phone.json");
if (DeviceMemory.Read(memoryFile) != null) throw new Exception("Missing preferences accepted");
DeviceMemory.Write(memoryFile, "chosen-phone-1");
if (DeviceMemory.Read(memoryFile) != "chosen-phone-1") throw new Exception("Phone choice not retained");
DeviceMemory.Write(memoryFile, "chosen-phone-2");
if (DeviceMemory.Read(memoryFile) != "chosen-phone-2") throw new Exception("Phone choice not replaced");
File.WriteAllText(memoryFile, "broken preferences");
if (DeviceMemory.Read(memoryFile) != null) throw new Exception("Corrupt preferences did not reset safely");
File.Delete(memoryFile); Directory.Delete(memoryDirectory);
connectionChecks += 4;
Console.WriteLine($"PASS: {connectionChecks} connection/preferences scenarios (serial retry, cancel in-flight/backoff, denied access, safe device memory).");

sealed class FragmentedStream(byte[] bytes) : MemoryStream(bytes)
{
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => base.ReadAsync(buffer[..Math.Min(1, buffer.Length)], cancellationToken);
}
