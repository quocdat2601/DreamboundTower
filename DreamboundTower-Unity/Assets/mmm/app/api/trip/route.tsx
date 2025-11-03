import { NextRequest, NextResponse } from "next/server";
import prisma from "@/prisma/prisma-client";

export async function GET(request: NextRequest) {
  const searchParams = new URL(request.url).searchParams;
  const email = searchParams.get("email");

  const tripPlans = await prisma.tripPlan.findMany({
    where: {
      author: {
        email: email as string,
      },
    },
    include: {
      members: {
        select: {
          email: true,
          name: true,
          image: true,
        },
      }
    },
  });

  return NextResponse.json(
    {
      message: "Trip plan updated/Fetched",
      tripPlans: tripPlans,
    },
    { status: 200 },
  );
}

export async function POST(request: Request) {
  const body = await request.json();
  const { email, tripPlan } = body;

 
  if (!email) {
    return NextResponse.json({ error: "Email is required" }, { status: 400 });
  }

  if (tripPlan && !tripPlan.id) {
    // If email is not null and tripPlan is not null:
    // Insert the tripPlan to the database with email and return the tripPlanId
    try {
      const newTripPlan = await prisma.tripPlan.create({
        data: {
          author: {
            connect: {
              email: email,
            },
          },
          members: {
            connect: {
              email: email,
            },
          },
          data: JSON.parse(tripPlan),
        },
      });
      return NextResponse.json(
        {
          message: "Trip plan updated/Fetched",
          tripPlan: newTripPlan,
        },
        { status: 200 },
      ); 
    } catch (error) {
      console.error("Error saving trip plan:", error);
      return NextResponse.json(
        { error: "Error saving trip plan" },
        { status: 500 },
      );
    } 
}

}

export async function PUT(request: Request) {
  const body = await request.json();
  const { email, tripPlan, tripPlanId } = body;

  if (!email) {
    return NextResponse.json({ error: "Email is required" }, { status: 400 });
  }


  // if user is not in the user table, add the user with passwordHash as password andd admin as role
  const user = await prisma.user.findUnique({
    where: {
      email: email,
    },
  });

  if (!user) {
    await prisma.user.create({
      data: {
        email: email,
        name : email.split("@")[0],
        passwordHash: "password",
      },
    });
  }
 
  if (tripPlan) {
    // If email is not null and tripPlan is not null:
    // Insert the tripPlan to the database with email and return the tripPlanId
    try {
      const newTripPlan = await prisma.tripPlan.update({
        where: {
          id: tripPlanId,
        },
        data: {
          members: {
            connect: {
              email: email,
            },
          },
          data: tripPlan,
        },
      });


      // Add Notification for the trip plan
      await prisma.notification.create({
        data: {
          content: `New member ${user?.name} added to trip plan`,
          tripPlan: {
            connect: {
              id: newTripPlan.id,
            },
          },
        },
      });


      return NextResponse.json({
        message: "Trip plan Updated",
        tripPlanId: newTripPlan.id,
      });
    } catch (error) {
      console.error("Error saving trip plan:", error);
      return NextResponse.json(
        { error: "Error saving trip plan" },
        { status: 500 },
      );
    }
  } else {
    // If email is not null and tripPlan is null:
    // Return the tripPlanId from the database with email
    try {
      const existingTripPlan = await prisma.tripPlan.findFirst({
        where: {
          author: {
            email: email,
          },
        },
      });

      if (existingTripPlan) {
        return NextResponse.json({
          message: "Trip plan found",
          tripPlanId: existingTripPlan.id,
        });
      } else {
        // Create a new trip plan if none exists
        const newTripPlan = await prisma.tripPlan.create({
          data: {
            author:{
              connect : {
                email : email
              }
            },
            tripData: {}, // Initialize with empty data if needed
          },
        });
        return NextResponse.json({
          message: "New trip plan created",
          tripPlanId: newTripPlan.id,
        });
      }
    } catch (error) {
      console.error("Error retrieving trip plan:", error);
      return NextResponse.json(
        { error: "Error retrieving trip plan" },
        { status: 500 },
      );
    }
  }
}
