# What went wrong, and what stops it happening again

A review of the development of HomerScribe, written after a run of changes that
each looked correct and several of which were not. It is here rather than in a
message because the next person to work on this, including me, needs it.

## The shape of the problem

Between the merge into HomerScribe and version 1.0.113 there were roughly
seventy changes. Most were sound. But a recognisable minority were not, and they
were not random: they fell into five kinds, and each kind recurred.

## 1. Analysing logs from builds that did not contain the change

The worst of them, by cost. The rule that refuses a moment with no room to
describe it was written, and then five separate runs were analysed before it had
ever executed once — variously because the moments were cached from an earlier
run, because Force overwrite cleared the record from disk but not from memory,
or because the build simply predated it.

Each time, the log looked wrong, and each time I reasoned about the algorithm
when the algorithm had not run.

**Why it kept happening.** Nothing in a log said what was in the build. A
version number says when a build was made, not what was decided by then, and
the two had drifted by sixty versions.

**What now prevents it.** Every run logs `This build does:` followed by the
names of the behaviours compiled into it. If a behaviour is not on that line, it
is not in that build, and no further analysis is warranted until it is.

## 2. Edits that silently did nothing

Changes are made by replacing one exact piece of text with another. When the
text to be replaced is not present, the operation does nothing and reports
nothing.

This destroyed about sixty entries in `History.md` — each written as a
replacement against the entry above it, each finding nothing, each failing in
silence — and caused at least two build failures where a change was believed
applied and was not.

**What now prevents it.** Every replacement asserts that its anchor exists
before making it. An edit that cannot find its place now stops rather than
proceeding as if it had worked.

## 3. Testing against my own belief

The most serious, because it is the one that can survive any amount of testing.

Moments placed on the timer were positioned at the *midpoint* of the quiet
measured for them, so a description had half the room the program believed it
had. I did not find this by testing, because when I wrote `placement_test.py` I
wrote it from the same mental picture as the code — and it agreed with the code,
because both were wrong in the same way.

A test derived from the same belief as the implementation cannot falsify that
belief. It can only confirm it.

**What now prevents it.** `placement_test.py` is checked against measured
behaviour from real runs, not against what the code is supposed to do. When the
two disagree, the run is right.

## 4. Optimising a proxy instead of the outcome

A great deal of effort went into raising "percent of moments in real gaps",
which is not what anybody wants. What is wanted is descriptions a listener can
hear. Those came apart badly: one film reported 73 percent of its moments in
real gaps while 82 percent of its descriptions were spoken over the narration.

**What now prevents it.** The measure of record is descriptions that overlap
speech. The proxy is still logged, because it is diagnostic, but it is not the
target.

## 5. Confusing what the program knows with what the person was told

Three separate times, HomerScribe established exactly what had gone wrong,
wrote it to the log, and told the user "Nothing was done." A missing file, a
withdrawn video, and a source that was neither. Everything needed was recorded
and none of it reached the person who needed it.

**What now prevents it.** Every source ends in one of a fixed set of announced
outcomes — described, Skipped, Resuming, Error, Rejected — and anything that
could not be used is named in the results with its reason. A silent ending is
now a bug by definition.

## What the field does, and where this differed

Reviewing the published work was worth doing, and one finding mattered.

The systems in the literature and in commercial use agree on the broad
architecture HomerScribe already had: find the gaps in the speech, decide which
of them deserve a description, generate the description, fit it to the gap.
Gap-driven placement is standard, and the three-second minimum used commercially
is close to the value arrived at here by measurement.

But they separate two things this program was conflating. The **interval** being
described — a stretch of picture with coherent content — is not the same as the
**placement period** where the description is spoken. One system places a
description within three seconds of the interval it describes, rather than
describing whatever is visible at the moment of speaking.

HomerScribe was sampling its frames *across the gap itself*. A gap is a pause in
the speech, and is frequently the least eventful part of a film: the thing the
listener needs described usually happened while somebody was talking, just
before it. The window now runs back from the end of the gap far enough to cover
the run-up, never shorter than the gap.

This is the one change in this review that is likely to improve the descriptions
themselves rather than their placement.

## The habits that follow

1. Check what is in the build before analysing what it did.
2. Assert that an edit found its place.
3. Do not write a test from the same picture as the code.
4. Measure the outcome, not the proxy for it.
5. If the program knows something, the person must be told it.
6. State a prediction before a run and record whether it held.

The sixth is the newest and the least practised. Two predictions in this project
were wrong: that overlap would fall to near zero, and that wider frames would
improve quality. Both were reasonable and both were refuted by measurement,
which is the point of making them out loud.
