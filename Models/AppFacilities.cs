namespace Fixawyy.Models;

public static class AppFacilities
{
    public const string FacultyOfEngineering = "Faculty of Engineering";
    public const string MallFoodCourt = "Mall Food Court";

    public static readonly IReadOnlyList<string> All = new[]
    {
        FacultyOfEngineering,
        MallFoodCourt
    };
}
