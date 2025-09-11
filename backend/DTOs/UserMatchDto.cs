namespace SkillForge.Api.DTOs
{
    public class UserMatchDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public double TimeCredits { get; set; }
        public double Rating { get; set; }
        public double AverageRating { get; set; } // Alias for compatibility
        public int ReviewCount { get; set; }
        public bool IsOnline { get; set; }
        public List<UserSkillDto> Skills { get; set; } = new();
        public List<UserSkillDto> SkillsOffered { get; set; } = new(); // Alias for compatibility
        public List<MatchUserSkillDto> MatchSkills { get; set; } = new(); // For old compatibility
        public double CompatibilityScore { get; set; }
    }

    public class MatchUserSkillDto
    {
        public int Id { get; set; }
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;
        public int ProficiencyLevel { get; set; }
        public string? Description { get; set; }
    }

    public class CompatibilityAnalysisDto
    {
        public int TargetUserId { get; set; }
        public string TargetUserName { get; set; } = string.Empty;
        public double OverallScore { get; set; }
        public double TargetUserRating { get; set; }
        public List<string> SharedSkills { get; set; } = new();
        public List<string> ComplementarySkills { get; set; } = new();
        public string RecommendationReason { get; set; } = string.Empty;
    }
}