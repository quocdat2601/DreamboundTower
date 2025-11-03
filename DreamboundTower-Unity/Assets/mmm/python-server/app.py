from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_core.prompts import ChatPromptTemplate
from langchain_core.output_parsers import StrOutputParser
from text2image import generate_image

import streamlit as st
from dotenv import load_dotenv
import json
import os
load_dotenv()


## Prompt Template
prompt=ChatPromptTemplate.from_messages(
    [
        ("system","You are a good story teller. your story are bengali folk tales"),
        ("user","Story topic:{topic}")
    ]
)

# openAI LLm 
llm=ChatGoogleGenerativeAI(model="gemini-1.5-flash")
output_parser=StrOutputParser()
chain=prompt|llm|output_parser



## streamlit framework

st.title('Langchain Story telling App')
input_text=st.text_input("Topic")


if input_text:
    # Ensure the stories directory exists
    os.makedirs('stories', exist_ok=True)

    # Generate the story
    story = chain.invoke({'topic':input_text})
    cover_image_url = generate_image(input_text)

    # Save the story to a JSON file
    story_data = {'topic': input_text, 'story': story, 'cover_image_url': cover_image_url}
    file_path = os.path.join('stories', f"{''.join(filter(str.isalpha, input_text)).lower()}.json")
    with open(file_path, 'w', encoding='utf-8') as f:
        json.dump(story_data, f, ensure_ascii=False, indent=4)

    # show the cover image
    if cover_image_url:
        st.image(cover_image_url, use_column_width=True)
    # Display the story
    st.write(story)


# in the sidebar load all the stories
with st.sidebar:
    st.title('Recent Stories')
    stories = os.listdir('stories')
    for story_file in stories:
        story_file_path = os.path.join('stories', story_file)
        with open(story_file_path, 'r', encoding='utf-8') as f:
            story_data = json.load(f)
            with st.expander(f"{story_data['topic'].capitalize()}"):
                if story_data['cover_image_url']:
                    st.image(story_data['cover_image_url'], use_column_width=True)
                st.write(story_data['story'])
