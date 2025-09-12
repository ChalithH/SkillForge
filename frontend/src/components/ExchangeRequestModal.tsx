import { useState, useEffect } from 'react';
import { User, Skill, CreateExchangeRequest } from '../types';
import { useAppSelector } from '../store/hooks';
import { useCreateExchangeMutation } from '../store/api/apiSlice';

interface ExchangeRequestModalProps {
  isOpen: boolean;
  onClose: () => void;
  targetUser: User; // The user offering the skill
  skill?: Skill; // Optional - for backward compatibility
  skills?: Skill[]; // Multiple skills to choose from
  onSuccess?: () => void;
}

export const ExchangeRequestModal: React.FC<ExchangeRequestModalProps> = ({
  isOpen,
  onClose,
  targetUser,
  skill,
  skills,
  onSuccess
}) => {
  const currentUser = useAppSelector((state) => state.auth.user);
  const [createExchange, { isLoading }] = useCreateExchangeMutation();

  // Use skills array if provided, otherwise fall back to single skill
  const availableSkills = skills || (skill ? [skill] : []);
  
  // Form state
  const [formData, setFormData] = useState({
    selectedSkillId: availableSkills[0]?.id || 0,
    date: '',
    time: '',
    duration: 1,
    meetingLink: '',
    notes: ''
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  // Reset form when modal opens/closes
  useEffect(() => {
    if (isOpen) {
      // Set default date to tomorrow
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      
      setFormData({
        selectedSkillId: availableSkills[0]?.id || 0,
        date: tomorrow.toISOString().split('T')[0],
        time: '10:00',
        duration: 1,
        meetingLink: '',
        notes: ''
      });
      setErrors({});
    }
  }, [isOpen, availableSkills]);

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    // Validate date
    if (!formData.date) {
      newErrors.date = 'Date is required';
    } else {
      const selectedDate = new Date(`${formData.date}T${formData.time}`);
      const now = new Date();
      if (selectedDate <= now) {
        newErrors.date = 'Please select a future date and time';
      }
    }

    // Validate time
    if (!formData.time) {
      newErrors.time = 'Time is required';
    }

    // Validate duration
    if (formData.duration < 0.5 || formData.duration > 4) {
      newErrors.duration = 'Duration must be between 0.5 and 4 hours';
    }

    // Validate meeting link (optional but must be valid URL if provided)
    if (formData.meetingLink && !isValidUrl(formData.meetingLink)) {
      newErrors.meetingLink = 'Please enter a valid meeting URL';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const isValidUrl = (url: string) => {
    try {
      new URL(url);
      return true;
    } catch {
      return false;
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm() || !currentUser) return;

    try {
      const scheduledAt = new Date(`${formData.date}T${formData.time}`).toISOString();
      
      const exchangeRequest: CreateExchangeRequest = {
        offererId: targetUser.id,
        learnerId: currentUser.id,
        skillId: formData.selectedSkillId,
        scheduledAt,
        duration: formData.duration,
        meetingLink: formData.meetingLink || undefined,
        notes: formData.notes || undefined
      };

      await createExchange(exchangeRequest).unwrap();
      
      onSuccess?.();
      onClose();
    } catch (error: any) {
      console.error('Failed to create exchange request:', error);
      setErrors({ 
        submit: error?.data?.message || 'Failed to create exchange request. Please try again.' 
      });
    }
  };

  const handleInputChange = (field: string, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  const durationOptions = [
    { value: 0.5, label: '30 minutes' },
    { value: 1, label: '1 hour' },
    { value: 1.5, label: '1.5 hours' },
    { value: 2, label: '2 hours' },
    { value: 2.5, label: '2.5 hours' },
    { value: 3, label: '3 hours' },
    { value: 3.5, label: '3.5 hours' },
    { value: 4, label: '4 hours' }
  ];

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
      <div className="relative top-20 mx-auto p-5 border w-full max-w-lg bg-white rounded-md shadow-lg">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-medium text-gray-900">
            Request Skill Session
          </h3>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Session Summary */}
        <div className="bg-blue-50 border border-blue-200 rounded-md p-4 mb-6">
          <h4 className="font-medium text-blue-900 mb-2">Session Details</h4>
          <div className="text-sm text-blue-800">
            <p><strong>Teacher:</strong> {targetUser.name}</p>
            <p><strong>Student:</strong> {currentUser?.name} (You)</p>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Skill Selection (if multiple skills available) */}
          {availableSkills.length > 1 && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Select Skill to Learn *
              </label>
              <select
                value={formData.selectedSkillId}
                onChange={(e) => handleInputChange('selectedSkillId', parseInt(e.target.value))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                {availableSkills.map(s => (
                  <option key={s.id} value={s.id}>
                    {s.name} - {s.category}
                  </option>
                ))}
              </select>
            </div>
          )}
          
          {/* Single skill display (if only one skill) */}
          {availableSkills.length === 1 && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Skill to Learn
              </label>
              <div className="px-3 py-2 bg-gray-50 border border-gray-200 rounded-md">
                <span className="text-sm text-gray-900">{availableSkills[0]?.name}</span>
                <span className="text-sm text-gray-500 ml-2">({availableSkills[0]?.category})</span>
              </div>
            </div>
          )}

          {/* Date Input */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Date *
            </label>
            <input
              type="date"
              value={formData.date}
              onChange={(e) => handleInputChange('date', e.target.value)}
              min={new Date().toISOString().split('T')[0]}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                errors.date ? 'border-red-300' : 'border-gray-300'
              }`}
            />
            {errors.date && <p className="text-red-500 text-xs mt-1">{errors.date}</p>}
          </div>

          {/* Time Input */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Time *
            </label>
            <input
              type="time"
              value={formData.time}
              onChange={(e) => handleInputChange('time', e.target.value)}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                errors.time ? 'border-red-300' : 'border-gray-300'
              }`}
            />
            {errors.time && <p className="text-red-500 text-xs mt-1">{errors.time}</p>}
          </div>

          {/* Duration Selection */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Duration *
            </label>
            <select
              value={formData.duration}
              onChange={(e) => handleInputChange('duration', parseFloat(e.target.value))}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                errors.duration ? 'border-red-300' : 'border-gray-300'
              }`}
            >
              {durationOptions.map(option => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
            {errors.duration && <p className="text-red-500 text-xs mt-1">{errors.duration}</p>}
          </div>

          {/* Meeting Link */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Meeting Link (Optional)
            </label>
            <input
              type="url"
              value={formData.meetingLink}
              onChange={(e) => handleInputChange('meetingLink', e.target.value)}
              placeholder="https://meet.google.com/abc-def-ghi"
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                errors.meetingLink ? 'border-red-300' : 'border-gray-300'
              }`}
            />
            {errors.meetingLink && <p className="text-red-500 text-xs mt-1">{errors.meetingLink}</p>}
            <p className="text-xs text-gray-500 mt-1">
              You can add this later if not available now
            </p>
          </div>

          {/* Notes */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Message to Teacher (Optional)
            </label>
            <textarea
              value={formData.notes}
              onChange={(e) => handleInputChange('notes', e.target.value)}
              placeholder="Let them know what you'd like to focus on or any special requirements..."
              rows={3}
              maxLength={500}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <p className="text-xs text-gray-500 mt-1">
              {formData.notes.length}/500 characters
            </p>
          </div>

          {/* Credit Cost Info */}
          <div className="bg-yellow-50 border border-yellow-200 rounded-md p-3">
            <div className="flex items-center">
              <svg className="w-5 h-5 text-yellow-400 mr-2" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
              </svg>
              <div className="text-sm">
                <p className="text-yellow-800">
                  <strong>Cost:</strong> {formData.duration} time credit{formData.duration !== 1 ? 's' : ''}
                </p>
                <p className="text-yellow-700">
                  Current balance: {currentUser?.timeCredits || 0} credits
                </p>
              </div>
            </div>
          </div>

          {/* Submit Error */}
          {errors.submit && (
            <div className="bg-red-50 border border-red-200 rounded-md p-3">
              <p className="text-red-800 text-sm">{errors.submit}</p>
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex items-center justify-end gap-3 pt-4">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isLoading || (currentUser?.timeCredits || 0) < formData.duration}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? 'Sending Request...' : 'Send Request'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};