"use client";
import React, { useEffect, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  MapPin,
  Calendar,
  Users,
  Wallet,
  Clock,
  Utensils,
  Building,
  Edit2,
  CheckCircle,
  Plus,
  Trash2,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Calendar as CalendarIcon } from "lucide-react";
import { Calendar as CalendarComponent } from "@/components/ui/calendar";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { format } from "date-fns";

// List of available places (you can expand this)
const AVAILABLE_PLACES = [
  "Dhaka",
  "Chittagong",
  "Sylhet",
  "Cox's Bazar",
  "Bandarban",
  "Rangamati",
  "Khulna",
  "Sundarbans",
  "Rajshahi",
  "Mymensingh",
];

// Utility function for formatting currency
const formatCurrency = (amount) => {
  return new Intl.NumberFormat("en-BD", {
    style: "currency",
    currency: "BDT",
  }).format(amount);
};

const TripDetails = () => {
  const [isEditing, setIsEditing] = useState(false);
  const [saveClick, setSaveClick] = useState(false);
  const [showConfirmation, setShowConfirmation] = useState(false);
  const [responseData, setResponseData] = useState(null);
  const [tripData, setTripData] = useState(null);
  const [selectedDate, setSelectedDate] = useState(null);
  const [maxBudget, setMaxBudget] = useState("");
  const [foodPlans, setFoodPlans] = useState([]);
  const [accommodations, setAccommodations] = useState([]);

  useEffect(() => {
    const storedData = localStorage.getItem("tripData");
    if (storedData) {
      const parsedData = JSON.parse(storedData);
      setResponseData(parsedData);
      if (parsedData?.output) {
        setTripData(parsedData);
        setSelectedDate(new Date(parsedData.output.journeyDate));
        setFoodPlans(parsedData.output.food || []);
        setAccommodations(parsedData.output.accommodation || []);
      }
    }
  }, []);

  const handleEdit = () => {
    setIsEditing(true);
    setShowConfirmation(false);
  };

  const handleSave = () => {
    setIsEditing(false);
    setSaveClick(true);
    const updatedTripData = {
      ...tripData,
      output: {
        ...tripData.output,
        food: foodPlans,
        accommodation: accommodations,
      },
    };
    localStorage.setItem("tripData", JSON.stringify(updatedTripData));
  };

  const handleConfirm = () => {
    setShowConfirmation(true);
  };

  const handleInputChange = (section, field, value) => {
    setTripData((prev) => ({
      ...prev,
      output: {
        ...prev?.output,
        [section]: value,
      },
    }));
  };

  // Add new food plan
  const addFoodPlan = () => {
    const newFoodPlan = {
      meal_type: "",
      name: "",
      type: "",
      cost: 0,
    };
    setFoodPlans([...foodPlans, newFoodPlan]);
  };

  // Remove food plan
  const removeFoodPlan = (index) => {
    const updatedPlans = foodPlans.filter((_, i) => i !== index);
    setFoodPlans(updatedPlans);
  };

  // Add new accommodation
  const addAccommodation = () => {
    const newAccommodation = {
      day: accommodations.length + 1,
      type: "",
      location: "",
      cost_per_night: 0,
    };
    setAccommodations([...accommodations, newAccommodation]);
  };

  // Remove accommodation
  const removeAccommodation = (index) => {
    const updatedAccommodations = accommodations.filter((_, i) => i !== index);
    setAccommodations(updatedAccommodations);
  };

  const output = tripData?.output || {};

  // Trip Name Edit Component
  const TripNameEdit = () => (
    <div className="space-y-2">
      <Label>Trip Name</Label>
      <Input
        value={output.trip_name || ""}
        onChange={(e) => handleInputChange("trip_name", null, e.target.value)}
        placeholder="Enter trip name"
        className="text-xl font-bold"
      />
    </div>
  );

  // Location Selector Component
  const LocationSelector = ({ value, onChange, label }) => (
    <div className="space-y-2">
      <Label>{label}</Label>
      <Select value={value} onValueChange={onChange}>
        <SelectTrigger>
          <SelectValue placeholder={`Select ${label.toLowerCase()}`} />
        </SelectTrigger>
        <SelectContent>
          {AVAILABLE_PLACES.map((place) => (
            <SelectItem key={place} value={place}>
              {place}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );

  // Update the useEffect hook where we initialize the date:
  useEffect(() => {
    const storedData = localStorage.getItem("tripData");
    if (storedData) {
      const parsedData = JSON.parse(storedData);
      setResponseData(parsedData);
      if (parsedData?.output) {
        setTripData(parsedData);
        // Safely parse the date
        if (parsedData.output.journeyDate) {
          try {
            const date = new Date(parsedData.output.journeyDate);
            // Check if the date is valid
            if (!isNaN(date.getTime())) {
              setSelectedDate(date);
            }
          } catch (error) {
            console.error("Error parsing date:", error);
          }
        }
        setFoodPlans(parsedData.output.food || []);
        setAccommodations(parsedData.output.accommodation || []);
      }
    }
  }, []);

  // Update the DateSelector component to handle dates more safely:
  const DateSelector = () => (
    <div className="space-y-2">
      <Label>Journey Date</Label>
      <Popover>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            className="w-full justify-start text-left font-normal"
          >
            <CalendarIcon className="mr-2 h-4 w-4" />
            {selectedDate && !isNaN(selectedDate.getTime())
              ? format(selectedDate, "PPP")
              : "Select date"}
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-auto p-0">
          <CalendarComponent
            mode="single"
            selected={selectedDate}
            onSelect={(date) => {
              setSelectedDate(date);
              if (date) {
                handleInputChange(
                  "journeyDate",
                  null,
                  format(date, "yyyy-MM-dd"),
                );
              }
            }}
            initialFocus
          />
        </PopoverContent>
      </Popover>
    </div>
  );

  // Food Plan Form Component
  const FoodPlanForm = () => (
    <div className="space-y-4">
      {foodPlans.map((plan, index) => (
        <div key={index} className="bg-gray-50 p-4 rounded-lg relative">
          <Button
            variant="destructive"
            size="sm"
            className="absolute top-2 right-2"
            onClick={() => removeFoodPlan(index)}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
          <div className="grid grid-cols-2 gap-4">
            <Select
              value={plan.meal_type}
              onValueChange={(value) => {
                const updatedPlans = [...foodPlans];
                updatedPlans[index].meal_type = value;
                setFoodPlans(updatedPlans);
              }}
            >
              <SelectTrigger>
                <SelectValue placeholder="Select meal type" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="breakfast">Breakfast</SelectItem>
                <SelectItem value="lunch">Lunch</SelectItem>
                <SelectItem value="dinner">Dinner</SelectItem>
              </SelectContent>
            </Select>
            <Input
              placeholder="Restaurant name"
              value={plan.name}
              onChange={(e) => {
                const updatedPlans = [...foodPlans];
                updatedPlans[index].name = e.target.value;
                setFoodPlans(updatedPlans);
              }}
            />
            <Input
              placeholder="Cuisine type"
              value={plan.type}
              onChange={(e) => {
                const updatedPlans = [...foodPlans];
                updatedPlans[index].type = e.target.value;
                setFoodPlans(updatedPlans);
              }}
            />
            <Input
              type="number"
              placeholder="Cost"
              value={plan.cost}
              onChange={(e) => {
                const updatedPlans = [...foodPlans];
                updatedPlans[index].cost = parseInt(e.target.value);
                setFoodPlans(updatedPlans);
              }}
            />
          </div>
        </div>
      ))}
      <Button onClick={addFoodPlan} className="w-full">
        <Plus className="mr-2 h-4 w-4" /> Add Food Plan
      </Button>
    </div>
  );

  // Accommodation Form Component
  const AccommodationForm = () => (
    <div className="space-y-4">
      {accommodations.map((accommodation, index) => (
        <div key={index} className="bg-gray-50 p-4 rounded-lg relative">
          <Button
            variant="destructive"
            size="sm"
            className="absolute top-2 right-2"
            onClick={() => removeAccommodation(index)}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
          <div className="grid grid-cols-2 gap-4">
            <Input
              placeholder="Hotel type"
              value={accommodation.type}
              onChange={(e) => {
                const updatedAccommodations = [...accommodations];
                updatedAccommodations[index].type = e.target.value;
                setAccommodations(updatedAccommodations);
              }}
            />
            <Input
              placeholder="Location"
              value={accommodation.location}
              onChange={(e) => {
                const updatedAccommodations = [...accommodations];
                updatedAccommodations[index].location = e.target.value;
                setAccommodations(updatedAccommodations);
              }}
            />
            <Input
              type="number"
              placeholder="Cost per night"
              value={accommodation.cost_per_night}
              onChange={(e) => {
                const updatedAccommodations = [...accommodations];
                updatedAccommodations[index].cost_per_night = parseInt(
                  e.target.value,
                );
                setAccommodations(updatedAccommodations);
              }}
            />
          </div>
        </div>
      ))}
      <Button onClick={addAccommodation} className="w-full">
        <Plus className="mr-2 h-4 w-4" /> Add Accommodation
      </Button>
    </div>
  );

  // Rest of your component remains the same, but update the respective sections
  // to use the new components when isEditing is true

  return (
    <div className="h-screen overflow-auto">
      <div className="max-w-4xl mx-auto p-4 space-y-4 pb-20">
        {/* Trip Overview */}
        <Card>
          <CardHeader>
            {isEditing ? (
              <TripNameEdit />
            ) : (
              <CardTitle className="text-xl font-bold">
                {output.trip_name}
              </CardTitle>
            )}
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {isEditing ? (
                <>
                  <LocationSelector
                    label="Origin"
                    value={output.origin}
                    onChange={(value) =>
                      handleInputChange("origin", null, value)
                    }
                  />
                  <LocationSelector
                    label="Destination"
                    value={output.destination}
                    onChange={(value) =>
                      handleInputChange("destination", null, value)
                    }
                  />
                  <DateSelector />
                </>
              ) : (
                <>
                  <div className="flex items-center gap-2">
                    <MapPin className="text-blue-500" />
                    <span>
                      {output.origin} → {output.destination}
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Calendar className="text-blue-500" />
                    <span>{output.journeyDate}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Users className="text-blue-500" />
                    <span>{output.people} People</span>
                  </div>
                </>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Budget Breakdown */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">
              <div className="flex items-center gap-2">
                <Wallet className="text-green-500" />
                Budget Breakdown
              </div>
            </CardTitle>
          </CardHeader>
          <CardContent>
            {(!output?.budget?.breakdown ||
              Object.keys(output?.budget?.breakdown).length === 0) &&
            isEditing ? (
              <div className="space-y-4">
                <Label>Maximum Budget</Label>
                <Input
                  type="number"
                  value={maxBudget}
                  onChange={(e) => setMaxBudget(e.target.value)}
                  placeholder="Enter maximum budget"
                />
              </div>
            ) : (
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                {Object.entries(output?.budget?.breakdown || {}).map(
                  ([key, value]) => (
                    <div
                      key={key}
                      className="text-center p-2 bg-gray-50 rounded-lg"
                    >
                      <div className="font-medium capitalize">{key}</div>
                      {isEditing ? (
                        <Input
                          type="number"
                          value={value}
                          onChange={(e) => {
                            const newBreakdown = {
                              ...output.budget.breakdown,
                              [key]: parseInt(e.target.value),
                            };
                            handleInputChange(
                              "budget",
                              "breakdown",
                              newBreakdown,
                            );
                          }}
                          className="mt-2"
                        />
                      ) : (
                        <div className="text-lg font-bold text-green-600">
                          {formatCurrency(value)}
                        </div>
                      )}
                    </div>
                  ),
                )}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Food Plan */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">
              <div className="flex items-center gap-2">
                <Utensils className="text-orange-500" />
                Food Plan
              </div>
            </CardTitle>
          </CardHeader>
          <CardContent>
            {isEditing ? (
              <FoodPlanForm />
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {Array.isArray(foodPlans) ? (
                  foodPlans.map((plan, index) => (
                    <div key={index} className="bg-gray-50 p-3 rounded-lg">
                      <div className="font-medium capitalize">
                        {plan.meal_type}
                      </div>
                      {/* Add more content based on your plan object */}
                    </div>
                  ))
                ) : (
                  <div>No food plans available</div>
                )}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Accommodation */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">
              <div className="flex items-center gap-2">
                <Building className="text-indigo-500" />
                Accommodation
              </div>
            </CardTitle>
          </CardHeader>
          <CardContent>
            {isEditing ? (
              <AccommodationForm />
            ) : (
              <div className="space-y-4">
                {Array.isArray(foodPlans) ? (
                  accommodations.map((accommodation, index) => (
                  <div key={index} className="bg-gray-50 p-3 rounded-lg">
                    <div className="font-medium">Day {accommodation.day}</div>
                    <div className="flex justify-between items-center mt-2">
                      <div>
                        <div>{accommodation.type}</div>
                        <div className="text-sm text-gray-600">
                          {accommodation.location}
                        </div>
                      </div>
                      <div className="font-bold text-indigo-600">
                        {formatCurrency(accommodation.cost_per_night)}
                      </div>
                    </div>
                  </div>
                ))) : (
                  <div>No accommodations available</div>
                )}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Cancel Edit Mode Alert */}
        {isEditing && (
          <Card className="bg-yellow-50 border-yellow-200">
            <CardContent className="pt-6">
              <div className="flex justify-between items-center">
                <p className="text-yellow-700">
                  You are in edit mode. Click 'Save Changes' to save your
                  modifications or refresh the page to cancel.
                </p>
                <Button
                  onClick={() => setIsEditing(false)}
                  className="bg-yellow-500 hover:bg-yellow-600"
                >
                  Cancel
                </Button>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Action Buttons */}
        <div className="fixed bottom-4 right-4 space-x-4">
          {!isEditing ? (
            <>
              <Button
                onClick={handleEdit}
                className="bg-blue-500 hover:bg-blue-600"
              >
                <Edit2 className="w-4 h-4 mr-2" />
                Modify Trip
              </Button>
              <Button
                onClick={handleConfirm}
                className="bg-green-500 hover:bg-green-600"
              >
                <CheckCircle className="w-4 h-4 mr-2" />
                Confirm Trip
              </Button>
            </>
          ) : (
            <Button
              onClick={handleSave}
              className="bg-green-500 hover:bg-green-600"
            >
              Save Changes
            </Button>
          )}
        </div>

        {/* Success Message After Save */}
        {!isEditing && saveClick && (
          <div className="sticky top-4 right-4 transition-opacity duration-500 z-10">
            <Alert className="bg-green-50 border-green-200 shadow-lg">
              <CheckCircle className="w-4 h-4 text-green-500" />
              <AlertDescription>Changes saved successfully!</AlertDescription>
            </Alert>
          </div>
        )}

        {/* Confirmation Alert */}
        {showConfirmation && (
          <div className="max-w-4xl mx-auto p-4">
            <Alert className="bg-green-50 border-green-200">
              <CheckCircle className="w-6 h-6 text-green-500" />
              <AlertDescription>
                Your trip has been confirmed! Redirecting to confirmation
                page...
              </AlertDescription>
            </Alert>
          </div>
        )}
      </div>
    </div>
  );
};

export default TripDetails;
