import { useState } from 'react';
import { Skill, UserSkill, CreateUserSkillRequest } from '../types';
import { useSkillSearch } from '../hooks/useSkillSearch';
import ModalLayout from './shared/ModalLayout';
import SkillSearchInput from './shared/SkillSearchInput';
import SkillSelectionCard from './shared/SkillSelectionCard';
import StarRating from './shared/StarRating';
import { BookOpen } from 'lucide-react';

interface AddSkillToLearnModalProps {
  isOpen: boolean;
  onClose: () => void;
  onAdd: (skillData: Omit<UserSkill, 'id' | 'userId'>) => void;
  isLoading?: boolean;
}

export default function AddSkillToLearnModal({ 
  isOpen, 
  onClose, 
  onAdd, 
  isLoading = false 
}: AddSkillToLearnModalProps) {
  const [selectedSkill, setSelectedSkill] = useState<Skill | null>(null);
  const [proficiencyLevel, setProficiencyLevel] = useState(1); // Default to Beginner for learning
  const [description, setDescription] = useState('');
  const [showDropdown, setShowDropdown] = useState(false);

  const {
    searchTerm,
    setSearchTerm,
    searchResults,
    isLoading: isSearchLoading
  } = useSkillSearch({ 
    filterContext: 'learning',
    maxResults: 10 
  });

  const resetForm = () => {
    setSelectedSkill(null);
    setSearchTerm('');
    setProficiencyLevel(1);
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
      isOffering: false, // This modal is specifically for learning
      description: description.trim(),
      skill: selectedSkill
    };

    onAdd(skillData);
  };

  return (
    <ModalLayout
      isOpen={isOpen}
      onClose={handleClose}
      title="Add Skill to Learn"
      subtitle="Discover new skills and connect with teachers in the community"
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
          placeholder="Search for skills you want to learn..."
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

        {/* Current Proficiency Level */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Your current level
            <span className="text-xs text-gray-500 ml-1">(be honest - this helps match you with the right teachers)</span>
          </label>
          <StarRating
            level={proficiencyLevel}
            onChange={setProficiencyLevel}
            showLabel={true}
          />
        </div>

        {/* Learning Goals */}
        <div>
          <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
            Learning goals <span className="text-gray-500">(optional)</span>
          </label>
          <textarea
            id="description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={3}
            placeholder="What specific aspects do you want to learn? What's your preferred learning style? Any specific goals or projects you're working on..."
            className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 text-sm"
          />
        </div>

        {/* Learning Benefits Info */}
        <div className="p-3 bg-blue-50 rounded-md border border-blue-200">
          <div className="flex items-start">
            <div className="flex-shrink-0">
              <BookOpen className="w-5 h-5 text-blue-600" />
            </div>
            <div className="ml-2">
              <h4 className="text-sm font-medium text-blue-800">Learning Benefits</h4>
              <p className="text-xs text-blue-700 mt-1">
                Use your time credits to learn from experienced teachers. Each hour of learning costs 1 time credit.
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
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {isLoading ? 'Adding...' : 'Add Learning Goal'}
          </button>
        </div>
      </form>
    </ModalLayout>
  );
}