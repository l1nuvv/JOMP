# Just One More Peel — MVP

Open Assets/JustOneMorePeel/Scenes/FirstShift.unity and press Play.
Click НАЧАТЬ СМЕНУ. Click inside Game view if Unity has not captured input.

WASD: move. Mouse: look. Space: jump. Shift: sprint.
Hold left mouse on a reachable poster strip and drag to peel faster.
E near the tool crates: tool shop. 1: scraper. 2: steam tool after purchase. Esc: pause.

19 overlapping poster patches, 2 layers, 4 strips per patch = 152 strips.
Each strip pays $2. Scraper upgrades cost $16, $36, $56; steam tool costs $50.
Clear all paper to finish the shift and uncover the old wall lettering.
Progress resets when Play Mode ends. No save system in this prototype.

Uses the imported Unity Starter Assets FirstPersonController, Input System and Cinemachine.
Game code, procedural poster art, text shader and scene builder are in this folder.
Just One More Peel > Create MVP scene recreates the prototype scene (overwrites that scene).
No paid assets or external generation services are required.

Verified through Unity MCP in Play Mode:
- Mesh deformation during partial peel.
- Covered strips become exposed only after the strip above is removed.
- Removal pays once, repeat Pull calls do not pay again.
- Insufficient funds reject upgrades; affordable upgrades and steam purchase work.
- Original mechanic smoke test: 96 strips cleared. Art revision has 152 strips.
- Starter Assets controller moves forward and stops at the wall collider.
- No console errors or warnings at the end of the gameplay check.
Screenshots in project Captures folder are development previews.

Revision: synchronized Starter Assets cursor ownership and focus handling; Enter starts shift; understated HUD; irregular overlapping local notices; 100 static renderers combined into 5 meshes; idle paper Update disabled; no paper shadow passes.

Interaction revision: keep holding LMB across layers; pull mouse downward for a bonus. Workbench requires direct line of sight within 2.5m. Steam peels up to three adjacent strips and has a 2.4x passive rate. New project policy and skill catalogue: root AGENTS.md and docs/README.md.
