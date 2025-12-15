export interface User {
  id: number;
  email: string;
  name: string;
  timeCredits: number;
  bio?: string;
  profileImageUrl?: string;
}

export interface Skill {
  id: number;
  name: string;
  category: string;
  description: string;
}

export interface UserSkill {
  id: number;
  userId: number;
  skillId: number;
  proficiencyLevel: number;
  isOffering: boolean;
  description?: string;
  skill?: Skill;
}

export interface AuthState {
  user: User | null;
  token: string | null;
  isLoading: boolean;
  error: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  name: string;
}

export interface ProfileUpdateData {
  name: string;
  bio?: string;
  profileImageUrl?: string;
}

export interface CreateUserSkillRequest {
  skillId: number;
  proficiencyLevel: number;
  isOffering: boolean;
  description?: string;
}

export enum ExchangeStatus {
  Pending = 0,
  Accepted = 1,
  Rejected = 2,
  Cancelled = 3,
  Completed = 4,
  NoShow = 5
}

export interface SkillExchange {
  id: number;
  offererId: number;
  offerer?: User;
  offererName?: string;
  offererProfileImageUrl?: string;
  learnerId: number;
  learner?: User;
  learnerName?: string;
  learnerProfileImageUrl?: string;
  skillId: number;
  skill?: Skill;
  skillName?: string;
  skillCategory?: string;
  scheduledAt: string;
  duration: number;
  status: ExchangeStatus;
  meetingLink?: string;
  notes?: string;
  createdAt: string;
  updatedAt: string;
  reviews: Review[];
  canReview?: boolean;
  hasReviewed?: boolean;
}

export interface Review {
  id: number;
  exchangeId: number;
  exchange?: SkillExchange;
  reviewerId: number;
  reviewer?: User;
  reviewedUserId: number;
  reviewedUser?: User;
  rating: number;
  comment?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateExchangeRequest {
  offererId: number;
  learnerId: number;
  skillId: number;
  scheduledAt: string;
  duration: number;
  meetingLink?: string;
  notes?: string;
}

// Matching system types
export interface UserMatchDto {
  id: number;
  name: string;
  email: string;
  bio?: string;
  profileImageUrl?: string;
  averageRating: number;
  reviewCount: number;
  skillsOffered: MatchUserSkillDto[];
  compatibilityScore: number;
  isOnline: boolean;
}

export interface MatchUserSkillDto {
  id: number;
  skillId: number;
  skillName: string;
  skillCategory: string;
  proficiencyLevel: number;
  description?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  limit: number;
  totalPages: number;
}

export interface CompatibilityAnalysisDto {
  targetUserId: number;
  targetUserName: string;
  overallScore: number;
  targetUserRating: number;
  sharedSkills: SkillMatchDto[];
  complementarySkills: SkillMatchDto[];
  recommendationReason: string;
}

export interface SkillMatchDto {
  skillId: number;
  skillName: string;
  skillCategory: string;
  myProficiency: number;
  theirProficiency: number;
  myRole: string;
  theirRole: string;
}

export interface BrowseFilters {
  category?: string;
  minRating?: number;
  isOnline?: boolean;
  skillName?: string;
  page?: number;
  limit?: number;
}