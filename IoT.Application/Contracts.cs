using IoT.Domain;

namespace IoT.Application;

public interface ISensorSimulator { IReadOnlyList<SensorDevice> Devices { get; } SensorReading Next(SensorDevice device, DateTimeOffset timestamp, bool anomaly = false); }
public interface IAlertEvaluator { AlertEvent? Evaluate(SensorDevice device, SensorReading reading, AlertRule rule); }
public interface ITelemetryRepository { ValueTask AppendAsync(IReadOnlyList<SensorReading> readings, CancellationToken cancellationToken = default); IReadOnlyList<SensorReading> GetRecent(Guid deviceId, int count); }
public sealed record TelemetryBatch(IReadOnlyList<SensorReading> Readings, IReadOnlyList<AlertEvent> Alerts, DateTimeOffset Timestamp);
public sealed record DeviceControl(Guid DeviceId, bool IsOnline);
public sealed record ThresholdUpdate(Guid DeviceId, double WarningMin, double WarningMax, double CriticalMin, double CriticalMax);

public sealed class AlertEvaluator : IAlertEvaluator
{
    public AlertEvent? Evaluate(SensorDevice device, SensorReading reading, AlertRule rule)
    {
        if (!rule.IsValid || !DomainValidation.Validate(reading).IsSuccess) return null;
        var severity = reading.Value < rule.CriticalMin || reading.Value > rule.CriticalMax ? DeviceStatus.Critical :
            reading.Value < rule.WarningMin || reading.Value > rule.WarningMax ? DeviceStatus.Warning : (DeviceStatus?)null;
        return severity is null ? null : new(Guid.NewGuid(), device.Id, reading.Timestamp, reading.Value, severity.Value, $"{device.Name} reading {reading.Value:0.##} {device.Unit} is outside its threshold.");
    }
}
