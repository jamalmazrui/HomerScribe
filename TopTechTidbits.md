Hello Aaron,

An item for Top Tech Tidbits, written so it can be used as it stands or cut as you see fit.

Jamal Mazrui has released HomerScribe, a free and open source Windows program that describes what happens on screen in a video and transcribes what is said in it, entirely on the local machine. Describing produces a new copy of the film with spoken audio description mixed into its sound, together with a written script of every description and its timing; transcribing produces a full transcript; choosing both also produces a document with the speech and the descriptions interleaved in the order they occur. Once installed the program makes no network request, so there is no account, no API key and no token cost for what would otherwise be several hundred model calls per film. It uses Ollama to serve a local vision model and whisper.cpp for speech, with ffmpeg doing the audio and video work, and both models can be changed by a command line setting without recompiling. The dialog is built for screen reader users and everything is available from the command line for batch work. Transcription requires roughly half a gigabyte of models and description roughly six and a half; the installer offers each separately. The author notes that professional audio description remains better by a wide margin, and that the program is aimed at the far larger body of material that has no description at all.

https://github.com/JamalMazrui/HomerScribe/releases/latest/download/HomerScribe_setup.exe

Jamal Mazrui
