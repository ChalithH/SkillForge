import { useGetUserSkillsQuery } from '../store/api/apiSlice';
import { useSkillFilters } from '../hooks/useSkillFilters';
import Navigation from '../components/Navigation';
import { ActivityFeed } from '../components/ActivityFeed';
import { GraduationCap, Clock, Star } from 'lucide-react';

export default function Dashboard() {
  const { data: userSkills = [] } = useGetUserSkillsQuery();

  // Use standardized skill filtering
  const { offeredSkillsCount } = useSkillFilters(userSkills);


  return (
    <div className="min-h-screen bg-gray-50">
      <Navigation />
      
      <main className="max-w-7xl mx-auto py-4 px-4 sm:py-6 sm:px-6 lg:px-8">
        <div className="py-4 sm:py-6">
          <div className="bg-white rounded-lg shadow-sm p-4 sm:p-6 lg:p-8">
            <h2 className="text-xl sm:text-2xl font-bold text-gray-900 mb-4 sm:mb-6">Dashboard</h2>

            <div className="grid grid-cols-1 gap-4 sm:gap-5 sm:grid-cols-2 lg:grid-cols-3">
              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-4 sm:p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className="w-8 h-8 bg-green-500 rounded-full flex items-center justify-center flex-shrink-0">
                        <GraduationCap className="w-4 h-4 text-white" />
                      </div>
                    </div>
                    <div className="ml-3 sm:ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">
                          Skills Offering
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
                <div className="p-4 sm:p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className="w-8 h-8 bg-purple-500 rounded-full flex items-center justify-center flex-shrink-0">
                        <Clock className="w-4 h-4 text-white" />
                      </div>
                    </div>
                    <div className="ml-3 sm:ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">
                          Sessions Completed
                        </dt>
                        <dd className="text-lg font-medium text-gray-900">
                          0
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>
              
              <div className="bg-white overflow-hidden shadow rounded-lg sm:col-span-2 lg:col-span-1">
                <div className="p-4 sm:p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className="w-8 h-8 bg-gray-500 rounded-full flex items-center justify-center flex-shrink-0">
                        <Star className="w-4 h-4 text-white" />
                      </div>
                    </div>
                    <div className="ml-3 sm:ml-5 w-0 flex-1">
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
            <div className="mt-6 sm:mt-8">
              <ActivityFeed />
            </div>
          </div>
        </div>
      </main>

    </div>
  );
}