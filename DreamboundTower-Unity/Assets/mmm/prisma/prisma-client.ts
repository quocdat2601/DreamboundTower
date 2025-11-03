import { PrismaClient, Prisma, Photo } from '@prisma/client'; 
// import { PrismaVectorStore } from "@langchain/community/vectorstores/prisma";
// import { GoogleGenerativeAIEmbeddings  } from "@langchain/google-genai";

const prismaClientSingleton = () => {
  return new PrismaClient();
};

declare global {
  var prisma: undefined | ReturnType<typeof prismaClientSingleton>;
}

const prisma = globalThis.prisma ?? prismaClientSingleton();



if (process.env.NODE_ENV !== 'production') globalThis.prisma = prisma;


// export const photoStore = PrismaVectorStore.withModel<Photo>(prisma).create(
//   new GoogleGenerativeAIEmbeddings(),
//   {
//     prisma: Prisma,
//     tableName: "Photo",
//     vectorColumnName: "vector",
//     columns: {
//       contentType: PrismaVectorStore.ContentColumn,
//       id: PrismaVectorStore.IdColumn,
//       location: PrismaVectorStore.ContentColumn, 
//       caption: PrismaVectorStore.ContentColumn,
//     },
//     filter: {
//       origin: {
//         equals: "tripPlanId",
//       },
//     },
//   }
// );



export default prisma;