import { NextRequest, NextResponse } from "next/server";
import prisma from "@/prisma/prisma-client";
import { OpenAI } from "openai";

import * as lancedb from "@lancedb/lancedb";
import * as arrow from "apache-arrow";
import { feature } from "@turf/turf";

// Initialize OpenAI client
const openai = new OpenAI({
  apiKey: process.env.OPENAI_API_KEY,
});

// Helper to generate embeddings using OpenAI
async function generateEmbedding(text) {
  const response = await openai.embeddings.create({
    model: "text-embedding-ada-002",
    input: text,
  });
  return response.data[0].embedding;
}

interface Trip {
  tripPlanId: string;
  [key: string]: any;
}

const filterAndRemoveVectorProperties = (
  trips: Trip[],
  tripPlanId: string,
): Trip[] => {
  return trips
    .filter((trip) => trip.tripPlanId === tripPlanId)
    .map((trip) => {
      const { vector, ...rest } = trip;
      return rest;
    });
};

// Semantic Search using Redis
async function searchByEmbedding(
  queryEmbedding: any,
  tripPlanId: string,
  n = 30,
) {
  const uri = "/tmp/lancedb/";
  const db = await lancedb.connect(uri);

  const tableNames = await db.tableNames();
  console.log(tableNames);

  const filter = `tripPlanId = ${tripPlanId}`;

  console.log(filter);

  if (tableNames.includes("image")) {
    const _tbl = await db.openTable("image");
    if (queryEmbedding.length === 0) {
      const _res = _tbl.query().limit(n).toArray();
      //    const _res = _tbl.query().where(filter).limit(n).toArray();
      return _res;
    }
    const _res = _tbl.vectorSearch(queryEmbedding).limit(n).toArray();
    return _res;
  } else {
    return [];
  }
}

// GET Endpoint
export async function GET(request: NextRequest) {
  try {
    const searchParams = new URL(request.url).searchParams;
    const tripPlanId = searchParams.get("tripPlanId");
    const similarSearchQuery = searchParams.get("similarSearchQuery");

    if (!tripPlanId) {
      return NextResponse.json(
        { error: "tripPlanId is required" },
        { status: 400 },
      );
    }

    let images = [];

    if (similarSearchQuery) {
      const searchEmbedding = await generateEmbedding(similarSearchQuery);
      images = await searchByEmbedding(searchEmbedding, tripPlanId);
    } else {
      images = await searchByEmbedding([], tripPlanId);
    }

    const filteredTrips = filterAndRemoveVectorProperties(images, tripPlanId);

    return NextResponse.json({ filteredTrips, count: filteredTrips.length });
  } catch (error) {
    console.error("Error fetching images:", error);
    return NextResponse.json(
      { error: "Failed to fetch images" },
      { status: 500 },
    );
  }
}

// POST Endpoint
export async function POST(request: NextRequest) {
    
  try {
    const body = await request.json();
    const { url, email, tripPlanId, name, location, time, device } = body;

    if (!url || !email || !tripPlanId) {
      return NextResponse.json(
        { error: "Missing required fields" },
        { status: 400 },
      );
    }

    const tripPlan = await prisma.tripPlan.findUnique({
      where: { id: tripPlanId },
    });

    const visionResponse = await openai.chat.completions.create({
        model: "gpt-4o-mini",
        messages: [
          {
            role: "user",
            content: [
              {
                type: "text",
                text: "Extract all Details of this image and give a comma separated list of features in a sentence."
              },
              {
                type: "image_url",
                image_url: {
                  "url": url
                }
              }
            ]
          }
        ],
    });

    const imageFeatures = visionResponse.choices[0].message.content;
    const documentText = `
      Trip Name: ${tripPlan?.data?.trip_name?.name || "Unknown"}
      Origin: ${tripPlan?.data?.trip_name?.origin || "Unknown"}
      Destination: ${tripPlan?.data?.trip_name?.destination || "Unknown"}
      Location: ${location}
      Features: ${imageFeatures}
    `;

    const embedding = await generateEmbedding(documentText);

    const uri = "/tmp/lancedb/";
    const db = await lancedb.connect(uri);

    const tableNames = await db.tableNames();
    console.log(tableNames);

    if (tableNames.includes("image")) {
      // await db.dropTable("image");
      const _tbl = await db.openTable("image");

      const data = [
        {
          vector: embedding,
          tripPlanId: tripPlanId,
          name: name,
          feature: imageFeatures,
          location: location,
          time: time,
          url : url,
          device: device,
        },
      ];
      await _tbl.add(data);
      console.log("Data added to existing table");
    } else {
      const tbl = await db.createTable(
        "image",
        [
          {
            vector: embedding,
            tripPlanId: tripPlanId,
            name: name,
            url: url,
            feature: imageFeatures,
            location: location,
            time: time,
            device: device,
          },
        ],
        { mode: "overwrite" },
      );
      //   index of tripPlanId
      await tbl.createIndex("tripPlanId");
      console.log("Table created and data added");
    }

    await prisma.notification.create({
        data: {
          content: `New Photo added by ${name}`,
          tripPlan:{
            connect: {
              id: tripPlanId,
            },
          }
        },
      });

    return NextResponse.json(
      { message: "Image processed successfully", id: tripPlanId },
      { status: 201 },
    );
  } catch (error) {
    console.error("Error processing image:", error);
    return NextResponse.json(
      { error: "Failed to process image" },
      { status: 500 },
    );
  }
}
