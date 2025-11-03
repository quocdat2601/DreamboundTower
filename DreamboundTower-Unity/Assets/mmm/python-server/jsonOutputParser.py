from langchain_core.output_parsers.transform import BaseTransformOutputParser
import json

class JSONOutputParser(BaseTransformOutputParser[dict]):
    """OutputParser that parses LLMResult into a valid JSON object."""
    
    @classmethod
    def is_lc_serializable(cls) -> bool:
        """Return whether this class is serializable."""
        return True

    @classmethod
    def get_lc_namespace(cls) -> list[str]:
        """Get the namespace of the Langchain object."""
        return ["langchain", "schema", "output_parser"]

    @property
    def _type(self) -> str:
        """Return the output parser type for serialization."""
        return "json_output_parser"

    def parse(self, text: str) -> dict:
        """Parses the input string as JSON, removing extra formatting if necessary."""
        try:
            return json.loads(text)
        except json.JSONDecodeError:
            # If it's inside code blocks (e.g., with ```json or ```), try removing them
            try:
                text = text.replace("```json", '').replace("```", '')
                return json.loads(text)
            except json.JSONDecodeError as e:
                print("Error decoding JSON:", str(e))
                return {"error": "Unable to decode JSON", "raw_output": text}
