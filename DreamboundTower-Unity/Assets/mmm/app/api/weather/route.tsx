import { NextResponse } from 'next/server';
const axios = require('axios');

const url = "http://api.weatherapi.com/v1/forecast.json?key="+process.env.WEATHER_API_KEY;

export async function GET(request: Request) {
    const { searchParams } = new URL(request.url);
    const lat = searchParams.get('lat');
    const lon = searchParams.get('lon'); 
    const days = searchParams.get('days') || 7; 

    if (!lat || !lon) {
        return NextResponse.json({ error: 'Latitude and Longitude are required' }, { status: 400 });
    }

    try {
        const response = await axios.get(`${url}&q=${lat},${lon}&days=${days}&aqi=no&alerts=no`);
        const data = response.data;

        // Format the response to include only essential information
        const result = {
            location: {
                name: data.location.name,
                region: data.location.region,
                country: data.location.country,
                lat: data.location.lat,
                lon: data.location.lon,
                localtime: data.location.localtime,
            },
            current: {
                temp_c: data.current.temp_c,
                temp_f: data.current.temp_f,
                condition: data.current.condition.text,
                wind_kph: data.current.wind_kph,
                wind_dir: data.current.wind_dir,
                humidity: data.current.humidity,
                uv: data.current.uv,
            },
            forecast: data.forecast.forecastday.map((day: any) => ({
                date: day.date,
                maxtemp_c: day.day.maxtemp_c,
                mintemp_c: day.day.mintemp_c,
                condition: day.day.condition.text,
                sunrise: day.astro.sunrise,
                sunset: day.astro.sunset,
            }))
        };

        return NextResponse.json(result);
    } catch (error) {
        return NextResponse.json({ error: 'Failed to fetch weather data' }, { status: 500 });
    }
}
