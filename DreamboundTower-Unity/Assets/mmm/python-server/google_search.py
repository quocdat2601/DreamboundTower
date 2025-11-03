import requests
import json
import os

def getHotels(place):
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
    print(response.text)
    return response.text