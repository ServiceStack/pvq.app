namespace MyApp.ServiceModel;

public class PeriodicTasks
{
    public PeriodicFrequency PeriodicFrequency { get; set; }
}
public enum PeriodicFrequency
{
    Minute,
    Hourly,
    Daily,
    Monthly,
}
