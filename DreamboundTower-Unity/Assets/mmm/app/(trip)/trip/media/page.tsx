/* eslint-disable react-hooks/rules-of-hooks */
"use client";

import React, { useState, useEffect } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import MultipleImageUpload from "./uploads";
import { useSession } from "next-auth/react";
import {
  Breadcrumb,
  Button,
  Input,
  Spin,
  Checkbox,
  message,
  Modal,
  Row,
  Col,
  Image,
  Empty,
  Card,
} from "antd";
import { UploadCloud, Search, DownloadCloud } from "lucide-react";
import axios from "axios";

const breadcrumbItems = [{ title: "Images", link: "/trip/media" }];

export default function Page() {
  const router = useRouter();
  const [isOpen, setIsOpen] = useState(false);
  const [userId, setUserId] = useState(undefined);
  const [tripPlanId, setTripPlanId] = useState(undefined);
  const { data: session } = useSession();
  const [photos, setPhotos] = useState([]);
  const [loading, setLoading] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedImages, setSelectedImages] = useState([]);
 
  const searchParams = useSearchParams(); // Use useSearchParams hook

  useEffect(() => {
    const tripPlanIdFromQuery = searchParams.get("tripPlanId"); // Get tripPlanId from query string
    if (tripPlanIdFromQuery) {
      setTripPlanId(tripPlanIdFromQuery);
    }
  }, [searchParams]);

  // Fetch images based on tripPlanId and search query
  const fetchImages = async () => {
    setLoading(true);
    try {
      const response = await axios.get(
        `/api/image?tripPlanId=${tripPlanId}&similarSearchQuery=${searchQuery}`,
      );
      setPhotos(response.data.filteredTrips);
      console.log(response.data.filteredTrips);
    } catch (error) {
      message.error("Failed to fetch images. Try again.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (tripPlanId) fetchImages();
  }, [tripPlanId]);

  const handleImageSelect = (image) => {
    setSelectedImages((prev) =>
      prev.includes(image)
        ? prev.filter((img) => img !== image)
        : [...prev, image],
    );
  };

  const downloadSelectedImages = () => {
    if (selectedImages.length === 0) {
      alert("No images selected for download.");
      return;
    }

    selectedImages.forEach((image) => {
      const link = document.createElement("a");
      link.href = image.url;
      link.download = image.name;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    });
  };

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
        <button
          className="flex items-center px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 transition"
          onClick={() => setIsOpen(true)}
        >
          <UploadCloud className="mr-2" /> Upload Image
        </button>
      </div>

      <div className="flex space-x-4 mb-4">
        <input
          type="text"
          placeholder="Type a natural language search..."
          className="w-full p-2 border rounded"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
        />
        <button
          className="flex items-center px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600 transition"
          onClick={fetchImages}
        >
          <Search className="mr-2" /> Search
        </button>
      </div>

      {loading ? (
        <div className="flex justify-center items-center py-16">
          <div className="text-center">
            <Spin tip="Loading images..." size="large" />
          </div>
        </div>
      ) : photos.length === 0 ? (
        <div className="flex justify-center items-center py-16">
          <Empty description="No images found" />
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
          {photos.map((photo, index) => (
            <div key={index} className="relative group">
              <img
                src={photo.url || "/placeholder.jpg"}
                alt={photo.name}
                className="w-full h-48 object-cover rounded-lg"
              />
              <input
                type="checkbox"
                checked={selectedImages.includes(photo)}
                onChange={() => handleImageSelect(photo)}
                className="absolute top-2 right-2 p-1 bg-white bg-opacity-75 rounded"
              />
              <div className="absolute bottom-0 left-0 w-full bg-black bg-opacity-50 text-white text-center p-2 opacity-0 group-hover:opacity-100 transition">
                {photo.feature || "N/A"}
              </div>
              <div className="mt-2">
                <h3 className="text-lg font-semibold">
                  {photo.feature.slice(0, 20) || "N/A"}
                </h3>
                <p className="text-sm text-gray-500">
                  Uploaded by: {photo.name}
                </p>
              </div>
            </div>
          ))}
        </div>
      )}

      {photos.length > 0 && (
        <div className="text-right mt-4">
          <button
            className="flex items-center px-4 py-2 bg-indigo-500 text-white rounded hover:bg-indigo-600 transition disabled:opacity-50"
            onClick={downloadSelectedImages}
            disabled={selectedImages.length === 0}
          >
            <DownloadCloud className="mr-2" /> Download Selected
          </button>
        </div>
      )}

      <Modal
        title="Upload Image"
        visible={isOpen}
        onCancel={() => setIsOpen(false)}
        footer={null}
      >
        <MultipleImageUpload
          email={session?.user?.email}
          name={session?.user?.name}
          tripPlanId={tripPlanId}
        />
      </Modal>
    </div>
  );
}
