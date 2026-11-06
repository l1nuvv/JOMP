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
- Steam costs $480 and requires a scraper. It softens glue at 2.4× rate and captures up to three nearby visible front posters within 1.5m of the aimed stack, subject to reach and occlusion. It works without dragging. Keys 1/2 select scraper/card or steam.
- Experimental wider steam costs $800 after buying steam. It expands the search radius to 2.5m and the limit to five visible, reachable front posters; occlusion still applies. The price and pacing are provisional.
- $2 per strip, $8 for a complete fresh poster. Previously paid strips do not pay again when finishing a partly peeled poster.
These are prototype pacing values and still need owner playtesting.
## Depth and completion
19 composed stacks × 128 posters = 2432 logical posters, equivalent to 9728 paid strip units. Each stack uses two four-strip interactive buffers and a combined mesh containing all deeper sheets in their pre-existing positions. There are 152 reusable interactive strips, 19 additional deeper-layer meshes, plus a fixed pool of 32 falling visuals. Visual quality and performance of the deeper meshes still require verification. Depth does not instantiate thousands of poster GameObjects.
The current top poster must be completely removed before the underlying sheet becomes interactive. Every grip ends before advancing buffers. All posters must be removed to complete the shift.
## Language and settings
English is the default; English/Russian can be selected from Settings on the intro and pause screens. Menus, hints, messages, wall writing and all ten printed notice designs switch together. Sound volume and look sensitivity are adjustable. Preferences persist locally; shift progress does not.
## Owner decisions and limits
The decorative bench was removed at the owner's request. The owner explicitly asked for a meaningful change from beginner strips to whole posters and then steam, with thousands of posters rather than the earlier 38-sheet slice.
No new city, finished chapters, prestige or release packaging is implemented in this prototype. The owner's latest product target is 1–2 hours of human play, extending beyond a short courtyard experience; the master plan proposes chapters and a complete save system. Model/art acceptance, pacing, chapter structure and specific new rules remain owner decisions.
