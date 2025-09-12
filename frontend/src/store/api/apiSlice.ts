import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { RootState } from '../store';
import { Skill, UserSkill, User, ProfileUpdateData, CreateUserSkillRequest, SkillExchange, ExchangeStatus, CreateExchangeRequest, UserMatchDto, PagedResult, CompatibilityAnalysisDto, BrowseFilters } from '../../types';

const baseQuery = fetchBaseQuery({
  baseUrl: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
  prepareHeaders: (headers, { getState }) => {
    const token = (getState() as RootState).auth.token;
    if (token) {
      headers.set('authorization', `Bearer ${token}`);
    }
    return headers;
  },
});

export const apiSlice = createApi({
  reducerPath: 'api',
  baseQuery,
  tagTypes: ['User', 'Skill', 'UserSkill', 'Exchange', 'Review', 'UserProfile', 'UserMatch'],
  endpoints: (builder) => ({
    // Skills endpoints
    getSkills: builder.query<Skill[], void>({
      query: () => '/skills',
      providesTags: ['Skill'],
      // Cache skills for 5 minutes since they're relatively static
      keepUnusedDataFor: 300,
    }),
    getSkillCategories: builder.query<string[], void>({
      query: () => '/skills/categories',
      providesTags: ['Skill'],
      // Cache categories for 10 minutes since they change even less frequently
      keepUnusedDataFor: 600,
    }),
    
    // User Skills endpoints
    getUserSkills: builder.query<UserSkill[], void>({
      query: () => '/userskills',
      providesTags: ['UserSkill'],
    }),
    addUserSkill: builder.mutation<UserSkill, CreateUserSkillRequest>({
      query: (skillData) => ({
        url: '/userskills',
        method: 'POST',
        body: skillData,
      }),
      invalidatesTags: ['UserSkill'],
      onQueryStarted: async (skillData, { dispatch, queryFulfilled, getState }) => {
        // Optimistic update
        const state = getState() as any;
        const userId = state.auth.user?.id;
        
        // Look up the skill data from the skills cache
        const skillsData = apiSlice.endpoints.getSkills.select()(state)?.data;
        const skill = skillsData?.find(s => s.id === skillData.skillId);
        
        const patchResult = dispatch(
          apiSlice.util.updateQueryData('getUserSkills', undefined, (draft) => {
            draft.push({ 
              ...skillData, 
              id: Date.now(), // Temporary ID
              userId: userId || 0, // Use current user ID
              skill: skill // Include the actual skill data if available
            });
          })
        );
        try {
          await queryFulfilled;
        } catch (error) {
          // Revert optimistic update on error
          patchResult.undo();
          
          // Log error for debugging
          console.error('Failed to add user skill:', error);
        }
      },
    }),
    updateUserSkill: builder.mutation<UserSkill, Partial<UserSkill> & { id: number }>({
      query: ({ id, ...updates }) => ({
        url: `/userskills/${id}`,
        method: 'PUT',
        body: updates,
      }),
      invalidatesTags: ['UserSkill'],
      onQueryStarted: async ({ id, ...updates }, { dispatch, queryFulfilled }) => {
        // Optimistic update
        const patchResult = dispatch(
          apiSlice.util.updateQueryData('getUserSkills', undefined, (draft) => {
            const index = draft.findIndex((skill) => skill.id === id);
            if (index !== -1) {
              draft[index] = { ...draft[index], ...updates };
            }
          })
        );
        try {
          await queryFulfilled;
        } catch (error) {
          // Revert optimistic update on error
          patchResult.undo();
          
          // Log error for debugging
          console.error('Failed to update user skill:', error);
        }
      },
    }),
    deleteUserSkill: builder.mutation<void, number>({
      query: (id) => ({
        url: `/userskills/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['UserSkill'],
      onQueryStarted: async (id, { dispatch, queryFulfilled }) => {
        // Optimistic update - immediately remove from UI
        const patchResult = dispatch(
          apiSlice.util.updateQueryData('getUserSkills', undefined, (draft) => {
            return draft.filter((skill) => skill.id !== id);
          })
        );
        try {
          await queryFulfilled;
        } catch (error) {
          // Revert optimistic update on error
          patchResult.undo();
          
          // Log error for debugging
          console.error('Failed to delete user skill:', error);
        }
      },
    }),
    
    // User profile endpoints
    updateProfile: builder.mutation<User, ProfileUpdateData>({
      query: (profileData) => ({
        url: '/auth/profile',
        method: 'PUT',
        body: profileData,
      }),
      invalidatesTags: ['User', 'UserProfile'],
      onQueryStarted: async (profileData, { dispatch, queryFulfilled, getState }) => {
        // Optimistic update for auth state
        const state = getState() as any;
        const currentUser = state.auth.user;
        
        if (currentUser) {
          dispatch({
            type: 'auth/updateUserProfile',
            payload: profileData
          });
          
          try {
            const { data: updatedUser } = await queryFulfilled;
            // Update with actual response data
            dispatch({
              type: 'auth/updateUserProfile',
              payload: updatedUser
            });
          } catch (error) {
            // Revert optimistic update on error
            dispatch({
              type: 'auth/updateUserProfile',
              payload: currentUser
            });
            
            // Log error for debugging
            console.error('Failed to update user profile:', error);
          }
        }
      },
    }),
    uploadProfileImage: builder.mutation<{imageUrl: string}, FormData>({
      query: (formData) => ({
        url: '/auth/profile/image',
        method: 'POST',
        body: formData,
      }),
      invalidatesTags: ['User', 'UserProfile'],
    }),
    
    // Exchange endpoints
    getExchanges: builder.query<SkillExchange[], ExchangeStatus | undefined>({
      query: (status) => `/exchanges${status !== undefined ? `?status=${status}` : ''}`,
      providesTags: ['Exchange'],
    }),
    createExchange: builder.mutation<SkillExchange, CreateExchangeRequest>({
      query: (exchangeData) => ({
        url: '/exchanges',
        method: 'POST',
        body: exchangeData,
      }),
      invalidatesTags: ['Exchange'],
      onQueryStarted: async (_, { dispatch, queryFulfilled }) => {
        try {
          await queryFulfilled;
          
          // Creating an exchange doesn't immediately affect credits, but refresh for consistency
          // Credits are transferred when exchange is completed, not when created
          // This ensures the UI shows the most up-to-date information
          const { loadUser } = await import('../slices/authSlice');
          dispatch(loadUser());
        } catch (error) {
          console.error('Failed to create exchange:', error);
        }
      },
    }),
    updateExchangeStatus: builder.mutation<SkillExchange, { id: number; status: ExchangeStatus }>({
      query: ({ id, status }) => ({
        url: `/exchanges/${id}/status`,
        method: 'PUT',
        body: { status },
      }),
      invalidatesTags: ['Exchange'],
      onQueryStarted: async ({ status }, { dispatch, queryFulfilled }) => {
        try {
          await queryFulfilled;
          
          // If exchange was completed, refresh user data to get updated credits
          if (status === ExchangeStatus.Completed) {
            // Import loadUser action dynamically to avoid circular imports
            const { loadUser } = await import('../slices/authSlice');
            dispatch(loadUser());
          }
        } catch (error) {
          console.error('Failed to update exchange status:', error);
        }
      },
    }),
    
    // User endpoints
    getCurrentUser: builder.query<User, void>({
      query: () => '/auth/me',
      providesTags: ['User'],
    }),
    getUserById: builder.query<UserMatchDto, number>({
      query: (userId) => `/matching/user/${userId}`,
      providesTags: (result, error, userId) => [{ type: 'User', id: userId }],
    }),
    
    // Review endpoints
    createReview: builder.mutation({
      query: (reviewData) => ({
        url: '/reviews',
        method: 'POST',
        body: reviewData,
      }),
      invalidatesTags: ['Review', 'Exchange'],
    }),
    getUserReviews: builder.query({
      query: (userId: number) => `/reviews/user/${userId}`,
      providesTags: ['Review'],
    }),
    
    // Matching endpoints
    browseUsers: builder.query<PagedResult<UserMatchDto>, BrowseFilters>({
      query: (filters) => {
        const params = new URLSearchParams();
        
        if (filters.category) params.append('category', filters.category);
        if (filters.minRating !== undefined) params.append('minRating', filters.minRating.toString());
        if (filters.isOnline !== undefined) params.append('isOnline', filters.isOnline.toString());
        if (filters.skillName) params.append('skillName', filters.skillName);
        if (filters.page) params.append('page', filters.page.toString());
        if (filters.limit) params.append('limit', filters.limit.toString());
        
        return `/matching/browse?${params.toString()}`;
      },
      providesTags: ['UserMatch'],
      keepUnusedDataFor: 60, // Cache for 1 minute since user data changes frequently
    }),
    getRecommendations: builder.query<UserMatchDto[], number | undefined>({
      query: (limit = 10) => `/matching/recommendations?limit=${limit}`,
      providesTags: ['UserMatch'],
      keepUnusedDataFor: 60, // Cache for 1 minute
    }),
    getCompatibilityAnalysis: builder.query<CompatibilityAnalysisDto, number>({
      query: (targetUserId) => `/matching/compatibility/${targetUserId}`,
      providesTags: ['UserMatch'],
      keepUnusedDataFor: 300, // Cache for 5 minutes since compatibility doesn't change often
    }),
  }),
});

export const {
  useGetSkillsQuery,
  useGetSkillCategoriesQuery,
  useGetUserSkillsQuery,
  useAddUserSkillMutation,
  useUpdateUserSkillMutation,
  useDeleteUserSkillMutation,
  useUpdateProfileMutation,
  useUploadProfileImageMutation,
  useGetExchangesQuery,
  useCreateExchangeMutation,
  useUpdateExchangeStatusMutation,
  useGetCurrentUserQuery,
  useGetUserByIdQuery,
  useCreateReviewMutation,
  useGetUserReviewsQuery,
  useBrowseUsersQuery,
  useGetRecommendationsQuery,
  useGetCompatibilityAnalysisQuery,
} = apiSlice;