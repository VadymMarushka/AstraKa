# World bible — AstraKa

Lore, vocabulary and tone for all product text and seed data. The product is English-only.
The sect is **entirely fictional**: no references to real religions, churches or organizations.

## Name and myth

**AstraKa — Temple of the Returning Star.** *Astra* = star, *ka* = the ancient Egyptian word for soul:
the soul-star.

**Belief.** Souls were once stars that fell to Earth. Every soul still has its own place in the sky —
its **astral address**. Astral exercises **align** the soul with that place.

**Mission: Alignment.** An aligned soul lives the way the Universe intended, and after death it returns
home, to its place among the stars. Alignment is the goal of the whole platform; each course
additionally promises something of its own.

## Roles

| Role | Who | Does | Where |
|---|---|---|---|
| **Wanderer** | Client. A soul-star that has drifted from its place (the ancient Greeks called planets "wandering stars") | Joins courses, follows the daily plan, reports how they feel | Web (Angular) |
| **Starshepherd** | Specialist, "shepherd of stars". Anyone can become one. Invents their own title | Creates exercises and courses, watches participants, sends Revelations | Desktop |
| **Oracle** | The top of the order, admin | Can edit or remove any course or exercise | No UI. The Oracle has no face; it speaks only through the API |

No ranks.

## Zodiac sign = direction home

The Wanderer's profile stores only the zodiac sign. Each sign owns a 30° sector, and the sign gives the
direction of the astral address as a compass azimuth: **0° = north** (the Pole Star has always pointed
the way home), clockwise. Aries 0°, Taurus 30°, … Libra 180°, Scorpio 210°, … Pisces 330°.

Every exercise is performed **facing home**. The UI shows a hint like "Face 330° (NNW)". Nobody checks.
*The stars will know.*

Birth dates are not stored. Phrases like "Ah, so you're a late Pisces" are oral folklore of
Starshepherds, not a system feature.

## Exercises

A catalog of astral exercises: system ones plus those added by Starshepherds. Each counts
**reps**, **seconds** or **breaths**. An exercise may have a demo video from its author.
Some are done on each side — the video explains it, or ask your Starshepherd.

Seed examples (catalog will grow):

| Name | Real basis | Unit |
|---|---|---|
| Eclipse | Side plank, each side: your body blocks the light | Seconds |
| The Arch *(working name)* | Glute bridge | Seconds |
| Crescent | High lunge, arms up (crescent pose) | Seconds |
| Meridian | Warrior II: arms divide the sky in half | Seconds |
| Homeward Gaze | Breathing with the head raised toward your sign's direction | Breaths |

Other ideas from brainstorming (optional): The Horizon (plank), Five-Pointed Self (star pose),
Axis (tree pose), Summer Triangle (triangle pose), Starburst (jumping jacks), Starfall (squats),
Orbits (arm circles), Comet (superman), Square of Saturn (box breathing 4-4-4-4), Return to Void (lie down, do nothing).

## Courses and Constellations

A **course** is a short program (one or two weeks is already "serious") made of days. Each day has a
title, optional description, optional video-meeting link with the Starshepherd and a list of exercises.
A day can have no exercises at all — only a meeting.

A course runs as **Constellations** (cohorts): a start date and a limited number of seats. Everyone in a
Constellation walks the program together. Money is **never** refunded.

Courses can be open to any sign or only to some signs. Seed examples:

| Course | For whom |
|---|---|
| HOMEWARD | Basic alignment for newcomers, any sign |
| For Cancers | Cancers only |
| Mercury Retrograde Survival | Everyone, during Mercury retrograde |
| Saturn Return Recovery | Those approaching 29 whose life is falling apart |

**Revelation** — a personal note from the Starshepherd to one Wanderer for an upcoming day, optionally
with extra exercises. The base program stays the same for everyone.

## Daily practice and the harmony scale

Each day the Wanderer sees the plan, marks every completed exercise and rates their state once:
a single 1–10 scale with three labels — **Chaos** (1), **Imbalance** (middle), **Harmony** (10).

## Daily horoscope

The Wanderer's home page shows today's horoscope for their sign. If the provider is down:
*"The stars are silent today."*

## Tone of voice

**Deadpan**: absolute nonsense said with a perfectly straight face. The product never winks and never
admits it is a joke — but it does not overdo it either. Lore lives in names, texts and seed data;
ordinary fields (price, dates, email) keep ordinary names.

| Where | Text |
|---|---|
| Missed practice | Your orbit has drifted. Return to practice before Saturn notices. |
| 404 | This path is not written in the stars. |
| Course finished | You are aligned. The rest is already written. |
| Fine print | Results may vary across lifetimes. |
| No refunds | Energy, once given, cannot be taken back. |

## Glossary

| Term | Meaning |
|---|---|
| AstraKa | The order and the platform |
| Temple of the Returning Star | Full official name |
| Alignment / Aligned | The soul matching its place in the sky; the mission |
| Astral address | The soul's place in the sky; direction given by the zodiac sign |
| Wanderer / Starshepherd / Oracle | Client / specialist / admin |
| Exercise | Astral exercise from the catalog |
| Course | Program template: days with exercises, price, sign restrictions |
| Constellation (Cohort) | One run of a course: start date, seat limit |
| Revelation | Personal note for an upcoming day, maybe with extra exercises |
| Any sign | Course without sign restriction |
| Chaos / Imbalance / Harmony | Daily state scale, 1–10 |

## Deferred ideas

Karma / lore names for missed days and progress; video consultations; "card of the day" from the
tarot API; optional pose check via computer vision.
