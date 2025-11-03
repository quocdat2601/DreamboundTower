"use client";

import React, { useEffect, useState } from "react";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Avatar } from "@/components/ui/avatar";
import BreadCrumb from "@/components/breadcrumb";
import {
  Plane,
  Send,
  Image as ImageIcon,
  MapPin,
  Hotel,
  Calendar,
  Users,
  Loader2,
  Mountain,
  Menu,
  Mountain as MountainIcon,
  X,
} from "lucide-react";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import ReactMarkdown from "react-markdown";

const breadcrumbItems = [
  { title: "Dashboard", link: "/dashboard" },
  { title: "Travel Assistant", link: "/dashboard/travel-assistant" },
];

const suggestions = [
  {
    text: "What's the best time to visit Bali?",
    icon: <Calendar className="w-4 h-4" />,
  },
  {
    text: "Suggest a 5-day itinerary in Thailand",
    icon: <MapPin className="w-4 h-4" />,
  },
  {
    text: "Luxury hotels in Maldives",
    icon: <Hotel className="w-4 h-4" />,
  },
  {
    text: "Group tour packages for Europe",
    icon: <Users className="w-4 h-4" />,
  },
];

export default function Page() {
  const [messages, setMessages] = useState<
    Array<{
      type: "user" | "assistant";
      content: string;
    }>
  >([]);

  const [tripData, setTripData] = useState(null);

  useEffect(() => {
    const savedTrip = localStorage.getItem("tripPlan");
    if (savedTrip) {
      const parsedData = JSON.parse(savedTrip);
      setTripData(parsedData);
    }
  }, []);

  const [inputMessage, setInputMessage] = useState("Hello");
  const [loading, setLoading] = useState(false);
  const [isUploadOpen, setIsUploadOpen] = useState(false);
  const [isMobileQuickActionsOpen, setIsMobileQuickActionsOpen] =
    useState(false);

  const handleSend = async () => {
    if (!inputMessage.trim()) return;

    setMessages((prev) => [
      ...prev,
      {
        type: "user",
        content: inputMessage,
      },
    ]);

    setLoading(true);
    try {
      const python_server_url = process.env.PYTHON_SERVER_URL;
      const response = await fetch(
        python_server_url+ "/qna/invoke",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            input: {
              context: "My Trip Plan Data is Here: "+ JSON.stringify(tripData),
              question: inputMessage,
            },
          }),
        },
      );

      if (!response.ok) throw new Error("Failed to get response");

      const data = await response.json();

      console.log(data);
      setMessages((prev) => [
        ...prev,
        {
          type: "assistant",
          content:
            data.output || "I'm here to help you plan your perfect trip!",
        },
      ]);
    } catch (error) {
      console.error("Error:", error);
      setMessages((prev) => [
        ...prev,
        {
          type: "assistant",
          content:
            "I am your AI Tour Guide, But I am unable to respond right now. Please try again later.",
        },
      ]);
    } finally {
      setLoading(false);
      setInputMessage("");
    }
  };

  const handleImageUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      // Handle image upload logic here
      setIsUploadOpen(false);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="flex-1 space-y-4 p-4 md:p-8 pt-6">
      <div className="flex items-center justify-between">
        <BreadCrumb items={breadcrumbItems} />

        {/* Mobile Quick Actions Button */}
        <Button
          variant="ghost"
          size="icon"
          className="md:hidden"
          onClick={() => setIsMobileQuickActionsOpen(true)}
        >
          <Menu className="h-6 w-6" />
        </Button>
      </div>

      {/* Mobile Quick Actions Modal */}
      {isMobileQuickActionsOpen && (
        <div className="fixed inset-0 flex z-50">
          <div
            className="bg-black bg-opacity-50 w-full h-full"
            onClick={() => setIsMobileQuickActionsOpen(false)}
          ></div>
          <div
            className="bg-white dark:bg-gray-800 w-72 p-4 h-full overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-lg font-semibold">Quick Actions</h2>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setIsMobileQuickActionsOpen(false)}
              >
                <X className="w-5 h-5" />
              </Button>
            </div>
            <div className="space-y-2">
              {suggestions.map((suggestion, index) => (
                <Button
                  key={index}
                  variant="ghost"
                  className="w-full justify-start"
                  onClick={() => {
                    setInputMessage(suggestion.text);
                    setIsMobileQuickActionsOpen(false);
                  }}
                >
                  {suggestion.icon}
                  <span className="ml-2">{suggestion.text}</span>
                </Button>
              ))}
            </div>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card className="md:col-span-3 p-4">
          {/* Main Content */}
          <div className="flex items-center space-x-2 mb-6">
            <Plane className="w-8 h-8 text-blue-500" />
            <h1 className="text-2xl font-bold">Travel Companion</h1>
          </div>

          <ScrollArea className="h-[calc(100vh-300px)]">
            {messages.length === 0 ? (
              <div className="space-y-6">
                <div className="text-center">
                  <div className="flex justify-center space-x-2 mb-4">
                    <MountainIcon className="w-8 h-8 text-green-500" />
                    <Mountain className="w-8 h-8 text-blue-500" />
                  </div>
                  <h2 className="text-xl font-semibold mb-2">
                    Welcome to Your Travel Companion!
                  </h2>
                  <p className="text-gray-600 dark:text-gray-400 mb-6">
                    I can help you plan trips, find accommodations, and provide
                    local insights.
                  </p>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {suggestions.map((suggestion, index) => (
                    <Button
                      key={index}
                      variant="outline"
                      className="flex items-center justify-start space-x-2 h-auto p-4 text-left"
                      onClick={() => setInputMessage(suggestion.text)}
                    >
                      {suggestion.icon}
                      <span>{suggestion.text}</span>
                    </Button>
                  ))}
                </div>
              </div>
            ) : (
              <div className="space-y-4">
                {messages.map((message, index) => (
                  <div
                    key={index}
                    className={`flex ${
                      message.type === "user" ? "justify-end" : "justify-start"
                    }`}
                  >
                    <div
                      className={`flex items-start space-x-2 max-w-[80%] ${
                        message.type === "user"
                          ? "flex-row-reverse space-x-reverse"
                          : ""
                      }`}
                    >
                      <Avatar>
                        <div
                          className={`w-full h-full rounded-full flex items-center justify-center ${
                            message.type === "user"
                              ? "bg-blue-500"
                              : "bg-green-500"
                          }`}
                        >
                          {message.type === "user" ? (
                            <Users className="w-4 h-4 text-white" />
                          ) : (
                            <Plane className="w-4 h-4 text-white" />
                          )}
                        </div>
                      </Avatar>
                      <div
                        className={`rounded-lg p-3 ${
                          message.type === "user"
                            ? "bg-blue-500 text-white"
                            : "bg-gray-100 dark:bg-gray-800"
                        }`}
                      >
                    
                        <ReactMarkdown>{message.content}</ReactMarkdown>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </ScrollArea>

          <div className="flex flex-row justify-between w-full items-center mt-4 space-x-2">
            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="outline"
                    size="icon"
                    onClick={() => setIsUploadOpen(!isUploadOpen)}
                  >
                    <ImageIcon className="w-5 h-5" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>Upload travel image</TooltipContent>
              </Tooltip>
            </TooltipProvider>

            <Input
              placeholder="Ask about your next adventure..."
              value={inputMessage}
              onChange={(e) => setInputMessage(e.target.value)}
              onKeyPress={handleKeyPress}
              className="flex-1 min-w-[700px]"
            />

            <Button onClick={handleSend} disabled={loading}>
              {loading ? (
                <Loader2 className="w-5 h-5 animate-spin" />
              ) : (
                <Send className="w-5 h-5" />
              )}
            </Button>
          </div>

          {isUploadOpen && (
            <div className="mt-2">
              <Input
                type="file"
                accept="image/*"
                onChange={handleImageUpload}
                className="w-full"
              />
            </div>
          )}
        </Card>

        {/* Desktop Quick Actions */}
        <Card className="hidden md:block p-4">
          <h2 className="text-lg font-semibold mb-4">Quick Actions</h2>
          <div className="space-y-2">
            {suggestions.map((suggestion, index) => (
              <Button
                key={index}
                variant="ghost"
                className="w-full justify-start"
                onClick={() => setInputMessage(suggestion.text)}
              >
                {suggestion.icon}
                <span className="ml-2">{suggestion.text}</span>
              </Button>
            ))}
          </div>
        </Card>
      </div>
    </div>
  );
}
