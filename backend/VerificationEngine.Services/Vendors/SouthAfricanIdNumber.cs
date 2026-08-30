namespace VerificationEngine.Services.Vendors;

/// <summary>
/// Structural validation of a 13-digit South African ID number: YYMMDD SSSS C A Z,
/// checked with the Luhn algorithm.
///
/// This is genuinely real logic, not a mock — it is the same check Home Affairs applies.
/// It proves the number is well-formed; it cannot prove the person exists, which is
/// exactly why the biometric step exists as well.
/// </summary>
public static class SouthAfricanIdNumber
{
    public static bool IsValid(string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber)) return false;

        var digits = idNumber.Trim();
        if (digits.Length != 13 || !digits.All(char.IsAsciiDigit)) return false;

        return DateOfBirth(digits) is not null && PassesLuhn(digits);
    }

    /// <summary>
    /// The first six digits are a two-digit year, so the century is ambiguous. We
    /// resolve it by assuming nobody claiming shares is over 100 years old.
    /// </summary>
    public static DateOnly? DateOfBirth(string idNumber)
    {
        if (idNumber.Length < 6) return null;

        if (!int.TryParse(idNumber[..2], out var yy) ||
            !int.TryParse(idNumber[2..4], out var mm) ||
            !int.TryParse(idNumber[4..6], out var dd))
            return null;

        var currentYear = DateTime.UtcNow.Year;
        var year = 2000 + yy;
        if (year > currentYear) year -= 100;

        try
        {
            return new DateOnly(year, mm, dd);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static bool PassesLuhn(string digits)
    {
        var sum = 0;
        var doubleIt = false;

        // Walk right to left, doubling every second digit.
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var value = digits[i] - '0';

            if (doubleIt)
            {
                value *= 2;
                if (value > 9) value -= 9;
            }

            sum += value;
            doubleIt = !doubleIt;
        }

        return sum % 10 == 0;
    }
}
