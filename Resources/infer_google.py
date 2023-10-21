from gtts import gTTS
tts = gTTS('VALUE_TEXT', lang='VALUE_LANGUAGE')
tts.save('output.mp3')

with open("finished.txt", "w") as file:
    pass