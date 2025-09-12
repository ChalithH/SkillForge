import React, { useState } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { Star, Award, Users, Clock, ArrowLeft, Loader2 } from 'lucide-react';
import Navigation from '../components/Navigation';
import { ExchangeRequestModal } from '../components/ExchangeRequestModal';
import { useGetUserByIdQuery } from '../store/api/apiSlice';
import { useNotifications } from '../contexts/NotificationContext';
import { UserMatchDto } from '../types';

export default function ViewProfile() {
  const { userId } = useParams<{ userId: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const { isUserOnline } = useNotifications();
  const [isExchangeModalOpen, setIsExchangeModalOpen] = useState(false);
  
  // Try to get user data from navigation state first, then fall back to API
  const stateUser = location.state?.user as UserMatchDto | undefined;
  const { data: apiUser, isLoading, error } = useGetUserByIdQuery(parseInt(userId || '0'), {
    skip: !userId || !!stateUser // Skip API call if we have state data
  });
  
  const user = stateUser || apiUser;

  const renderStars = (rating: number) => {
    const stars = [];
    const fullStars = Math.floor(rating);
    const hasHalfStar = rating % 1 !== 0;

    for (let i = 0; i < fullStars; i++) {
      stars.push(
        <Star key={i} className="w-5 h-5 fill-yellow-400 text-yellow-400" />
      );
    }

    if (hasHalfStar) {
      stars.push(
        <div key="half" className="relative inline-block">
          <Star className="w-5 h-5 text-gray-300" />
          <div className="absolute inset-0 overflow-hidden w-1/2">
            <Star className="w-5 h-5 fill-yellow-400 text-yellow-400" />
          </div>
        </div>
      );
    }

    for (let i = stars.length; i < 5; i++) {
      stars.push(
        <Star key={i} className="w-5 h-5 text-gray-300" />
      );
    }

    return stars;
  };

  const getProficiencyLabel = (level: number) => {
    const levels = ['', 'Beginner', 'Intermediate', 'Advanced', 'Expert', 'Master'];
    return levels[level] || '';
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50">
        <Navigation />
        <div className="flex items-center justify-center py-12">
          <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
          <span className="ml-2 text-gray-600">Loading profile...</span>
        </div>
      </div>
    );
  }

  if (error || !user) {
    return (
      <div className="min-h-screen bg-gray-50">
        <Navigation />
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="text-center py-12">
            <h2 className="text-2xl font-bold text-gray-900 mb-4">User Not Found</h2>
            <p className="text-gray-600 mb-6">The user profile you're looking for doesn't exist.</p>
            <button
              onClick={() => navigate('/browse')}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
            >
              Back to Browse
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <Navigation />
      
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Back Button */}
        <button
          onClick={() => navigate(-1)}
          className="flex items-center text-gray-600 hover:text-gray-900 mb-6"
        >
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back
        </button>

        {/* Profile Header */}
        <div className="bg-white rounded-lg shadow-sm p-6 mb-6">
          <div className="flex items-start space-x-6">
            <img
              src={user.profileImageUrl || `https://ui-avatars.com/api/?name=${encodeURIComponent(user.name)}&background=6366f1&color=ffffff`}
              alt={user.name}
              className="w-24 h-24 rounded-full object-cover"
            />
            <div className="flex-1">
              <div className="flex items-start justify-between">
                <div>
                  <h1 className="text-3xl font-bold text-gray-900">{user.name}</h1>
                  <div className="flex items-center space-x-4 mt-3">
                    <div className="flex items-center">
                      {renderStars(user.averageRating)}
                      <span className="ml-2 text-sm text-gray-600">
                        {user.averageRating > 0 ? user.averageRating.toFixed(1) : 'No ratings'}
                      </span>
                    </div>
                    <span className="text-sm text-gray-500">
                      ({user.reviewCount} {user.reviewCount === 1 ? 'review' : 'reviews'})
                    </span>
                    <div className="flex items-center">
                      <div className={`w-2 h-2 rounded-full ${isUserOnline(user.id) ? 'bg-green-400' : 'bg-gray-300'}`} />
                      <span className="ml-1 text-sm text-gray-600">
                        {isUserOnline(user.id) ? 'Online' : 'Offline'}
                      </span>
                    </div>
                  </div>
                </div>
                {user.skillsOffered && user.skillsOffered.length > 0 && (
                  <button
                    onClick={() => setIsExchangeModalOpen(true)}
                    className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 flex items-center"
                  >
                    <Clock className="w-4 h-4 mr-2" />
                    Request Session
                  </button>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Bio */}
        {user.bio && (
          <div className="bg-white rounded-lg shadow-sm p-6 mb-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-3">About</h2>
            <p className="text-gray-600 whitespace-pre-wrap">{user.bio}</p>
          </div>
        )}

        {/* Stats */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
          <div className="bg-white rounded-lg shadow-sm p-6 text-center">
            <Award className="w-8 h-8 text-green-600 mx-auto mb-3" />
            <div className="text-3xl font-bold text-gray-900">{user.skillsOffered?.length || 0}</div>
            <div className="text-sm text-gray-600">Skills Teaching</div>
          </div>
          <div className="bg-white rounded-lg shadow-sm p-6 text-center">
            <Star className="w-8 h-8 text-yellow-500 mx-auto mb-3" />
            <div className="text-3xl font-bold text-gray-900">
              {user.averageRating > 0 ? user.averageRating.toFixed(1) : '—'}
            </div>
            <div className="text-sm text-gray-600">Average Rating</div>
          </div>
          <div className="bg-white rounded-lg shadow-sm p-6 text-center">
            <Users className="w-8 h-8 text-blue-600 mx-auto mb-3" />
            <div className="text-3xl font-bold text-gray-900">{user.reviewCount || 0}</div>
            <div className="text-sm text-gray-600">Reviews Received</div>
          </div>
        </div>

        {/* Skills Offered */}
        <div className="bg-white rounded-lg shadow-sm p-6">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">Skills Offered</h2>
          {user.skillsOffered && user.skillsOffered.length > 0 ? (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {user.skillsOffered.map((skill) => (
                <div key={skill.id} className="bg-green-50 border border-green-200 rounded-lg p-4">
                  <div className="flex items-start justify-between mb-2">
                    <div>
                      <h3 className="font-medium text-gray-900">{skill.skillName}</h3>
                      <span className="text-sm text-gray-600">{skill.category}</span>
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className="text-sm text-green-700">{getProficiencyLabel(skill.proficiencyLevel)}</span>
                      <div className="flex">
                        {Array.from({ length: skill.proficiencyLevel }).map((_, i) => (
                          <Star key={i} className="w-4 h-4 fill-green-600 text-green-600" />
                        ))}
                      </div>
                    </div>
                  </div>
                  {skill.description && (
                    <p className="text-sm text-gray-600">{skill.description}</p>
                  )}
                </div>
              ))}
            </div>
          ) : (
            <p className="text-gray-500 text-center py-8">This user hasn't added any skills yet.</p>
          )}
        </div>
      </div>

      {/* Exchange Request Modal */}
      {user && user.skillsOffered && user.skillsOffered.length > 0 && (
        <ExchangeRequestModal
          isOpen={isExchangeModalOpen}
          onClose={() => setIsExchangeModalOpen(false)}
          targetUser={{
            id: user.id,
            name: user.name,
            email: user.email,
            bio: user.bio,
            profileImageUrl: user.profileImageUrl,
          }}
          skills={user.skillsOffered.map(s => ({
            id: s.skillId,
            name: s.skillName,
            category: s.category,
            description: s.description || '',
          }))}
          onSuccess={() => {
            setIsExchangeModalOpen(false);
            navigate('/exchanges');
          }}
        />
      )}
    </div>
  );
}