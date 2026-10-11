namespace Swaps.Domain.Models;

public class SwapSkill
{
    public int SwapId { get; set; }
    public int SkillId { get; set; }
    public string SkillName { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
