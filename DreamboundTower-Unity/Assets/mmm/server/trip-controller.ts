/* eslint-disable no-console */
// eslint-disable-next-line import/no-unresolved
import prisma from "@/prisma/prisma-client";
import { TRPCError } from "@trpc/server";
import { FilterQueryInput } from "./trip-schema";  
import NodeCache from "node-cache"; 
import { trpc } from "@/utils/trpc";
 
const cache = new NodeCache({ stdTTL: 60 });

  

export const getTripHandler = async ({ filterQuery }: { filterQuery: any }) => {
  try {
    
    const { tripPlanId } = filterQuery;

    const cachedResult = cache.get(tripPlanId);
  
    if (cachedResult) {
      console.log("Cache hit: sending cached result");
      return cachedResult;
    }else{
      console.log("Cache miss: fetching from database");
    }

 
    const result = await prisma.tripPlan.findUnique({
      where: {
        id: tripPlanId,
      },
      include: {
        members : true,
        notifications: true,
      },
    });
    cache.set(tripPlanId, result); 
    return result;

  } catch (err: any) {
    console.error(err);
    throw new TRPCError({
      code: "INTERNAL_SERVER_ERROR",
      message: err.message,
    });
  }
}




