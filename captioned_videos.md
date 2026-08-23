# captioned_videos.md -- a playlist for testing the caption feature
#
# Pass this file as the source path. HomerScribe reads .md as a list, takes one
# address per line, and ignores any line beginning with # or ;.
#
# The addresses below are bare on purpose. This file is read by the program,
# not by a person, and a link with friendly text would not parse.
#
# Suggested run, transcribe only, which is the quickest way to see whether the
# captions came through:
#
#   HomerScribe --transcribe --output-dir C:\CaptionTest captioned_videos.md
#
# And then the same films again with --captions no, into a different output
# folder, to compare what Whisper heard against what the captions said.
#
# ---------------------------------------------------------------------------
# 1. W3C, "Video Introduction to Web Accessibility and W3C Standards", 4 min.
#    Uploader-written captions, and subtitles in a dozen other languages.
#    THE MAIN TEST: only the English track should be fetched and used.
#    A translated track appearing in transcribed.md is a failure.
#    W3C publishes the English VTT itself, so the track is certain to exist.
https://www.youtube.com/watch?v=20SHvU2PKsM

# 2. W3C, "Web Accessibility Perspectives" compilation, 7:36.
#    Ten short pieces, narrated, with an SRT that W3C publishes separately.
#    This is HomerScribe's own default source, so there may already be a
#    Whisper-only run of it to compare against. Same film, both ways.
https://www.youtube.com/watch?v=3f31oufqFSM

# 3. TED, Ken Robinson, "Do schools kill creativity?", about 19 min.
#    THE SOUND TEST. TED's captions write (Laughter) and (Applause) as cues of
#    their own. Those should appear in transcribed.md as they stand, and in
#    scribed.md labelled Sound rather than Spoken. This is the one thing a
#    transcript of speech can never hold, so it is the point of the feature.
https://www.youtube.com/watch?v=iG9CE55wbtY

# 4. W3C, first video of the audio-described Perspectives playlist, about 1 min.
#    Short, and already carries a described audio track, so it is a fair check
#    that describing a film that describes itself still behaves.
https://www.youtube.com/watch?v=21yWr7evHTs
