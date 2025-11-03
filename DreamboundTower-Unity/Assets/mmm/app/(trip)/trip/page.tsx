"use client";

import { useEffect, useState, useCallback } from "react";
import { useSearchParams, usePathname, useRouter } from "next/navigation";
import TripPlan from "@/components/layout/TripPlan";
import { useSession } from "next-auth/react";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { useToast } from "@/components/ui/use-toast";
import { 
  Plane, 
  Calendar, 
  Users, 
  MapPin,
  Plus,
  Loader2
} from "lucide-react";

export default function Page() {
  const { toast } = useToast();
  const searchParams = useSearchParams();
  const pathname = usePathname();
  const router = useRouter();
  const { data: session } = useSession();

  const [loading, setLoading] = useState(true);
  const [tripPlanId, setTripPlanId] = useState(searchParams.get("tripPlanId") || "");
  const [tripPlans, setTripPlans] = useState([]);
  const [selectedTripPlan, setSelectedTripPlan] = useState(null);
  const [savingInProgress, setSavingInProgress] = useState(false);

  // Update URL with the tripPlanId
  const updateURL = useCallback(() => {
    const params = new URLSearchParams(searchParams.toString());
    if (tripPlanId) {
      params.set("tripPlanId", tripPlanId);
      router.replace(`${pathname}?${params.toString()}`);
    }
  }, [tripPlanId, pathname, router, searchParams]);

  // Fetch trip plans using GET API
  const fetchTripPlans = async (email) => {
    setLoading(true);
    try {
      const response = await fetch(`/api/trip?email=${email}`);
      const data = await response.json();
      
      if (data.tripPlans && data.tripPlans.length > 0) {
        setTripPlans(data.tripPlans);
        
        // If there's a tripPlanId in the URL, find and select that plan
        const urlTripPlanId = searchParams.get("tripPlanId");
        if (urlTripPlanId) {
          const selectedPlan = data.tripPlans.find(plan => plan.id === urlTripPlanId);
          if (selectedPlan) {
            setSelectedTripPlan(selectedPlan);
            setTripPlanId(urlTripPlanId);
          }
        } else {
          // If no tripPlanId in URL, select the first plan
          setSelectedTripPlan(data.tripPlans[0]);
          setTripPlanId(data.tripPlans[0].id);
        }
      }
    } catch (error) {
      console.error("Error fetching trip plans:", error);
      toast({
        variant: "destructive",
        title: "Error",
        description: "Failed to fetch trip plans. Please try again.",
      });
    } finally {
      setLoading(false);
    }
  };

  // Save trip plan from localStorage
  const saveTripPlan = async () => {
    const tripPlanData = localStorage.getItem("tripPlan");

    // Return early if any of these conditions are met
    if (!tripPlanData || !session?.user?.email || savingInProgress) {
      return;
    }

    try {
      setSavingInProgress(true);
      setLoading(true);

      const tripPlanObj = JSON.parse(tripPlanData);
      const response = await fetch("/api/trip", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          email: session.user.email,
          tripPlan: tripPlanData,
        }),
      });

      const data = await response.json();
      
      if (data.tripPlan) {
        setTripPlans(prev => [...prev, data.tripPlan]);
        setSelectedTripPlan(data.tripPlan);
        setTripPlanId(data.tripPlan.id);
        
        // Remove from localStorage only after successful save
        localStorage.removeItem("tripPlan");
        localStorage.setItem("tripPlanPreview", JSON.stringify(data.tripPlan.data));
        
        toast({
          title: "Trip Saved Successfully!",
          description: `Your trip "${tripPlanObj.trip_name}" has been saved.`,
        });
      }
    } catch (error) {
      console.error("Error saving trip plan:", error);
      toast({
        variant: "destructive",
        title: "Error",
        description: "Failed to save trip plan. Please try again.",
      });
    } finally {
      setLoading(false);
      setSavingInProgress(true); // Keep this true to prevent future save attempts
    }
  };

  // Handle trip selection
  const handleTripSelection = useCallback((trip) => {
    setTripPlanId(trip.id);
    setSelectedTripPlan(trip);
    localStorage.setItem("tripPlanPreview", JSON.stringify(trip.data));
    updateURL();
  }, [updateURL]);

  // Initial fetch
  useEffect(() => {
    if (session?.user?.email) {
      fetchTripPlans(session.user.email);
    }
  }, [session?.user?.email]);

  // Save trip plan
  useEffect(() => {
    if (session?.user?.email && !savingInProgress) {
      saveTripPlan();
    }
  }, [session?.user?.email, savingInProgress]);

  // Update URL when tripPlanId changes
  useEffect(() => {
    updateURL();
  }, [updateURL]);

  // Force re-render of TripPlan component when selectedTripPlan changes
  useEffect(() => {
    if (selectedTripPlan) {
      const element = document.querySelector('.trip-plan-container');
      if (element) {
        element.key = selectedTripPlan.id;
      }
    }
  }, [selectedTripPlan]);

  return (
    <div className="flex flex-col p-8 space-y-6">
      <div className="flex justify-between items-center">
        <div className="space-y-1">
          <h1 className="text-3xl font-bold tracking-tight">My Trip Plans</h1>
          <p className="text-sm text-muted-foreground">
            Manage and view your travel itineraries
          </p>
        </div>
        <Button asChild>
          <a href="/" className="flex items-center gap-2">
            <Plus size={16} /> Create New Trip
          </a>
        </Button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Trip List Section */}
        <div className="lg:col-span-1">
          {loading ? (
            <div className="flex justify-center items-center h-40">
              <Loader2 className="h-8 w-8 animate-spin" />
            </div>
          ) : tripPlans.length > 0 ? (
            <ScrollArea className="h-[calc(100vh-12rem)]">
              <div className="space-y-4">
                {tripPlans.map((trip) => (
                  <Card 
                    key={trip.id}
                    className={`cursor-pointer m-2 hover:shadow-lg transition-all ${
                      selectedTripPlan?.id === trip.id ? 'ring-1 border-violet-800 bg-violet-100 m-2 ring-primary' : ''
                    }`}
                    onClick={() => handleTripSelection(trip)}
                  >
                    <CardHeader>
                      <CardTitle className="flex items-start gap-2">
                        <Plane className="h-5 w-5 mt-1 flex-shrink-0" />
                        <span className="line-clamp-2">{trip?.data?.trip_name}</span>
                      </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-2">
                      {trip?.data && (
                        <>
                          <div className="flex items-center gap-2 text-sm text-muted-foreground">
                            <Calendar className="h-4 w-4" />
                            <span>{trip.data.journeyDate}</span>
                            <span>•</span>
                            <Users className="h-4 w-4" />
                            <span>{trip.data.people} people</span>
                          </div>
                          <div className="flex items-center gap-2 text-sm text-muted-foreground">
                            <MapPin className="h-4 w-4" />
                            <span>{trip.data.origin} to {trip.data.destination}</span>
                          </div>
                        </>
                      )}
                    </CardContent>
                  </Card>
                ))}
              </div>
            </ScrollArea>
          ) : (
            <Card>
              <CardContent className="flex flex-col items-center justify-center h-40 space-y-4">
                <p className="text-muted-foreground text-center">
                  No trips found. Get started by joining a public trip or creating your own!
                </p>
                <div className="flex gap-4">
                  <Button variant="outline" asChild>
                    <a href="/public-trip">Browse Public Trips</a>
                  </Button>
                  <Button asChild>
                    <a href="/">Create New Trip</a>
                  </Button>
                </div>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Trip Details Section */}
        <div className="lg:col-span-2">
          {selectedTripPlan ? (
            <ScrollArea className="h-[calc(100vh-12rem)]">
              <div className="trip-plan-container" key={selectedTripPlan.id}>
                <TripPlan tripPlan={selectedTripPlan} />
              </div>
            </ScrollArea>
          ) : (
            <Card>
              <CardContent className="flex flex-col items-center justify-center h-40">
                <p className="text-muted-foreground">
                  Select a trip plan to view details
                </p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}