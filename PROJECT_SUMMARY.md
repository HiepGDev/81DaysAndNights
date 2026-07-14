# 81 Days & Nights - AI Evolutionary System Map

This file serves as a quick reference map for developers and AI coding assistants working on the AI optimization loop of **81 Days & Nights**.

---

## 1. System Architecture Flow

```
[ Unity Client ] ---------------------> [ ASP.NET Core API ] (Port 5093)
   - AISyncService (Pulls DNA)              - Receives session logs (DTO -> Model)
   - AIEvaluationTracker (Logs stats)       - Runs Heuristic Genetic Optimizer
   - GameOverManager (Upload hook)          - Hosts Admin Panel (wwwroot/index.html)
          |                                               |
          v                                               v
[ Local SQLite / PlayerPrefs ]                    [ Supabase Database ]
   - Config cache (Fallback)                        - ai_generation_configs (Stats DNA)
                                                    - ai_combat_logs (History)
                                                    - admins (Hashed credentials)
```

---

## 2. Directory & Script Registry

### A. ASP.NET Core API Service (`81DaysAndNights_AIService/`)
* **[Controllers/AIConfigController.cs](file:///G:/Capstone/81DaysAndNights_AIService/Controllers/AIConfigController.cs)**: Exposes `GET /api/aiconfig` to serve the latest evolved parameters.
* **[Controllers/AIStatsController.cs](file:///G:/Capstone/81DaysAndNights_AIService/Controllers/AIStatsController.cs)**: Exposes `POST /api/aistats/session-results` (accepts `List<AiCombatLogDto>` in snake_case and translates them to database models).
* **[Controllers/AdminController.cs](file:///G:/Capstone/81DaysAndNights_AIService/Controllers/AdminController.cs)**: Authenticates admin credentials, handles JWT generation, logs queries, and handles manual weights override (`tweak`).
* **[Models/Dtos.cs](file:///G:/Capstone/81DaysAndNights_AIService/Models/Dtos.cs)**: Holds the snake_case Data Transfer Objects (`AiGenerationConfigDto`, `AiCombatLogDto`) to map 1-to-1 to Unity's serializer.
* **[Models/SupabaseModels.cs](file:///G:/Capstone/81DaysAndNights_AIService/Models/SupabaseModels.cs)**: Maps to Supabase database tables using `postgrest-csharp`. Uses nullable `DateTime? CreatedAt` to support real-time timezone offsets.
* **[Services/SupabaseService.cs](file:///G:/Capstone/81DaysAndNights_AIService/Services/SupabaseService.cs)**: Wrapper for database inserts/queries.
* **[Services/OptimizationEngine.cs](file:///G:/Capstone/81DaysAndNights_AIService/Services/OptimizationEngine.cs)**: Implements Heuristic Evolutionary mutations. Modifies push/cover weights and bloom based on win rates.
* **[wwwroot/index.html](file:///G:/Capstone/81DaysAndNights_AIService/wwwroot/index.html)**: Admin panel featuring dark-mode glassmorphism. Shows real-time tickers and lets you manually tweak weights.

### B. Unity Client Scripts (`81DaysAndNights/Assets/Scripts/`)
* **[Phat's Scripts/AISyncService.cs](file:///G:/Capstone/81DaysAndNights/Assets/Scripts/Phat's Scripts/AISyncService.cs)**: Syncs `EnemySO` scriptable objects with the latest server generation weights.
* **[Phat's Scripts/AIEvaluationTracker.cs](file:///G:/Capstone/81DaysAndNights/Assets/Scripts/Phat's Scripts/AIEvaluationTracker.cs)**:
  * Records individual enemy lifespans, damage dealt, and cover states.
  * Uses a local `timeAlive` accumulator in `Update()` (`Time.deltaTime`) to prevent negative values during transitions.
  * Employs a static `sessionEnded` lock to block double submissions.
  * Uses `DontDestroyOnLoad` on `LogDispatcher` to ensure uploads finish during scene reloads.
* **[Phat's Scripts/EnemyBehaviorAgent.cs](file:///G:/Capstone/81DaysAndNights/Assets/Scripts/Phat's Scripts/EnemyBehaviorAgent.cs)**: Exposes low-health ($< 30\%$) dynamic branch (Push vs. Cover decision). Restricted lock range checks in `GetClosestTarget` to `Sniper` mode to preserve Ambush mode locking.
* **[Phat's Scripts/EnemySO.cs](file:///G:/Capstone/81DaysAndNights/Assets/Scripts/Phat's Scripts/EnemySO.cs)** / **[EnemyDetection.cs](file:///G:/Capstone/81DaysAndNights/Assets/Scripts/Phat's Scripts/EnemyDetection.cs)**: Holds the `detectionRadius` attribute.
* **[Hiep's Scripts/GameOverManager.cs](file:///G:/Capstone/81DaysAndNights/Assets/Scripts/Hiep's Scripts/GameOverManager.cs)**: Activates on player death; contains the trigger call: `AIEvaluationTracker.SubmitSessionLogs(true, SceneManager.GetActiveScene().buildIndex);`
* **[Hiep's Scripts/PlayerHealth.cs](file:///G:/Capstone/81DaysAndNights/Assets/Scripts/Hiep's Scripts/PlayerHealth.cs)**: Disables movement, locks camera, and sets the Game Over Canvas active upon death.

---

## 3. Database Schema Reference (Supabase)

```sql
-- 1. Evolved AI Configurations
CREATE TABLE ai_generation_configs (
    id SERIAL PRIMARY KEY,
    generation_number INT UNIQUE NOT NULL,
    base_health INT DEFAULT 100,
    min_spread FLOAT DEFAULT 0.01,
    max_spread FLOAT DEFAULT 0.08,
    push_probability FLOAT DEFAULT 0.5,
    cover_probability FLOAT DEFAULT 0.5,
    player_id VARCHAR(100) DEFAULT 'default',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 2. Combat Session Logs
CREATE TABLE ai_combat_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id VARCHAR(100) NOT NULL,
    enemy_type VARCHAR(50) NOT NULL,
    damage_dealt INT NOT NULL,
    damage_taken INT NOT NULL,
    time_alive FLOAT NOT NULL,
    died_in_cover BOOLEAN NOT NULL,
    stage_number INT NOT NULL,
    player_died BOOLEAN NOT NULL,
    player_id VARCHAR(100) DEFAULT 'default',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 3. Admin Credentials
CREATE TABLE admins (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
```

---

## 4. Run & Test Instructions

### Running C# Web Service
```bash
cd G:\Capstone\81DaysAndNights_AIService
dotnet run --urls=http://localhost:5093
```
* **Admin Dashboard URL**: `http://localhost:5093`
* **Localhost Bypass**: Unity scripts point to `http://127.0.0.1:5093` to prevent IPv6/DNS lookup delays.

### Testing Cycle
1. Press **Play** in Unity. The console should log `Successfully loaded latest AI configuration`.
2. Play the game and fight enemies.
3. **To test Defeat**: Let the player die. The game over screen will automatically upload the session combat stats.
4. **To test Victory**: Win the stage, change the scene, or press `T` (manual key simulation).
5. **Verify**: Open `http://localhost:5093` and look at the logs table. You will see real-time records at the top of the table.

---

## 5. Core Data & Communication Flows

### Flow A: Startup AI Configuration Sync
This workflow updates the enemy properties inside the Unity Editor when you enter Play mode:

1. **Unity Start**: `AISyncService.Start()` initiates the sync.
2. **HTTP Fetch**: Calls `GetLatestAIConfig()` coroutine to request `GET /api/aiconfig?playerId=PLAYER_ID` (retrieving the unique player ID from Local PlayerPrefs).
3. **API Logic**: `AIConfigController.GetLatestConfig()` calls `SupabaseService.GetLatestGenerationConfigAsync(playerId)` to fetch the newest row from the `ai_generation_configs` table matching that specific player.
4. **Local Update**: Unity receives the JSON payload, converts it, and calls `AISyncService.UpdateScriptableObjects()`.
5. **DNA Tweak**: Writes the new values (`baseHealth`, `minSpread`, `maxSpread`, `pushProbability`, `coverProbability`) into the respective `EnemySO` Scriptable Objects.
6. **Agent Init**: Spawned enemies read these configurations in their `Awake()` and `Start()` calls.

---

### Flow B: Combat Logging & Accumulation
This workflow tracks combat metrics in real-time during gameplay:

1. **Damage Taken**: Player shoots enemy -> Calls `EnemyHealth.TakeDamage(amount)`.
2. **Damage Dealt**: Enemy shoots player -> Calls `EnemyShooting.ShootManual()`, which increments `totalDamageDealt` by bullet damage.
3. **Survival Clock**: `AIEvaluationTracker.Update()` increments local `timeAlive` by `Time.deltaTime` each frame.
4. **Enemy Death Event**: If `EnemyHealth.CurrentHealth <= 0`:
   * Triggers `AIEvaluationTracker.RegisterLog(died: true)`.
   * Saves the snapshot of survival time, damage dealt, and cover state in the static queue `collectedLogs`.

---

### Flow C: Session Logs Submission (Player Defeat)
This workflow handles packaging and sending logs to the server when the player fails:

1. **Player Death**: `PlayerHealth.Die()` calls `gameOverCanvas.SetActive(true)`.
2. **Canvas Activation**: Triggers `GameOverManager.OnEnable()`.
3. **Trigger upload**: Calls static `AIEvaluationTracker.SubmitSessionLogs(playerDied: true, stageNumber)`.
4. **Lock Activation**: Sets static `sessionEnded = true` to reject any further log updates during level reloading.
5. **Remaining Gather**: Loops through all remaining alive enemies in the scene and registers them as survived (`died: false`).
6. **Upload Dispatch**: Spawns the `LogDispatcher` GameObject, calls `DontDestroyOnLoad()`, and starts `PostLogs()` coroutine.
7. **HTTP Post**: Sends the JSON data (including the unique `player_id`) to `POST /api/aistats/session-results`.
8. **Server Database Save**: `AIStatsController.SubmitSessionResults()` receives the array, maps the `player_id`, and saves the logs to the `ai_combat_logs` table.
9. **Evolutionary Optimization**: Passes the logs to `OptimizationEngine.ProcessCombatBatchAsync()`. If a new generation is ready, it mutates the weights specifically for that player, and saves the new DNA configuration under their `player_id` to the `ai_generation_configs` table.

---

## 6. Function & Method Dictionary

### A. Unity Client Scripts

#### 1. `AIEvaluationTracker.cs`
* **`InitializeSceneEvents()`** (static, private)
  * *Role*: Binds callback listeners to Unity's `sceneLoaded` and `sceneUnloaded` events at runtime launch.
* **`OnSceneLoaded(Scene, LoadSceneMode)`** (static, private)
  * *Role*: Resets the `sessionEnded` static flag back to `false` when a new stage loads.
* **`OnSceneUnloaded(Scene)`** (static, private)
  * *Role*: Triggers a victory log upload when transitioning to a new scene (if the queue is populated).
* **`GetPlayerId()`** (static, public) -> `string`
  * *Role*: Retrieves the unique Player ID. If not cached, generates a new UUID and persists it in `PlayerPrefs` under `"AI_PlayerID"`.
* **`Awake()`** (instance, private)
  * *Role*: Caches local references to `EnemyHealth`, `EnemyShooting`, and `EnemyBehaviorAgent`.
* **`Start()`** (instance, private)
  * *Role*: Stores the initial maximum health from the enemy profile.
* **`Update()`** (instance, private)
  * *Role*: Accumulates `timeAlive` via `Time.deltaTime` and registers a death log if health drops to `<= 0`.
* **`OnDestroy()`** (instance, private)
  * *Role*: Registers survivors in the combat queue when the level is unloaded/reloaded.
* **`RegisterLog(bool died)`** (instance, private)
  * *Role*: Extracts survival time, cover states, and damage statistics, then pushes the resulting log to the static list.
* **`SubmitSessionLogs(bool playerDied, int stageNumber, string submitUrl)`** (static, public)
  * *Role*: Closes the logging window (`sessionEnded = true`), loops through alive enemies, flushes the queue, and spawns the `LogDispatcher` GameObject.
* **`LogDispatcherComponent.StartSubmit(string url, List<AiCombatLogData> data)`** (instance, public)
  * *Role*: Initiates the async upload coroutine.
* **`LogDispatcherComponent.PostLogs(string url, List<AiCombatLogData> data)`** (instance, private IEnumerator)
  * *Role*: Serializes the logs to JSON, configures a raw `UnityWebRequest` POST, sends it, and prints the response status in the console.

#### 2. `AISyncService.cs`
* **`Start()`** (instance, private)
  * *Role*: Dispatches the configuration downloading coroutine.
* **`FetchLatestAIConfig()`** (instance, private IEnumerator)
  * *Role*: Dispatches `GET /api/aiconfig?playerId=PLAYER_ID` and parses the JSON response into Scriptable Objects.
* **`ApplyConfig(AiGenerationConfigData data)`** (instance, private)
  * *Role*: Updates the health, spread, and behavior probabilities in the `EnemySO` assets.

#### 3. `GameOverManager.cs`
* **`OnEnable()`** (instance, private)
  * *Role*: Triggers `SubmitSessionLogs(playerDied: true, activeSceneIndex)`, chooses a random quote, and starts the scene reload timer.
* **`ReloadScene()`** (instance, private IEnumerator)
  * *Role*: Performs `SceneManager.LoadScene` after the respawn delay.

#### 4. `PlayerHealth.cs`
* **`Die()`** (instance, private)
  * *Role*: Flags `isDead = true`, disables gun mechanics, movement controllers, physics, and turns on the `gameOverCanvas`.

#### 5. `EnemyBehaviorAgent.cs`
* **`Awake()`** (instance, private)
  * *Role*: Applies the dynamically synced `detectionRadius` from `EnemySO`.
* **`GetClosestTarget()`** (instance, private) -> `Transform`
  * *Role*: Queries targets. Restricts detection radius checks *specifically* to `Sniper` mode to prevent breaking global lock-ons in `Ambush` mode.

---

### B. C# Web Service Scripts

#### 1. `AIConfigController.cs`
* **`GetConfig(string? playerId)`** (`GET /api/aiconfig`) -> `ActionResult<AiGenerationConfigDto>`
  * *Role*: Queries the latest generation configuration for the requested `playerId` (defaults to `"default"` if null).

#### 2. `AIStatsController.cs`
* **`SubmitSessionResults(List<AiCombatLogDto> dtos)`** (`POST /api/aistats/session-results`) -> `IActionResult`
  * *Role*: Deserializes the combat logs array, maps `player_id`, and passes them to the evolutionary engine.

#### 3. `AdminController.cs`
* **`GetLogs()`** (`GET /api/admin/logs`) -> `IActionResult`
  * *Role*: Retrieves the last 100 historical combat logs for the dashboard.
* **`TweakAI(TweakRequest request)`** (`POST /api/admin/tweak`) -> `IActionResult`
  * *Role*: Validates JWT token and forces manual configuration overrides for the specified `PlayerId`.

#### 4. `SupabaseService.cs`
* **`GetLatestConfigAsync(string playerId)`** -> `Task<AiGenerationConfig>`
  * *Role*: Queries the database for the highest generation number matching that player. Seeds a new default config if it's their first time.

#### 5. `OptimizationEngine.cs`
* **`ProcessCombatBatchAsync(List<AiCombatLog> batchLogs)`** -> `Task`
  * *Role*: Calculates the batch's player death rate and cover-seeking behaviors, runs genetic heuristics, and saves the new DNA parameters for that player.

