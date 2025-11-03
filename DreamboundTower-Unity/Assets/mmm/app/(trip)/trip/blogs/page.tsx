/* eslint-disable react-hooks/rules-of-hooks */
"use client";

import React, { useState, useEffect } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useSession } from "next-auth/react";
import {
  UploadCloud,
  Search,
  Loader2,
  Heart,
  MessageSquare,
  PenLine,
} from "lucide-react";
import axios from "axios";
import { useToast } from "@/components/ui/use-toast";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import BlogGenerator from "./generate";
import { ModalFullScreen } from "@/components/ui/modal-full";

const breadcrumbItems = [{ title: "Blog", link: "/trip/blog" }];

export default function Page() {
  const router = useRouter();
  const [isOpen, setIsOpen] = useState(false);
  const [tripPlanId, setTripPlanId] = useState(undefined);
  const { data: session } = useSession();
  const [blogs, setBlogs] = useState([]);
  const [loading, setLoading] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedBlog, setSelectedBlog] = useState(null);
  const { toast } = useToast();

  const searchParams = useSearchParams(); // Use useSearchParams hook

  useEffect(() => {
    const tripPlanIdFromQuery = searchParams.get("tripPlanId"); // Get tripPlanId from query string
    if (tripPlanIdFromQuery) {
      setTripPlanId(tripPlanIdFromQuery);
    }
  }, [searchParams]);

  // Fetch blogs based on tripPlanId and search query
  const fetchBlogs = async () => {
    setLoading(true);
    try {
      const response = await axios.get(
        `/api/blog?tripPlanId=${tripPlanId}&query=${searchQuery}`,
      );
      setBlogs(response.data.blogs);
      console.log(response.data.blogs);
    } catch (error) {
      toast({
        variant: "destructive",
        title: "Error",
        description: "Failed to fetch blogs. Try again.",
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (tripPlanId) fetchBlogs();
  }, [tripPlanId]);

  return (
    <div className="min-h-screen p-4 container mx-auto">
      <div className="flex justify-between items-center mb-4">
        <div>
          <nav className="text-sm breadcrumbs">
            <ul className="flex space-x-2">
              {breadcrumbItems.map((item, index) => (
                <li key={index}>
                  <a href={item.link} className="text-blue-500 hover:underline">
                    {item.title}
                  </a>
                </li>
              ))}
            </ul>
          </nav>
        </div>
      </div>

      <div className="flex space-x-4 mb-4 justify-between">
        <div className="flex space-x-4 ">
          <Input
            type="text"
            placeholder="Type a natural language search..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
          <Button variant='secondary' onClick={fetchBlogs}>
            <Search className="mr-2" /> Search Blogs
          </Button>
        </div>

        <Button onClick={() => setIsOpen(true)}>
          <PenLine className="mr-2" /> Generate New Blog
        </Button>
      </div>

      {loading ? (
        <div className="flex justify-center items-center py-16">
          <div className="text-center flex flex-col items-center">
            <Loader2 className="w-8 h-8 animate-spin" />
            <p>Loading blogs...</p>
          </div>
        </div>
      ) : blogs.length === 0 ? (
        <div className="flex justify-center items-center py-16">
          <p>No blogs found</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
          {blogs.map((blog, index) => (
            <div
              key={index}
              className="bg-white border rounded-lg shadow-md overflow-hidden"
            >
              <div className="p-4">
                {/* Blog Title */}
                <h2 className="text-xl font-semibold mb-2">
                  {blog.title || "Untitled Blog"}
                </h2>
                {/* Small preview of blog content */}
                <p className="text-gray-700 text-sm mb-4">
                  {blog.content.substring(0, 100)}...
                </p>
                {/* Author and date */}
                <div className="flex items-center text-gray-500 text-sm mb-4">
                  <span>By {blog.author.name}</span>
                  <span className="mx-2">•</span>
                  <span>{new Date(blog.createdAt).toLocaleDateString()}</span>
                </div>
                {/* Action buttons */}
                <div className="flex items-center justify-between">
                  <div className="flex space-x-4">
                    <button className="flex items-center text-gray-500 hover:text-red-500">
                      <Heart className="w-5 h-5 mr-1" />
                      <span>Like</span>
                    </button>
                    <button className="flex items-center text-gray-500 hover:text-blue-500">
                      <MessageSquare className="w-5 h-5 mr-1" />
                      <span>Comment</span>
                    </button>
                  </div>
                  {/* Preview Button */}
                  <Button
                    variant="outline"
                    onClick={() => setSelectedBlog(blog)}
                  >
                    Preview
                  </Button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {selectedBlog && (
        <ModalFullScreen
          isOpen={true}
          onClose={() => setSelectedBlog(null)}
          title="Blog Preview"
          description=""
        >
          <div className="space-y-4">
            <ReactMarkdown>{selectedBlog?.content}</ReactMarkdown>
          </div>
        </ModalFullScreen>
      )}

      {/* Dialog for Generate Blog */}
      <ModalFullScreen
        isOpen={isOpen}
        onClose={() => setIsOpen(false)}
        title="Blog Generator"
        description="AI Blog generator will help to make your Blog"
      >
        <BlogGenerator
          email={session?.user?.email}
          name={session?.user?.name}
          tripPlanId={tripPlanId}
        />
      </ModalFullScreen>
    </div>
  );
}
