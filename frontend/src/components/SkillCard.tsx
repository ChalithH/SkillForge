import { Skill, UserSkill } from '../types';
import { GraduationCap, BookOpen } from 'lucide-react';

interface SkillCardProps {
  skill: Skill;
  userSkill?: UserSkill;
  showActions?: boolean;
  onDelete?: (userSkillId: number) => void;
  onClick?: () => void;
}

const StarRating = ({ rating, maxRating = 5 }: { rating: number; maxRating?: number }) => {
  return (
    <div className="flex items-center space-x-1">
      {[...Array(maxRating)].map((_, index) => (
        <svg
          key={index}
          className={`w-4 h-4 ${
            index < rating ? 'text-amber-400 fill-current' : 'text-gray-300'
          }`}
          viewBox="0 0 20 20"
        >
          <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
        </svg>
      ))}
      <span className="text-sm text-gray-600 ml-1">({rating}/5)</span>
    </div>
  );
};

export default function SkillCard({ 
  skill, 
  userSkill, 
  showActions = false, 
  onDelete, 
  onClick 
}: SkillCardProps) {
  const isOffering = userSkill?.isOffering;
  const proficiencyLevel = userSkill?.proficiencyLevel || 0;

  return (
    <div 
      className={`
        bg-white rounded-lg border border-gray-200 p-4 hover:shadow-md transition-shadow duration-200
        ${onClick ? 'cursor-pointer hover:border-blue-300' : ''}
      `}
      onClick={onClick}
    >
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <h3 className="text-lg font-semibold text-gray-900 mb-1">{skill.name}</h3>
          
          {/* Category Badge */}
          <div className="mb-2">
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
              {skill.category}
            </span>
          </div>

          {/* Description */}
          <p className="text-sm text-gray-600 mb-3 line-clamp-2">
            {skill.description}
          </p>

          {/* User's proficiency and offering status */}
          {userSkill && (
            <div className="space-y-2">
              <div>
                <span className="text-sm font-medium text-gray-700">Your level:</span>
                <div className="mt-1">
                  <StarRating rating={proficiencyLevel} />
                </div>
              </div>
              
              {userSkill.description && (
                <div>
                  <span className="text-sm font-medium text-gray-700">Your notes:</span>
                  <p className="text-sm text-gray-600 mt-1">{userSkill.description}</p>
                </div>
              )}

              <div className="flex items-center space-x-2">
                <span className={`
                  inline-flex items-center px-2 py-1 rounded-full text-xs font-medium
                  ${isOffering 
                    ? 'bg-green-100 text-green-800' 
                    : 'bg-blue-100 text-blue-800'
                  }
                `}>
                  {isOffering ? (
                    <><GraduationCap className="w-3 h-3 mr-1" /> I can teach</>
                  ) : (
                    <><BookOpen className="w-3 h-3 mr-1" /> I want to learn</>
                  )}
                </span>
              </div>
            </div>
          )}
        </div>

        {/* Action buttons */}
        {showActions && userSkill && (
          <div className="flex flex-col space-y-2 ml-4">
            {onDelete && (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  onDelete(userSkill.id);
                }}
                className="text-red-600 hover:text-red-800 text-sm font-medium"
              >
                Remove
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );
}