import json


def extract_json(input):
    try:
        return json.loads(input)
    except json.JSONDecodeError as e:
        try:
            input = input.replace("```json", '')
            input = input.replace("```", '')
            return json.loads(input)
        except json.JSONDecodeError as e:
            print("Error decoding JSON:", str(e))
            return input