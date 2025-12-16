interface NotificationBadgeProps {
  count: number;
  className?: string;
  maxCount?: number;
  showZero?: boolean;
}

export const NotificationBadge: React.FC<NotificationBadgeProps> = ({
  count,
  className = '',
  maxCount = 99,
  showZero = false
}) => {
  if (!showZero && count === 0) {
    return null;
  }

  const displayCount = count > maxCount ? `${maxCount}+` : count.toString();

  return (
    <span className={`inline-flex items-center justify-center min-w-[16px] h-[16px] px-1 text-[10px] font-bold leading-none text-white bg-red-600 rounded-full ${className}`}>
      {displayCount}
    </span>
  );
};