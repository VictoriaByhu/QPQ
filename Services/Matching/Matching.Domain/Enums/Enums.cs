namespace Matching.Domain.Enums;

public enum UserSkillType { Offer, Want }

public enum UserSkillStatus { Active, Paused, Archived }

/// <summary>У документі зберігається як "oneWay" / "mutual" (див. серіалізатор в Infrastructure).</summary>
public enum MatchKind { OneWay, Mutual }

public enum MatchStatus { Suggested, Viewed, Dismissed }
