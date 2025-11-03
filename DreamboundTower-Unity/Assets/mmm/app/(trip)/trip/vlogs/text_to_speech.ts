const axios = require('axios');
const fs = require('fs').promises;

/**
 * Convert text to speech using ElevenLabs API
 * @param {string} text - The text to convert to speech
 * @param {string} apiKey - Your ElevenLabs API key
 * @param {string} voiceId - Voice ID (optional, defaults to "Rachel")
 * @returns {Promise<Buffer>} Audio buffer
 */
async function textToSpeech(text, apiKey, voiceId = '21m00Tcm4TlvDq8ikWAM') {
    try {
        const response = await axios({
            method: 'POST',
            url: `https://api.elevenlabs.io/v1/text-to-speech/${voiceId}`,
            headers: {
                'Accept': 'audio/mpeg',
                'xi-api-key': apiKey,
                'Content-Type': 'application/json',
            },
            data: {
                text,
                model_id: 'eleven_multilingual_v2',
                voice_settings: {
                    stability: 0.5,
                    similarity_boost: 0.5,
                }
            },
            responseType: 'arraybuffer'
        });

        return response.data;
    } catch (error) {
        if (error.response) {
            console.error('API Error:', {
                status: error.response.status,
                data: error.response.data.toString()
            });
        }
        throw new Error('Failed to generate speech');
    }
}

/**
 * Save audio buffer to file
 * @param {Buffer} audioBuffer - The audio buffer to save
 * @param {string} filePath - Path where to save the file
 */
async function saveAudioFile(audioBuffer, filePath) {
    try {
        await fs.writeFile(filePath, audioBuffer);
        console.log(`Audio saved successfully to ${filePath}`);
    } catch (error) {
        console.error('Error saving file:', error);
        throw new Error('Failed to save audio file');
    }
}

/**
 * Get available voices from ElevenLabs
 * @param {string} apiKey - Your ElevenLabs API key
 * @returns {Promise<Array>} List of available voices
 */
async function getVoices(apiKey) {
    try {
        const response = await axios.get('https://api.elevenlabs.io/v1/voices', {
            headers: {
                'xi-api-key': apiKey
            }
        });
        return response.data;
    } catch (error) {
        console.error('Error fetching voices:', error);
        throw new Error('Failed to fetch voices');
    }
}

/**
 * Convert text to speech and save to file in one function
 * @param {string} text - The text to convert
 * @param {string} apiKey - Your ElevenLabs API key
 * @param {string} outputPath - Where to save the file
 * @param {string} voiceId - Optional voice ID
 */
async function textToSpeechToFile(text, apiKey, outputPath, voiceId) {
    try {
        const audioBuffer = await textToSpeech(text, apiKey, voiceId);
        await saveAudioFile(audioBuffer, outputPath);
        return true;
    } catch (error) {
        console.error('Error in text-to-speech process:', error);
        return false;
    }
}

// Export all functions
module.exports = {
    textToSpeech,
    saveAudioFile,
    getVoices,
    textToSpeechToFile
};