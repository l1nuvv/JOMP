# Just One More Peel — working design
Current `ThreeAreaPrototype` has 250 courtyard posters, 104 basement posters in seven stacks and 96 in the final hall. The HUD counts remaining posters in the current area; a prototype world autosave is implemented. The older two-area experiment is separate. See ROADMAP for verification and open human review.
Status: this file describes the implemented prototype rules. Future product proposals and the owner's latest 60–120 minute human-playthrough requirement are in [MASTER_DEVELOPMENT_PLAN](MASTER_DEVELOPMENT_PLAN.md). Multiple compact locations are now part of planning; their themes, transition conditions and depth changes are not approved implementation rules yet.

## Intent
A tactile first-person game about removing layered street notices. The owner's intended unit is a poster: a starter tool tears strips, a scraper removes a whole sheet, steam softens glue across nearby sheets. The physical overlapping-paper courtyard remains the current setting; locality and marketing direction are not final art acceptance.
## Current loop
Look at reachable paper → hold LMB → remove paper and continue on the next reachable visible target → earn money → look at the tool cart within 2.5m → E to buy tools → uncover deeper posters and eventually the wall.
The HUD briefly shows the money awarded by a removal, combining payments made in the same frame. This feedback remains a candidate for owner playtest.
Holding LMB can start another grip after the previous one finishes. The raycast is repeated for each grip; camera look is freed between grips. Menu transitions and tool changes still require releasing LMB before peeling resumes.
## Implemented progression
- Start with an expired plastic membership card. One grip removes one of four strips. Rate .4/s (2.5 seconds per strip; provisional after owner feedback). Mouse motion gives no bonus.
- First scraper costs $25. A grip now captures every remaining part of the current poster, including a sheet partly torn with the card. An intact poster falls as one sheet. An experimental left/right mouse movement accelerates peeling; its feel awaits owner playtest.
- Scraper sharpening costs $160 and $640. Scraper rates for levels 1/2/3 are .45/.55/.65 per second. Horizontal speed up to 1200 pixels/s adds up to 80% in either direction; vertical motion adds nothing. Bonus is based on pixels per second, not pixels per frame, and frame progress is capped to .1s.
- Steam requires a scraper and costs $39 in the 26-sheet `MainRoute` shift and $480 in `ThreeAreaPrototype`. In the main shift, clearing six required courtyard sheets and the two-sheet Record Night stack pays for both tools before the descent. Skipping that stack makes steam affordable halfway through the basement. [Route timings](development/JOMP_NEXT_TASKS.md#m4-eco-001)
- Steam has a base rate of 1.08/s and captures up to three nearby visible front posters within 1.5m of the aimed stack, subject to reach and occlusion. While holding LMB, clockwise mouse motion raises steam speed by up to 80%. Sharpening applies only to the scraper. Keys 1/2 select scraper/card or steam.
- Experimental wider steam costs $800 after buying steam. It expands the search radius to 2.5m and the limit to five visible, reachable front posters; occlusion still applies. The price and pacing are provisional.
- $2 per strip, $8 for a complete fresh poster. Previously paid strips do not pay again when finishing a partly peeled poster.
These are prototype pacing values and still need owner playtesting.
## Depth and completion
The current top poster must be completely removed before the underlying sheet becomes interactive. Every grip ends before advancing buffers. The shift follows the required objectives through the courtyard, basement and archive. Side notices in the courtyard have a separate remaining count and can be cleared before or after finishing the route.
Stacks reuse two four-strip interactive buffers and a combined mesh for deeper sheets. The earlier 19 × 128 layout was a depth experiment. See [ARCHITECTURE](ARCHITECTURE.md) for the implementation and [PERFORMANCE](PERFORMANCE.md) for the separate stress scene.
## Language and settings
English is the default. English/Russian can be selected from Settings on the intro and pause screens. Menus, hints, wall text and paired notice designs switch together. Sound volume and look sensitivity are adjustable. Preferences persist locally. The main route also autosaves shift progress in a separate file, including partial poster progress, purchases and route state. See [ARCHITECTURE](ARCHITECTURE.md#known-debt) for save compatibility.
## Owner decisions and limits
The decorative bench was removed at the owner's request. The owner explicitly asked for a meaningful change from beginner strips to whole posters and then steam, with thousands of posters rather than the earlier 38-sheet slice.
The product target is 1–2 hours of human play. The [master plan](MASTER_DEVELOPMENT_PLAN.md) documents release proposals and the [roadmap](ROADMAP.md) records implementation and owner acceptance, including open questions on artwork and pacing.
