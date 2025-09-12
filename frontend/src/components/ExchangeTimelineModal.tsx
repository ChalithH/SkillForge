import { SkillExchange } from '../types';
import { ExchangeTimeline } from './ExchangeTimeline';
import { useAppSelector } from '../store/hooks';

interface ExchangeTimelineModalProps {
  isOpen: boolean;
  onClose: () => void;
  exchange: SkillExchange | null;
}

export const ExchangeTimelineModal: React.FC<ExchangeTimelineModalProps> = ({
  isOpen,
  onClose,
  exchange
}) => {
  const currentUser = useAppSelector((state) => state.auth.user);

  if (!isOpen || !exchange) return null;

  const isOfferer = currentUser?.id === exchange.offererId;
  const otherUser = isOfferer ? exchange.learner : exchange.offerer;
  const role = isOfferer ? 'Teaching' : 'Learning';

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
      <div className="relative top-4 sm:top-20 mx-auto p-4 sm:p-5 border w-full max-w-2xl bg-white rounded-md shadow-lg">
        {/* Header */}
        <div className="flex items-start justify-between mb-4 sm:mb-6">
          <div className="flex-1">
            <h2 className="text-lg sm:text-xl font-semibold text-gray-900">
              Exchange Details
            </h2>
            <div className="mt-1 text-sm text-gray-600">
              <span className="font-medium">{role}</span> {exchange.skill?.name} with{' '}
              <span className="font-medium text-gray-900">
                {otherUser?.name || 'Unknown User'}
              </span>
            </div>
          </div>
          <button
            onClick={onClose}
            className="ml-4 text-gray-400 hover:text-gray-600 transition-colors"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Session Summary */}
        <div className="mb-4 sm:mb-6 p-3 sm:p-4 bg-gray-50 rounded-lg">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4 text-sm">
            <div>
              <span className="font-medium text-gray-700">Duration:</span>
              <span className="ml-2 text-gray-900">{exchange.duration} hour{exchange.duration !== 1 ? 's' : ''}</span>
            </div>
            <div>
              <span className="font-medium text-gray-700">Scheduled:</span>
              <span className="ml-2 text-gray-900">
                {new Date(exchange.scheduledAt).toLocaleString()}
              </span>
            </div>
            {exchange.meetingLink && (
              <div className="sm:col-span-2">
                <span className="font-medium text-gray-700">Meeting Link:</span>
                <a 
                  href={exchange.meetingLink} 
                  target="_blank" 
                  rel="noopener noreferrer"
                  className="ml-2 text-blue-600 hover:text-blue-800 underline break-all"
                >
                  {exchange.meetingLink}
                </a>
              </div>
            )}
          </div>
        </div>

        {/* Timeline */}
        <div className="mb-4 sm:mb-6">
          <ExchangeTimeline exchange={exchange} />
        </div>

        {/* User Information */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-4 sm:mb-6">
          <div className="p-3 sm:p-4 border border-gray-200 rounded-lg">
            <h4 className="font-medium text-gray-900 mb-2">Teacher</h4>
            <div className="text-sm text-gray-600">
              <p className="font-medium text-gray-900">{exchange.offerer?.name || 'Unknown'}</p>
              <p>Time Credits: {exchange.offerer?.timeCredits || 0}</p>
            </div>
          </div>
          <div className="p-3 sm:p-4 border border-gray-200 rounded-lg">
            <h4 className="font-medium text-gray-900 mb-2">Student</h4>
            <div className="text-sm text-gray-600">
              <p className="font-medium text-gray-900">{exchange.learner?.name || 'Unknown'}</p>
              <p>Time Credits: {exchange.learner?.timeCredits || 0}</p>
            </div>
          </div>
        </div>

        {/* Reviews Section */}
        {exchange.reviews && exchange.reviews.length > 0 && (
          <div className="mb-4 sm:mb-6">
            <h4 className="font-medium text-gray-900 mb-3">Reviews</h4>
            <div className="space-y-3">
              {exchange.reviews.map((review) => (
                <div key={review.id} className="p-3 bg-gray-50 rounded-lg">
                  <div className="flex items-center justify-between mb-2">
                    <span className="font-medium text-sm text-gray-900">
                      {review.reviewer?.name || 'Anonymous'}
                    </span>
                    <div className="flex items-center">
                      {[1, 2, 3, 4, 5].map((star) => (
                        <svg
                          key={star}
                          className={`w-4 h-4 ${star <= review.rating ? 'text-yellow-400' : 'text-gray-300'}`}
                          fill="currentColor"
                          viewBox="0 0 20 20"
                        >
                          <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                        </svg>
                      ))}
                    </div>
                  </div>
                  {review.comment && (
                    <p className="text-sm text-gray-600">{review.comment}</p>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Close Button */}
        <div className="flex justify-end">
          <button
            onClick={onClose}
            className="px-4 py-2 bg-gray-600 text-white text-sm rounded-md hover:bg-gray-700 transition-colors"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};