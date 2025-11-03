"use client";

import React, { useState, useRef, useEffect } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import {
  ImagePlus,
  Music,
  Play,
  Square,
  Download,
  Trash2,
  Edit2,
  Loader,
} from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import Progress from "antd/es/progress/progress";
import { useSearchParams } from "next/navigation";
import { marked } from "marked";
import DOMPurify from "dompurify";

const parseSubtitleContent = (content) => {
  if (!content) return [];

  // Split by double newlines to separate scenes
  const scenes = content.split("\n\n").filter(Boolean);

  return scenes.map((scene) => {
    // Extract title and description
    const lines = scene.split("\n");
    const title = lines[0].replace(/\*\*/g, "").trim();
    const description = lines
      .slice(1)
      .join("\n")
      .replace(/\*\*/g, "")
      .replace(/\[|\]/g, "")
      .replace(/\"/g, "")
      .trim();

    return { title, description };
  });
};

export default function SlideshowCreator() {
  const [images, setImages] = useState([]);
  const [parsedScenes, setParsedScenes] = useState([]);
  const [currentSceneIndex, setCurrentSceneIndex] = useState(0);
  const [audioFile, setAudioFile] = useState(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [progress, setProgress] = useState(0);
  const [isGeneratingAudio, setIsGeneratingAudio] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const audioRef = useRef(null);
  const [status, setStatus] = useState("");
  const [tripPlanId, setTripPlanId] = useState(undefined);
  const [query, setQuery] = useState("");
  const [subtitle, setSubtitle] = useState("");

  const searchParams = useSearchParams();

  useEffect(() => {
    const tripPlanIdFromQuery = searchParams.get("tripPlanId");
    if (tripPlanIdFromQuery) {
      setTripPlanId(tripPlanIdFromQuery);
    }
  }, [searchParams]);

  const getVlogDataFromQuery = async () => {
    try {
      setIsLoading(true);
      setStatus("Fetching vlog data...");
      const response = await fetch("/api/vlog", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          query,
          tripPlanId,
        }),
      });

      if (!response.ok) throw new Error("Failed to get response");

      const data = await response.json();
      setImages(data.images);
      setSubtitle(data.content);
      setParsedScenes(parseSubtitleContent(data.content));
      setStatus("");
    } catch (error) {
      setStatus("Error fetching vlog data");
      console.error(error);
    } finally {
      setIsLoading(false);
    }
  };

  // Function to convert Markdown to plain text
  const markdownToText = (markdown) => {
    const html = marked(markdown);
    const cleanHtml = DOMPurify.sanitize(html);
    const tempDiv = document.createElement("div");
    tempDiv.innerHTML = cleanHtml;
    return tempDiv.textContent || tempDiv.innerText || "";
  };

  const generateAudio = async () => {
    try {
      setIsGeneratingAudio(true);
      setStatus("Generating audio...");

      const response = await fetch(
        "https://api.elevenlabs.io/v1/text-to-speech/21m00Tcm4TlvDq8ikWAM",
        {
          method: "POST",
          headers: {
            Accept: "audio/mpeg",
            "Content-Type": "application/json",
            "xi-api-key": process.env.ELEVEN_LABS_API_KEY,
          },
          body: JSON.stringify({
            text: markdownToText(subtitle),
            model_id: "eleven_monolingual_v1",
            voice_settings: {
              stability: 0.5,
              similarity_boost: 0.5,
            },
          }),
        },
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(
          `Failed to generate audio: ${
            errorData.message || response.statusText
          }`,
        );
      }

      const audioBlob = await response.blob();
      const audioUrl = URL.createObjectURL(audioBlob);
      setAudioFile(audioUrl);
      setStatus("");

      // Clean up the object URL when it's no longer needed
      return () => {
        URL.revokeObjectURL(audioUrl);
      };
    } catch (error) {
      setStatus(`Error generating audio: ${error.message}`);
      console.error("Text-to-speech error:", error);
    } finally {
      setIsGeneratingAudio(false);
    }
  };

  const startSlideshow = () => {
    setIsPlaying(true);
    audioRef.current?.play();
    setCurrentSceneIndex(0);
  };

  const stopSlideshow = () => {
    setIsPlaying(false);
    audioRef.current?.pause();
    audioRef.current.currentTime = 0;
    setCurrentSceneIndex(0);
  };

  useEffect(() => {
    if (isPlaying && images.length > 0) {
      const interval = setInterval(() => {
        setCurrentSceneIndex((prevIndex) => {
          if (prevIndex === parsedScenes.length - 1) {
            setIsPlaying(false);
            return 0;
          }
          return prevIndex + 1;
        });
      }, 3000);

      return () => clearInterval(interval);
    }
  }, [isPlaying, images.length, parsedScenes.length]);

  return (
    <div className="h-screen w-full overflow-hidden flex flex-col">
      <div className="flex-1 overflow-y-auto p-4">
        <div className="container mx-auto max-w-7xl">
          <Card className="h-full">
            <CardHeader className=" top-0 bg-white z-10 border-b">
              <CardTitle className="text-2xl flex items-center gap-2">
                <ImagePlus className="w-6 h-6" />
                Video Vlog Creator
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-6 p-6">
              {/* Query Section */}
              <div className="space-y-4">
                <div className="flex flex-col gap-2">
                  <Label>Query</Label>
                  <Input
                    type="text"
                    placeholder="Enter a prompt on what you want to create a vlog about..."
                    value={query}
                    onChange={(e) => setQuery(e.target.value)}
                  />
                  <Button
                    onClick={getVlogDataFromQuery}
                    disabled={isLoading}
                    className="flex items-center gap-2"
                  >
                    {isLoading ? (
                      <Loader className="w-4 h-4 animate-spin" />
                    ) : (
                      <ImagePlus className="w-4 h-4" />
                    )}
                    Get Vlog Data
                  </Button>
                </div>

                {/* Preview Section */}
                <div className="space-y-4">
                  <div className="relative aspect-video bg-slate-950 rounded-lg overflow-hidden">
                    {images.length > 0 ? (
                      <div className="relative h-full">
                        <img
                          src={images[currentSceneIndex % images.length]}
                          alt="Preview"
                          className="w-full h-full object-contain transition-opacity duration-500"
                        />
                        <div className="absolute bottom-0 left-0 right-0 bg-black/70 p-4">
                          {parsedScenes[currentSceneIndex] && (
                            <div className="space-y-2 animate-fade-in">
                              <h3 className="text-white font-bold">
                                {parsedScenes[currentSceneIndex].title}
                              </h3>
                              <p className="text-white">
                                {parsedScenes[currentSceneIndex].description}
                              </p>
                            </div>
                          )}
                        </div>
                      </div>
                    ) : (
                      <div className="flex items-center justify-center h-full text-slate-500">
                        <p>Generated Video will preview here</p>
                      </div>
                    )}
                  </div>

                  <div className="flex flex-wrap justify-center gap-4">
                    <Button
                      onClick={startSlideshow}
                      disabled={!images.length || !audioFile}
                      className="flex items-center gap-2"
                    >
                      <Play className="w-4 h-4" /> Start Preview
                    </Button>
                    <Button
                      onClick={stopSlideshow}
                      disabled={!isPlaying}
                      className="flex items-center gap-2"
                    >
                      <Square className="w-4 h-4" /> Stop Preview
                    </Button>
                    <Button
                      onClick={generateAudio}
                      disabled={isGeneratingAudio || !subtitle}
                      className="flex items-center gap-2"
                    >
                      {isGeneratingAudio ? (
                        <Loader className="w-4 h-4 animate-spin" />
                      ) : (
                        <Music className="w-4 h-4" />
                      )}
                      Generate Audio
                    </Button>
                  </div>

                  {audioFile && (
                    <audio
                      ref={audioRef}
                      src={audioFile}
                      className="w-full"
                      controls
                    />
                  )}
                </div>
              </div>

              <Separator />

              {/* Image Grid */}
              <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
                {images.map((image, index) => (
                  <Card key={index} className="overflow-hidden">
                    <CardContent className="p-3">
                      <div className="relative aspect-square">
                        <img
                          src={image}
                          alt={`Scene ${index + 1}`}
                          className="w-full h-full object-cover rounded-md"
                        />
                      </div>
                      {parsedScenes[index] && (
                        <p className="mt-2 text-sm truncate">
                          {parsedScenes[index].title}
                        </p>
                      )}
                    </CardContent>
                  </Card>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Status Bar */}
      {status && (
        <div className="border-t bg-white p-4">
          <Alert>
            <AlertTitle>Status</AlertTitle>
            <AlertDescription className="flex items-center gap-4">
              {status}
              <Progress value={progress} className="w-[60%]" />
            </AlertDescription>
          </Alert>
        </div>
      )}
    </div>
  );
}
