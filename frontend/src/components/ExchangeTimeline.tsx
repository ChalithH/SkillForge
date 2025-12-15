import { SkillExchange, ExchangeStatus } from '../types';

interface ExchangeTimelineProps {
  exchange: SkillExchange;
  className?: string;
}

interface TimelineStep {
  status: ExchangeStatus;
  label: string;
  description: string;
  icon: React.ReactNode;
  isCompleted: boolean;
  isCurrent: boolean;
  isError?: boolean;
}

export const ExchangeTimeline: React.FC<ExchangeTimelineProps> = ({
  exchange,
  className = ''
}) => {
  const getTimelineSteps = (): TimelineStep[] => {
    const currentStatus = exchange.status;
    
    // Define the normal flow
    const normalFlow = [
      ExchangeStatus.Pending,
      ExchangeStatus.Accepted,
      ExchangeStatus.Completed
    ];
    
    // Define error states
    const errorStates = [ExchangeStatus.Rejected, ExchangeStatus.Cancelled, ExchangeStatus.NoShow];
    
    const steps: TimelineStep[] = [
      {
        status: ExchangeStatus.Pending,
        label: 'Request Sent',
        description: 'Exchange request submitted',
        icon: (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        ),
        isCompleted: true,
        isCurrent: currentStatus === ExchangeStatus.Pending
      },
      {
        status: ExchangeStatus.Accepted,
        label: 'Accepted',
        description: 'Exchange confirmed',
        icon: (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        ),
        isCompleted: normalFlow.indexOf(currentStatus) > normalFlow.indexOf(ExchangeStatus.Accepted) || currentStatus === ExchangeStatus.Accepted,
        isCurrent: currentStatus === ExchangeStatus.Accepted
      },
      {
        status: ExchangeStatus.Completed,
        label: 'Completed',
        description: 'Exchange finished successfully',
        icon: (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
          </svg>
        ),
        isCompleted: currentStatus === ExchangeStatus.Completed,
        isCurrent: currentStatus === ExchangeStatus.Completed
      }
    ];
    
    // Add error state if applicable
    if (errorStates.includes(currentStatus)) {
      const errorStep: TimelineStep = {
        status: currentStatus,
        label: currentStatus === ExchangeStatus.Rejected ? 'Rejected' : 
               currentStatus === ExchangeStatus.Cancelled ? 'Cancelled' : 'No Show',
        description: currentStatus === ExchangeStatus.Rejected ? 'Exchange was declined' :
                    currentStatus === ExchangeStatus.Cancelled ? 'Exchange was cancelled' : 'Participant did not attend',
        icon: (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
        ),
        isCompleted: true,
        isCurrent: true,
        isError: true
      };
      
      // Insert error step in the appropriate position
      if (currentStatus === ExchangeStatus.Rejected) {
        steps.splice(1, 0, errorStep); // After pending
      } else {
        steps.push(errorStep); // At the end for cancelled/no-show
      }
    }
    
    return steps;
  };

  const steps = getTimelineSteps();
  
  const getStepStyles = (step: TimelineStep) => {
    if (step.isError) {
      return {
        dot: 'bg-red-500 border-red-500',
        line: 'bg-red-200',
        text: 'text-red-700',
        description: 'text-red-600'
      };
    } else if (step.isCompleted) {
      return {
        dot: 'bg-green-500 border-green-500',
        line: 'bg-green-200',
        text: 'text-green-700',
        description: 'text-green-600'
      };
    } else if (step.isCurrent) {
      return {
        dot: 'bg-blue-500 border-blue-500',
        line: 'bg-gray-200',
        text: 'text-blue-700',
        description: 'text-blue-600'
      };
    } else {
      return {
        dot: 'bg-gray-300 border-gray-300',
        line: 'bg-gray-200',
        text: 'text-gray-500',
        description: 'text-gray-400'
      };
    }
  };

  const formatDateTime = (dateTime: string) => {
    const date = new Date(dateTime);
    return {
      date: date.toLocaleDateString(),
      time: date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    };
  };

  const { date, time } = formatDateTime(exchange.scheduledAt);
  const skillDisplayName = exchange.skill?.name || exchange.skillName || `Skill ID: ${exchange.skillId}`;

  return (
    <div className={`bg-white rounded-lg border border-gray-200 p-4 sm:p-6 ${className}`}>
      <div className="mb-4 sm:mb-6">
        <h3 className="text-base sm:text-lg font-semibold text-gray-900 mb-1">
          Exchange Timeline
        </h3>
        <p className="text-xs sm:text-sm text-gray-600">
          {skillDisplayName} • {date} at {time}
        </p>
      </div>
      
      <div className="relative">
        {steps.map((step, index) => {
          const styles = getStepStyles(step);
          const isLast = index === steps.length - 1;
          
          return (
            <div key={step.status} className="relative flex items-start pb-6 last:pb-0">
              {/* Vertical line */}
              {!isLast && (
                <div className={`absolute left-2 top-6 bottom-0 w-0.5 ${styles.line}`} />
              )}
              
              {/* Status dot */}
              <div className={`relative flex-shrink-0 w-4 h-4 rounded-full border-2 ${styles.dot} flex items-center justify-center`}>
                <div className="text-white">
                  {step.icon}
                </div>
              </div>
              
              {/* Content */}
              <div className="ml-3 sm:ml-4 flex-1 min-w-0">
                <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
                  <div className="flex-1">
                    <p className={`text-sm font-medium ${styles.text}`}>
                      {step.label}
                    </p>
                    <p className={`text-xs mt-0.5 ${styles.description}`}>
                      {step.description}
                    </p>
                  </div>
                  
                  {/* Timestamp for key events */}
                  {(step.status === ExchangeStatus.Pending || step.isCompleted) && (
                    <div className="text-xs text-gray-500 mt-1 sm:mt-0 sm:ml-4">
                      {step.status === ExchangeStatus.Pending && (
                        <span>Created {new Date(exchange.createdAt).toLocaleDateString()}</span>
                      )}
                      {step.isCompleted && step.status !== ExchangeStatus.Pending && (
                        <span>Updated {new Date(exchange.updatedAt).toLocaleDateString()}</span>
                      )}
                    </div>
                  )}
                </div>
                
                {/* Additional info for specific statuses */}
                {step.status === ExchangeStatus.Accepted && step.isCompleted && exchange.meetingLink && (
                  <div className="mt-2 p-2 bg-blue-50 rounded text-xs">
                    <p className="text-blue-700 font-medium">Meeting Link Available</p>
                    <p className="text-blue-600 truncate">{exchange.meetingLink}</p>
                  </div>
                )}
                
                {step.status === ExchangeStatus.Completed && exchange.reviews && exchange.reviews.length > 0 && (
                  <div className="mt-2 p-2 bg-green-50 rounded text-xs">
                    <p className="text-green-700 font-medium">Reviews Submitted</p>
                    <p className="text-green-600">{exchange.reviews.length} review(s) available</p>
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>
      
      {/* Additional exchange info */}
      {exchange.notes && (
        <div className="mt-4 sm:mt-6 pt-4 border-t border-gray-100">
          <h4 className="text-sm font-medium text-gray-900 mb-2">Notes</h4>
          <p className="text-xs sm:text-sm text-gray-600 bg-gray-50 p-2 sm:p-3 rounded">
            {exchange.notes}
          </p>
        </div>
      )}
    </div>
  );
};