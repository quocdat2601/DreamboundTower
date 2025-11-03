from langchain_openai import ChatOpenAI
from langchain_core.prompts import ChatPromptTemplate
from langchain_core.output_parsers import StrOutputParser

import streamlit as st
from dotenv import load_dotenv
from utility import extract_json
import json
import os
import random
load_dotenv()


## Prompt Template
trip_data_extract_prompt=ChatPromptTemplate.from_messages(
   [ ("system","""you are good at extracting data from natural language. your response will be in pure JSON format. Example Response Structure:
        'origin': 'Dhaka',
        'destination': 'Sajek',
        'days': 5,
        'max_budget': 5000,
        'people': 2,
        'preferences': 'hill'
     """),
     ("user","extract the data from this input: {input}")]
)

trip_plan_prompt=ChatPromptTemplate.from_messages(
    [
       ("system", """You are a good trip planner specialized in Bangladeshi trips and provide accurate data. You will provide the response in pure JSON format. Example Response Structure:
            'trip_name': 'Dhaka to Sajek 5 days trip with 2 people to enjoy the hill',
            'origin': 'Dhaka',
            'destination': 'Sajek',
            'days': 5,
            'max_budget': 5000,
            'people': 2,
            'preferences': 'hill',
            'checkpoints': [ 
                'origin': 
                    'location': 'Dhaka',
                    'latitude': 23.8103,
                    'longitude': 90.4125
                ,
                'destination': 
                    'location': 'Sylhet',
                    'latitude': 24.8949,
                    'longitude': 91.8687
                ,
                'logistics': 
                    'departure_time': '06:00 AM',
                    'arrival_time': '12:00 PM',
                    'tips': 'Take an early morning bus from Dhaka to Sylhet to enjoy the scenic beauty along the way.'
                
                ,
                
                'origin': 
                    'location': 'Sylhet',
                    'latitude': 24.8949,
                    'longitude': 91.8687
                ,
                'destination': 
                    'location': 'Sajek',
                    'latitude': 23.3814,
                    'longitude': 92.2938
                ,
                'logistics': 
                    'departure_time': '07:00 AM',
                    'arrival_time': '03:00 PM',
                    'tips': 'Hire a local jeep from Sylhet to Sajek for a thrilling ride through the hills.'
                
                
            ],
            'food': 
                '1': 
                'breakfast':
                            'name': 'BFC',
                            'type': 'Fast Food',
                            'cost': 400
                        ,
                'launch':
                            'name': 'Local Restaurants',
                            'type': 'Bengali Cuisine',
                            'cost': 250
                        ,
                'dinner':
                        'name': 'Continental Hotel',
                        'type': 'Chinese Dish',
                        'cost': 550
                        
                ,
            ,
            'accommodation': 
                '1': 
                'location': 'Sylhet',
                'type': 'Budget hotel',
                'cost_per_night': 800
                ,
                '2': 
                'location': 'Sajek',
                'type': 'Cottage',
                'cost_per_night': 1000
                ,
                '3': 
                'location': 'Sajek',
                'type': 'Cottage',
                'cost_per_night': 1000
                ,
                '4': 
                'location': 'Sylhet',
                'type': 'Budget hotel',
                'cost_per_night': 800
                
            ,
            'budget': 
                'total_budget': 5100,
                'breakdown': 
                'transportation': 1500,
                'food': 1500,
                'accommodation': 2000,
                'miscellaneous': 100
            """),
       ("user","create a trip plan for {origin} to {destination} for {days} days with {people} people and max budget {max budget} taka. preferences are {preferences}")
    ]
)

# openAI LLm 
llm_gpt = ChatOpenAI(
    model="gpt-4o",
    temperature=0,
    max_tokens=None,
    timeout=None,
    max_retries=2,
)
output_parser=StrOutputParser()
extract_chain=trip_data_extract_prompt|llm_gpt|output_parser
trip_chain=trip_plan_prompt|llm_gpt|output_parser



## streamlit framework

st.title('Easy Trip Planner')
input=st.text_input("Enter your trip details")
# origin=st.text_input("Origin")
# destination=st.text_input("Destination")
# days=st.number_input("Days",min_value=1)
# max_budget=st.number_input("Max Budget",min_value=1000)
# people=st.number_input("People",min_value=1)
# preferences=st.text_input("Preferences")
submit=st.button("Submit")

if submit:
    trip_data = extract_chain.invoke({'input':input})
    st.write(trip_data)
    print(trip_data)
    trip_data_json = extract_json(trip_data)


    origin = trip_data_json['origin']
    destination = trip_data_json['destination']
    days = trip_data_json['days']
    max_budget = trip_data_json["max_budget"]
    people = trip_data_json["people"]
    preferences = trip_data_json['preferences']

    if origin and destination and days and max_budget and people and preferences:
        # Ensure the trips directory exists
        os.makedirs('trips', exist_ok=True)

        # Generate the story
        trip_plan = trip_chain.invoke({'origin':origin,'destination':destination,'days':days,'max budget':max_budget,'people':people,'preferences':preferences})
        # cover_image_url = generate_image(input_text)

        # Save the story to a JSON file
        trip_data = trip_plan
        try:
            trip_name = trip_data['trip_name']
        except:
            trip_name = f"{origin} to {destination} {days} days trip with {people} people to enjoy the {preferences}"
        trip_name = trip_name.replace(' ', '_')

        random_number = random.randint(1, 1000)
        file_path = os.path.join('trips', f"{trip_name}_{random_number}.json")
        with open(file_path, 'w', encoding='utf-8') as f:
            json.dump(trip_data, f, ensure_ascii=False, indent=4)

        st.write(trip_plan)


# # in the sidebar load all the stories
with st.sidebar:
    st.title('Recent Trips')
    trips = os.listdir('trips')
    for trip_file in trips:
        trip_file_path = os.path.join('trips', trip_file)
        with open(trip_file_path, 'r', encoding='utf-8') as f:
            trip_data = json.load(f)
            with st.expander(f"{trip_file.replace('_', ' ').replace('.json', '').capitalize()}"):
                st.write(trip_data)
