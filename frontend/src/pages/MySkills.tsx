import { useState } from 'react';
import { useAppSelector } from '../store/hooks';
import { 
  useGetUserSkillsQuery, 
  useAddUserSkillMutation, 
  useDeleteUserSkillMutation 
} from '../store/api/apiSlice';
import { useSkillFilters } from '../hooks/useSkillFilters';
import Navigation from '../components/Navigation';
import SkillCard from '../components/SkillCard';
import AddSkillToTeachModal from '../components/AddSkillToTeachModal';
import AddSkillToLearnModal from '../components/AddSkillToLearnModal';
import { useToast } from '../contexts/ToastContext';
import { UserSkill, CreateUserSkillRequest } from '../types';
import { GraduationCap, BookOpen, Coins } from 'lucide-react';

export default function MySkills() {
  const { user } = useAppSelector((state) => state.auth);
  const { data: userSkills = [], isLoading } = useGetUserSkillsQuery();
  const [addUserSkill, { isLoading: isAdding }] = useAddUserSkillMutation();
  const [deleteUserSkill] = useDeleteUserSkillMutation();
  
  const [isAddTeachModalOpen, setIsAddTeachModalOpen] = useState(false);
  const [isAddLearnModalOpen, setIsAddLearnModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState<'offering' | 'learning'>('offering');
  const { showSuccess, showError } = useToast();

  // Use standardized skill filtering
  const { offeredSkills, learningSkills } = useSkillFilters(userSkills);

  const handleAddSkill = async (skillData: Omit<UserSkill, 'id' | 'userId'>) => {
    try {
      const createRequest: CreateUserSkillRequest = {
        skillId: skillData.skillId,
        proficiencyLevel: skillData.proficiencyLevel,
        isOffering: skillData.isOffering,
        description: skillData.description,
      };
      
      await addUserSkill(createRequest).unwrap();
      
      showSuccess(skillData.isOffering ? 'Teaching skill added successfully!' : 'Learning goal added successfully!');
      
      // Close the appropriate modal
      if (skillData.isOffering) {
        closeTeachModal();
      } else {
        closeLearnModal();
      }
    } catch (error) {
      const errorMessage = (error as any)?.data?.message || 'Please try again.';
      showError('Failed to add skill', errorMessage);
    }
  };


  const handleDeleteSkill = async (userSkillId: number) => {
    try {
      await deleteUserSkill(userSkillId).unwrap();
      showSuccess('Skill removed successfully!');
    } catch (error) {
      const errorMessage = (error as any)?.data?.message || 'Please try again.';
      showError('Failed to remove skill', errorMessage);
    }
  };

  const currentSkills = activeTab === 'offering' ? offeredSkills : learningSkills;

  const openTeachModal = () => {
    setIsAddTeachModalOpen(true);
  };

  const openLearnModal = () => {
    setIsAddLearnModalOpen(true);
  };

  const closeTeachModal = () => {
    setIsAddTeachModalOpen(false);
  };

  const closeLearnModal = () => {
    setIsAddLearnModalOpen(false);
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <Navigation />
      
      <main className="max-w-7xl mx-auto py-4 px-4 sm:py-6 sm:px-6 lg:px-8">
        <div className="py-4 sm:py-6">
          {/* Header */}
          <div className="mb-4 sm:mb-8">
            <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between space-y-3 sm:space-y-0">
              <div>
                <h1 className="text-2xl sm:text-3xl font-bold text-gray-900">My Skills</h1>
                <p className="mt-1 sm:mt-2 text-sm text-gray-600">
                  Manage the skills you offer to teach and the skills you want to learn
                </p>
              </div>
              <div className="flex flex-col sm:flex-row space-y-2 sm:space-y-0 sm:space-x-3">
                <button
                  onClick={openTeachModal}
                  className="px-3 sm:px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 font-medium text-sm sm:text-base w-full sm:w-auto"
                >
                  + Add Skill to Teach
                </button>
                <button
                  onClick={openLearnModal}
                  className="px-3 sm:px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 font-medium text-sm sm:text-base w-full sm:w-auto"
                >
                  + Add Skill to Learn
                </button>
              </div>
            </div>

            {/* Stats */}
            <div className="mt-4 sm:mt-6 grid grid-cols-1 gap-3 sm:gap-5 sm:grid-cols-3">
              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-4 sm:p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className="w-8 h-8 bg-green-500 rounded-full flex items-center justify-center">
                        <GraduationCap className="w-4 h-4 text-white" />
                      </div>
                    </div>
                    <div className="ml-3 sm:ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">
                          Skills Offering
                        </dt>
                        <dd className="text-lg font-medium text-gray-900">
                          {offeredSkills.length}
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>

              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-4 sm:p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className="w-8 h-8 bg-blue-500 rounded-full flex items-center justify-center">
                        <BookOpen className="w-4 h-4 text-white" />
                      </div>
                    </div>
                    <div className="ml-3 sm:ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">
                          Skills Learning
                        </dt>
                        <dd className="text-lg font-medium text-gray-900">
                          {learningSkills.length}
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>

              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-4 sm:p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className="w-8 h-8 bg-amber-500 rounded-full flex items-center justify-center">
                        <Coins className="w-4 h-4 text-white" />
                      </div>
                    </div>
                    <div className="ml-3 sm:ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">
                          Time Credits
                        </dt>
                        <dd className="text-lg font-medium text-gray-900">
                          {user?.timeCredits || 0}
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Tabs */}
          <div className="border-b border-gray-200 mb-4 sm:mb-6 overflow-x-auto">
            <nav className="-mb-px flex space-x-4 sm:space-x-8 min-w-max">
              <button
                onClick={() => setActiveTab('offering')}
                className={`py-2 px-1 border-b-2 font-medium text-xs sm:text-sm whitespace-nowrap ${
                  activeTab === 'offering'
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                <GraduationCap className="w-3 h-3 sm:w-4 sm:h-4 inline mr-1" />
                <span className="inline-block">Skills I Can Teach ({offeredSkills.length})</span>
              </button>
              <button
                onClick={() => setActiveTab('learning')}
                className={`py-2 px-1 border-b-2 font-medium text-xs sm:text-sm whitespace-nowrap ${
                  activeTab === 'learning'
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                <BookOpen className="w-3 h-3 sm:w-4 sm:h-4 inline mr-1" />
                <span className="inline-block">Skills I Want to Learn ({learningSkills.length})</span>
              </button>
            </nav>
          </div>

          {/* Skills Content */}
          <div className="space-y-4 sm:space-y-6">
            {isLoading ? (
              <div className="flex items-center justify-center py-8 sm:py-12">
                <div className="animate-spin rounded-full h-6 w-6 sm:h-8 sm:w-8 border-b-2 border-blue-600"></div>
                <span className="ml-2 text-sm sm:text-base text-gray-600">Loading your skills...</span>
              </div>
            ) : currentSkills.length > 0 ? (
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-6">
                {currentSkills.map((userSkill) => (
                  <SkillCard
                    key={userSkill.id}
                    skill={userSkill.skill!}
                    userSkill={userSkill}
                    showActions={true}
                    onDelete={handleDeleteSkill}
                  />
                ))}
              </div>
            ) : (
              <div className="text-center py-12">
                <div className="w-24 h-24 mx-auto mb-4 text-gray-300">
                  {activeTab === 'offering' ? (
                    <svg fill="currentColor" viewBox="0 0 24 24">
                      <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
                    </svg>
                  ) : (
                    <svg fill="currentColor" viewBox="0 0 24 24">
                      <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
                    </svg>
                  )}
                </div>
                <h3 className="text-lg font-medium text-gray-900 mb-2">
                  {activeTab === 'offering' 
                    ? "No skills offered yet" 
                    : "No learning goals set yet"
                  }
                </h3>
                <p className="text-gray-500 mb-6">
                  {activeTab === 'offering'
                    ? "Start sharing your expertise with others by adding skills you can teach"
                    : "Discover new skills you'd like to learn from the community"
                  }
                </p>
                <button
                  onClick={activeTab === 'offering' ? openTeachModal : openLearnModal}
                  className={`px-6 py-3 text-white rounded-md focus:outline-none focus:ring-2 font-medium ${
                    activeTab === 'offering' 
                      ? 'bg-green-600 hover:bg-green-700 focus:ring-green-500' 
                      : 'bg-blue-600 hover:bg-blue-700 focus:ring-blue-500'
                  }`}
                >
                  {activeTab === 'offering' ? 'Add Your First Teaching Skill' : 'Add Your First Learning Goal'}
                </button>
              </div>
            )}
          </div>
        </div>
      </main>

      {/* Add Skill Modals */}
      <AddSkillToTeachModal
        isOpen={isAddTeachModalOpen}
        onClose={closeTeachModal}
        onAdd={handleAddSkill}
        isLoading={isAdding}
      />
      
      <AddSkillToLearnModal
        isOpen={isAddLearnModalOpen}
        onClose={closeLearnModal}
        onAdd={handleAddSkill}
        isLoading={isAdding}
      />
    </div>
  );
}