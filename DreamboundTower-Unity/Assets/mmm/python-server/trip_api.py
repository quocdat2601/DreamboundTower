from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from langchain_openai import ChatOpenAI
from langchain_core.prompts import ChatPromptTemplate
from langchain_core.output_parsers import StrOutputParser
from jsonOutputParser import JSONOutputParser
from langchain_core.runnables import RunnablePassthrough
from langchain.tools import tool
from langserve import add_routes
import uvicorn
import requests
import json

import streamlit as st
from dotenv import load_dotenv
from utility import extract_json
import json
import os
import random
load_dotenv()

# todays date
from datetime import date
today = date.today().strftime("%d/%m/%Y")


app=FastAPI(
    title="Easy Trip API Server",
    version="1.0",
    description="API Server of Easy Trip Planner",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@tool
def get_hotels_tool(place: str) -> str:
    """Search for hotels in Bangladesh for this specific place"""
    url = "https://google.serper.dev/places"
    
    payload = json.dumps({
        "q": f"{place} hotel list bangladesh",
        "gl": "bd"
    })
    headers = {
        'X-API-KEY': os.getenv('GOOGLE_SEARCH_API_KEY'),
        'Content-Type': 'application/json'
    }
    
    response = requests.request("POST", url, headers=headers, data=payload)
    return response.text

## Prompt Template
trip_data_extract_prompt=ChatPromptTemplate.from_messages(
   [ ("system",f"""you are good at extracting data from natural language. your response will be in pure JSON format. Example Response Structure:
        'origin': 'Dhaka',
        'destination': 'Sajek',
        'days': 5,
        'budget': 5000,
        'people': 2,
        'preferences': 'hill',
        'tripType': 'oneWay' | 'roundTrip' | 'multiCity',
        'journeyDate': {today},
        'travelClass': 'economy' | 'business' | 'luxury',
     """),
     ("user","extract the data from this input: {trip_text}. default value for tripType is oneWay, journeyDate is today, travelClass is economy. all data will be in string. dont use null value for any field,use blank string if not available.")]
)

trip_plan_prompt=ChatPromptTemplate.from_messages(
    [
       ("system", f"""You are a good trip planner specialized in Bangladeshi trips and provide accurate data. You will provide the response in pure JSON format. Example Response Structure:
            'trip_name': 'Dhaka to Sajek 5 days trip with 2 people to enjoy the hill',
            'origin': 'Dhaka',
            'destination': 'Sajek',
            'days': '5',
            'budget': '5000',
            'people': '2',
            'preferences': 'hill',
            'tripType': 'oneWay',
            'journeyDate': {today},
            'travelClass': 'economy',
            'checkpoints': [ 
                'origin': 
                    'location': 'Dhaka',
                    'latitude': '23.8103',
                    'longitude': '90.4125'
                ,
                'destination': 
                    'location': 'Sylhet',
                    'latitude': '24.8949',
                    'longitude': '91.8687'
                ,
                'logistics': 
                    'departure_time': '06:00 AM',
                    'arrival_time': '12:00 PM',
                    'tips': 'Take an early morning bus from Dhaka to Sylhet to enjoy the scenic beauty along the way.'
                
                ,
                
                'origin': 
                    'location': 'Sylhet',
                    'latitude': '24.8949',
                    'longitude': '91.8687'
                ,
                'destination': 
                    'location': 'Sajek',
                    'latitude': '23.3814',
                    'longitude': '92.2938'
                ,
                'logistics': 
                    'departure_time': '07:00 AM',
                    'arrival_time': '03:00 PM',
                    'tips': 'Hire a local jeep from Sylhet to Sajek for a thrilling ride through the hills.'
                
                
            ],
            'food': 
                '1': 
                'breakfast':
                            "title": "Montana Restaurant",
                            "address": "97MV M73, Sajek",
                            "latitude": 23.384133499999997,
                            "longitude": 92.2931734,
                            "rating": 4.1,
                            "ratingCount": 168,
                            "category": "Restaurant",
                            "phoneNumber": "01879-086308",
                            "cid": "2530207382346985444",
                            "website": "https://montanarestaurant.com/"
                            "cost": "200"
                        ,
                'launch':
                            "title": "Kashbon Restaurant",
                            "address": "97PR 8M4, Z1603, Sajek",
                            "latitude": 23.385738699999997,
                            "longitude": 92.2916992,
                            "rating": 4.4,
                            "ratingCount": 40,
                            "category": "Restaurant",
                            "phoneNumber": "01648-676555",
                            "website": "http://facebook.com/kashbonsajek",
                            "cid": "13782344097276060861"
                            'cost': '250'
                        ,
                'dinner':
                        "title": "Sajek Chilekotha Restaurant",
                        "address": "Dighinala - Sajek Rd, Sajek",
                        "latitude": 23.383672400000002,
                        "longitude": 92.2933472,
                        "rating": 4,
                        "ratingCount": 25,
                        "category": "Restaurant",
                        "phoneNumber": "01862-000810",
                        "cid": "14729805586170578036"
                        'cost': '550'
                        
                ,
                 '2': 
                'breakfast':
                            "title": "Montana Restaurant",
                            "address": "97MV M73, Sajek",
                            "latitude": 23.384133499999997,
                            "longitude": 92.2931734,
                            "rating": 4.1,
                            "ratingCount": 168,
                            "category": "Restaurant",
                            "phoneNumber": "01879-086308",
                            "cid": "2530207382346985444",
                            "website": "https://montanarestaurant.com/"
                            "cost": "200"
                        ,
                'launch':
                            "title": "Kashbon Restaurant",
                            "address": "97PR 8M4, Z1603, Sajek",
                            "latitude": 23.385738699999997,
                            "longitude": 92.2916992,
                            "rating": 4.4,
                            "ratingCount": 40,
                            "category": "Restaurant",
                            "phoneNumber": "01648-676555",
                            "website": "http://facebook.com/kashbonsajek",
                            "cid": "13782344097276060861"
                            'cost': '250'
                        ,
                'dinner':
                        "title": "Sajek Chilekotha Restaurant",
                        "address": "Dighinala - Sajek Rd, Sajek",
                        "latitude": 23.383672400000002,
                        "longitude": 92.2933472,
                        "rating": 4,
                        "ratingCount": 25,
                        "category": "Restaurant",
                        "phoneNumber": "01862-000810",
                        "cid": "14729805586170578036"
                        'cost': '550'
                        
                ,
            ,
            'accommodation': 
                '1': 
                "title": "D'more Sajek Valley Hotel & Resort",
                "address": "97PR GM6, Sajek",
                "latitude": 23.386285899999997,
                "longitude": 92.2916461,
                "rating": 4.6,
                "ratingCount": 18,
                "category": "Hotel",
                "phoneNumber": "01844-114834",
                "website": "http://www.hoteldmore.com/",
                "cid": "12290140610591984361"
                ,
                '2': 
                "title": "Chander Bari Resort",
                "address": "Sajek 4590",
                "latitude": 23.3826789,
                "longitude": 92.2942886,
                "rating": 3.9,
                "ratingCount": 51,
                "category": "Resort hotel",
                "phoneNumber": "01880-088272",
                "cid": "13340532329700638370"
                ,
                '3': 
                "title": "Gospel Resort",
                "address": "Valley, Sajek 4590",
                "latitude": 23.384605099999998,
                "longitude": 92.29249159999999,
                "rating": 3.8,
                "ratingCount": 52,
                "category": "Resort hotel",
                "phoneNumber": "01400-676763",
                "cid": "11821975963652076579"
                ,
                '4': 
                "title": "Fodang Thang Resort",
                "address": "Sajek",
                "latitude": 23.382413699999997,
                "longitude": 92.2943782,
                "rating": 4.5,
                "ratingCount": 63,
                "category": "Resort hotel",
                "phoneNumber": "01817-722572",
                "website": "https://fodangthangresort.com/",
                "cid": "10525083696798722411"
                
            ,
            'budget': 
                'total': '5100',
                'breakdown': 
                'transportation': '1500',
                'food': '1500',
                'accommodation': '2000',
                'miscellaneous': '100'
            """),
       ("user","create a trip plan for {origin} to {destination} for {days} days with {people} people and budget {budget} taka. preferences are {preferences}. tripType is {tripType} and journeyDate is {journeyDate}. travelClass is {travelClass} and must use these hotel search results: {hotels} for accommodation. must use hotels from {restaurants} list also all days should have breakfast launch and dinner minimum. determine the cost based on the rating as bangladeshi standard, if rating not given determine by own. default value for tripType is oneWay, journeyDate is today, travelClass is economy. all values will in string. use blank instead of null")
    ]
)

qna_prompt=ChatPromptTemplate.from_messages(
    [
       ("system", "You are a QnA model specialized in Bangladeshi trips and provide accurate data based on the data given to you"),
       ("user","question: {question} . context: {context}")
    ]
)

# openAI LLm 
llm_gpt = ChatOpenAI(
    model="gpt-4o",
    temperature=0,
    max_tokens=None,
    timeout=None,
    max_retries=2
)
output_parser=StrOutputParser()
json_output_parser=JSONOutputParser()


add_routes(
    app,
    trip_data_extract_prompt|llm_gpt|json_output_parser,
    path="/extract_trip_data",
)

add_routes(
    app,
    trip_plan_prompt|llm_gpt|json_output_parser,
    path="/trip_plan",
)

add_routes(
    app,
    qna_prompt|llm_gpt|output_parser,
    path="/qna",
)


if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=5000)