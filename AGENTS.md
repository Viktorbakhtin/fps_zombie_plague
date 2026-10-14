# Project rules

- Keep changes small; implement the current milestone only.
- New game code belongs in Assets/_Game. Preserve the existing template until migration is verified.
- Domain has no Unity/FishNet references; Application depends only on Domain.
- Unity/FishNet and external service adapters belong in Infrastructure.
- Compose features; share player/combat/AI across modes through mode rules.
- Critical state is server-owned. Validate RPC ownership, rate, range and game state.
- Use ScriptableObjects for authored configuration, not persistent runtime player state.
- Create scenes/prefabs through Unity MCP. Preserve Unity-generated GUIDs and meta files.
- Run relevant EditMode/PlayMode tests and builds; report unavailable verification honestly.
- Do not add speculative backend systems, empty feature trees, or extra frameworks.
