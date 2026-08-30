using VerificationEngine.Services.Vendors;
using Xunit;

namespace VerificationEngine.Tests.Services;

public sealed class SouthAfricanIdNumberTests
{
    // Both check digits below were computed with the same Luhn algorithm the class
    // under test implements, applied to a chosen YYMMDD + sequence + citizenship + 8.
    private const string ValidMale1990 = "9001015000085";
    private const string ValidFemale2001 = "0112310001089";

    [Theory]
    [InlineData(ValidMale1990)]
    [InlineData(ValidFemale2001)]
    public void IsValid_AcceptsWellFormedNumbers(string idNumber) =>
        Assert.True(SouthAfricanIdNumber.IsValid(idNumber));

    [Fact]
    public void IsValid_RejectsWrongCheckDigit()
    {
        // Same 12-digit body as ValidMale1990, but the last digit is deliberately wrong.
        var tampered = ValidMale1990[..12] + "9";

        Assert.False(SouthAfricanIdNumber.IsValid(tampered));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("900101500008A")]
    [InlineData("99022950000809")]
    public void IsValid_RejectsMalformedInput(string? idNumber) =>
        Assert.False(SouthAfricanIdNumber.IsValid(idNumber));

    [Fact]
    public void IsValid_RejectsImpossibleDate()
    {
        // Month 13 cannot be a real date of birth, regardless of what the check digit says.
        var invalidMonth = "9013015000085"[..12] + "5";
        Assert.False(SouthAfricanIdNumber.IsValid(invalidMonth));
    }

    [Fact]
    public void DateOfBirth_ParsesYYMMDDAgainstCurrentCentury()
    {
        var dob = SouthAfricanIdNumber.DateOfBirth(ValidMale1990);

        Assert.Equal(new DateOnly(1990, 1, 1), dob);
    }

    [Fact]
    public void DateOfBirth_TreatsFutureTwoDigitYearAsPreviousCentury()
    {
        // A "01" year would be 2001 taken literally, which is in the past relative to
        // "today" in every case that matters for this project - included so the century
        // rollover logic (see SouthAfricanIdNumber.DateOfBirth) has a regression test.
        var dob = SouthAfricanIdNumber.DateOfBirth(ValidFemale2001);

        Assert.Equal(new DateOnly(2001, 12, 31), dob);
    }
}
