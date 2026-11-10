using BlueprintSchool.School;
using BlueprintSchool.Simulation.Staff;
using BlueprintSchool.UI;
using Godot;

namespace BlueprintSchool.Core;

/// <summary>
/// An art review sheet for the smoke test: pupils from Year 7 to Year 11 in each house's tie, then
/// staff with their lanyards (green teachers, purple support staff, gold leadership), each standing and seated.
/// </summary>
public partial class FigureSheet : Control
{
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), UiStyle.Ground);
        var font = GetThemeDefaultFont();
        const float zoom = 2.2f;
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawString(font, new Vector2(40, 50), "Pupils: Year 7 to Year 11, one row per house", HorizontalAlignment.Left, -1, 22, UiStyle.TextDark);

        for (var house = 0; house < 4; house++)
        {
            for (var year = 7; year <= 11; year++)
            {
                for (var k = 0; k < 2; k++)
                {
                    var seed = (house * 97) + (year * 13) + k;
                    var x = 70 + ((((year - 7) * 2) + k) * 72);
                    var y = 160 + (house * 120);
                    DrawSetTransform(new Vector2(x, y), 0, new Vector2(zoom, zoom) * 0.6f);
                    Figures.Standing(this, Vector2.Zero, Looks.Pupil(seed, year, house, female: k == 1), 0, walking: false, seed);
                }
            }

            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            DrawString(font, new Vector2(800, 150 + (house * 120)), new[] { "Red house", "Blue house", "Gold house", "Purple house" }[house], HorizontalAlignment.Left, -1, 16, Looks.HouseColours[house]);
        }

        DrawString(font, new Vector2(1000, 50), "Staff and their lanyards", HorizontalAlignment.Left, -1, 22, UiStyle.TextDark);
        var staff = new (StaffRole Role, string Grade, string Label)[]
        {
            (StaffRole.Teacher, "Teacher", "Teacher"),
            (StaffRole.Teacher, "Experienced teacher", "Experienced"),
            (StaffRole.Teacher, "Head of Year", "Leadership"),
            (StaffRole.TeachingAssistant, "Teaching assistant", "TA"),
            (StaffRole.Receptionist, "Receptionist", "Reception"),
            (StaffRole.SiteStaff, "Site manager", "Site team"),
        };
        for (var i = 0; i < staff.Length; i++)
        {
            var (role, grade, label) = staff[i];
            for (var k = 0; k < 2; k++)
            {
                var member = new StaffMember { Id = 3 + (i * 5) + (k * 2), Name = label, Role = role, Grade = grade, DepartmentId = "school", Age = 30 + (i * 4) + (k * 9) };
                var x = 1010 + ((i % 3) * 180) + (k * 80);
                var y = 240 + ((i / 3) * 300);
                DrawSetTransform(new Vector2(x, y), 0, new Vector2(zoom, zoom) * 0.8f);
                Figures.Standing(this, Vector2.Zero, Looks.Staff(member), 0, walking: false, member.Id);
            }

            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            DrawString(font, new Vector2(1010 + ((i % 3) * 180), 280 + ((i / 3) * 300)), label, HorizontalAlignment.Left, -1, 16, UiStyle.TextMuted);
        }
    }
}
