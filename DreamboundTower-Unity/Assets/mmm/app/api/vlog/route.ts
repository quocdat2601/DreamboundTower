import { NextRequest, NextResponse } from "next/server";
import prisma from "@/prisma/prisma-client";
import { OpenAI } from "openai";

import * as lancedb from "@lancedb/lancedb";

const openai = new OpenAI({
  apiKey: process.env.OPENAI_API_KEY,
});

 
async function generateEmbedding(text) {
  const response = await openai.embeddings.create({
    model: "text-embedding-ada-002",
    input: text,
  });
  return response.data[0].embedding;
}

// GET Endpoint
export async function GET(request: NextRequest) {
  try {
    const searchParams = new URL(request.url).searchParams;
    const tripPlanId = searchParams.get("tripPlanId");
    const query = searchParams.get("query");
     
    if (!tripPlanId) {
      return NextResponse.json(
        { error: "tripPlanId is required" },
        { status: 400 },
      );
    }
 
    const vlogs = await prisma.vlog.findMany({
      where: {
        tripPlanId: tripPlanId,
        content: {
          contains: query as string,
          mode: 'insensitive', // Optional: Makes the search case-insensitive
        },
      },
      orderBy: {
        createdAt: 'desc',
      },
      include: {
        author: true,
      },
    });

    return NextResponse.json({ vlogs, count: vlogs.length });
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
    const { tripPlanId, query } = body;

    if ( !tripPlanId) {
      return NextResponse.json(
        { error: "Missing required fields" },
        { status: 400 },
      );
    }

    const tripPlan = await prisma.tripPlan.findUnique({
      where: { id: tripPlanId },
      include: {
        members: {
          select: {
            name: true,
          },
        }
      },
    });  
  
    const uri = "/tmp/lancedb/";
    const db = await lancedb.connect(uri);
  
    const tableNames = await db.tableNames();

    const queryEmbedding = await generateEmbedding(query);
    console.log(tableNames);
    let images = [];
    if(tableNames.includes("image")) { 
      const _tbl = await db.openTable("image");
            images = (await _tbl.search(queryEmbedding).limit(30).toArray())
        .filter((image) => image.tripPlanId === tripPlanId).slice(0, 6);
    }


    const completion = await openai.chat.completions.create({
      model: "gpt-4o-mini",
      messages: [
          { role: "system", content: "You are a travel vlog subtitle maker" },
          {
              role: "user",
              content: ` Suppose You are a travel vlogger subtitle Maker. Write a captivating travel vlog subtitle about your recent trip titled: "${tripPlan?.data?.trip_nime}". Include info about your experience, the journey, and the group members: ${tripPlan?.members.map(member => member.name).join(", ")}. 
      
              The trip information is provided below in JSON format: ${JSON.stringify(tripPlan)}.
        
              User Query: ${query},
              Make the subtile as so that It cant exceeds 30 seconds. 
              Your can generate maximum 5 s for each Photo `,
          },
      ],
  });
   
    const contents = completion.choices[0].message.content;

    const imageArray = images.map(image => image.url);
  
    return NextResponse.json({ success: true, content : contents, images:imageArray}, { status: 200 });

  } catch (error) {
    console.error(`Error in POST /api/vlog: ${error.message}`);
    return NextResponse.json({ error: "Internal Server Error" }, { status: 500 });
  }
}

  

  