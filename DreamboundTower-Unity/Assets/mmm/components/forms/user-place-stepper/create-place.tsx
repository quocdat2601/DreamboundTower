"use client";

import React, { useState } from "react";
import { useRouter } from "next/navigation";
import { AlertTriangle, Trash2 } from "lucide-react";
import { cn } from "@/lib/utils";
import { Heading } from "@/components/ui/heading";
import { Separator } from "@/components/ui/separator";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";
import MapSelection from "@/components/layout/MapSelection";
import { trpc } from "@/utils/trpc";
import { useSession } from "next-auth/react";
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { MapPin, Bell, Satellite } from 'lucide-react';
import { toast, useToast } from "@/components/ui/use-toast";
 

const DataDisplay = ({ formData }) => {
  if (!formData) return null;

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center">
            <MapPin className="mr-2" /> Location Information
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p><strong>Name:</strong> {formData.name || 'N/A'}</p>
          <p><strong>Latitude:</strong> {formData.latitude}</p>
          <p><strong>Longitude:</strong> {formData.longitude}</p>
        </CardContent>
      </Card>

      {
        // Display GeoJSON data if available
        formData.geojson && (
          <Card>
        <CardHeader>
          <CardTitle className="flex items-center">
            <MapPin className="mr-2" /> GeoJSON Data
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p><strong>Type:</strong> {formData.geojson.type}</p>
          <p><strong>Geometry Type:</strong> {formData.geojson.geometry.type}</p>
          <details>
            <summary className="cursor-pointer">View Coordinates</summary>
            <pre className="mt-2 p-2 bg-gray-100 rounded-md text-xs overflow-auto">
              {JSON.stringify(formData.geojson.geometry.coordinates, null, 2)}
            </pre>
          </details>
        </CardContent>
      </Card>
        )
      }

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center">
            <Bell className="mr-2" /> Notifications
          </CardTitle>
        </CardHeader>
        <CardContent>
          {formData.notifications.map((notification, index) => (
            <div key={index} className="mb-4 last:mb-0">
              <p className="flex items-center">
                <Satellite className="mr-2" /> <strong>{notification.satellite}</strong>
              </p>
              <p><strong>Notify Before:</strong> {notification.notifyBefore} days</p>
              <p><strong>Notify Via:</strong> {notification.notifyIn}</p>
              {notification.smsNumber && <p><strong>SMS Number:</strong> {notification.smsNumber}</p>}
              {notification.email && <p><strong>Email:</strong> {notification.email}</p>}
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  );
};

 