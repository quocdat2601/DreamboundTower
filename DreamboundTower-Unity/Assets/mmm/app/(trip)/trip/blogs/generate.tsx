'use client';

import React, { useState } from 'react';
import { Upload, X, Image as ImageIcon } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button"; 
import { Input } from "@/components/ui/input";
import { ref, uploadBytesResumable, getDownloadURL } from 'firebase/storage';
import exifr from 'exifr';
import axios from 'axios'; 
import MdEditor from 'react-markdown-editor-lite';
import 'react-markdown-editor-lite/lib/index.css';
import ReactMarkdown from 'react-markdown';
import { storage } from '../media/firebase';
import { title } from 'process';

const Progress = ({ 
  value = 0, 
  className = '', 
  showLabel = true,
  size = 'default',
  variant = 'default',
  animated = true,
}) => {
  const clampedValue = Math.min(Math.max(value, 0), 100);
  
  const sizeStyles = {
    sm: 'h-1',
    default: 'h-2',
    lg: 'h-3',
    xl: 'h-4'
  };

  const variantStyles = {
    default: 'bg-blue-500',
    primary: 'bg-blue-600',
    secondary: 'bg-purple-500',
    success: 'bg-green-500',
    warning: 'bg-yellow-500',
    danger: 'bg-red-500',
  };

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
          {animated && (
            <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/10 to-transparent" />
          )}
        </div>
      </div>
    </div>
  );
};

