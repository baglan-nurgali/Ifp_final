namespace IoT.Domain;

public enum SensorType { Temperature, Humidity, Pressure, PowerConsumption }
public enum DeviceStatus { Online, Warning, Critical, Offline }
public sealed record SensorDevice(Guid Id, string Name, string Location, SensorType Type, string Unit, DeviceStatus Status);
public sealed record SensorReading(Guid DeviceId, DateTimeOffset Timestamp, double Value, DeviceStatus Status);
public sealed record AlertRule(Guid DeviceId, double WarningMin, double WarningMax, double CriticalMin, double CriticalMax)
{
    public bool IsValid => double.IsFinite(WarningMin) && double.IsFinite(WarningMax) && double.IsFinite(CriticalMin) && double.IsFinite(CriticalMax) && CriticalMin <= WarningMin && WarningMin < WarningMax && WarningMax <= CriticalMax;
}
public sealed record AlertEvent(Guid Id, Guid DeviceId, DateTimeOffset Timestamp, double Value, DeviceStatus Severity, string Message, bool Acknowledged = false);
public readonly record struct Result<T, TError>(T? Value, TError? Error, bool IsSuccess)
{
    public static Result<T,TError> Ok(T value) => new(value, default, true);
    public static Result<T,TError> Fail(TError error) => new(default, error, false);
}
public static class DomainValidation
{
    public static Result<SensorReading,string> Validate(SensorReading reading) =>
        reading.DeviceId == Guid.Empty || !double.IsFinite(reading.Value) ? Result<SensorReading,string>.Fail("Reading must have a device and a finite value.") : Result<SensorReading,string>.Ok(reading);
}
