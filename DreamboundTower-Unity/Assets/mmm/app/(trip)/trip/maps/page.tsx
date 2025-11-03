"use client";

import React, { useState, useEffect, useRef } from "react";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import {
  MapPin,
  Clock,
  Info,
  Navigation,
  Loader2,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";

const TripPlannerPage = () => {
  const [tripPlan, setTripPlan] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [currentSlide, setCurrentSlide] = useState(0);
  const [weatherData, setWeatherData] = useState(null);
  const sliderRef = useRef(null);

  useEffect(() => {
    const fetchTripPlan = () => {
      try {
        const storedTripPlan = localStorage.getItem("tripPlanPreview");
        if (storedTripPlan) {
          setTripPlan(JSON.parse(storedTripPlan));
        }
      } catch (error) {
        console.error("Error loading trip plan:", error);
      } finally {
        setIsLoading(false);
      }
    };

    fetchTripPlan();
  }, []);

  // call http://localhost:3000/api/weather?lat=24.6&lon=-9.4&days=5
  useEffect(() => {
    if (tripPlan) {
      const fetchWeatherData = async () => {
        try {
          const queryParams = new URLSearchParams({
            lat: tripPlan.checkpoints[currentSlide].destination.latitude,
            lon: tripPlan.checkpoints[currentSlide].destination.longitude,
            days: 5,
          });
          const response = await fetch(`/api/weather?${queryParams}`);
          const data = await response.json();
          setWeatherData(data);
        } catch (error) {
          console.error("Error fetching weather data:", error);
        }
      };

      fetchWeatherData();
    }
  }, [currentSlide,tripPlan]);

  const scrollToSlide = (index) => {
    if (sliderRef.current) {
      const slider = sliderRef.current;
      const slideWidth = slider.offsetWidth;
      slider.scrollTo({
        left: slideWidth * index,
        behavior: "smooth",
      });
      setCurrentSlide(index);
    }
  };

  const handleScroll = (direction) => {
    const newIndex =
      direction === "next"
        ? Math.min(currentSlide + 1, (tripPlan?.checkpoints.length || 1) - 1)
        : Math.max(currentSlide - 1, 0);
    scrollToSlide(newIndex);
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="flex flex-col items-center gap-4">
          <Loader2 className="h-8 w-8 animate-spin text-blue-500" />
          <p className="text-gray-500 font-medium">Loading trip details...</p>
        </div>
      </div>
    );
  }

  if (!tripPlan) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <p className="text-gray-500 font-medium">No trip plan found</p>
          <p className="text-sm text-gray-400">
            Please create a new trip plan to get started
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-full px-4 py-8">
        <div className="space-y-8">
          <div className="max-w-7xl mx-auto px-4">
            <h1 className="text-4xl font-bold text-gray-900">
              {tripPlan.trip_name || "Your Trip Plan"}
            </h1>
            {weatherData && (
              <>
                <div className="flex space-x-2 mt-4 overflow-x-auto">
                  {weatherData.forecast.map((day, index) => (
                    <div
                      key={index}
                      className={`flex-none bg-white p-2 rounded-lg shadow-md w-48 ${
                        day.condition.toLowerCase() !== "sunny" ? "border border-red-500" : ""
                      }`}
                    >
                      <p className="text-sm font-medium text-gray-700">{day.date}</p>
                      <ul className="list-disc list-inside text-xs text-gray-500">
                        <li>{day.condition}</li>
                      </ul>
                    </div>
                  ))}
                </div>
              </>
            )}
          </div>

          <div className="relative">
            {/* Navigation Buttons */}
            <div className="absolute left-4 top-1/2 -translate-y-1/2 z-10">
              <button
                onClick={() => handleScroll("prev")}
                disabled={currentSlide === 0}
                className="p-2 rounded-full bg-white shadow-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-all"
                aria-label="Previous checkpoint"
              >
                <ChevronLeft className="h-6 w-6" />
              </button>
            </div>

            <div className="absolute right-4 top-1/2 -translate-y-1/2 z-10">
              <button
                onClick={() => handleScroll("next")}
                disabled={currentSlide === tripPlan.checkpoints.length - 1}
                className="p-2 rounded-full bg-white shadow-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-all"
                aria-label="Next checkpoint"
              >
                <ChevronRight className="h-6 w-6" />
              </button>
            </div>

            {/* Slider Container */}
            <div
              ref={sliderRef}
              className="flex overflow-x-hidden snap-x snap-mandatory"
              style={{ scrollbarWidth: "none", msOverflowStyle: "none" }}
            >
              {tripPlan.checkpoints.map((checkpoint, index) => (
                <div key={index} className="flex-none w-full snap-center px-4">
                  <Card className="mx-auto max-w-7xl border-0 shadow-lg">
                    <CardHeader className="bg-white border-b border-gray-100">
                      <CardTitle className="flex items-center gap-3">
                        <div className="flex items-center justify-center w-8 h-8 rounded-full bg-blue-100">
                          <Navigation className="h-5 w-5 text-blue-500" />
                        </div>
                        <span className="text-xl">Checkpoint {index + 1}</span>
                      </CardTitle>
                    </CardHeader>
                    <CardContent className="p-6">
                      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
                        <div className="space-y-6">
                          <div className="rounded-lg bg-white p-4 shadow-sm">
                            <div className="space-y-4">
                              <div className="flex gap-3">
                                <MapPin className="h-5 w-5 text-green-500 flex-shrink-0" />
                                <div>
                                  <p className="font-medium text-gray-900">
                                    From
                                  </p>
                                  <p className="text-gray-700">
                                    {checkpoint.origin.location}
                                  </p>
                                  <p className="text-sm text-gray-500">
                                    ({checkpoint.origin.latitude},{" "}
                                    {checkpoint.origin.longitude})
                                  </p>
                                </div>
                              </div>

                              <div className="flex gap-3">
                                <MapPin className="h-5 w-5 text-red-500 flex-shrink-0" />
                                <div>
                                  <p className="font-medium text-gray-900">
                                    To
                                  </p>
                                  <p className="text-gray-700">
                                    {checkpoint.destination.location}
                                  </p>
                                  <p className="text-sm text-gray-500">
                                    ({checkpoint.destination.latitude},{" "}
                                    {checkpoint.destination.longitude})
                                  </p>
                                </div>
                              </div>

                              <div className="flex gap-3">
                                <Clock className="h-5 w-5 text-blue-500 flex-shrink-0" />
                                <div>
                                  <p className="font-medium text-gray-900">
                                    Schedule
                                  </p>
                                  <p className="text-gray-700">
                                    Departure:{" "}
                                    {checkpoint.logistics.departure_time}
                                  </p>
                                  <p className="text-gray-700">
                                    Arrival: {checkpoint.logistics.arrival_time}
                                  </p>
                                </div>
                              </div>

                              {checkpoint.logistics.tips && (
                                <div className="flex gap-3">
                                  <Info className="h-5 w-5 text-yellow-500 flex-shrink-0" />
                                  <div>
                                    <p className="font-medium text-gray-900">
                                      Travel Tips
                                    </p>
                                    <p className="text-gray-600">
                                      {checkpoint.logistics.tips}
                                    </p>
                                  </div>
                                </div>
                              )}
                            </div>
                          </div>
                        </div>

                        <div className="h-[400px] rounded-lg overflow-hidden shadow-sm">
                          <iframe
                            className="w-full h-full border-0"
                            src={`https://www.google.com/maps/embed/v1/directions?key=AIzaSyBeuC6T-GjA7P-9yE0kkTZIXiRamAbqqY8&origin=${checkpoint.origin.latitude},${checkpoint.origin.longitude}&destination=${checkpoint.destination.latitude},${checkpoint.destination.longitude}&mode=driving`}
                            allowFullScreen
                          ></iframe>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                </div>
              ))}
            </div>

            {/* Progress Indicators */}
            <div className="flex justify-center mt-6 gap-2">
              {tripPlan.checkpoints.map((_, index) => (
                <button
                  key={index}
                  onClick={() => scrollToSlide(index)}
                  className={`w-2 h-2 rounded-full transition-all ${
                    currentSlide === index ? "bg-blue-500 w-4" : "bg-gray-300"
                  }`}
                  aria-label={`Go to checkpoint ${index + 1}`}
                />
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default TripPlannerPage;
