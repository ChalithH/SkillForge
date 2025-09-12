import { useState, useMemo } from 'react';
import { ExchangeCard } from '../components/ExchangeCard';
import { ExchangeTimelineModal } from '../components/ExchangeTimelineModal';
import Navigation from '../components/Navigation';
import { useGetExchangesQuery } from '../store/api/apiSlice';
import { ExchangeStatus, SkillExchange } from '../types';
import { ArrowLeftRight } from 'lucide-react';

export default function MyExchanges() {
  const [activeTab, setActiveTab] = useState<'all' | ExchangeStatus>('all');
  const [selectedExchange, setSelectedExchange] = useState<SkillExchange | null>(null);
  const [isTimelineModalOpen, setIsTimelineModalOpen] = useState(false);
  
  // Fetch all exchanges once for better UX and accurate counts
  // TODO: Add pagination/filtering when user base grows
  const { data: allExchanges = [], isLoading, error } = useGetExchangesQuery(undefined);
  
  // Filter exchanges based on active tab
  const exchanges = useMemo(() => {
    if (activeTab === 'all') return allExchanges;
    return allExchanges.filter(exchange => exchange.status === activeTab);
  }, [allExchanges, activeTab]);

  // Calculate counts for each status from all exchanges
  const tabs = useMemo(() => [
    { key: 'all' as const, label: 'All', count: allExchanges.length },
    { key: ExchangeStatus.Pending, label: 'Pending', status: ExchangeStatus.Pending, count: allExchanges.filter(e => e.status === ExchangeStatus.Pending).length },
    { key: ExchangeStatus.Accepted, label: 'Accepted', status: ExchangeStatus.Accepted, count: allExchanges.filter(e => e.status === ExchangeStatus.Accepted).length },
    { key: ExchangeStatus.Completed, label: 'Completed', status: ExchangeStatus.Completed, count: allExchanges.filter(e => e.status === ExchangeStatus.Completed).length },
    { key: ExchangeStatus.Cancelled, label: 'Cancelled', status: ExchangeStatus.Cancelled, count: allExchanges.filter(e => e.status === ExchangeStatus.Cancelled).length },
    { key: ExchangeStatus.Rejected, label: 'Rejected', status: ExchangeStatus.Rejected, count: allExchanges.filter(e => e.status === ExchangeStatus.Rejected).length },
    { key: ExchangeStatus.NoShow, label: 'No Show', status: ExchangeStatus.NoShow, count: allExchanges.filter(e => e.status === ExchangeStatus.NoShow).length },
  ], [allExchanges]);

  const handleViewDetails = (exchange: SkillExchange) => {
    setSelectedExchange(exchange);
    setIsTimelineModalOpen(true);
  };

  const handleJoinMeeting = (meetingLink: string) => {
    window.open(meetingLink, '_blank');
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50">
        <Navigation />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-6 lg:py-8">
          <div className="animate-pulse">
            <div className="h-6 sm:h-8 bg-gray-200 rounded w-1/2 sm:w-1/4 mb-4 sm:mb-6"></div>
            <div className="overflow-x-auto mb-6 sm:mb-8">
              <div className="flex space-x-3 sm:space-x-4 min-w-max">
                {[1, 2, 3, 4, 5].map((i) => (
                  <div key={i} className="h-8 sm:h-10 bg-gray-200 rounded w-16 sm:w-20 flex-shrink-0"></div>
                ))}
              </div>
            </div>
            <div className="space-y-3 sm:space-y-4">
              {[1, 2, 3].map((i) => (
                <div key={i} className="h-24 sm:h-32 bg-gray-200 rounded"></div>
              ))}
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50">
        <Navigation />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-6 lg:py-8">
          <div className="bg-red-50 border border-red-200 rounded-md p-3 sm:p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-4 w-4 sm:h-5 sm:w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-2 sm:ml-3">
                <h3 className="text-xs sm:text-sm font-medium text-red-800">
                  Error loading exchanges
                </h3>
                <div className="mt-1 sm:mt-2 text-xs sm:text-sm text-red-700">
                  <p>Unable to load your exchanges. Please try refreshing the page.</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <Navigation />
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-6 lg:py-8">
        {/* Header */}
        <div className="mb-6 sm:mb-8">
          <h1 className="text-2xl sm:text-3xl font-bold text-gray-900">My Sessions</h1>
          <p className="mt-1 sm:mt-2 text-sm sm:text-base text-gray-600">
            Manage your learning and teaching sessions
          </p>
        </div>

        {/* Tabs - Mobile Responsive with Horizontal Scroll */}
        <div className="border-b border-gray-200 mb-6 sm:mb-8">
          <div className="overflow-x-auto overflow-y-hidden">
            <nav className="-mb-px flex space-x-4 sm:space-x-8 min-w-max px-1" aria-label="Tabs">
              {tabs.map((tab) => {
                const isActive = activeTab === tab.key;
                return (
                  <button
                    key={tab.key}
                    onClick={() => setActiveTab(tab.key)}
                    className={`flex-shrink-0 whitespace-nowrap py-2 px-2 sm:px-1 border-b-2 font-medium text-sm transition-colors ${
                      isActive
                        ? 'border-blue-500 text-blue-600'
                        : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                    }`}
                  >
                    <span className="block sm:inline">{tab.label}</span>
                    {tab.count !== undefined && (
                      <span className={`ml-1 sm:ml-2 py-0.5 px-1.5 sm:px-2 rounded-full text-xs font-medium ${
                        isActive
                          ? 'bg-blue-100 text-blue-600'
                          : 'bg-gray-100 text-gray-900'
                      }`}>
                        {tab.count}
                      </span>
                    )}
                  </button>
                );
              })}
            </nav>
          </div>
          {/* Mobile scroll hint */}
          <div className="sm:hidden text-center text-xs text-gray-400 mt-2">
            ← Swipe to see more tabs →
          </div>
        </div>

        {/* Content */}
        <div className="space-y-4 sm:space-y-6">
          {exchanges.length === 0 ? (
            <div className="text-center py-8 sm:py-12 px-4">
              <ArrowLeftRight className="mx-auto h-10 w-10 sm:h-12 sm:w-12 text-gray-400" />
              <h3 className="mt-2 text-sm sm:text-base font-medium text-gray-900">
                {activeTab === 'all' ? 'No sessions yet' : `No ${tabs.find(t => t.key === activeTab)?.label.toLowerCase()} sessions`}
              </h3>
              <p className="mt-1 text-xs sm:text-sm text-gray-500 max-w-sm mx-auto">
                {activeTab === 'all' 
                  ? 'Start by browsing available skills and requesting a session.'
                  : `You don't have any ${tabs.find(t => t.key === activeTab)?.label.toLowerCase()} sessions.`
                }
              </p>
              {activeTab === 'all' && (
                <div className="mt-4 sm:mt-6">
                  <button
                    type="button"
                    onClick={() => window.location.href = '/browse'}
                    className="inline-flex items-center px-3 py-2 sm:px-4 sm:py-2 border border-transparent shadow-sm text-xs sm:text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
                  >
                    Browse Skills
                  </button>
                </div>
              )}
            </div>
          ) : (
            <div className="space-y-4 sm:space-y-6">
              {exchanges.map((exchange) => (
                <ExchangeCard
                  key={exchange.id}
                  exchange={exchange}
                  onViewDetails={handleViewDetails}
                  onJoinMeeting={handleJoinMeeting}
                  onViewTimeline={handleViewDetails}
                />
              ))}
            </div>
          )}
        </div>

        {/* Quick Stats */}
        {allExchanges.length > 0 && activeTab === 'all' && (
          <div className="mt-8 sm:mt-12 bg-white rounded-lg shadow p-4 sm:p-6">
            <h2 className="text-base sm:text-lg font-medium text-gray-900 mb-3 sm:mb-4">Session Overview</h2>
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3 sm:gap-4">
              {tabs.slice(1).map((tab) => {
                return (
                  <div key={tab.key} className="text-center p-2 sm:p-0">
                    <div className="text-xl sm:text-2xl font-bold text-gray-900">{tab.count}</div>
                    <div className="text-xs sm:text-sm text-gray-500 leading-tight">{tab.label}</div>
                  </div>
                );
              })}
            </div>
          </div>
        )}
      </div>

      {/* Timeline Modal */}
      <ExchangeTimelineModal
        isOpen={isTimelineModalOpen}
        onClose={() => {
          setIsTimelineModalOpen(false);
          setSelectedExchange(null);
        }}
        exchange={selectedExchange}
      />
    </div>
  );
}