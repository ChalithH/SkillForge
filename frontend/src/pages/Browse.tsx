import React, { useState } from 'react';
import Navigation from '../components/Navigation';
import { UserCard } from '../components/UserCard';
import { ExchangeRequestModal } from '../components/ExchangeRequestModal';
import { useBrowseUsersQuery, useGetRecommendationsQuery, useGetSkillCategoriesQuery } from '../store/api/apiSlice';
import { useAppSelector } from '../store/hooks';
import { UserMatchDto, BrowseFilters } from '../types';
import { Search, Filter, Users, TrendingUp, Loader2, ChevronLeft, ChevronRight } from 'lucide-react';

export default function Browse() {
  const { user } = useAppSelector((state) => state.auth);
  const [filters, setFilters] = useState<BrowseFilters>({
    page: 1,
    limit: 12,
  });
  const [showFilters, setShowFilters] = useState(false);
  const [selectedUser, setSelectedUser] = useState<UserMatchDto | null>(null);
  const [isExchangeModalOpen, setIsExchangeModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState<'browse' | 'recommendations'>('browse');

  const { data: browseResult, isLoading: isBrowseLoading, error: browseError } = useBrowseUsersQuery(filters);
  const { data: recommendations = [], isLoading: isRecommendationsLoading } = useGetRecommendationsQuery(10);
  const { data: categories = [] } = useGetSkillCategoriesQuery();

  const handleFilterChange = (newFilters: Partial<BrowseFilters>) => {
    setFilters(prev => ({
      ...prev,
      ...newFilters,
      page: newFilters.page !== undefined ? newFilters.page : 1, // Reset to page 1 when changing filters (except explicit page changes)
    }));
  };

  const handlePageChange = (newPage: number) => {
    setFilters(prev => ({ ...prev, page: newPage }));
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handleRequestExchange = (targetUser: UserMatchDto) => {
    setSelectedUser(targetUser);
    setIsExchangeModalOpen(true);
  };

  const clearFilters = () => {
    setFilters({
      page: 1,
      limit: 12,
    });
  };

  const isFiltered = filters.category || filters.skillName || filters.minRating !== undefined || filters.isOnline !== undefined;

  if (!user) {
    return <div>Please log in to browse users.</div>;
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <Navigation />
      
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Discover Skill Partners</h1>
          <p className="text-gray-600">Find users to exchange skills with based on your interests and compatibility.</p>
        </div>

        {/* Tabs */}
        <div className="border-b border-gray-200 mb-6">
          <nav className="-mb-px flex space-x-8">
            <button
              onClick={() => setActiveTab('browse')}
              className={`py-2 px-1 border-b-2 font-medium text-sm ${
                activeTab === 'browse'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              <Search className="w-4 h-4 inline mr-2" />
              Browse All Users
            </button>
            <button
              onClick={() => setActiveTab('recommendations')}
              className={`py-2 px-1 border-b-2 font-medium text-sm ${
                activeTab === 'recommendations'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              <TrendingUp className="w-4 h-4 inline mr-2" />
              Recommended for You
            </button>
          </nav>
        </div>

        {activeTab === 'browse' && (
          <>
            {/* Search and Filters */}
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6 mb-6">
              <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between space-y-4 lg:space-y-0">
                {/* Search Input */}
                <div className="flex-1 max-w-md">
                  <div className="relative">
                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-5 h-5" />
                    <input
                      type="text"
                      placeholder="Search by skill name..."
                      value={filters.skillName || ''}
                      onChange={(e) => handleFilterChange({ skillName: e.target.value || undefined })}
                      className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    />
                  </div>
                </div>

                {/* Filter Toggle */}
                <button
                  onClick={() => setShowFilters(!showFilters)}
                  className={`flex items-center px-4 py-2 rounded-md text-sm font-medium transition-colors ${
                    showFilters || isFiltered
                      ? 'bg-blue-100 text-blue-700 border border-blue-300'
                      : 'bg-gray-100 text-gray-700 border border-gray-300 hover:bg-gray-200'
                  }`}
                >
                  <Filter className="w-4 h-4 mr-2" />
                  Filters
                  {isFiltered && (
                    <span className="ml-2 bg-blue-600 text-white text-xs rounded-full px-2 py-1">
                      Active
                    </span>
                  )}
                </button>
              </div>

              {/* Expanded Filters */}
              {showFilters && (
                <div className="mt-6 pt-6 border-t border-gray-200">
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    {/* Category Filter */}
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Skill Category
                      </label>
                      <select
                        value={filters.category || ''}
                        onChange={(e) => handleFilterChange({ category: e.target.value || undefined })}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      >
                        <option value="">All Categories</option>
                        {categories.map((category) => (
                          <option key={category} value={category}>
                            {category}
                          </option>
                        ))}
                      </select>
                    </div>

                    {/* Rating Filter */}
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Minimum Rating
                      </label>
                      <select
                        value={filters.minRating?.toString() || ''}
                        onChange={(e) => handleFilterChange({ 
                          minRating: e.target.value ? parseFloat(e.target.value) : undefined 
                        })}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      >
                        <option value="">Any Rating</option>
                        <option value="4.5">4.5+ Stars</option>
                        <option value="4.0">4.0+ Stars</option>
                        <option value="3.5">3.5+ Stars</option>
                        <option value="3.0">3.0+ Stars</option>
                      </select>
                    </div>

                    {/* Online Status Filter */}
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Availability
                      </label>
                      <select
                        value={filters.isOnline?.toString() || ''}
                        onChange={(e) => handleFilterChange({ 
                          isOnline: e.target.value ? e.target.value === 'true' : undefined 
                        })}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      >
                        <option value="">All Users</option>
                        <option value="true">Online Now</option>
                        <option value="false">Offline</option>
                      </select>
                    </div>
                  </div>

                  {/* Clear Filters */}
                  {isFiltered && (
                    <div className="mt-4">
                      <button
                        onClick={clearFilters}
                        className="text-sm text-blue-600 hover:text-blue-800 font-medium"
                      >
                        Clear all filters
                      </button>
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Browse Results */}
            <div className="mb-8">
              {isBrowseLoading ? (
                <div className="flex items-center justify-center py-12">
                  <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
                  <span className="ml-2 text-gray-600">Loading users...</span>
                </div>
              ) : browseError ? (
                <div className="text-center py-12">
                  <p className="text-red-600">Error loading users. Please try again.</p>
                </div>
              ) : browseResult && browseResult.items.length > 0 ? (
                <>
                  {/* Results Summary */}
                  <div className="flex items-center justify-between mb-6">
                    <p className="text-gray-600">
                      Showing {browseResult.items.length} of {browseResult.totalCount} users
                      {isFiltered && ' (filtered)'}
                    </p>
                  </div>

                  {/* User Grid */}
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
                    {browseResult.items.map((user) => (
                      <UserCard
                        key={user.id}
                        user={user}
                        onRequestExchange={handleRequestExchange}
                        showCompatibilityScore={true}
                      />
                    ))}
                  </div>

                  {/* Pagination */}
                  {browseResult.totalPages > 1 && (
                    <div className="flex items-center justify-center space-x-2">
                      <button
                        onClick={() => handlePageChange(filters.page! - 1)}
                        disabled={filters.page === 1}
                        className="flex items-center px-3 py-2 text-sm font-medium text-gray-500 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        <ChevronLeft className="w-4 h-4 mr-1" />
                        Previous
                      </button>
                      
                      <div className="flex space-x-1">
                        {Array.from({ length: browseResult.totalPages }, (_, i) => i + 1)
                          .filter(page => 
                            page === 1 || 
                            page === browseResult.totalPages || 
                            Math.abs(page - filters.page!) <= 2
                          )
                          .map((page, index, array) => {
                            const showEllipsis = index > 0 && array[index - 1] !== page - 1;
                            return (
                              <React.Fragment key={page}>
                                {showEllipsis && (
                                  <span className="px-3 py-2 text-sm text-gray-500">...</span>
                                )}
                                <button
                                  onClick={() => handlePageChange(page)}
                                  className={`px-3 py-2 text-sm font-medium rounded-md ${
                                    page === filters.page
                                      ? 'bg-blue-600 text-white'
                                      : 'text-gray-500 bg-white border border-gray-300 hover:bg-gray-50'
                                  }`}
                                >
                                  {page}
                                </button>
                              </React.Fragment>
                            );
                          })}
                      </div>

                      <button
                        onClick={() => handlePageChange(filters.page! + 1)}
                        disabled={filters.page === browseResult.totalPages}
                        className="flex items-center px-3 py-2 text-sm font-medium text-gray-500 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        Next
                        <ChevronRight className="w-4 h-4 ml-1" />
                      </button>
                    </div>
                  )}
                </>
              ) : (
                <div className="text-center py-12">
                  <Users className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                  <p className="text-gray-600 mb-2">No users found</p>
                  <p className="text-sm text-gray-500">
                    {isFiltered ? 'Try adjusting your filters or search terms.' : 'Check back later as more users join.'}
                  </p>
                </div>
              )}
            </div>
          </>
        )}

        {activeTab === 'recommendations' && (
          <div>
            {isRecommendationsLoading ? (
              <div className="flex items-center justify-center py-12">
                <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
                <span className="ml-2 text-gray-600">Loading recommendations...</span>
              </div>
            ) : recommendations.length > 0 ? (
              <>
                <div className="mb-6">
                  <p className="text-gray-600">
                    Here are users who match well with your skills and interests.
                  </p>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                  {recommendations.map((user) => (
                    <UserCard
                      key={user.id}
                      user={user}
                      onRequestExchange={handleRequestExchange}
                      showCompatibilityScore={true}
                    />
                  ))}
                </div>
              </>
            ) : (
              <div className="text-center py-12">
                <TrendingUp className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                <p className="text-gray-600 mb-2">No recommendations yet</p>
                <p className="text-sm text-gray-500">
                  Add some skills to your profile to get personalized recommendations.
                </p>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Exchange Request Modal */}
      {selectedUser && (
        <ExchangeRequestModal
          isOpen={isExchangeModalOpen}
          onClose={() => {
            setIsExchangeModalOpen(false);
            setSelectedUser(null);
          }}
          targetUser={{
            id: selectedUser.id,
            name: selectedUser.name,
            email: selectedUser.email,
            timeCredits: 0, // We don't have this data in UserMatchDto
            bio: selectedUser.bio,
            profileImageUrl: selectedUser.profileImageUrl,
          }}
          skill={{
            id: selectedUser.skillsOffered[0]?.skillId || 0,
            name: selectedUser.skillsOffered[0]?.skillName || '',
            category: selectedUser.skillsOffered[0]?.skillCategory || '',
            description: selectedUser.skillsOffered[0]?.description || '',
          }}
          onSuccess={() => {
            setIsExchangeModalOpen(false);
            setSelectedUser(null);
          }}
        />
      )}
    </div>
  );
}