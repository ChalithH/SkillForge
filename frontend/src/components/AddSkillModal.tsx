import { useState, useEffect, useRef } from 'react';
import { Skill, UserSkill } from '../types';
import { useGetSkillsQuery } from '../store/api/apiSlice';
import { searchSkills, highlightSearchTerm } from '../utils/searchUtils';
import { GraduationCap, BookOpen } from 'lucide-react';

interface AddSkillModalProps {
  isOpen: boolean;
  onClose: () => void;
  onAdd: (skillData: Omit<UserSkill, 'id' | 'userId'>) => void;
  isLoading?: boolean;
  defaultIsOffering?: boolean;
}

export default function AddSkillModal({ isOpen, onClose, onAdd, isLoading = false, defaultIsOffering }: AddSkillModalProps) {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedSkill, setSelectedSkill] = useState<Skill | null>(null);
  const [proficiencyLevel, setProficiencyLevel] = useState(1);
  const [isOffering, setIsOffering] = useState(true);
  const [description, setDescription] = useState('');
  const [showDropdown, setShowDropdown] = useState(false);
  
  const searchInputRef = useRef<HTMLInputElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Get all skills for client-side filtering
  const { data: allSkills = [], isLoading: isLoadingSkills } = useGetSkillsQuery();
  
  // Enhanced client-side search with fuzzy matching and relevance scoring
  const filteredSkills = searchTerm.trim() 
    ? searchSkills(allSkills, searchTerm, 10)
    : [];

  useEffect(() => {
    if (isOpen) {
      // Reset form when modal opens
      setSearchTerm('');
      setSelectedSkill(null);
      setProficiencyLevel(1);
      setIsOffering(defaultIsOffering !== undefined ? defaultIsOffering : true);
      setDescription('');
      setShowDropdown(false);
      
      // Focus search input
      setTimeout(() => {
        searchInputRef.current?.focus();
      }, 100);
    }
  }, [isOpen, defaultIsOffering]);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setShowDropdown(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleSkillSelect = (skill: Skill) => {
    setSelectedSkill(skill);
    setSearchTerm(skill.name);
    setShowDropdown(false);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!selectedSkill) {
      return;
    }

    const skillData = {
      skillId: selectedSkill.id,
      proficiencyLevel,
      isOffering,
      description: description.trim(),
      skill: selectedSkill
    };

    onAdd(skillData);
  };

  const renderStars = (level: number, onChange: (level: number) => void) => {
    const [hoveredStar, setHoveredStar] = useState<number | null>(null);
    
    return (
      <div className="flex items-center space-x-1">
        {[1, 2, 3, 4, 5].map((star) => {
          const isActive = star <= level;
          const isHovered = hoveredStar !== null && star <= hoveredStar;
          const shouldHighlight = isActive || isHovered;
          
          return (
            <button
              key={star}
              type="button"
              onClick={() => onChange(star)}
              onMouseEnter={() => setHoveredStar(star)}
              onMouseLeave={() => setHoveredStar(null)}
              className={`w-6 h-6 transition-colors duration-150 ${
                shouldHighlight ? 'text-yellow-400' : 'text-gray-300'
              } hover:scale-110`}
            >
              <svg fill="currentColor" viewBox="0 0 20 20">
                <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
              </svg>
            </button>
          );
        })}
        <span className="text-sm text-gray-600 ml-2">
          {hoveredStar ? `${hoveredStar}/5 - ${['Beginner', 'Novice', 'Intermediate', 'Advanced', 'Expert'][hoveredStar - 1]}` : `${level}/5 - ${['Beginner', 'Novice', 'Intermediate', 'Advanced', 'Expert'][level - 1]}`}
        </span>
      </div>
    );
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
      <div className="relative top-20 mx-auto p-5 border w-full max-w-md shadow-lg rounded-md bg-white">
        <div className="mt-3">
          {/* Header */}
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-medium text-gray-900">
              {defaultIsOffering === true ? 'Add Skill to Offer' : 
               defaultIsOffering === false ? 'Add Skill to Learn' : 
               'Add New Skill'}
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

          {/* Context hint */}
          {defaultIsOffering !== undefined && (
            <div className="mb-4 p-3 bg-blue-50 rounded-md">
              <p className="text-sm text-blue-700 flex items-center">
                {defaultIsOffering 
                  ? (<><GraduationCap className="w-4 h-4 mr-1" /> Adding a skill you can teach to others</>) 
                  : (<><BookOpen className="w-4 h-4 mr-1" /> Adding a skill you want to learn from others</>)}
              </p>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Skill Search */}
            <div className="relative" ref={dropdownRef}>
              <label htmlFor="skill-search" className="block text-sm font-medium text-gray-700 mb-1">
                Search for a skill
              </label>
              <input
                ref={searchInputRef}
                type="text"
                id="skill-search"
                value={searchTerm}
                onChange={(e) => {
                  setSearchTerm(e.target.value);
                  setSelectedSkill(null);
                  setShowDropdown(true);
                }}
                onFocus={() => setShowDropdown(true)}
                placeholder="Type to search skills..."
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                required
              />
              
              {/* Dropdown */}
              {showDropdown && searchTerm && (
                <div className="absolute z-10 mt-1 w-full bg-white shadow-lg max-h-60 rounded-md py-1 text-base ring-1 ring-black ring-opacity-5 overflow-auto">
                  {isLoadingSkills ? (
                    <div className="px-4 py-2 text-sm text-gray-500">Loading skills...</div>
                  ) : filteredSkills.length > 0 ? (
                    filteredSkills.map((skill) => (
                      <button
                        key={skill.id}
                        type="button"
                        onClick={() => handleSkillSelect(skill)}
                        className="w-full text-left px-4 py-2 text-sm text-gray-900 hover:bg-blue-50 focus:outline-none focus:bg-blue-50"
                      >
                        <div 
                          className="font-medium"
                          dangerouslySetInnerHTML={{ 
                            __html: highlightSearchTerm(skill.name, searchTerm) 
                          }}
                        />
                        <div 
                          className="text-xs text-gray-500"
                          dangerouslySetInnerHTML={{ 
                            __html: highlightSearchTerm(skill.category, searchTerm) 
                          }}
                        />
                      </button>
                    ))
                  ) : (
                    <div className="px-4 py-2 text-sm text-gray-500">No skills found</div>
                  )}
                </div>
              )}
            </div>

            {/* Selected Skill Info */}
            {selectedSkill && (
              <div className="p-3 bg-blue-50 rounded-md">
                <h4 className="font-medium text-blue-900">{selectedSkill.name}</h4>
                <p className="text-sm text-blue-700">{selectedSkill.description}</p>
                <span className="inline-block mt-1 px-2 py-1 text-xs font-medium bg-blue-100 text-blue-800 rounded-full">
                  {selectedSkill.category}
                </span>
              </div>
            )}

            {/* Proficiency Level */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Your proficiency level
              </label>
              {renderStars(proficiencyLevel, setProficiencyLevel)}
            </div>

            {/* Offering or Learning */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                What would you like to do?
              </label>
              <div className="space-y-2">
                <label className="flex items-center">
                  <input
                    type="radio"
                    value="offering"
                    checked={isOffering}
                    onChange={() => setIsOffering(true)}
                    className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300"
                  />
                  <span className="ml-2 text-sm text-gray-700 flex items-center"><GraduationCap className="w-4 h-4 mr-1" /> I can teach this skill</span>
                </label>
                <label className="flex items-center">
                  <input
                    type="radio"
                    value="learning"
                    checked={!isOffering}
                    onChange={() => setIsOffering(false)}
                    className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300"
                  />
                  <span className="ml-2 text-sm text-gray-700 flex items-center"><BookOpen className="w-4 h-4 mr-1" /> I want to learn this skill</span>
                </label>
              </div>
            </div>

            {/* Description */}
            <div>
              <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
                Additional notes (optional)
              </label>
              <textarea
                id="description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                placeholder="Describe your experience, specific areas of interest, or what you'd like to learn..."
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 text-sm"
              />
            </div>

            {/* Actions */}
            <div className="flex justify-end space-x-3 pt-4">
              <button
                type="button"
                onClick={onClose}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-gray-500"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={!selectedSkill || isLoading}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isLoading ? 'Adding...' : 'Add Skill'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}