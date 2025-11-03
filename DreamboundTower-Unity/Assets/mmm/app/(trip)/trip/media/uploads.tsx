'use client';

import React, { useState, useCallback } from 'react';
import { Upload, X, Image as ImageIcon } from 'lucide-react';
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button"; 
import { ref, uploadBytesResumable, getDownloadURL } from 'firebase/storage';
 
import exifr from 'exifr';

import axios from 'axios';
import { storage } from './firebase';

 

const Progress = ({ 
  value = 0, 
  className = '', 
  showLabel = true,
  size = 'default',
  variant = 'default',
  animated = true,
}) => {
  // Ensure value is between 0 and 100
  const clampedValue = Math.min(Math.max(value, 0), 100);
  
  // Size variants
  const sizeStyles = {
    sm: 'h-1',
    default: 'h-2',
    lg: 'h-3',
    xl: 'h-4'
  };

  // Color variants
  const variantStyles = {
    default: 'bg-blue-500',
    primary: 'bg-blue-600',
    secondary: 'bg-purple-500',
    success: 'bg-green-500',
    warning: 'bg-yellow-500',
    danger: 'bg-red-500',
  };

  // Generate styles for the indicator
  const indicatorStyles = `
    h-full 
    rounded-full 
    ${variantStyles[variant]} 
    ${animated ? 'transition-all duration-500 ease-out' : ''}
    ${animated && value > 0 ? 'animate-pulse' : ''}
  `;

  return (
    <div className="w-full space-y-1">
      {showLabel && (
        <div className="flex justify-between text-sm">
          <span className="text-gray-500 font-medium">Progress</span>
          <span className="text-gray-700 font-semibold">{Math.round(clampedValue)}%</span>
        </div>
      )}
      <div 
        className={`
          w-full 
          rounded-full 
          bg-gray-100 
          overflow-hidden 
          ${sizeStyles[size]} 
          ${className}
        `}
        role="progressbar"
        aria-valuenow={clampedValue}
        aria-valuemin={0}
        aria-valuemax={100}
      >
        <div 
          className={indicatorStyles}
          style={{ 
            width: `${clampedValue}%`,
            transform: animated ? 'translateZ(0)' : undefined 
          }}
        >
          {/* Optional shine effect for indeterminate state */}
          {animated && (
            <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/10 to-transparent" />
          )}
        </div>
      </div>
    </div>
  );
};


