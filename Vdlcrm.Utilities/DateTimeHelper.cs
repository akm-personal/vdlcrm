namespace Vdlcrm.Utilities;

public static class DateTimeHelper
{
    public static DateTime GetUtcNow()
    {
        return DateTime.UtcNow;
    }

    public static bool IsValidDate(string dateString)
    {
        return DateTime.TryParse(dateString, out _);
    }

    public static int GetAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age))
            age--;
        return age;
    }
}
