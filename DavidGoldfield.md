Hello David,

I thought this might be worth circulating, and I would value your own view of it.

I have released HomerScribe, a free and open source Windows program that describes what happens on screen in a video and transcribes what is said in it. Both jobs run on the local machine: once installed it makes no network request, so there is no account, no key, and no token cost for what amounts to several hundred model calls per film.

Describing produces a new copy of the film with spoken description mixed into its sound, plus a written script of every description with its timing. Transcribing produces the transcript. Both together also produce the speech and the descriptions interleaved in the order they occur.

Given your reporting on local AI on the BT Speak, the engineering may interest you as much as the result. It orchestrates Ollama for the vision model, whisper.cpp for speech and ffmpeg for everything to do with audio and video, and links to none of them. Both models are settings rather than assumptions, so a different vision model, a different Whisper size, or an Ollama instance on another machine needs no recompiling.

Transcribing needs about half a gigabyte of models, describing about six and a half, offered separately by the installer. Professional audio description is better than this by a wide margin; the program is for the far larger body of material that has none at all.

https://github.com/JamalMazrui/HomerScribe/releases/latest/download/HomerScribe_setup.exe

Jamal Mazrui