const FileUploader = ({ email,name, tripPlanId }) => {
  const [isDragging, setIsDragging] = useState(false);
  const [files, setFiles] = useState([]);
  const [progressList, setProgressList] = useState([]);
  const [uploadedUrls, setUploadedUrls] = useState([]);
  const [isUploading, setIsUploading] = useState(false);

  const handleDragOver = useCallback((e) => {
    e.preventDefault();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback((e) => {
    e.preventDefault();
    setIsDragging(false);
  }, []);

  const handleDrop = useCallback((e) => {
    e.preventDefault();
    setIsDragging(false);
    const droppedFiles = Array.from(e.dataTransfer.files);
    handleFiles(droppedFiles);
  }, []);

  const handleFileChange = useCallback((e) => {
    const selectedFiles = Array.from(e.target.files);
    handleFiles(selectedFiles);
  }, []);

  const handleFiles = (newFiles) => {
    const imageFiles = newFiles.filter(file => file.type.startsWith('image/'));
    setFiles(prev => [...prev, ...imageFiles]);
    setProgressList(prev => [...prev, ...Array(imageFiles.length).fill(0)]);
  };

  async function extractImageMetadata(file) {
    try {
      const metadata = await exifr.parse(file);
      console.log(metadata)
      if (!metadata) {
        return null;
      }
  
      // Extract relevant data
      const deviceModel = metadata.Make + metadata.Model || 'Unknown device';
      const place = getLocationName(metadata.latitude, metadata.longitude); 
      const timestamp = metadata.DateTimeOriginal
        ? new Date(metadata.DateTimeOriginal).toLocaleString('en-US', {
            weekday: 'long',
            day: 'numeric',
            month: 'long',
          })
        : 'Unknown time';
  
      return {
        deviceModel,
        location: place,
        time: timestamp,
      };
    } catch (error) {
      console.error('Error reading EXIF data', error);
      return null;
    }
  }
  
  const getLocationName = async (latitude, longitude) => {
    // 
    try {
      const response = await fetch(
        `https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=${latitude}&lon=${longitude}`
      );
  
      if (!response.ok) {
        throw new Error(`HTTP error! Status: ${response.status}`);
      }
  
      const data = await response.json();
      return data.address && data.address.city ? data.address.city : 'Unknown';
    } catch (error) {
      console.error('Error fetching location:', error);
      return 'Unknown';
    }
  };
  

  const uploadFile = async (file, index) => {
    console.log("Uploading...",  index)
    const storageRef = ref(storage, `${tripPlanId}/${file.name}`);
    console.log(storageRef)
    const uploadTask = uploadBytesResumable(storageRef, file);

    try {
       const metadata = await extractImageMetadata(file);

       console.log(metadata)

      console.log("Uploading...")

      return new Promise((resolve, reject) => {
        uploadTask.on(
          'state_changed',
          (snapshot) => {
            const progress = (snapshot.bytesTransferred / snapshot.totalBytes) * 100;
            setProgressList(prev => {
              console.log(prev)
              const newList = [...prev];
              newList[index] = progress;
              return newList;
            });
          },
          (error) => {
            console.error('Upload failed:', error);
            reject(error);
          },
          async () => {
            const downloadURL = await getDownloadURL(uploadTask.snapshot.ref);
            await postImageMetadata(file.name, downloadURL, metadata);
            resolve(downloadURL);
          }
        );
      });
    } catch (error) {
      console.error('Error in upload process:', error);
      throw error;
    }
  };

  const postImageMetadata = async (fileName, url, metadata) => {
    console.log("Posting image metadata...")
    console.log({
      id: fileName,
      email,
      tripPlanId,
      name,
      url,
      device: metadata?.deviceModel || 'Unknown',
      location: metadata?.location || 'Unknown',
      time : metadata?.time || 'Unknown',
    })
    try {
      const er = await axios.post('/api/image', {
        id: fileName,
        email,
        tripPlanId,
        name,
        url,
        device: metadata?.deviceModel || 'Unknown',
        location: metadata?.location || 'Unknown',
        time : metadata?.time || 'Unknown',
      });
      console.log(er)
    } catch (error) {
      console.error('Error saving image metadata:', error);
    }
  };

  const handleUpload = async () => {
    setIsUploading(true);
    try {
      const uploadPromises = files.map((file, index) => uploadFile(file, index));
      const urls = await Promise.all(uploadPromises);
      setUploadedUrls(prev => [...prev, ...urls]);
    } catch (error) {
      console.error('Upload failed:', error);
    } finally { 
      setIsUploading(false);
    }
  };

  const removeFile = (index) => {
    setFiles(prev => prev.filter((_, i) => i !== index));
    setProgressList(prev => prev.filter((_, i) => i !== index));
  };

  return (
    <div className="w-full max-w-2xl mx-auto space-y-4">
      <Card className='p-3'>
        <CardContent>
          <div
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
            className={`
              relative h-64 border-2 border-dashed rounded-lg
              ${isDragging ? 'border-blue-500 bg-blue-50' : 'border-gray-300'}
              transition-colors duration-200 ease-in-out
            `}
          >
            <input
              type="file"
              multiple
              accept="image/*"
              onChange={handleFileChange}
              className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
            />
            <div className="flex flex-col items-center justify-center h-full space-y-4">
              <Upload className="w-12 h-12 text-gray-400" />
              <div className="text-center">
                <p className="text-lg font-medium">Drag and drop your images here</p>
                <p className="text-sm text-gray-500">or click to browse</p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {files.length > 0 && (
        <Card className='p-3'>
          <CardContent className="space-y-4">
            <div className="grid gap-4">
              {files.map((file, index) => (
                <div key={index} className="flex items-center space-x-4">
                  <div className="flex items-center justify-center w-12 h-12 bg-gray-100 rounded">
                    <ImageIcon className="w-6 h-6 text-gray-500" />
                  </div>
                  <div className="flex-1">
                    <p className="text-sm font-medium truncate">{file.name}</p>
                    <Progress value={progressList[index]} className="h-2 mt-2" />
                  </div>
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => removeFile(index)}
                    className="text-gray-500 hover:text-red-500"
                    disabled={isUploading}
                  >
                    <X className="w-4 h-4" />
                  </Button>
                </div>
              ))}
            </div>
            <Button 
              onClick={handleUpload} 
              className="w-full"
              disabled={isUploading || files.length === 0}
            >
              {isUploading ? 'Uploading...' : `Upload ${files.length} file${files.length !== 1 ? 's' : ''}`}
            </Button>
          </CardContent>
        </Card>
      )}

      {uploadedUrls.length > 0 && (
        <Card className='p-3'>
          <CardContent>
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">
              {uploadedUrls.map((url, index) => (
                <div key={index} className="relative aspect-square">
                  <img
                    src={url}
                    alt={`Uploaded ${index + 1}`}
                    className="object-cover w-full h-full rounded-lg"
                  />
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

export default FileUploader;