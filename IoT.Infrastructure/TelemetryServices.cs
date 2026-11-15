using System.Threading.Channels;
using IoT.Application;
using IoT.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IoT.Infrastructure;

public sealed class SensorSimulator : ISensorSimulator
{
    private readonly Random _random = Random.Shared;
    private readonly Dictionary<Guid,double> _last = [];
    public IReadOnlyList<SensorDevice> Devices { get; } = new[] {
        new SensorDevice(Guid.Parse("10000000-0000-0000-0000-000000000001"),"Boiler Outlet","Plant room",SensorType.Temperature,"°C",DeviceStatus.Online),
        new SensorDevice(Guid.Parse("10000000-0000-0000-0000-000000000002"),"Clean Room","Zone A",SensorType.Humidity,"%",DeviceStatus.Online),
        new SensorDevice(Guid.Parse("10000000-0000-0000-0000-000000000003"),"Main Line","North wing",SensorType.Pressure,"bar",DeviceStatus.Online),
        new SensorDevice(Guid.Parse("10000000-0000-0000-0000-000000000004"),"Power Feed","Utility room",SensorType.PowerConsumption,"kW",DeviceStatus.Online) };
    public SensorReading Next(SensorDevice device, DateTimeOffset timestamp, bool anomaly = false)
    {
        var baseline = device.Type switch { SensorType.Temperature => 68d, SensorType.Humidity => 46d, SensorType.Pressure => 4.2d, _ => 18d };
        var value = _last.GetValueOrDefault(device.Id,baseline) + (_random.NextDouble()-.5) * (device.Type == SensorType.Temperature ? 1.6 : .8);
        if (anomaly) value += device.Type == SensorType.Temperature ? 38 : 12;
        else if (_random.NextDouble() < .015) value += 10;
        value = Math.Clamp(value, 0, device.Type == SensorType.Temperature ? 160 : device.Type == SensorType.Humidity ? 100 : 100);
        _last[device.Id] = value;
        return new(device.Id,timestamp,value,DeviceStatus.Online);
    }
}
public sealed class TelemetryRepository : ITelemetryRepository
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid,CircularBuffer<SensorReading>> _buffers = new();
    public ValueTask AppendAsync(IReadOnlyList<SensorReading> readings, CancellationToken cancellationToken = default) { foreach(var reading in readings) { cancellationToken.ThrowIfCancellationRequested(); if(DomainValidation.Validate(reading).IsSuccess) _buffers.GetOrAdd(reading.DeviceId,_=>new(240)).Add(reading); } return ValueTask.CompletedTask; }
    public IReadOnlyList<SensorReading> GetRecent(Guid deviceId, int count) => _buffers.TryGetValue(deviceId,out var buffer) ? buffer.Snapshot(count) : [];
}
public sealed class TelemetryPipeline
{
    private readonly Channel<TelemetryBatch> _channel = Channel.CreateBounded<TelemetryBatch>(new BoundedChannelOptions(8) { FullMode=BoundedChannelFullMode.DropOldest, SingleWriter=true });
    public ChannelReader<TelemetryBatch> Reader => _channel.Reader;
    public bool Publish(TelemetryBatch batch) => _channel.Writer.TryWrite(batch);
}
public sealed class SensorSimulationWorker(ISensorSimulator simulator, ITelemetryRepository repository, TelemetryPipeline pipeline, IAlertEvaluator evaluator, ILogger<SensorSimulationWorker> logger) : BackgroundService
{
    private volatile bool _running = true;
    private volatile bool _anomaly;
    private int _speed = 1;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid,bool> _online = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid,AlertRule> _rules = new();
    public void SetRunning(bool value) => _running=value;
    public void SetSpeed(int value) => _speed = value is 1 or 2 or 5 ? value : 1;
    public void InjectAnomaly() => _anomaly=true;
    public void SetDeviceOnline(Guid id,bool online) => _online[id]=online;
    public bool IsDeviceOnline(Guid id) => _online.GetValueOrDefault(id,true);
    public bool SetRule(AlertRule rule) { if (!rule.IsValid) return false; _rules[rule.DeviceId]=rule; return true; }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested) {
            await Task.Delay(TimeSpan.FromMilliseconds(800 / _speed),stoppingToken);
            if(!_running) continue;
            try {
                var now=DateTimeOffset.UtcNow; var anomaly=_anomaly; _anomaly=false;
                var devices=simulator.Devices.Where(d=>_online.GetValueOrDefault(d.Id,true)).ToArray();
                var readings=devices.Select((d,i)=>simulator.Next(d,now,anomaly && i==0)).ToArray();
                var alerts=readings.Select(r=> { var device=devices.First(d=>d.Id==r.DeviceId); var rule=_rules.GetOrAdd(r.DeviceId,_=>DefaultRule(device)); return evaluator.Evaluate(device,r,rule); }).Where(a=>a is not null).Cast<AlertEvent>().ToArray();
                var alertByDevice=alerts.ToDictionary(a=>a.DeviceId);
                var statusReadings=readings.Select(r=>alertByDevice.TryGetValue(r.DeviceId,out var alert) ? r with { Status=alert.Severity } : r).ToArray();
                await repository.AppendAsync(statusReadings,stoppingToken);
                pipeline.Publish(new(statusReadings,alerts,now));
            } catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { break; }
            catch(Exception ex) { logger.LogError(ex,"Telemetry simulation iteration failed"); }
        }
    }
    private static AlertRule DefaultRule(SensorDevice device) => device.Type switch {
        SensorType.Temperature=>new(device.Id,60,82,45,95),
        SensorType.Humidity=>new(device.Id,30,65,20,80),
        SensorType.Pressure=>new(device.Id,3.5,5.2,2.5,6.5),
        _=>new(device.Id,10,30,5,40)
    };
}
