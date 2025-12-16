import { useState, useMemo } from 'react';
import { Skill, UserSkill } from '../types';
import { useGetSkillsQuery, useGetUserSkillsQuery } from '../store/api/apiSlice';
import { searchSkills } from '../utils/searchUtils';

export interface UseSkillSearchOptions {
  /** Filter out skills based on context (teaching vs learning) */
  filterContext?: 'teaching' | 'learning';
  /** Maximum number of results to return */
  maxResults?: number;
}

export function useSkillSearch(options: UseSkillSearchOptions = {}) {
  const { filterContext, maxResults = 10 } = options;
  const [searchTerm, setSearchTerm] = useState('');
  
  // Get all skills and user skills
  const { data: allSkills = [], isLoading: isLoadingSkills } = useGetSkillsQuery();
  const { data: userSkills = [] } = useGetUserSkillsQuery();
  
  // Filter available skills based on context
  const availableSkills = useMemo(() => {
    if (!filterContext) return allSkills;
    
    return allSkills.filter(skill => {
      // Check if user already has this skill in the specified context
      const existingUserSkill = userSkills.find(us => us.skillId === skill.id);
      
      if (!existingUserSkill) return true; // Skill not added yet, show it
      
      if (filterContext === 'teaching') {
        // For teaching context, hide skills already being taught
        return !existingUserSkill.isOffering;
      } else {
        // For learning context, hide skills already being learned
        return existingUserSkill.isOffering;
      }
    });
  }, [allSkills, userSkills, filterContext]);
  
  // Perform search on available skills
  const searchResults = useMemo(() => {
    if (!searchTerm.trim()) return [];
    return searchSkills(availableSkills, searchTerm, maxResults);
  }, [availableSkills, searchTerm, maxResults]);
  
  // Get user's existing skills for context awareness
  const userSkillsMap = useMemo(() => {
    const map = new Map<number, UserSkill>();
    userSkills.forEach(us => {
      map.set(us.skillId, us);
    });
    return map;
  }, [userSkills]);
  
  // Enhanced search results with context information
  const enhancedResults = useMemo(() => {
    return searchResults.map(skill => {
      const existingUserSkill = userSkillsMap.get(skill.id);
      
      return {
        skill,
        isAlreadyAdded: !!existingUserSkill,
        isTeaching: existingUserSkill?.isOffering || false,
        isLearning: existingUserSkill ? !existingUserSkill.isOffering : false,
        contextMessage: getContextMessage(skill, existingUserSkill, filterContext)
      };
    });
  }, [searchResults, userSkillsMap, filterContext]);
  
  return {
    searchTerm,
    setSearchTerm,
    searchResults: enhancedResults,
    isLoading: isLoadingSkills,
    availableSkillsCount: availableSkills.length,
    totalSkillsCount: allSkills.length
  };
}

function getContextMessage(
  _skill: Skill,
  existingUserSkill: UserSkill | undefined,
  context?: 'teaching' | 'learning'
): string | null {
  if (!existingUserSkill || !context) return null;
  
  if (context === 'teaching' && !existingUserSkill.isOffering) {
    return `Currently learning - upgrade to teaching`;
  }
  
  if (context === 'learning' && existingUserSkill.isOffering) {
    return `Currently teaching - also learn advanced aspects`;
  }
  
  return null;
}