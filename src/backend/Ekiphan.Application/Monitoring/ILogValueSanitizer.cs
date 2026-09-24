namespace Ekiphan.Application.Monitoring;

public interface ILogValueSanitizer
{
    object? Sanitize(string propertyName, object? value);
}
