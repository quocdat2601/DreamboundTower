import requests

def generate_image(prompt, aspect_ratio="1:1"):
    url = "https://api.limewire.com/api/image/generation"

    payload = {
        "prompt": prompt,
        "aspect_ratio": aspect_ratio
    }

    headers = {
        "Content-Type": "application/json",
        "X-Api-Version": "v1",
        "Accept": "application/json",
        "Authorization": f"Bearer lmwr_sk_gSbdYIhat8_FYxOdenE3dabzBmanjJZ9K5It5jLIR6O7SUu3"
    }

    response = requests.post(url, json=payload, headers=headers)
    response_data = response.json()
    print(response_data)
    if response_data['status'] == 'COMPLETED' and 'data' in response_data and len(response_data['data']) > 0:
        return response_data['data'][0]['asset_url']
    else:
        return None