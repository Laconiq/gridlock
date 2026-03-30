# Gridlock

Isometric grid-based Tower Defense built in Unity 6 with URP. Neon aesthetic with Geometry Wars-style grid deformation, bloom, and screen shake.

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| Unity | 6000.4.0f1 | [Unity Hub](https://unity.com/download) |
| Node.js | >= 18 | `brew install node` |
| Python | >= 3.11 | `brew install python` |
| uv / uvx | latest | `brew install uv` |
| Claude Code | latest | `npm install -g @anthropic-ai/claude-code` |

## Project Setup

1. **Clone the repo**
   ```bash
   git clone <repo-url> gridlock
   cd gridlock
   ```

2. **Open in Unity Hub**
   - Add the project folder in Unity Hub
   - Unity Hub will prompt to install 6000.4.0f1 if missing
   - Open the project — package resolution happens automatically

3. **Verify packages**

   Unity resolves all packages from `Packages/manifest.json` on first open. Key packages:
   - `com.coplaydev.unity-mcp` (MCP for Unity — via Git)
   - `com.unity.inputsystem` 1.19.0
   - `com.unity.render-pipelines.universal` 17.4.0
   - Odin Inspector (must be imported separately if not cached in Library)
   - More Mountains Feel (must be imported separately if not cached in Library)

4. **Open the scene**

   `Assets/Scenes/GameScene.unity` (build index 0)

## MCP Setup (Claude Code)

The project uses three MCP servers to let Claude Code interact with Unity, generate UI designs, and more.

### 1. Unity MCP

Connects Claude Code to the Unity Editor for live scene manipulation, script creation, console reading, etc.

**In Unity:**
1. Open **Window > MCP for Unity**
2. Click **Auto-Setup** — this registers the server with Claude Code and starts the bridge
3. Verify the bridge shows **Running** (green dot)

**In Claude Code (if manual registration needed):**
```bash
claude mcp add UnityMCP --transport http --url http://127.0.0.1:8080/mcp -s local
```

The bridge must be running in Unity for the connection to work. Restart it via the MCP for Unity window if Claude Code can't connect.

### 2. Google Stitch (UI Design)

Stitch generates UI mockups from text prompts. The project uses it with the `@google/stitch-sdk` as an MCP server.

**Setup:**
```bash
# Create the server directory
mkdir -p ~/.claude/mcp-servers/stitch
cd ~/.claude/mcp-servers/stitch

# Initialize and install dependencies
npm init -y
npm install @google/stitch-sdk @modelcontextprotocol/sdk
```

**Create `~/.claude/mcp-servers/stitch/server.mjs`:**
```javascript
import { StitchProxy } from "@google/stitch-sdk";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";

const proxy = new StitchProxy({ apiKey: process.env.STITCH_API_KEY });
const transport = new StdioServerTransport();
await proxy.start(transport);
```

**Register with Claude Code:**
```bash
claude mcp add stitch \
  -e STITCH_API_KEY=<your-stitch-api-key> \
  -s local \
  -- node ~/.claude/mcp-servers/stitch/server.mjs
```

Get a Stitch API key from [Google AI Studio](https://aistudio.google.com/).

### 3. Coplay MCP (optional)

General-purpose MCP server (disabled by default in this project).

```bash
claude mcp add coplay-mcp \
  -e MCP_TOOL_TIMEOUT=720000 \
  -s user \
  -- uvx --python ">=3.11" coplay-mcp-server@latest
```

### Verify MCP Servers

```bash
claude mcp list
```

All connected servers should show `Connected`.

## GitHub Actions

Two workflows are configured in `.github/workflows/`:

| Workflow | File | Trigger |
|----------|------|---------|
| **Claude Code** | `claude.yml` | `@claude` mention in issues/PRs |
| **Claude Code Review** | `claude-code-review.yml` | PR opened/updated |

Both require the `CLAUDE_CODE_OAUTH_TOKEN` secret in the repo settings.

## Git Workflow

Gitflow model:
- `main` — production, protected
- `dev` — integration branch
- `feature/<name>` — branch from `dev`, squash merge back via PR
- `release/<version>` — branch from `dev`, merge into `main` + `dev`
- `hotfix/<name>` — branch from `main`, merge into `main` + `dev`

## Project Structure

```
Assets/
├── Data/              # ScriptableObjects (modules, enemies, levels)
├── Prefabs/           # Enemy, tower, projectile prefabs
├── Scenes/            # GameScene.unity
├── Scripts/
│   ├── Camera/        # Isometric camera setup + pan/zoom
│   ├── Core/          # Bootstrap, GameManager, ServiceLocator
│   ├── Enemies/       # EnemyController, EnemyAI, spawning
│   ├── Grid/          # GridDefinition, GridManager, GridVisual, warp
│   ├── Mods/          # Mod pipeline system (stages, compiler, projectile)
│   ├── NodeEditor/    # UI Toolkit node editor
│   ├── Towers/        # TowerChassis, TowerExecutor, placement
│   └── Visual/        # GameJuice, VoxelDeath, WarpFollower, ImpactFlash
├── Shaders/           # CyberGrid, VectorGlow, VectorOutline
└── UI/                # UXML/USS (HUD, NodeEditor, DesignTokens)
docs/
├── GAME_DESIGN.md     # Full game design document
├── LEVEL_DESIGN_GUIDE.md
└── SOUND_DESIGN.md
```

## Quick Reference

- **Play the game:** Open `GameScene.unity`, hit Play. Place up to 5 towers, click "Start Wave".
- **Edit a level:** Modify `Assets/Data/Levels/TestGrid.asset` in Odin Inspector.
- **Add a mod:** Create a new `IModStage` implementation in `Assets/Scripts/Mods/Pipeline/Stages/`, add a `ModType` enum value.
- **Bake meshes:** Menu > Gridlock > Bake Meshes into Prefabs.
