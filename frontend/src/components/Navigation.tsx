import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import { logout } from '../store/slices/authSlice';
import { useState, useEffect, useRef } from 'react';
import { Bell, Menu, Coins, TrendingUp, TrendingDown, X } from 'lucide-react';
import { NotificationBadge } from './NotificationBadge';
import { usePendingRequests } from '../hooks/usePendingRequests';

export default function Navigation() {
  const location = useLocation();
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const { user } = useAppSelector((state) => state.auth);
  const [previousCredits, setPreviousCredits] = useState<number | null>(null);
  const [creditChange, setCreditChange] = useState<'increase' | 'decrease' | null>(null);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const mobileMenuRef = useRef<HTMLDivElement>(null);
  const { incomingCount, totalPendingCount } = usePendingRequests();

  // Track credit changes for visual feedback
  useEffect(() => {
    if (user?.timeCredits !== undefined) {
      if (previousCredits !== null && previousCredits !== user.timeCredits) {
        setCreditChange(user.timeCredits > previousCredits ? 'increase' : 'decrease');
        // Clear the change indicator after animation
        setTimeout(() => setCreditChange(null), 2000);
      }
      setPreviousCredits(user.timeCredits);
    }
  }, [user?.timeCredits, previousCredits]);

  // Close mobile menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (mobileMenuRef.current && !mobileMenuRef.current.contains(event.target as Node)) {
        setIsMobileMenuOpen(false);
      }
    };

    if (isMobileMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => {
        document.removeEventListener('mousedown', handleClickOutside);
      };
    }
  }, [isMobileMenuOpen]);

  // Close mobile menu on route change
  useEffect(() => {
    setIsMobileMenuOpen(false);
  }, [location]);

  const handleLogout = () => {
    dispatch(logout());
    navigate('/login');
  };


  const navigation = [
    { name: 'Dashboard', href: '/dashboard', current: location.pathname === '/dashboard' },
    { name: 'Browse', href: '/browse', current: location.pathname === '/browse' },
    { name: 'My Skills', href: '/skills', current: location.pathname === '/skills' },
    { name: 'My Sessions', href: '/exchanges', current: location.pathname === '/exchanges' },
    { name: 'Profile', href: '/profile', current: location.pathname === '/profile' },
  ];

  return (
    <nav className="bg-white shadow">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between h-16">
          <div className="flex">
            <div className="flex-shrink-0 flex items-center">
              <Link to="/dashboard" className="text-xl font-semibold text-gray-900">
                SkillForge
              </Link>
            </div>
            <div className="hidden sm:ml-6 sm:flex sm:space-x-8">
              {navigation.map((item) => (
                <Link
                  key={item.name}
                  to={item.href}
                  className={`${
                    item.current
                      ? 'border-blue-500 text-gray-900'
                      : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'
                  } inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium transition-colors relative`}
                >
                  {item.name}
                  {item.name === 'My Sessions' && incomingCount > 0 && (
                    <NotificationBadge 
                      count={incomingCount} 
                      className="ml-2 -mt-1"
                    />
                  )}
                </Link>
              ))}
            </div>
            
            {/* Mobile menu button */}
            <div className="sm:hidden flex items-center ml-2">
              <button
                onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
                className="text-gray-500 hover:text-gray-700 p-2 rounded-md transition-colors"
                aria-label="Toggle mobile menu"
              >
                {isMobileMenuOpen ? (
                  <X className="w-5 h-5" />
                ) : (
                  <Menu className="w-5 h-5" />
                )}
              </button>
            </div>
          </div>
          <div className="flex items-center space-x-2 sm:space-x-4">
            {/* Notification Bell */}
            <div className="relative">
              <Link
                to="/exchanges"
                className="text-gray-400 hover:text-gray-600 transition-colors p-1"
                title={`${incomingCount} pending request${incomingCount !== 1 ? 's' : ''}`}
              >
                <Bell className="h-5 w-5" />
              </Link>
              {incomingCount > 0 && (
                <NotificationBadge 
                  count={incomingCount} 
                  className="absolute -top-1 -right-1 min-w-[18px] h-[18px] text-[10px]"
                />
              )}
            </div>
            
            {/* Credit Balance with Animation */}
            <div className="flex items-center space-x-1">
              <div className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium transition-all duration-500 ${
                creditChange === 'increase' ? 'bg-amber-200 text-amber-900 scale-110' :
                creditChange === 'decrease' ? 'bg-red-200 text-red-900 scale-110' :
                'bg-amber-100 text-amber-800'
              }`}>
                <Coins className="w-3 h-3 mr-1" />
                {user?.timeCredits || 0} credits
                {creditChange === 'increase' && (
                  <TrendingUp className="w-3 h-3 ml-1 text-amber-600" />
                )}
                {creditChange === 'decrease' && (
                  <TrendingDown className="w-3 h-3 ml-1 text-red-600" />
                )}
              </div>
            </div>
            
            <button
              onClick={handleLogout}
              className="text-gray-500 hover:text-gray-700 px-2 sm:px-3 py-2 text-sm font-medium transition-colors"
            >
              Logout
            </button>
          </div>
        </div>

        {/* Mobile menu */}
        {isMobileMenuOpen && (
          <div ref={mobileMenuRef} className="sm:hidden border-t border-gray-200">
            <div className="pt-2 pb-3 space-y-1">
              {navigation.map((item) => (
                <Link
                  key={item.name}
                  to={item.href}
                  className={`${
                    item.current
                      ? 'bg-blue-50 border-blue-500 text-blue-700'
                      : 'border-transparent text-gray-500 hover:bg-gray-50 hover:border-gray-300 hover:text-gray-700'
                  } block pl-3 pr-4 py-2 border-l-4 text-base font-medium transition-colors relative`}
                >
                  <span className="flex items-center justify-between">
                    {item.name}
                    {item.name === 'My Sessions' && incomingCount > 0 && (
                      <NotificationBadge
                        count={incomingCount}
                        className="mr-2"
                      />
                    )}
                  </span>
                </Link>
              ))}
            </div>
            <div className="pt-3 pb-3 border-t border-gray-200">
              <div className="flex items-center px-4 space-x-3">
                {/* Credit Balance Mobile */}
                <div className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium transition-all duration-500 ${
                  creditChange === 'increase' ? 'bg-amber-200 text-amber-900' :
                  creditChange === 'decrease' ? 'bg-red-200 text-red-900' :
                  'bg-amber-100 text-amber-800'
                }`}>
                  <Coins className="w-3 h-3 mr-1" />
                  {user?.timeCredits || 0} credits
                  {creditChange === 'increase' && (
                    <TrendingUp className="w-3 h-3 ml-1 text-amber-600" />
                  )}
                  {creditChange === 'decrease' && (
                    <TrendingDown className="w-3 h-3 ml-1 text-red-600" />
                  )}
                </div>

                {/* Notification Bell Mobile */}
                <div className="relative">
                  <Link
                    to="/exchanges"
                    className="text-gray-400 hover:text-gray-600 transition-colors p-1"
                    title={`${incomingCount} pending request${incomingCount !== 1 ? 's' : ''}`}
                  >
                    <Bell className="h-5 w-5" />
                  </Link>
                  {incomingCount > 0 && (
                    <NotificationBadge
                      count={incomingCount}
                      className="absolute -top-1 -right-1 min-w-[18px] h-[18px] text-[10px]"
                    />
                  )}
                </div>
              </div>

              <div className="mt-3 px-4">
                <button
                  onClick={handleLogout}
                  className="block w-full text-left px-3 py-2 text-base font-medium text-gray-500 hover:text-gray-700 hover:bg-gray-50 rounded-md transition-colors"
                >
                  Logout
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </nav>
  );
}