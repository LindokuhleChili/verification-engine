namespace VerificationEngine.Services.Vendors;

/// <summary>
/// Every simulated vendor response carries this prefix in its detail message, so a
/// mocked result is never mistaken for a real one — in the UI, in logs, or in a demo.
/// </summary>
public static class MockNotice
{
    public const string Prefix = "[SIMULATED]";

    public static string Wrap(string message) => $"{Prefix} {message}";
}
