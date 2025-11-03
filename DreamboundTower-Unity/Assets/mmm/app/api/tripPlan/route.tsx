import { NextResponse } from 'next/server'
const axios = require('axios');

 
const pythonServer = process.env.PYTHON_SERVER_URL;

 
export async function GET(request: Request) {
    const { searchParams } = new URL(request.url);
    const origin = searchParams.get('origin') || 'Dhaka';
    const destination = searchParams.get('destination') || 'Sylhet';
    const days = searchParams.get('days') || '3';
    const budget = searchParams.get('budget') || '5000';
    const people = searchParams.get('people') || '5';
    const preferences = searchParams.get('preferences') || '';
    const tripType = searchParams.get('tripType') || 'oneWay';
    const journeyDate = searchParams.get('journeyDate') || 'today';
    const travelClass = searchParams.get('travelClass') || 'economy';

    console.log(origin, destination, days, budget, people, preferences, tripType, journeyDate, travelClass);

    try {
        const hotels = await getHotels(destination);
        const hotelsString = JSON.stringify(hotels);
        console.log(hotelsString);

        const restaurants = await getRestaurants(destination);
        const restaurantsString = JSON.stringify(restaurants);
        console.log(restaurantsString);

        const response = await axios.post(`${pythonServer}/trip_plan/invoke`, {
        input: {
            origin,
            destination,
            days,
            budget,
            people,
            preferences,
            tripType,
            journeyDate,
            travelClass,
            hotels: hotelsString,
            restaurants: restaurantsString
            }
        });

        return NextResponse.json(response.data);
    } catch (error) {
        console.log(error);
        return NextResponse.json({ error: 'Failed to fetch trip plan' }, { status: 500 });
    }
}

async function getHotels(place:string) {

    let data = JSON.stringify({
        "q": `residential hotel list near ${place},bangladesh`,
        "gl": "bd"
    });

    let config = {
        method: 'post',
        url: 'https://google.serper.dev/places',
        headers: { 
            'X-API-KEY':process.env.GOOGLE_SEARCH_API_KEY, 
            'Content-Type': 'application/json'
        },
        data: data
    };

    try {
        const response: { data: any } = await axios(config);
        return response.data;
    } catch (error: any) {
        return { error: error.message };
    }
    
}


async function getRestaurants(place:string) {

    let data = JSON.stringify({
        "q": `restaurants list near ${place},bangladesh`,
        "gl": "bd"
    });

    let config = {
        method: 'post',
        url: 'https://google.serper.dev/places',
        headers: { 
            'X-API-KEY': process.env.GOOGLE_SEARCH_API_KEY,
            'Content-Type': 'application/json'
        },
        data: data
    };

    try {
        const response: { data: any } = await axios(config);
        return response.data;
    } catch (error: any) {
        return { error: error.message };
    }
    
}