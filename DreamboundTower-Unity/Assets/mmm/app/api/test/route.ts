import { NextRequest, NextResponse } from 'next/server';
 
// // Initialize clients
// const openai = new OpenAI({
//   apiKey: process.env.OPENAI_API_KEY,
// });

// const embeddings = new OpenAIEmbeddings({
//   openAIApiKey: process.env.OPENAI_API_KEY,
// });

// const chromaClient = new ChromaClient({
//   path: process.env.CHROMA_DB_URL || "http://localhost:8000"
// });

// GET endpoint to fetch images for a trip
export async function GET(request: NextRequest) {
  
    return NextResponse.json(
      {
        images: "fgerswg"
      },
      {
        status: 200,
      }
    );
}
 