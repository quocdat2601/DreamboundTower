import { NextResponse } from 'next/server';
import prisma from "@/prisma/prisma-client";
export async function GET() {
  return NextResponse.json({ message: 'Hello, World!' });
}

export async function POST(request: Request) {
  const body = await request.json();
  const { user_email, tripPlanId } = body;

  if (!user_email) {
    return NextResponse.json({ error: 'Email is required' }, { status: 400 });
  }

  if (tripPlanId) {
    // If email is not null and tripPlan is not null:
    // Insert the tripPlan to the database with email and return the tripPlanId
    try {
      const newTripPlan = await prisma.tripPlan.update({
        data: {
          members: {
            connect: {
              email: user_email
            }
          },
        },
        where: {
          id : tripPlanId
        }
      });
      return NextResponse.json({
        message: 'User added to trip plan',
        tripPlanId: newTripPlan.id,
      });
    } catch (error) {
      console.error('Error saving trip plan:', error);
      return NextResponse.json({ error: 'Error saving trip plan' }, { status: 500 });
    }
  }
}

 