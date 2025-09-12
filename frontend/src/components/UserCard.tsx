import React from 'react';
import { Star, Clock, Users, ChevronRight } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { UserMatchDto } from '../types';
import { useNotifications } from '../contexts/NotificationContext';

interface UserCardProps {
  user: UserMatchDto;
  onViewProfile?: (user: UserMatchDto) => void;
  onRequestExchange?: (user: UserMatchDto) => void;
  showCompatibilityScore?: boolean;
}

export const UserCard: React.FC<UserCardProps> = ({
  user,
  onViewProfile,
  onRequestExchange,
  showCompatibilityScore = true,
}) => {
  const navigate = useNavigate();
  const { isUserOnline } = useNotifications();
  const renderStars = (rating: number) => {
    const stars = [];
    const fullStars = Math.floor(rating);
    const hasHalfStar = rating % 1 !== 0;

    for (let i = 0; i < fullStars; i++) {
      stars.push(
        <Star key={i} className="w-4 h-4 fill-yellow-400 text-yellow-400" />
      );
    }

    if (hasHalfStar) {
      stars.push(
        <div key="half" className="relative">
          <Star className="w-4 h-4 text-gray-300" />
          <div className="absolute inset-0 overflow-hidden w-1/2">
            <Star className="w-4 h-4 fill-yellow-400 text-yellow-400" />
          </div>
        </div>
      );
    }

    for (let i = stars.length; i < 5; i++) {
      stars.push(
        <Star key={i} className="w-4 h-4 text-gray-300" />
      );
    }

    return stars;
  };

  const getCompatibilityColor = (score: number) => {
    if (score >= 80) return 'text-green-600 bg-green-100';
    if (score >= 60) return 'text-blue-600 bg-blue-100';
    if (score >= 40) return 'text-yellow-600 bg-yellow-100';
    return 'text-gray-600 bg-gray-100';
  };

  const getOnlineStatusColor = (userId: number) => {
    return isUserOnline(userId) ? 'bg-green-400' : 'bg-gray-300';
  };

  return (
    <div className="bg-white rounded-lg shadow-md hover:shadow-lg transition-shadow duration-200 p-6 border border-gray-200">
      {/* Header */}
      <div className="flex items-start justify-between mb-4">
        <div className="flex items-center space-x-4">
          <div className="relative">
            <img
              src={user.profileImageUrl || `https://ui-avatars.com/api/?name=${encodeURIComponent(user.name)}&background=6366f1&color=ffffff`}
              alt={user.name}
              className="w-16 h-16 rounded-full object-cover"
            />
            <div 
              className={`absolute bottom-0 right-0 w-4 h-4 rounded-full border-2 border-white ${getOnlineStatusColor(user.id)}`}
              title={isUserOnline(user.id) ? 'Online' : 'Offline'}
            />
          </div>
          <div className="flex-1">
            <h3 className="font-semibold text-lg text-gray-900">{user.name}</h3>
            <div className="flex items-center space-x-2 mt-1">
              <div className="flex items-center">
                {renderStars(user.averageRating)}
              </div>
              <span className="text-sm text-gray-600">
                {user.averageRating > 0 ? user.averageRating.toFixed(1) : 'No ratings'}
              </span>
              <span className="text-sm text-gray-500">
                ({user.reviewCount} {user.reviewCount === 1 ? 'review' : 'reviews'})
              </span>
            </div>
          </div>
        </div>
        
        {showCompatibilityScore && user.compatibilityScore > 0 && (
          <div className={`px-3 py-1 rounded-full text-sm font-medium ${getCompatibilityColor(user.compatibilityScore)}`}>
            {Math.round(user.compatibilityScore)}% match
          </div>
        )}
      </div>

      {/* Bio */}
      {user.bio && (
        <p className="text-gray-600 text-sm mb-4 line-clamp-2">{user.bio}</p>
      )}

      {/* Skills Offered */}
      <div className="mb-4">
        <h4 className="font-medium text-gray-900 mb-2 flex items-center">
          <Users className="w-4 h-4 mr-1" />
          Skills Offered ({user.skillsOffered.length})
        </h4>
        {user.skillsOffered.length > 0 ? (
          <div className="flex flex-wrap gap-2">
            {user.skillsOffered.slice(0, 4).map((skill) => (
              <div
                key={skill.id}
                className="inline-flex items-center px-3 py-1 bg-green-50 text-green-800 text-xs font-medium rounded-full border border-green-200"
              >
                <span>{skill.skillName}</span>
                <div className="ml-2 flex">
                  {Array.from({ length: skill.proficiencyLevel }).map((_, i) => (
                    <Star key={i} className="w-3 h-3 fill-green-600 text-green-600" />
                  ))}
                </div>
              </div>
            ))}
            {user.skillsOffered.length > 4 && (
              <div className="inline-flex items-center px-3 py-1 bg-gray-100 text-gray-600 text-xs font-medium rounded-full">
                +{user.skillsOffered.length - 4} more
              </div>
            )}
          </div>
        ) : (
          <p className="text-gray-500 text-sm">No skills offered yet</p>
        )}
      </div>

      {/* Action Buttons */}
      <div className="flex space-x-3 pt-4 border-t border-gray-100">
        <button
          onClick={() => navigate(`/profile/${user.id}`, { state: { user } })}
          className="flex-1 flex items-center justify-center px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 transition-colors duration-200"
        >
          View Profile
          <ChevronRight className="w-4 h-4 ml-1" />
        </button>
        <button
          onClick={() => onRequestExchange?.(user)}
          disabled={user.skillsOffered.length === 0}
          className={`flex-1 flex items-center justify-center px-4 py-2 rounded-md text-sm font-medium transition-colors duration-200 ${
            user.skillsOffered.length === 0
              ? 'bg-gray-300 text-gray-500 cursor-not-allowed'
              : 'bg-blue-600 text-white hover:bg-blue-700'
          }`}
          title={user.skillsOffered.length === 0 ? 'This user has no skills to teach' : 'Request a learning session'}
        >
          <Clock className="w-4 h-4 mr-1" />
          Request Session
        </button>
      </div>
    </div>
  );
};