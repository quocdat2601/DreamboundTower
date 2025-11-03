"use client";

import React, { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  Send,
  Navigation,
  MessageSquare,
  CalendarDays,
  Users,
  Banknote,
  X,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { ScrollArea } from "@/components/ui/scroll-area";

interface TravelPlannerModalProps {
  isOpen: boolean;
  inputText: string;
  onClose: () => void;
}

const TravelPlannerModal = ({
  isOpen,
  inputText,
  onClose,
}: TravelPlannerModalProps) => {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    origin: "",
    destination: "",
    days: "",
    budget: "",
    people: "1",
    preferences: "",
    tripType: "oneWay",
    journeyDate: "",
    travelClass: "economy",
  });

  const [nlQuery, setNlQuery] = useState(inputText);
  const [showNlInput, setShowNlInput] = useState(true);

  const handleNaturalLanguageProcess = async () => {
    setLoading(true);
    try {
      const response = await fetch(
        `/api/tripPlan/extract?text=${encodeURIComponent(nlQuery)}`,
      );
      const data = await response.json();
      setFormData(data.output);
      setShowNlInput(false);
    } catch (error) {
      console.error("Error processing natural language query:", error);
    }
    setLoading(false);
  };

  useEffect(() => {
    if (inputText) {
      setNlQuery(inputText);
      handleNaturalLanguageProcess();
    }
  }, [inputText]);

  const generateTripPlan = async () => {
    setLoading(true);
    try {
      const queryParams = new URLSearchParams({
        origin: formData?.origin,
        destination: formData?.destination,
        days: formData?.days,
        budget: formData?.budget,
        people: formData?.people,
        preferences: formData?.preferences,
        tripType: formData?.tripType,
        journeyDate: formData?.journeyDate,
        travelClass: formData?.travelClass,
      });

      const response = await fetch(`/api/tripPlan?${queryParams}`);
      const data = await response.json();
      localStorage.setItem("tripPlan", JSON.stringify(data.output));
      onClose();
      router.push("/trip/preview");
    } catch (error) {
      console.error("Error generating trip plan:", error);
    }
    setLoading(false);
  };

  const getTodayDateString = () => {
    const today = new Date();
    const year = today.getFullYear();
    const month = String(today.getMonth() + 1).padStart(2, "0"); // Add leading zero if needed
    const day = String(today.getDate()).padStart(2, "0");
    return `${year}-${month}-${day}`;
  };

  const handleJourneyDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedDate = e.target.value;
    const today = getTodayDateString();

    // If the selected date is before today, set it to today's date
    if (selectedDate < today) {
      setFormData({ ...formData, journeyDate: today });
    } else {
      setFormData({ ...formData, journeyDate: selectedDate });
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-3xl h-[90vh] p-0">
        <DialogHeader className="px-6 pt-6">
          <div className="flex justify-between items-center">
            <DialogTitle className="text-2xl flex items-center gap-2">
              <Navigation className="h-6 w-6" />
              Travel Planner
            </DialogTitle>

            {loading ? (
              <div>
                {/* A Loading Component */}
                <div className="flex items-center space-x-2">
                  <div className="w-10 h-10 bg-gray-200 rounded-full animate-pulse"></div>
                  <div className="w-10 h-10 bg-gray-200 rounded-full animate-pulse"></div>
                  <div className="w-10 h-10 bg-gray-200 rounded-full animate-pulse"></div>
                </div>
              </div>
            ) : null}
            <div className="flex gap-2">
              <Button
                variant="outline"
                onClick={() => setShowNlInput(!showNlInput)}
                className="flex items-center gap-2"
              >
                <MessageSquare className="h-4 w-4" />
                Quick Fill
              </Button>
              <Button variant="ghost" size="icon" onClick={onClose}>
                <X className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </DialogHeader>

        <ScrollArea className="h-full px-6 pb-6">
          <div className="space-y-6">
            {showNlInput && (
              <Alert>
                <AlertDescription>
                  <div className="space-y-4">
                    <p className="text-sm text-muted-foreground">
                      Describe your trip in natural language, e.g., "journey
                      from dhaka to sylhet with 3 friends 4 days budget 5k"
                    </p>
                    <div className="flex gap-2">
                      <Input
                        value={nlQuery}
                        onChange={(e) => setNlQuery(e.target.value)}
                        placeholder="Describe your trip..."
                        className="w-auto"
                      />
                      <Button
                        onClick={handleNaturalLanguageProcess}
                        disabled={loading}
                        className="bg-purple-600 font-semibold"
                      >
                        <Send className="h-4 w-4 mr-2" />
                        Process
                      </Button>
                    </div>
                  </div>
                </AlertDescription>
              </Alert>
            )}

            <Card>
              <CardContent className="pt-6">
                <div className="space-y-6">
                  <RadioGroup
                    value={formData?.tripType}
                    onValueChange={(value) =>
                      setFormData({ ...formData, tripType: value })
                    }
                    className="flex flex-wrap gap-4"
                  >
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="oneWay" id="oneWay" />
                      <Label htmlFor="oneWay">One Way</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="roundTrip" id="roundTrip" />
                      <Label htmlFor="roundTrip">Round Trip</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="multiCity" id="multiCity" />
                      <Label htmlFor="multiCity">Multi City</Label>
                    </div>
                  </RadioGroup>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label>Origin</Label>
                      <Input
                        value={formData?.origin}
                        onChange={(e) =>
                          setFormData({ ...formData, origin: e.target.value })
                        }
                        placeholder="Enter origin city"
                      />
                    </div>

                    <div className="space-y-2">
                      <Label>Destination</Label>
                      <Input
                        value={formData?.destination}
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            destination: e.target.value,
                          })
                        }
                        placeholder="Enter destination city"
                      />
                    </div>

                    <div className="space-y-2">
                      <Label className="flex items-center gap-2">
                        <CalendarDays className="h-4 w-4" />
                        Journey Date
                      </Label>
                      {/* <Input
                        type="date"
                        value={formData?.journeyDate}
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            journeyDate: e.target.value,
                          })
                        }
                      /> */}
                      <Input
                        type="date"
                        value={formData?.journeyDate || getTodayDateString()} // Set default value to today's date if empty
                        onChange={handleJourneyDateChange}
                      />
                    </div>

                    <div className="space-y-2">
                      <Label className="flex items-center gap-2">
                        <CalendarDays className="h-4 w-4" />
                        Duration (Days)
                      </Label>
                      <Input
                        type="number"
                        min="1"
                        value={formData?.days}
                        onChange={(e) =>
                          setFormData({ ...formData, days: e.target.value })
                        }
                        placeholder="Number of days"
                      />
                    </div>

                    <div className="space-y-2">
                      <Label className="flex items-center gap-2">
                        <Users className="h-4 w-4" />
                        Number of People
                      </Label>
                      <Input
                        type="number"
                        min="1"
                        value={formData?.people}
                        onChange={(e) =>
                          setFormData({ ...formData, people: e.target.value })
                        }
                        placeholder="Number of travelers"
                      />
                    </div>

                    <div className="space-y-2">
                      <Label>Travel Class</Label>
                      <Select
                        value={formData?.travelClass}
                        onValueChange={(value) =>
                          setFormData({ ...formData, travelClass: value })
                        }
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Select class" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="economy">Economy</SelectItem>
                          <SelectItem value="business">Business</SelectItem>
                          <SelectItem value="first">First Class</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Label className="flex items-center gap-2">
                      <Banknote className="h-4 w-4" />
                      Budget
                    </Label>
                    <Input
                      type="number"
                      value={formData?.budget}
                      onChange={(e) =>
                        setFormData({ ...formData, budget: e.target.value })
                      }
                      placeholder="Enter your budget"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label>Preferences</Label>
                    <Textarea
                      value={formData?.preferences}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          preferences: e.target.value,
                        })
                      }
                      placeholder="Any special preferences? (e.g., hill views, near beach, etc.)"
                    />
                  </div>

                  <Button
                    onClick={generateTripPlan}
                    disabled={loading}
                    className="flex items-center w-full gap-2 bg-purple-600 hover:bg-purple-700  font-semibold"
                  >
                    <Navigation className="h-4 w-4 mr-2" />
                    Generate Trip Plan
                  </Button>
                </div>
              </CardContent>
            </Card>
          </div>
        </ScrollArea>
      </DialogContent>
    </Dialog>
  );
};

export default TravelPlannerModal;
