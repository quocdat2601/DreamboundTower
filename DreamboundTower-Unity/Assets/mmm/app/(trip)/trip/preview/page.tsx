"use client";
import React, { useState, useEffect } from "react";
import {
  Edit,
  MapPin,
  Clock,
  Users,
  Calendar,
  Utensils,
  Hotel,
  Navigation,
  Banknote,
  Check,
  ChevronDown,
  Plane,
  Coffee,
  UtensilsCrossed,
  Star,
  Building2,
  Phone,
  Globe,
} from "lucide-react";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { useSession } from "next-auth/react";
import { useRouter } from "next/navigation";

const TripPreview = () => {
  const [tripPlanId, setTripPlanId] = useState("");
  const [tripData, setTripData] = useState(null);
  const [editMode, setEditMode] = useState(false);
  const [editData, setEditData] = useState({});
  const [isEditing, setIsEditing] = useState(false);
  const [selectedDay, setSelectedDay] = useState(1);
  const [showConfirmation, setShowConfirmation] = useState(false);

  useEffect(() => {
    const savedTrip = localStorage.getItem("tripPlan");
    if (savedTrip) {
      const parsedData = JSON.parse(savedTrip);
      setTripData(parsedData);
      setEditData(parsedData);
    }
  }, []);

  console.log(tripData);

  const handleSave = async () => {
    try {
      const response = await fetch("/api/tripPlan/update", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(editData),
      });
      const updatedData = await response.json();
      setTripData(updatedData);
      localStorage.setItem("tripPlan", JSON.stringify(updatedData));
      setIsEditing(false);
    } catch (error) {
      console.error("Error updating trip plan:", error);
    }
  };

  const handleConfirmTrip = () => {
    setShowConfirmation(true);
    // Additional confirmation logic here
  };

  const { data: session } = useSession();

  const router = useRouter();

  const handleConfirmTripFinal = () => {
    if (session) {
      console.log("Trip confirmed!");
      router.push("/trip");
    } else {
      alert("Please sign in to confirm your trip.");
      router.push("/signin");
    }
  };

  const updateTripDetails = (field, value) => {
    setEditData((prev) => ({
      ...prev,
      [field]: value,
    }));
  };

  const updateBudget = (category, amount) => {
    setEditData((prev) => ({
      ...prev,
      budget: {
        ...prev.budget,
        breakdown: {
          ...prev.budget.breakdown,
          [category]: amount,
        },
      },
    }));
  };

  const MealIcon = ({ meal }) => {
    switch (meal) {
      case "breakfast":
        return <Coffee className="h-5 w-5 text-amber-600" />;
      case "lunch":
        return <UtensilsCrossed className="h-5 w-5 text-amber-600" />;
      case "dinner":
        return <Utensils className="h-5 w-5 text-amber-600" />;
      default:
        return <Utensils className="h-5 w-5 text-amber-600" />;
    }
  };

  if (!tripData)
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500"></div>
      </div>
    );

  return (
    <div className="h-screen overflow-auto bg-gray-50">
      <div className="container mx-auto p-6 space-y-6">
        {/* Header Actions - Fixed at top */}
        <div className="sticky top-0 z-50 bg-gray-50 py-4 mb-8">
          <div className="flex justify-between items-center mb-4">
            <h1 className="text-3xl font-bold text-gray-800">
              {tripData.trip_name}
            </h1>
            <div className="space-x-4">
              <Dialog>
                <DialogTrigger asChild>
                  <Button
                    variant="outline"
                    className="bg-blue-50 hover:bg-blue-100 text-blue-600"
                  >
                    <Edit className="h-4 w-4 mr-2" />
                    Edit Trip Data
                  </Button>
                </DialogTrigger>
                <DialogContent className="max-w-4xl max-h-[80vh] overflow-y-auto">
                  <DialogHeader>
                    <DialogTitle className="text-2xl mb-6">
                      Edit Trip Details
                    </DialogTitle>
                  </DialogHeader>
                  <ScrollArea className="max-h-[60vh]">
                    <div className="space-y-6 p-4">
                      {/* Basic Details */}
                      <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                          <Label>Trip Name</Label>
                          <Input
                            value={editData.trip_name || ""}
                            onChange={(e) =>
                              updateTripDetails("trip_name", e.target.value)
                            }
                            className="border-gray-300"
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Journey Date</Label>
                          <Input
                            type="date"
                            value={editData.journeyDate || ""}
                            onChange={(e) =>
                              updateTripDetails("journeyDate", e.target.value)
                            }
                            className="border-gray-300"
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Number of Days</Label>
                          <Input
                            type="number"
                            value={editData.days || ""}
                            onChange={(e) =>
                              updateTripDetails(
                                "days",
                                parseInt(e.target.value),
                              )
                            }
                            className="border-gray-300"
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Number of Travelers</Label>
                          <Input
                            type="number"
                            value={editData.people || ""}
                            onChange={(e) =>
                              updateTripDetails(
                                "people",
                                parseInt(e.target.value),
                              )
                            }
                            className="border-gray-300"
                          />
                        </div>
                      </div>

                      {/* Budget Section */}
                      <div className="space-y-4">
                        <h3 className="text-lg font-semibold">
                          Budget Breakdown
                        </h3>
                        <div className="grid grid-cols-2 gap-4">
                          {Object.entries(editData.budget?.breakdown || {}).map(
                            ([category, amount]) => (
                              <div key={category} className="space-y-2">
                                <Label className="capitalize">{category}</Label>
                                <Input
                                  type="number"
                                  value={amount}
                                  onChange={(e) =>
                                    updateBudget(
                                      category,
                                      parseInt(e.target.value),
                                    )
                                  }
                                  className="border-gray-300"
                                />
                              </div>
                            ),
                          )}
                        </div>
                      </div>

                      {/* Itinerary Editor */}
                      <div className="space-y-4">
                        <h3 className="text-lg font-semibold">
                          Daily Itinerary
                        </h3>
                        <Select
                          value={selectedDay.toString()}
                          onValueChange={(value) =>
                            setSelectedDay(parseInt(value))
                          }
                        >
                          <SelectTrigger>
                            <SelectValue placeholder="Select day" />
                          </SelectTrigger>
                          <SelectContent>
                            {Array.from({ length: editData.days || 0 }).map(
                              (_, idx) => (
                                <SelectItem
                                  key={idx + 1}
                                  value={(idx + 1).toString()}
                                >
                                  Day {idx + 1}
                                </SelectItem>
                              ),
                            )}
                          </SelectContent>
                        </Select>

                        <div className="space-y-4">
                          {["breakfast", "lunch", "dinner"].map((meal) => (
                            <div key={meal} className="space-y-2">
                              <Label className="capitalize">{meal}</Label>
                              <Input
                                value={
                                  editData.food?.[selectedDay]?.[meal]?.title ||
                                  ""
                                }
                                onChange={(e) => {
                                  const updatedFood = {
                                    ...editData.food,
                                    [selectedDay]: {
                                      ...editData.food?.[selectedDay],
                                      [meal]: {
                                        ...editData.food?.[selectedDay]?.[meal],
                                        title: e.target.value,
                                      },
                                    },
                                  };
                                  updateTripDetails("food", updatedFood);
                                }}
                                className="border-gray-300"
                              />
                            </div>
                          ))}
                        </div>
                      </div>

                      <div className="flex justify-end space-x-4">
                        <Button
                          variant="outline"
                          onClick={() => setIsEditing(false)}
                        >
                          Cancel
                        </Button>
                        <Button
                          onClick={handleSave}
                          className="bg-blue-600 hover:bg-blue-700 text-white"
                        >
                          Save Changes
                        </Button>
                      </div>
                    </div>
                  </ScrollArea>
                </DialogContent>
              </Dialog>

              <Button
                onClick={handleConfirmTrip}
                className="bg-green-600 hover:bg-green-700 text-white font-semibold"
              >
                <Check className="h-4 w-4 mr-2" />
                Confirm Trip
              </Button>
            </div>
          </div>

          {/* Trip Overview Card */}
          <div className="space-y-6 pb-20">
            {/* Trip Overview Card */}
            <Card className="shadow-lg hover:shadow-xl transition-shadow duration-300">
              <CardHeader className="bg-gradient-to-r from-blue-50 to-blue-100">
                <CardTitle className="text-2xl text-blue-800">
                  Trip Overview
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 p-4">
                  <div className="flex items-center gap-4 bg-blue-50 p-4 rounded-lg">
                    <div className="bg-blue-100 p-2 rounded-full">
                      <MapPin className="h-6 w-6 text-blue-600" />
                    </div>
                    <div>
                      <p className="text-sm text-blue-600 font-medium">
                        From - To
                      </p>
                      <p className="font-semibold text-gray-800">
                        {tripData.origin} - {tripData.destination}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center gap-4 bg-green-50 p-4 rounded-lg">
                    <div className="bg-green-100 p-2 rounded-full">
                      <Calendar className="h-6 w-6 text-green-600" />
                    </div>
                    <div>
                      <p className="text-sm text-green-600 font-medium">
                        Duration
                      </p>
                      <p className="font-semibold text-gray-800">
                        {tripData.days} Days
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center gap-4 bg-purple-50 p-4 rounded-lg">
                    <div className="bg-purple-100 p-2 rounded-full">
                      <Users className="h-6 w-6 text-purple-600" />
                    </div>
                    <div>
                      <p className="text-sm text-purple-600 font-medium">
                        People
                      </p>
                      <p className="font-semibold text-gray-800">
                        {tripData.people} Travelers
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center gap-4 bg-amber-50 p-4 rounded-lg">
                    <div className="bg-amber-100 p-2 rounded-full">
                      <Banknote className="h-6 w-6 text-amber-600" />
                    </div>
                    <div>
                      <p className="text-sm text-amber-600 font-medium">
                        Total Budget
                      </p>
                      <p className="font-semibold text-gray-800">
                        {tripData.budget.total} BDT
                      </p>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Budget Breakdown */}
            <Card className="shadow-lg">
              <CardHeader className="bg-gradient-to-r from-amber-50 to-amber-100">
                <CardTitle className="text-2xl text-amber-800">
                  Budget Breakdown
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-6 p-4">
                  {Object.entries(tripData.budget.breakdown).map(
                    ([category, amount]) => (
                      <div
                        key={category}
                        className="p-6 bg-amber-50 rounded-lg hover:bg-amber-100 transition-colors duration-300"
                      >
                        <p className="text-sm text-amber-600 font-medium capitalize">
                          {category}
                        </p>
                        <p className="text-2xl font-bold text-amber-800">
                          {amount} BDT
                        </p>
                      </div>
                    ),
                  )}
                </div>
              </CardContent>
            </Card>

            {/* Food Schedule Card */}
            <Card className="shadow-lg">
              <CardHeader className="bg-gradient-to-r from-amber-50 to-amber-100">
                <CardTitle className="text-2xl text-amber-800">
                  <div className="flex items-center gap-2">
                    <Utensils className="h-6 w-6" />
                    Food Schedule
                  </div>
                </CardTitle>
              </CardHeader>
              <CardContent>
                <ScrollArea className="h-[500px] pr-4">
                  <Accordion type="single" collapsible className="w-full">
                    {Array.from({ length: tripData.days }).map(
                      (_, dayIndex) => {
                        const dayNumber = (dayIndex + 1).toString();
                        return (
                          <AccordionItem
                            key={dayIndex}
                            value={`day-${dayNumber}`}
                          >
                            <AccordionTrigger className="text-lg font-semibold hover:bg-amber-50 p-4 rounded-lg">
                              Day {dayNumber}
                            </AccordionTrigger>
                            <AccordionContent className="space-y-4 p-4">
                              {["breakfast", "lunch", "dinner"].map((meal) => {
                                const mealData =
                                  tripData.food[dayNumber]?.[meal];
                                return mealData ? (
                                  <div
                                    key={meal}
                                    className="flex items-start gap-4 p-4 bg-amber-50 rounded-lg hover:bg-amber-100 transition-colors duration-300"
                                  >
                                    <div className="bg-amber-100 p-2 rounded-full">
                                      <MealIcon meal={meal} />
                                    </div>
                                    <div className="flex-1">
                                      <p className="font-medium capitalize text-amber-800">
                                        {meal}
                                      </p>
                                      <p className="text-lg font-semibold">
                                        {mealData.title}
                                      </p>
                                      <p className="text-sm text-gray-600">
                                        {mealData.address}
                                      </p>
                                      <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-gray-500 mt-2">
                                        <span className="flex items-center gap-1">
                                          <Star
                                            className="h-4 w-4 text-amber-500"
                                            fill="currentColor"
                                          />
                                          {mealData.rating}
                                        </span>
                                        <span>•</span>
                                        <span>{mealData.category}</span>
                                        {mealData.phoneNumber && (
                                          <>
                                            <span>•</span>
                                            <span>{mealData.phoneNumber}</span>
                                          </>
                                        )}
                                        {mealData.website && (
                                          <>
                                            <span>•</span>
                                            <span className="text-amber-600 hover:underline cursor-pointer">
                                              Visit Website
                                            </span>
                                          </>
                                        )}
                                      </div>
                                    </div>
                                  </div>
                                ) : null;
                              })}
                            </AccordionContent>
                          </AccordionItem>
                        );
                      },
                    )}
                  </Accordion>
                </ScrollArea>
              </CardContent>
            </Card>

            {/* Accommodation Card */}
            <Card className="shadow-lg">
              <CardHeader className="bg-gradient-to-r from-indigo-50 to-indigo-100">
                <CardTitle className="text-2xl text-indigo-800">
                  <div className="flex items-center gap-2">
                    <Hotel className="h-6 w-6" />
                    Accommodations
                  </div>
                </CardTitle>
              </CardHeader>
              <CardContent>
                <ScrollArea className="h-[500px] pr-4">
                  <div className="grid gap-6">
                    {Array.from({ length: tripData.days }).map(
                      (_, dayIndex) => {
                        const dayNumber = (dayIndex + 1).toString();
                        const accommodationData =
                          tripData.accommodation[dayNumber];

                        if (!accommodationData) return null;

                        return (
                          <div
                            key={dayIndex}
                            className="bg-white rounded-lg shadow-sm hover:shadow-md transition-shadow duration-300"
                          >
                            <div className="bg-indigo-50 px-4 py-2 rounded-t-lg">
                              <h3 className="text-lg font-semibold text-indigo-800">
                                Day {dayNumber}
                              </h3>
                            </div>
                            <div className="p-4">
                              <div className="flex items-start gap-4">
                                <div className="bg-indigo-100 p-3 rounded-full">
                                  <Building2 className="h-6 w-6 text-indigo-600" />
                                </div>
                                <div className="flex-1">
                                  <h4 className="text-xl font-semibold">
                                    {accommodationData.title}
                                  </h4>
                                  <p className="text-gray-600 mt-1">
                                    {accommodationData.address}
                                  </p>
                                  <div className="grid grid-cols-2 md:grid-cols-3 gap-4 mt-4">
                                    <div className="bg-indigo-50 p-3 rounded-lg">
                                      <p className="text-sm text-indigo-600 font-medium">
                                        Rating
                                      </p>
                                      <p className="text-lg font-semibold flex items-center gap-1">
                                        <Star
                                          className="h-4 w-4 text-indigo-500"
                                          fill="currentColor"
                                        />
                                        {accommodationData.rating}
                                      </p>
                                    </div>
                                    <div className="bg-indigo-50 p-3 rounded-lg">
                                      <p className="text-sm text-indigo-600 font-medium">
                                        Category
                                      </p>
                                      <p className="text-lg font-semibold">
                                        {accommodationData.category}
                                      </p>
                                    </div>
                                    {accommodationData.amenities && (
                                      <div className="bg-indigo-50 p-3 rounded-lg">
                                        <p className="text-sm text-indigo-600 font-medium">
                                          Amenities
                                        </p>
                                        <p className="text-lg font-semibold">
                                          {accommodationData.amenities}
                                        </p>
                                      </div>
                                    )}
                                  </div>
                                  <div className="flex flex-wrap gap-4 mt-4">
                                    {accommodationData.phoneNumber && (
                                      <a
                                        href={`tel:${accommodationData.phoneNumber}`}
                                      >
                                        <Button
                                          variant="outline"
                                          className="text-indigo-600"
                                        >
                                          <Phone className="h-4 w-4 mr-2" />
                                          {accommodationData.phoneNumber}
                                        </Button>
                                      </a>
                                    )}
                                    {accommodationData.website && (
                                      <a
                                        href={accommodationData.website}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                      >
                                        <Button
                                          variant="outline"
                                          className="text-indigo-600"
                                        >
                                          <Globe className="h-4 w-4 mr-2" />
                                          Visit Website
                                        </Button>
                                      </a>
                                    )}
                                  </div>
                                </div>
                              </div>
                            </div>
                          </div>
                        );
                      },
                    )}
                  </div>
                </ScrollArea>
              </CardContent>
            </Card>

            {/* Checkpoints */}
            <Card className="shadow-lg">
              <CardHeader className="bg-gradient-to-r from-green-50 to-green-100">
                <CardTitle className="text-2xl text-green-800">
                  Travel Checkpoints
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="overflow-x-auto">
                  <Table>
                    <TableHeader>
                      <TableRow className="bg-green-50">
                        <TableHead className="font-semibold text-green-800">
                          From
                        </TableHead>
                        <TableHead className="font-semibold text-green-800">
                          To
                        </TableHead>
                        <TableHead className="font-semibold text-green-800">
                          Departure
                        </TableHead>
                        <TableHead className="font-semibold text-green-800">
                          Arrival
                        </TableHead>
                        <TableHead className="font-semibold text-green-800">
                          Tips
                        </TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {tripData.checkpoints.map((checkpoint, index) => (
                        <TableRow
                          key={index}
                          className="hover:bg-green-50 transition-colors duration-200"
                        >
                          <TableCell className="flex items-center gap-2">
                            <MapPin className="h-4 w-4 text-green-600" />
                            {checkpoint.origin.location}
                          </TableCell>
                          <TableCell className="flex items-center gap-2">
                            <Navigation className="h-4 w-4 text-green-600" />
                            {checkpoint.destination.location}
                          </TableCell>
                          <TableCell className="flex items-center gap-2">
                            <Clock className="h-4 w-4 text-green-600" />
                            {checkpoint.logistics.departure_time}
                          </TableCell>
                          <TableCell className="flex items-center gap-2">
                            <Plane className="h-4 w-4 text-green-600" />
                            {checkpoint.logistics.arrival_time}
                          </TableCell>
                          <TableCell>
                            <div className="bg-green-50 p-2 rounded-lg text-sm">
                              {checkpoint.logistics.tips}
                            </div>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Confirmation Dialog */}
          <Dialog open={showConfirmation} onOpenChange={setShowConfirmation}>
            <DialogContent className="max-w-md">
              <DialogHeader>
                <DialogTitle className="text-2xl">
                  Confirm Your Trip
                </DialogTitle>
              </DialogHeader>
              <div className="space-y-4">
                <Alert className="bg-green-50 border-green-200">
                  <AlertDescription className="text-green-800">
                    You're about to confirm your trip to {tripData.destination}.
                    This will:
                    <ul className="list-disc list-inside mt-2 space-y-1">
                      <li>Lock in your itinerary</li>
                      <li>Send confirmation emails to all travelers</li>
                      <li>Begin the booking process for accommodations</li>
                      <li>Generate your travel documents</li>
                    </ul>
                  </AlertDescription>
                </Alert>
                <div className="flex justify-end space-x-4">
                  <Button
                    variant="outline"
                    onClick={() => setShowConfirmation(false)}
                  >
                    Cancel
                  </Button>
                  <Button
                    className="bg-green-600 hover:bg-green-700 text-white"
                    onClick={() => {
                      console.log("Trip confirmed!");
                      handleConfirmTripFinal();
                      setShowConfirmation(false);
                    }}
                  >
                    Confirm Trip
                  </Button>
                </div>
              </div>
            </DialogContent>
          </Dialog>
        </div>
      </div>
    </div>
  );
};

export default TripPreview;
