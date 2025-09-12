import { useState } from 'react';
import { Skill, UserSkill, CreateUserSkillRequest } from '../types';
import { useSkillSearch } from '../hooks/useSkillSearch';
import ModalLayout from './shared/ModalLayout';
import SkillSearchInput from './shared/SkillSearchInput';
import SkillSelectionCard from './shared/SkillSelectionCard';
import StarRating from './shared/StarRating';
import { GraduationCap } from 'lucide-react';

interface AddSkillToTeachModalProps {
  isOpen: boolean;
  onClose: () => void;
  onAdd: (skillData: Omit<UserSkill, 'id' | 'userId'>) => void;
  isLoading?: boolean;
}

export default function AddSkillToTeachModal({ 
  isOpen, 
  onClose, 
  onAdd, 
  isLoading = false 
}: AddSkillToTeachModalProps) {
  const [selectedSkill, setSelectedSkill] = useState<Skill | null>(null);
  const [proficiencyLevel, setProficiencyLevel] = useState(3); // Default to Intermediate for teaching
  const [description, setDescription] = useState('');
  const [showDropdown, setShowDropdown] = useState(false);

  const {
    searchTerm,
    setSearchTerm,
    searchResults,
    isLoading: isSearchLoading
  } = useSkillSearch({ 
    filterContext: 'teaching',
    maxResults: 10 
  });

  const resetForm = () => {
    setSelectedSkill(null);
    setSearchTerm('');
    setProficiencyLevel(3);
    setDescription('');
    setShowDropdown(false);
  };

  const handleClose = () => {
    resetForm();
    onClose();
  };

  const handleSkillSelect = (skill: Skill) => {
    setSelectedSkill(skill);
    setSearchTerm(skill.name);
    setShowDropdown(false);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!selectedSkill) return;

    const skillData = {
      skillId: selectedSkill.id,
      proficiencyLevel,
      isOffering: true, // This modal is specifically for teaching
      description: description.trim(),
      skill: selectedSkill
    };

    onAdd(skillData);
  };

  return (
    <ModalLayout
      isOpen={isOpen}
      onClose={handleClose}
      title="Add Skill to Teach"
      subtitle="Share your expertise with others and earn time credits"
      maxWidth="md"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Skill Search */}
        <SkillSearchInput
          searchTerm={searchTerm}
          onSearchChange={setSearchTerm}
          searchResults={searchResults}
          onSkillSelect={handleSkillSelect}
          isLoading={isSearchLoading}
          placeholder="Search for skills you can teach..."
          showDropdown={showDropdown}
          onDropdownChange={setShowDropdown}
        />

        {/* Selected Skill Info */}
        {selectedSkill && (
          <SkillSelectionCard 
            skill={selectedSkill}
            onClear={() => {
              setSelectedSkill(null);
              setSearchTerm('');
            }}
          />
        )}

        {/* Proficiency Level */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Your proficiency level
            <span className="text-xs text-gray-500 ml-1">(recommended: Intermediate or higher for teaching)</span>
          </label>
          <StarRating
            level={proficiencyLevel}
            onChange={setProficiencyLevel}
            showLabel={true}
          />
        </div>

        {/* Teaching Description */}
        <div>
          <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
            Teaching notes <span className="text-gray-500">(optional)</span>
          </label>
          <textarea
            id="description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={3}
            placeholder="Describe your teaching approach, what you can cover, or any prerequisites students should have..."
            className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 text-sm"
          />
        </div>

        {/* Teaching Benefits Info */}
        <div className="p-3 bg-green-50 rounded-md border border-green-200">
          <div className="flex items-start">
            <div className="flex-shrink-0">
              <GraduationCap className="w-5 h-5 text-green-600" />
            </div>
            <div className="ml-2">
              <h4 className="text-sm font-medium text-green-800">Teaching Benefits</h4>
              <p className="text-xs text-green-700 mt-1">
                Earn 1 time credit for every hour you teach. Build your reputation and help others grow their skills.
              </p>
            </div>
          </div>
        </div>

        {/* Actions */}
        <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200">
          <button
            type="button"
            onClick={handleClose}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-gray-500 transition-colors"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={!selectedSkill || isLoading}
            className="px-4 py-2 text-sm font-medium text-white bg-green-600 hover:bg-green-700 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {isLoading ? 'Adding...' : 'Add Teaching Skill'}
          </button>
        </div>
      </form>
    </ModalLayout>
  );
}