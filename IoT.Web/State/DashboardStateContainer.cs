using IoT.Application;
using IoT.Domain;

namespace IoT.Web.State;

public sealed class DashboardStateContainer
{
    private readonly object _gate = new();
    private IReadOnlyList<SensorReading> _readings=[];
    private IReadOnlyList<AlertEvent> _alerts=[];
    public event Action? OnChange;
    public IReadOnlyList<SensorReading> Readings { get { lock(_gate) return _readings; } }
    public IReadOnlyList<AlertEvent> Alerts { get { lock(_gate) return _alerts; } }
    public void Apply(TelemetryBatch batch) { lock(_gate) { _readings=_readings.Concat(batch.Readings).TakeLast(240).ToArray(); _alerts=_alerts.Concat(batch.Alerts).TakeLast(100).ToArray(); } OnChange?.Invoke(); }
    public void AddAlert(AlertEvent alert) { lock(_gate) _alerts=_alerts.Append(alert).TakeLast(100).ToArray(); OnChange?.Invoke(); }
    public void Acknowledge(Guid id) { lock(_gate) _alerts=_alerts.Select(a=>a.Id==id?a with {Acknowledged=true}:a).ToArray(); OnChange?.Invoke(); }
}
