import asyncio
import edge_tts

TEXT = "VALUE_TEXT"
VOICE = "VALUE_VOICE"
OUTPUT_FILE = "output.mp3"

async def amain() -> None:
    communicate = edge_tts.Communicate(TEXT, VOICE)
    await communicate.save(OUTPUT_FILE)
    
    with open("finished.txt", "w") as file:
        pass

if __name__ == "__main__":
    loop = asyncio.get_event_loop_policy().get_event_loop()
    try:
        loop.run_until_complete(amain())
    finally:
        loop.close()