import { useState, useEffect, useRef } from 'react';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import { useUpdateProfileMutation, useUploadProfileImageMutation, useGetUserSkillsQuery } from '../store/api/apiSlice';
import { updateUserProfile } from '../store/slices/authSlice';
import { useSkillFilters } from '../hooks/useSkillFilters';
import Navigation from '../components/Navigation';
import { useToast } from '../contexts/ToastContext';

export default function Profile() {
  const dispatch = useAppDispatch();
  const { user } = useAppSelector((state) => state.auth);
  const { data: userSkills = [] } = useGetUserSkillsQuery();
  const [updateProfile, { isLoading: isUpdating }] = useUpdateProfileMutation();
  const [uploadProfileImage, { isLoading: isUploading }] = useUploadProfileImageMutation();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { showSuccess, showError } = useToast();

  // Get user skills statistics
  const { offeredSkillsCount } = useSkillFilters(userSkills);

  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState({
    name: '',
    bio: '',
    profileImageUrl: '',
  });
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string>('');

  // Initialize form data when user data loads
  useEffect(() => {
    if (user) {
      setFormData({
        name: user.name || '',
        bio: user.bio || '',
        profileImageUrl: user.profileImageUrl || '',
      });
    }
  }, [user]);

  // Cleanup preview URL on unmount
  useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  // Helper function to get the full image URL
  const getImageUrl = (imageUrl: string | null | undefined) => {
    if (!imageUrl) return '/api/placeholder/80/80';
    if (imageUrl.startsWith('http')) return imageUrl; // External URL
    if (imageUrl.startsWith('/uploads')) {
      // Our uploaded image - construct full URL
      const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000';
      return `${baseUrl}${imageUrl}`;
    }
    return imageUrl;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      // Validate file type
      const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
      if (!allowedTypes.includes(file.type)) {
        showError('Invalid file type', 'Please select a JPG, PNG, or GIF image.');
        return;
      }

      // Validate file size (5MB)
      const maxSize = 5 * 1024 * 1024; // 5MB in bytes
      if (file.size > maxSize) {
        showError('File too large', 'Please select an image smaller than 5MB.');
        return;
      }

      setSelectedFile(file);
      // Create preview URL
      const url = URL.createObjectURL(file);
      setPreviewUrl(url);
    }
  };

  const handleImageUpload = async () => {
    if (!selectedFile) return null;

    try {
      const formData = new FormData();
      formData.append('image', selectedFile);
      
      const result = await uploadProfileImage(formData).unwrap();
      showSuccess('Image uploaded successfully!');
      return result.imageUrl;
    } catch (error: any) {
      console.error('Failed to upload image:', error);
      showError('Upload failed', error?.data?.message || 'Failed to upload image. Please try again.');
      throw error;
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      let updatedFormData = { ...formData };

      // Upload image first if a new file was selected
      if (selectedFile) {
        try {
          const imageUrl = await handleImageUpload();
          if (imageUrl) {
            updatedFormData.profileImageUrl = imageUrl;
          }
        } catch (error) {
          // Image upload failed, don't proceed with profile update
          return;
        }
      }

      // Only send fields that the backend accepts
      const profileUpdateData: any = {
        name: updatedFormData.name,
        bio: updatedFormData.bio,
      };

      // Only include profileImageUrl if it has a valid value
      if (updatedFormData.profileImageUrl && updatedFormData.profileImageUrl.trim() !== '') {
        profileUpdateData.profileImageUrl = updatedFormData.profileImageUrl;
      }

      const updatedUser = await updateProfile(profileUpdateData).unwrap();
      dispatch(updateUserProfile(updatedUser));
      
      // Show success message
      showSuccess('Profile updated successfully!');
      
      // Clean up
      setSelectedFile(null);
      setPreviewUrl('');
      setIsEditing(false);
    } catch (error: any) {
      console.error('Failed to update profile:', error);
      showError('Update failed', error?.data?.message || 'Failed to update profile. Please try again.');
    }
  };

  const handleCancel = () => {
    // Reset form data to original user data
    if (user) {
      setFormData({
        name: user.name || '',
        bio: user.bio || '',
        profileImageUrl: user.profileImageUrl || '',
      });
    }
    // Clean up file selection
    setSelectedFile(null);
    setPreviewUrl('');
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
    setIsEditing(false);
  };

  if (!user) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <h2 className="text-2xl font-bold text-gray-900 mb-4">Loading Profile...</h2>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <Navigation />
      <div className="max-w-3xl mx-auto py-8 px-4 sm:px-6 lg:px-8">
        <div className="bg-white shadow rounded-lg">
          {/* Header */}
          <div className="px-6 py-4 border-b border-gray-200">
            <div className="flex items-center justify-between">
              <h1 className="text-2xl font-bold text-gray-900">Profile</h1>
              {!isEditing && (
                <button
                  onClick={() => setIsEditing(true)}
                  className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  Edit Profile
                </button>
              )}
            </div>
          </div>

          {/* Profile Content */}
          <div className="px-6 py-6">
            {isEditing ? (
              /* Edit Form */
              <form onSubmit={handleSubmit} className="space-y-6">
                <div className="grid grid-cols-1 gap-6">
                  {/* Profile Image */}
                  <div className="flex items-center space-x-6">
                    <div className="flex-shrink-0">
                      <img
                        className="h-20 w-20 rounded-full object-cover"
                        src={previewUrl || getImageUrl(formData.profileImageUrl)}
                        alt="Profile"
                        onError={(e) => {
                          const target = e.target as HTMLImageElement;
                          target.src = '/api/placeholder/80/80';
                        }}
                      />
                    </div>
                    <div className="flex-1">
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Profile Image
                      </label>
                      <div className="space-y-2">
                        <input
                          ref={fileInputRef}
                          type="file"
                          accept="image/*"
                          onChange={handleFileChange}
                          className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-medium file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100"
                        />
                        <p className="text-xs text-gray-500">
                          {selectedFile ? `Selected: ${selectedFile.name}` : 'JPG, JPEG, PNG, or GIF. Max size: 5MB.'}
                        </p>
                      </div>
                    </div>
                  </div>

                  {/* Name */}
                  <div>
                    <label htmlFor="name" className="block text-sm font-medium text-gray-700">
                      Full Name
                    </label>
                    <input
                      type="text"
                      id="name"
                      name="name"
                      value={formData.name}
                      onChange={handleChange}
                      required
                      className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                    />
                  </div>

                  {/* Bio */}
                  <div>
                    <label htmlFor="bio" className="block text-sm font-medium text-gray-700">
                      Bio
                    </label>
                    <textarea
                      id="bio"
                      name="bio"
                      rows={4}
                      value={formData.bio}
                      onChange={handleChange}
                      className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                      placeholder="Tell us about yourself and your skills..."
                    />
                  </div>
                </div>

                {/* Form Actions */}
                <div className="flex justify-end space-x-3">
                  <button
                    type="button"
                    onClick={handleCancel}
                    className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={isUpdating || isUploading}
                    className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {isUploading ? 'Uploading...' : isUpdating ? 'Saving...' : 'Save Changes'}
                  </button>
                </div>
              </form>
            ) : (
              /* Display Mode */
              <div className="space-y-6">
                {/* Profile Image and Basic Info */}
                <div className="flex items-start space-x-6">
                  <div className="flex-shrink-0">
                    <img
                      className="h-24 w-24 rounded-full object-cover"
                      src={getImageUrl(user.profileImageUrl)}
                      alt="Profile"
                      onError={(e) => {
                        const target = e.target as HTMLImageElement;
                        target.src = '/api/placeholder/96/96';
                      }}
                    />
                  </div>
                  <div className="flex-1">
                    <h2 className="text-2xl font-bold text-gray-900">{user.name}</h2>
                    <p className="text-sm text-gray-500">{user.email}</p>
                    <div className="mt-2">
                      <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800">
                        {user.timeCredits} time credits
                      </span>
                    </div>
                  </div>
                </div>

                {/* Bio */}
                <div>
                  <h3 className="text-lg font-medium text-gray-900 mb-2">About</h3>
                  <p className="text-gray-700 whitespace-pre-wrap">
                    {user.bio || 'No bio provided yet. Click "Edit Profile" to add your bio.'}
                  </p>
                </div>

                {/* Profile Stats */}
                <div className="grid grid-cols-3 gap-4 pt-6 border-t border-gray-200">
                  <div className="text-center">
                    <div className="text-2xl font-bold text-gray-900">{offeredSkillsCount}</div>
                    <div className="text-sm text-gray-500">Skills Offered</div>
                  </div>
                  <div className="text-center">
                    <div className="text-2xl font-bold text-gray-900">0</div>
                    <div className="text-sm text-gray-500">Exchanges Completed</div>
                  </div>
                  <div className="text-center">
                    <div className="text-2xl font-bold text-gray-900">0</div>
                    <div className="text-sm text-gray-500">Reviews Received</div>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}