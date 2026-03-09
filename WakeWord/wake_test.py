import struct
import pvporcupine
import pyaudio

ACCESS_KEY = "4Ykv74uINKWCoLMpwvmb+2I+kUsBK6LtNBQETT2hH5y/w1JyhUTzhw=="

porcupine = pvporcupine.create(
    access_key=ACCESS_KEY,
    keyword_paths=["reet-sue-koh.ppn"]
)

pa = pyaudio.PyAudio()

stream = pa.open(
    rate=porcupine.sample_rate,
    channels=1,
    format=pyaudio.paInt16,
    input=True,
    frames_per_buffer=porcupine.frame_length
)

print("Listening...")

try:
    while True:
        pcm = stream.read(porcupine.frame_length, exception_on_overflow=False)
        pcm = struct.unpack_from("h" * porcupine.frame_length, pcm)

        keyword_index = porcupine.process(pcm)

        if keyword_index >= 0:
            print("Wake word detected!")
except KeyboardInterrupt:
    print("Stopping")

stream.close()
pa.terminate()
porcupine.delete()