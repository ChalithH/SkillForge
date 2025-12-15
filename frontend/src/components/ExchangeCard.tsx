import { SkillExchange, ExchangeStatus } from '../types';
import { useAppSelector } from '../store/hooks';
import {
  useAcceptExchangeMutation,
  useRejectExchangeMutation,
  useCancelExchangeMutation,
  useCompleteExchangeMutation
} from '../store/api/apiSlice';

interface ExchangeCardProps {
  exchange: SkillExchange;
  onViewDetails?: (exchange: SkillExchange) => void;
  onJoinMeeting?: (meetingLink: string) => void;
}

export const ExchangeCard: React.FC<ExchangeCardProps> = ({
  exchange,
  onViewDetails,
  onJoinMeeting
}) => {
  const currentUser = useAppSelector((state) => state.auth.user);
  const [acceptExchange, { isLoading: isAccepting }] = useAcceptExchangeMutation();
  const [rejectExchange, { isLoading: isRejecting }] = useRejectExchangeMutation();
  const [cancelExchange, { isLoading: isCancelling }] = useCancelExchangeMutation();
  const [completeExchange, { isLoading: isCompleting }] = useCompleteExchangeMutation();

  const isLoading = isAccepting || isRejecting || isCancelling || isCompleting;

  const getStatusBadge = (status: ExchangeStatus) => {
    const badges = {
      [ExchangeStatus.Pending]: 'bg-yellow-100 text-yellow-800 border-yellow-200',
      [ExchangeStatus.Accepted]: 'bg-green-100 text-green-800 border-green-200',
      [ExchangeStatus.Rejected]: 'bg-red-100 text-red-800 border-red-200',
      [ExchangeStatus.Cancelled]: 'bg-gray-100 text-gray-800 border-gray-200',
      [ExchangeStatus.Completed]: 'bg-blue-100 text-blue-800 border-blue-200',
      [ExchangeStatus.NoShow]: 'bg-orange-100 text-orange-800 border-orange-200',
    };

    const labels = {
      [ExchangeStatus.Pending]: 'Pending',
      [ExchangeStatus.Accepted]: 'Accepted',
      [ExchangeStatus.Rejected]: 'Rejected',
      [ExchangeStatus.Cancelled]: 'Cancelled',
      [ExchangeStatus.Completed]: 'Completed',
      [ExchangeStatus.NoShow]: 'No Show',
    };

    return (
      <span className={`px-2 py-1 text-xs rounded-full border ${badges[status]}`}>
        {labels[status]}
      </span>
    );
  };

  const isOfferer = currentUser?.id === exchange.offererId;
  const otherUserName = isOfferer
    ? (exchange.learner?.name || exchange.learnerName || 'Unknown User')
    : (exchange.offerer?.name || exchange.offererName || 'Unknown User');
  const role = isOfferer ? 'Teaching' : 'Learning';
  const skillDisplayName = exchange.skill?.name || exchange.skillName || `Skill ID: ${exchange.skillId}`;

  const formatDateTime = (dateTime: string) => {
    const date = new Date(dateTime);
    return {
      date: date.toLocaleDateString(),
      time: date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    };
  };

  const { date, time } = formatDateTime(exchange.scheduledAt);

  const handleAccept = async () => {
    try {
      await acceptExchange({ id: exchange.id }).unwrap();
    } catch (error) {
      console.error('Failed to accept exchange:', error);
    }
  };

  const handleReject = async () => {
    try {
      await rejectExchange({ id: exchange.id }).unwrap();
    } catch (error) {
      console.error('Failed to reject exchange:', error);
    }
  };

  const handleCancel = async () => {
    try {
      await cancelExchange({ id: exchange.id }).unwrap();
    } catch (error) {
      console.error('Failed to cancel exchange:', error);
    }
  };

  const handleMarkCompleted = async () => {
    try {
      await completeExchange({ id: exchange.id }).unwrap();
    } catch (error) {
      console.error('Failed to mark as completed:', error);
    }
  };

  const canAcceptOrReject = isOfferer && exchange.status === ExchangeStatus.Pending;
  const canCancel = exchange.status === ExchangeStatus.Pending || exchange.status === ExchangeStatus.Accepted;
  const canMarkCompleted = exchange.status === ExchangeStatus.Accepted && new Date(exchange.scheduledAt) < new Date();
  const canJoinMeeting = exchange.status === ExchangeStatus.Accepted && exchange.meetingLink;

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-4 sm:p-6 hover:shadow-lg transition-shadow">
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between mb-3 sm:mb-4">
        <div className="flex-1">
          <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-3 mb-2">
            <h3 className="text-base sm:text-lg font-semibold text-gray-900 leading-tight">
              {skillDisplayName}
            </h3>
            <div className="self-start sm:self-auto">
              {getStatusBadge(exchange.status)}
            </div>
          </div>
          
          <div className="text-sm text-gray-600 mb-2">
            <span className="font-medium">{role}</span> with{' '}
            <span className="font-medium text-gray-900">
              {otherUserName}
            </span>
          </div>

          <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-4 text-xs sm:text-sm text-gray-500">
            <div className="flex items-center gap-1">
              <svg className="w-3 h-3 sm:w-4 sm:h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              <span>{date}</span>
            </div>
            <div className="flex items-center gap-1">
              <svg className="w-3 h-3 sm:w-4 sm:h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <span>{time}</span>
            </div>
            <div className="flex items-center gap-1">
              <svg className="w-3 h-3 sm:w-4 sm:h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <span>{exchange.duration}h</span>
            </div>
          </div>

          {exchange.notes && (
            <p className="text-xs sm:text-sm text-gray-600 mt-2 sm:mt-3 p-2 sm:p-3 bg-gray-50 rounded">
              {exchange.notes}
            </p>
          )}
        </div>
      </div>

      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between pt-3 sm:pt-4 border-t border-gray-100 gap-3 sm:gap-0">
        <div className="flex flex-wrap gap-2">
          {canAcceptOrReject && (
            <>
              <button
                onClick={handleAccept}
                disabled={isLoading}
                className="px-3 py-1.5 sm:px-4 sm:py-2 bg-green-600 text-white text-xs sm:text-sm rounded-md hover:bg-green-700 disabled:opacity-50 transition-colors"
              >
                Accept
              </button>
              <button
                onClick={handleReject}
                disabled={isLoading}
                className="px-3 py-1.5 sm:px-4 sm:py-2 bg-red-600 text-white text-xs sm:text-sm rounded-md hover:bg-red-700 disabled:opacity-50 transition-colors"
              >
                Reject
              </button>
            </>
          )}

          {canJoinMeeting && (
            <button
              onClick={() => onJoinMeeting?.(exchange.meetingLink!)}
              className="px-3 py-1.5 sm:px-4 sm:py-2 bg-blue-600 text-white text-xs sm:text-sm rounded-md hover:bg-blue-700 transition-colors"
            >
              Join Meeting
            </button>
          )}

          {canMarkCompleted && (
            <button
              onClick={handleMarkCompleted}
              disabled={isLoading}
              className="px-3 py-1.5 sm:px-4 sm:py-2 bg-purple-600 text-white text-xs sm:text-sm rounded-md hover:bg-purple-700 disabled:opacity-50 transition-colors"
            >
              Mark Completed
            </button>
          )}

          {canCancel && (
            <button
              onClick={handleCancel}
              disabled={isLoading}
              className="px-3 py-1.5 sm:px-4 sm:py-2 bg-gray-600 text-white text-xs sm:text-sm rounded-md hover:bg-gray-700 disabled:opacity-50 transition-colors"
            >
              Cancel
            </button>
          )}
        </div>

        <button
          onClick={() => onViewDetails?.(exchange)}
          className="px-3 py-1.5 sm:px-4 sm:py-2 text-gray-600 text-xs sm:text-sm border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
        >
          View Details
        </button>
      </div>
    </div>
  );
};