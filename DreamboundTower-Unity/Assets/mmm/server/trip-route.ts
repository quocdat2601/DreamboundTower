 
import { t } from '@/utils/trpc-server';
import { getTripHandler } from './trip-controller';
import { filterQuery } from './trip-schema';
 
const tripRouter = t.router({
  getTrip: t.procedure
    .input(filterQuery)
    .query(({ input }) => getTripHandler({ filterQuery: input })),
});




export default tripRouter;
