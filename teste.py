from google.cloud import texttospeech

client = texttospeech.TextToSpeechClient()

synthesis_input = texttospeech.SynthesisInput(text="Bem-vindo ao nosso podcast!")

voice = texttospeech.VoiceSelectionParams(
    language_code="pt-BR",
    name="pt-BR-Wavenet-A"
)

audio_config = texttospeech.AudioConfig(
    audio_encoding=texttospeech.AudioEncoding.MP3
)

response = client.synthesize_speech(
    input=synthesis_input,
    voice=voice,
    audio_config=audio_config
)

with open("podcast.mp3", "wb") as out:
    out.write(response.audio_content)
