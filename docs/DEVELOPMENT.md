# Development workflow
Use Unity MCP for Editor tasks; do not drive the desktop. Read AGENTS.md and the relevant documentation route.
Preserve the current engine, pipeline and package baseline. Inspect actual APIs with available reflection/docs tools; generic skill snippets are examples, not guaranteed signatures.
Read relevant files and scene links before changing them. Protect GUIDs. Inspect Git state and focused diffs where a Git workflow is available; never claim those checks if not run.
## Verification levels
1. Written: source/assets changed.
2. Compiled: Unity completes compilation; Console checked.
3. Automated scene check: invariants exercised through Unity MCP in Play Mode.
4. Human playtest: owner operates the actual game.
5. Player build/release: separately requested build and platform checks.
Each level has distinct evidence. A simulated method call is not a physical mouse test.
Do not widen testing after the relevant checks pass unless a new failure/change justifies it. No routine release packaging.
## Imported skills
12 reusable skill folders were adapted from Hustle & Scale. Their project context takes precedence over generic examples. Load only the applicable skill and its needed references. Uninstalled related skills are optional suggestions, not dependencies to install automatically.
