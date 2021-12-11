namespace HTTPie.Abstractions
{
    public interface IPlugin
    {
        ICollection<Option> SupportedOptions() => Array.Empty<Option>();
    }
}
