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
import { GraduationCap, Clock, Star, Plus, Users, ArrowRight } from 'lucide-react';
import { Link } from 'react-router-dom';

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
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-2xl font-bold text-gray-900">Dashboard</h2>
              <div className="flex gap-3">
                <Link
                  to="/skills"
                  className="inline-flex items-center px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 text-sm font-medium"
                >
                  <Plus className="w-4 h-4 mr-1" />
                  Add Skills
                </Link>
                <Link
                  to="/browse"
                  className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 text-sm font-medium"
                >
                  <Users className="w-4 h-4 mr-1" />
                  Find Partners
                </Link>
                <Link
                  to="/exchanges"
                  className="inline-flex items-center px-4 py-2 border border-gray-300 bg-white text-gray-700 rounded-md hover:bg-gray-50 text-sm font-medium"
                >
                  <ArrowRight className="w-4 h-4 mr-1" />
                  My Exchanges
                </Link>
              </div>
            </div>
            
            <div className="mt-6 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className="w-8 h-8 bg-green-500 rounded-full flex items-center justify-center">
                        <GraduationCap className="w-4 h-4 text-white" />
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
                      <div className="w-8 h-8 bg-blue-500 rounded-full flex items-center justify-center">
                        <Clock className="w-4 h-4 text-white" />
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
                      <div className="w-8 h-8 bg-gray-500 rounded-full flex items-center justify-center">
                        <Star className="w-4 h-4 text-white" />
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
            
            {/* Activity Feed */}
            <div className="mt-8">
              <ActivityFeed />
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