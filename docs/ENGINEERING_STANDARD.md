# Engineering standard
Adapted from Hustle & Scale engineering policy for this smaller Unity game.
Use Unity-supported C# and APIs on the actual 6000.6.0f1 baseline. Desktop .NET versions do not establish game-runtime compatibility.
Use integer currency, idempotent removal/payout and validated purchases. UI reads the same balance as gameplay; do not invent duplicate state.
Cache dependencies and paper collections. Avoid allocations, reflection and global searches in hot paths when changing those paths. Measure before introducing Jobs/Burst or other infrastructure.
Keep configuration, runtime state and eventual save DTOs distinct. ScriptableObjects are optional authoring assets, not required mutable global state.
Use explicit composition and small components. No new DI container, EventBus, ECS, multiplayer or generic manager framework without a demonstrated requirement.
Keep the working Starter Assets controller; do not reimplement walk/jump while improving peeling.
A source-level test does not prove cursor usability, visual quality, or enjoyable pacing. Those need relevant runtime/owner checks.
