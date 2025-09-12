import { useState } from 'react';
import { useAppSelector } from '../store/hooks';
import { useGetUserSkillsQuery, useAddUserSkillMutation } from '../store/api/apiSlice';
import { useSkillFilters } from '../hooks/useSkillFilters';
import Navigation from '../components/Navigation';
import SkillCard from '../components/SkillCard';
import AddSkillModal from '../components/AddSkillModal';
import { ActivityFeed } from '../components/ActivityFeed';
import { useToast } from '../contexts/ToastContext';
import { UserSkill, CreateUserSkillRequest } from '../types';

export default function Dashboard() {
  const { user } = useAppSelector((state) => state.auth);
  const { data: userSkills = [], isLoading } = useGetUserSkillsQuery();
  const [addUserSkill, { isLoading: isAdding }] = useAddUserSkillMutation();
  const [isAddSkillModalOpen, setIsAddSkillModalOpen] = useState(false);
  const { showSuccess, showError } = useToast();

  // Use standardized skill filtering
  const { validSkills, offeredSkillsCount, totalValidSkillsCount } = useSkillFilters(userSkills);
  const previewSkills = validSkills.slice(0, 2);

  const handleAddSkill = async (skillData: Omit<UserSkill, 'id' | 'userId'>) => {
    try {
      const createRequest: CreateUserSkillRequest = {
        skillId: skillData.skillId,
        proficiencyLevel: skillData.proficiencyLevel,
        isOffering: skillData.isOffering,
        description: skillData.description,
      };
      
      await addUserSkill(createRequest).unwrap();
      
      showSuccess('Skill added successfully!');
      setIsAddSkillModalOpen(false);
    } catch (error: any) {
      showError('Failed to add skill', error?.data?.message || 'Please try again.');
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <Navigation />
      
      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          <div className="bg-white rounded-lg shadow-sm p-8">
            <h2 className="text-2xl font-bold text-gray-900 mb-4">Dashboard</h2>
            <p className="text-gray-600">
              You have <span className="font-semibold">{user?.timeCredits || 0}</span> time credits available.
            </p>
            <p className="text-gray-600 mt-2">
              Welcome to SkillForge, {user?.name}! Your skill exchange platform is ready.
            </p>
            
            <div className="mt-6 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className="w-8 h-8 bg-blue-500 rounded-full flex items-center justify-center">
                        <span className="text-white text-sm font-medium">{offeredSkillsCount}</span>
                      </div>
                    </div>
                    <div className="ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">
                          Skills Offered
                        </dt>
                        <dd className="text-lg font-medium text-gray-900">
                          {offeredSkillsCount}
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>
              
              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className="w-8 h-8 bg-green-500 rounded-full flex items-center justify-center">
                        <span className="text-white text-sm font-medium">0</span>
                      </div>
                    </div>
                    <div className="ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">
                          Exchanges Completed
                        </dt>
                        <dd className="text-lg font-medium text-gray-900">
                          0
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>
              
              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className="w-8 h-8 bg-yellow-500 rounded-full flex items-center justify-center">
                        <span className="text-white text-sm font-medium">0</span>
                      </div>
                    </div>
                    <div className="ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">
                          Reviews Received
                        </dt>
                        <dd className="text-lg font-medium text-gray-900">
                          0
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            
            {/* Main Content Grid */}
            <div className="mt-8 grid grid-cols-1 lg:grid-cols-3 gap-8">
              {/* Skills Preview Section */}
              <div className="lg:col-span-2">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-lg font-medium text-gray-900">Your Skills Preview</h3>
                </div>
              
              {isLoading ? (
                <div className="flex items-center justify-center py-8">
                  <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600"></div>
                  <span className="ml-2 text-gray-600">Loading skills...</span>
                </div>
              ) : previewSkills.length > 0 ? (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {previewSkills.map((userSkill) => (
                    <SkillCard
                      key={userSkill.id}
                      skill={userSkill.skill!}
                      userSkill={userSkill}
                      showActions={false}
                      onClick={() => window.location.href = '/skills'}
                    />
                  ))}
                </div>
              ) : (
                <div className="text-center py-8">
                  <div className="w-16 h-16 mx-auto mb-4 text-gray-300">
                    <svg fill="currentColor" viewBox="0 0 24 24">
                      <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
                    </svg>
                  </div>
                  <h4 className="text-lg font-medium text-gray-900 mb-2">No skills added yet</h4>
                  <p className="text-gray-500 mb-4">Start building your skill profile to connect with others</p>
                  <button
                    onClick={() => 
                      // setIsAddSkillModalOpen(true)
                      window.location.href = '/skills'
                    }
                    className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm font-medium"
                  >
                    Add Your First Skill
                  </button>
                </div>
              )}
              
                {previewSkills.length > 0 && (
                  <div className="mt-4 text-center">
                    <a
                      href="/skills"
                      className="text-blue-600 hover:text-blue-800 text-sm font-medium"
                    >
                      View all skills ({totalValidSkillsCount}) →
                    </a>
                  </div>
                )}
              </div>

              {/* Activity Feed Section */}
              <div className="lg:col-span-1">
                <ActivityFeed />
              </div>
            </div>
          </div>
        </div>
      </main>

      {/* Add Skill Modal */}
      <AddSkillModal
        isOpen={isAddSkillModalOpen}
        onClose={() => setIsAddSkillModalOpen(false)}
        onAdd={handleAddSkill}
        isLoading={isAdding}
      />
    </div>
  );
}