const BlogGenerator = ({ email, name, tripPlanId }) => { 
  const [query, setQuery] = useState('');
  const [isGenerating, setIsGenerating] = useState(false);
  const [generatedContent, setGeneratedContent] = useState('');
  const [generatedTitle, setGeneratedTitle] = useState('');
  const [editedContent, setEditedContent] = useState('');
    const [editedTitle, setEditedTitle] = useState('');
  const [files, setFiles] = useState([]);
  const [progress, setProgress] = useState([]);

  const handleGenerateBlog = async () => {
    console.log("Generating blog from your query");
    try {
      setIsGenerating(true);
      const response = await axios.post('/api/blog', { 
        email,
        tripPlanId,
        query,
      });
      const content = response.data.content;
      setGeneratedContent(content);
      setGeneratedTitle(response.data.title);
      
      setEditedContent(content);
      setEditedTitle(response.data.title);
    } catch (error) {
      console.error('Error generating blog content:', error);
    } finally {
      setIsGenerating(false);
    }
  };

  const handleContentChange = ({ html, text }) => {
    setEditedContent(text);
  };

  const handleFileChange = (e) => {
    const selectedFiles = Array.from(e.target.files);
    setFiles((prevFiles) => [...prevFiles, ...selectedFiles]);
    setProgress((prevProgress) => [...prevProgress, ...selectedFiles.map(() => 0)]);
  };

  const removeFile = (index) => {
    setFiles((prevFiles) => prevFiles.filter((_, i) => i !== index));
    setProgress((prevProgress) => prevProgress.filter((_, i) => i !== index));
  };

  const handlePostBlog = async () => {
    console.log("Posting blog");
    try {
      let downloadURLs = [];
      let metadataList = [];

      if (files.length > 0) {
        setProgress(files.map(() => 0));

        const uploadPromises = files.map((file, index) => {
          return new Promise(async (resolve, reject) => {
            try {
              const metadata = await exifr.parse(file);
              const storageRef = ref(storage, `blog_images/${file.name}_${Date.now()}`);
              const uploadTask = uploadBytesResumable(storageRef, file);

              uploadTask.on('state_changed', 
                (snapshot) => {
                  const progressPercent = (snapshot.bytesTransferred / snapshot.totalBytes) * 100;
                  setProgress((prevProgress) => {
                    const newProgress = [...prevProgress];
                    newProgress[index] = progressPercent;
                    return newProgress;
                  });
                }, 
                (error) => {
                  console.error('Upload error:', error);
                  reject(error);
                }, 
                () => {
                  getDownloadURL(uploadTask.snapshot.ref).then((downloadURL) => {
                    resolve({ downloadURL, metadata });
                  });
                }
              );
            } catch (error) {
              console.error('Error extracting EXIF data:', error);
              reject(error);
            }
          });
        });

        const uploadResults = await Promise.all(uploadPromises);
        downloadURLs = uploadResults.map(result => result.downloadURL);
        metadataList = uploadResults.map(result => result.metadata);
      }

      const response = await axios.post('/api/blog', {
        email,
        tripPlanId,
        content: editedContent,
        query,
        title: editedTitle, 
      });

      console.log('Blog posted successfully:', response.data);
      // Optionally, you can redirect the user or clear the form
    } catch (error) {
      console.error('Error posting blog:', error);
    }
  };

  return (
    <div className="flex flex-col space-y-6 p-6 max-h-[calc(100vh-8rem)] overflow-y-auto">
      {/* Query Section */}
      <Card className="w-full">
        <CardHeader>
          <CardTitle>Generate Your Blog</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col md:flex-row gap-4">
            <Input 
              type="text" 
              placeholder="What would you like to write about?" 
              value={query} 
              onChange={(e) => setQuery(e.target.value)}
              className="flex-1"
            />
            <Button 
              onClick={handleGenerateBlog} 
              disabled={isGenerating || !query}
              className="md:w-auto w-full"
            >
              {isGenerating ? 'Generating...' : 'Generate Blog'}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Generated Content Display */}
      {generatedContent && (
        <Card>
          <CardHeader>
            <CardTitle>Generated Blog Preview</CardTitle>
          </CardHeader>
          <CardContent className="prose max-w-none">
            <h2 className="text-xl font-semibold mb-4">{generatedTitle}</h2>
            <div className="bg-gray-50 p-4 rounded-lg">
              <ReactMarkdown>{generatedContent}</ReactMarkdown>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Editor Section */}
      {generatedContent && (
        <Card>
          <CardHeader>
            <CardTitle>Edit Your Blog</CardTitle>
          </CardHeader>
          <CardContent className="space-y-6">
            <Input
              type="text"
              placeholder="Blog Title"
              value={editedTitle}
              onChange={(e) => setEditedTitle(e.target.value)}
              className="text-lg font-semibold"
            />
            
            <div className="h-[400px] border rounded-lg overflow-hidden">
              <MdEditor
                value={editedContent}
                style={{ height: "100%" }}
                renderHTML={(text) => <ReactMarkdown>{text}</ReactMarkdown>}
                onChange={handleContentChange}
                className="w-full"
              />
            </div>

            {/* Image Upload Section */}
            <div className="space-y-4">
              <div className="flex items-center gap-4">
                <Button
                  variant="outline"
                  onClick={() => document.getElementById('file-upload').click()}
                  className="w-full md:w-auto"
                >
                  <Upload className="w-4 h-4 mr-2" />
                  Add Images
                </Button>
                <Input
                  id="file-upload"
                  type="file"
                  multiple
                  onChange={handleFileChange}
                  className="hidden"
                />
              </div>

              {/* File Preview */}
              {files.length > 0 && (
                <div className="grid gap-4">
                  {files.map((file, index) => (
                    <div key={index} className="flex items-center p-3 bg-gray-50 rounded-lg">
                      <ImageIcon className="w-6 h-6 text-gray-500 mr-3" />
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium truncate">{file.name}</p>
                        <Progress value={progress[index]} size="sm" className="mt-2" />
                      </div>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => removeFile(index)}
                        className="ml-2"
                      >
                        <X className="w-4 h-4" />
                      </Button>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <Button 
              onClick={handlePostBlog} 
              className="w-full md:w-auto"
            >
              Publish Blog
            </Button>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

export default BlogGenerator;