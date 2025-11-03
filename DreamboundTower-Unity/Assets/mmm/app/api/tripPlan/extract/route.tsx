import { NextResponse } from 'next/server'
const axios = require('axios');
 

const pythonServer = process.env.PYTHON_SERVER_URL;



 
export async function GET(request: Request) {
    const { searchParams } = new URL(request.url);
    const trip_text = searchParams.get('text');
    console.log(trip_text);

    try {
        const response = await axios.post(`${pythonServer}/extract_trip_data/invoke`, {
            input: {
            trip_text
            }
        });

        return NextResponse.json(response.data);
    } catch (error) {
        return NextResponse.json({ error: 'Failed extract parameters from user text' }, { status: 500 });
    }
}
