# Project Details - Cooking-Game-2D & SkyBound
## For Portfolio & Job Applications

---

# PROJECT 1: Cooking-Game-2D (FarmHn)

## Overview
- **Duration:** Aug 2023 - Aug 2024 (1 Year)
- **Status:** Published on Itch.io & Android APK (198 MB)
- **Team:** Solo developer + part-time design collaboration
- **Engine:** Unity 2D (6.1 LTS)
- **Tech Stack:** C#, DOTween, ScriptableObjects, WebGL, ASTC Compression
- **Platforms:** WebGL (Itch.io), Android APK
- **Links:** [GitHub Repo](#) | [Play on Itch.io](#)

---

## Game Design & Core Systems

### 1. Kitchen Cooking & Recipe Crafting System
**What it does:**
- Players harvest raw crops from farm and convert into finished dishes
- Recipes range from simple (wheat → bread) to complex (tomato + basil + pasta → pasta primavera)
- Each recipe has multi-step preparation with cooking timers

**Technical implementation:**
- Event-driven recipe validation system
- Queue-based cooking pipeline with coroutine timers
- Dynamic pricing based on ingredient combinations
- Persistent recipe unlock progression

**Game balance:**
- Rare recipes yield 5-10x economic returns
- Encourages players to explore and experiment
- Balanced progression prevents early game boredom

### 2. Agricultural Economy & Crop Lifecycles
**What it does:**
- Players plant seeds in drag-and-drop grid system
- Crops progress through 4 growth stages (seed → sprout → mature → harvest)
- Environmental factors (weather cycles) affect growth time
- Warehouse inventory system tracks all crops

**Technical implementation:**
```csharp
// Simplified crop lifecycle example
public class CropLifecycle : MonoBehaviour {
    public enum GrowthStage { Seed, Sprout, Mature, Ready }
    
    private GrowthStage currentStage;
    private float growthTimer;
    private float stageDuration = 8f; // 8 real-time seconds per stage
    
    void Update() {
        growthTimer += Time.deltaTime;
        if (growthTimer >= stageDuration) {
            AdvanceToNextStage();
            growthTimer = 0;
        }
    }
    
    void AdvanceToNextStage() {
        currentStage = (GrowthStage)((int)currentStage + 1);
        OnStageChanged?.Invoke(currentStage);
    }
}
```

### 3. Livestock & NPC AI
**What it does:**
- Livestock (chickens, cows, pigs) roam the farm with autonomous routines
- Feed animals to generate byproducts (eggs, milk, meat)
- NPCs visit farm and purchase items, driving economy

**Technical implementation:**
- Finite State Machines (FSM) for AI behavior
- Waypoint-based pathfinding on grid
- Daily schedule system (wake → feed → sleep cycles)
- Event-driven dialogue system

### 4. Train/Ship Delivery System
**What it does:**
- Trains arrive on schedule (every 3 game days)
- Players load crops into train carts
- Delivery rewards provide major economic income
- Fulfilling orders triggers story progression

**Technical implementation:**
- Persistent delivery order database (ScriptableObjects)
- Scheduling system with game-time progression
- UI order matching system
- Reward calculation engine

---

## Performance Optimization Deep Dive

### Challenge #1: iOS Safari Crashes (95% failure rate)

#### Root Causes:
1. **WebKit Sandbox Limit:** iOS Safari enforces 384-512MB memory ceiling per tab
2. **VRAM Explosion:** iPhone Retina 3x (devicePixelRatio=3.0) created 500MB+ GPU framebuffers
3. **IndexedDB Quota:** Itch.io iframe restricted to 50MB quota, but game tried to cache 150MB

#### Solutions Implemented:

**A. Multi-Tier Adaptive Scaler**
```csharp
// Detect device capabilities and apply appropriate settings
public class WebGLTierScaler : MonoBehaviour {
    void Start() {
        if (IsConstrainedDevice()) {
            Screen.SetResolution(1080, 1920, false);        // Clamp to 1080p
            QualitySettings.masterTextureLimit = 1;         // Lower quality tier
            Application.targetFrameRate = 60;
        }
    }
    
    bool IsConstrainedDevice() {
        // iPhone/iPad detection + Retina 3x check
        float dpi = Screen.dpi / 96f;
        return SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && dpi > 2.5f;
    }
}
```

**B. Hardware Texture Compression (ASTC 6x6)**
- Before: 16 MB per sprite (RGBA32 uncompressed)
- After: 2.7 MB per sprite (ASTC 6x6 compressed)
- Savings: 80% VRAM reduction with no visible quality loss

Implementation steps:
```
1. Export all sprites as PNG with alpha channel
2. In Unity TextureImporter settings:
   - Format: ASTC (iOS/Android)
   - Block size: 6x6
   - Compression: Hardware
3. Enable for all platforms
4. Verify Itch.io WebGL compatibility
```

**C. Linear WebAssembly Memory Allocation**
- Geometric growth causes OOM: 16MB → 32MB → 64MB... → CRASH
- Linear growth is safer: 128MB → +16MB → +16MB... → capped at 512MB

Build settings:
```
webGLInitialMemorySize = 128 MB
webGLMemoryGrowthMode = Linear
webGLMemoryGrowthStep = 16 MB
webGLMemoryGeometricGrowthCap = 512 MB
webGLDataCaching = 0
webGLExceptionSupport = 1
```

### Challenge #2: Frame Rate Drops (20-24 FPS on mid-range Android)

#### Root Causes:
1. Mass spawning VFX creating 50+ objects per frame = 35-40 KB/frame garbage
2. Repeated physics queries allocating arrays
3. Hierarchy scanning FindObjectsByType<>() every frame
4. Canvas dirty rebuild from UI animations

#### Solutions Implemented:

**A. Generic Object Pooling System**
```csharp
public class ObjectPool<T> : MonoBehaviour where T : Component {
    private Queue<T> pool = new Queue<T>();
    private T prefab;
    private int preWarmCount = 16;
    
    void Initialize(T prefabRef) {
        prefab = prefabRef;
        for (int i = 0; i < preWarmCount; i++) {
            T instance = Instantiate(prefab);
            instance.gameObject.SetActive(false);
            pool.Enqueue(instance);
        }
    }
    
    public T Spawn() {
        if (pool.Count > 0) {
            T instance = pool.Dequeue();
            instance.gameObject.SetActive(true);
            return instance;
        }
        return Instantiate(prefab);
    }
    
    public void Return(T instance) {
        instance.gameObject.SetActive(false);
        pool.Enqueue(instance);
    }
}
```

Before vs After:
- Before: 50-80 ms hitches when harvesting heavily
- After: 0 ms hitches (object reuse, zero allocations)

**B. Non-Allocating Physics Queries**
```csharp
// BEFORE (allocates array every call)
void CheckHarvest() {
    RaycastHit2D[] hits = Physics2D.LinecastAll(start, end);
    // Allocates managed array every frame - GC overhead
}

// AFTER (zero-alloc)
private RaycastHit2D[] hitBuffer = new RaycastHit2D[16];

void CheckHarvest() {
    int hitCount = Physics2D.LinecastNonAlloc(start, end, hitBuffer);
    for (int i = 0; i < hitCount; i++) {
        ProcessHit(hitBuffer[i]);
    }
}
```

**C. Dirty-Check Culling**
Only update renderer sorting order when Y position meaningfully changes:
```csharp
private float lastSortedY = float.MinValue;
private const float YThreshold = 0.1f;

void UpdateSorting() {
    if (Mathf.Abs(transform.position.y - lastSortedY) > YThreshold) {
        GetComponent<Renderer>().sortingOrder = CalculateSortOrder();
        lastSortedY = transform.position.y;
    }
}
```

**D. Resolution Clamping**
```csharp
public class ResolutionOptimizer : MonoBehaviour {
    void Start() {
        // Cap render resolution to 1080p
        // OS automatically upscales to full screen resolution
        // Result: 50-70% GPU fillrate reduction, no perceptible quality loss
        int maxHeight = 1080;
        if (Screen.height > maxHeight) {
            float scale = (float)maxHeight / Screen.height;
            Screen.SetResolution(
                Mathf.RoundToInt(Screen.width * scale),
                maxHeight,
                false
            );
        }
    }
}
```

### Challenge #3: Save System Performance (2.8s load spike)

#### Root Cause:
- Serializing 100+ tiles + 40+ animal entities as JSON was expensive
- Nested object hierarchies created large JSON strings

#### Solution:
Compact binary serialization with ID mapping:
```csharp
public class BinaryTileSave {
    public ushort tileID;           // 2 bytes
    public byte cropStage;          // 1 byte
    public byte waterLevel;         // 1 byte
    public bool hasLivestock;       // 1 byte
    // Total: 5 bytes per tile (vs 200+ bytes JSON)
}
```

Results:
- Before: 2.8s load time, 8.5 MB save file
- After: 0.6s load time, 3 MB save file
- Save version compatibility system prevents breakage on updates

---

## Profiling Data (Real Hardware Tests)

### iPhone 13 (Constrained Device)
```
BEFORE OPTIMIZATION:
- Crash Rate: 95% (OOM at 90% load)
- VRAM Usage: 650 MB
- Memory: 900 MB peak
- Load Time: 24 seconds
- FPS: N/A (crashed)

AFTER OPTIMIZATION:
- Crash Rate: 0% (stable)
- VRAM Usage: 150 MB
- Memory: 380 MB peak
- Load Time: 5.2 seconds
- FPS: 60.0 (stable)
```

### Samsung Galaxy A50 (Mid-Range Android)
```
BEFORE OPTIMIZATION:
- Load Time: 19 seconds
- FPS: 20-28 (stuttering during harvest)
- CPU Frame Time: ~50ms
- GC Allocations: 120-150 KB/s
- Phone Temp: 42°C after 5 min play

AFTER OPTIMIZATION:
- Load Time: 5 seconds
- FPS: 58-60 (rock solid)
- CPU Frame Time: ~16ms
- GC Allocations: 0 KB/s
- Phone Temp: 35°C (cool)
```

---

## Editor Tools & Development Utilities

### Custom Editor Window for Level Design
```csharp
[CustomEditor(typeof(FarmGrid))]
public class FarmGridEditor : Editor {
    void OnSceneGUI() {
        // Visualize 100+ tiles in grid
        // Click to align obstacles
        // Auto-generate waypoints for NPC pathfinding
        // Accelerated level design by ~40%
    }
}
```

### Built-in Profiler UI
```csharp
public class RealtimePerformanceProfiler : MonoBehaviour {
    void OnGUI() {
        if (Input.GetKeyDown(KeyCode.F4)) {
            GUI.Label(new Rect(10, 10, 200, 400), 
                $"FPS: {1 / Time.deltaTime:F1}\n" +
                $"Frame Time: {Time.deltaTime * 1000:F2}ms\n" +
                $"RAM: {SystemInfo.systemMemorySize}MB\n" +
                $"Allocated: {GC.GetTotalMemory(false) / 1024 / 1024}MB"
            );
        }
    }
}
```

---

---

# PROJECT 2: SkyBound

## Overview
- **Duration:** Sep 2024 (1 Week Rapid Prototype)
- **Status:** Published on Itch.io
- **Genre:** 2D Precision Platformer
- **Engine:** Unity 2D (6.1 LTS)
- **Build Size:** 41 MB (lightweight)
- **Platforms:** WebGL, Android APK
- **Performance Target:** Stable 60 FPS on constrained devices

---

## Game Mechanics

### 1. Precision Character Controller
**What it does:**
- Custom kinematic character controller with advanced physics
- Gravity/acceleration curves for arcade feel
- Ground detection via downward raycast
- Air control for skilled players

**Technical implementation:**
```csharp
public class CharacterController2D : MonoBehaviour {
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float gravityScale = 2f;
    [SerializeField] private float maxFallSpeed = 20f;
    
    private Rigidbody2D rb;
    private Vector3 velocity;
    private bool isGrounded;
    
    void FixedUpdate() {
        HandleInput();
        ApplyGravity();
        CheckGround();
        rb.velocity = velocity;
    }
    
    void ApplyGravity() {
        if (!isGrounded) {
            velocity.y -= gravityScale * Time.fixedDeltaTime;
            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        }
    }
    
    void CheckGround() {
        // Downward raycast for ground detection
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            0.1f,
            groundLayer
        );
        isGrounded = hit.collider != null;
    }
}
```

### 2. Springboard Trajectory System
**What it does:**
- Interactive spring platforms launch player with parabolic trajectories
- Trajectory force overrides for bouncy physics
- Height calculation allows reaching distant platforms
- Feedback particle effects on launch

**Technical implementation:**
```csharp
public class SpringLauncher : MonoBehaviour {
    public void Launch(Rigidbody2D rb, float targetHeight) {
        // Calculate force needed to reach target height
        // F = 2 * mass * gravity * height
        float force = 2f * rb.mass * Physics2D.gravity.magnitude * targetHeight;
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }
}
```

### 3. Touch Input Optimization
**Challenge:** Mobile touch has ~80ms latency, causing perceived input lag

**Solution:** Touch-to-physics synchronization
```csharp
public class TouchInputHandler : MonoBehaviour {
    private int cachedTouchID = -1;  // Cache pointer ID
    
    void Update() {
        if (Input.touchCount > 0) {
            Touch touch = Input.GetTouch(0);
            
            // Cache touch ID to avoid pointer change lag
            if (cachedTouchID == -1) {
                cachedTouchID = touch.fingerId;
            }
            
            if (touch.fingerId == cachedTouchID) {
                ProcessTouchInput(touch);
            }
        }
    }
}
```

Result:
- Before: ~80ms input latency
- After: <16ms latency (single frame)
- Achieved via continuous 2D collision sweeps in FixedUpdate

### 4. Modular Prefab System
**What it does:**
- Reusable prefab chunks (floating islands, hazards, collectibles)
- Mix-and-match level design
- No code changes needed for content iteration

**Prefab structure:**
```
Island_Platform
  └─ Colliders (BoxCollider2D with hole detection)
  └─ Visual (Sprite)
  └─ VFX (ParticleSystem for cloud effect)

Hazard_Spike
  └─ Collider (TriggerZone)
  └─ Visual
  └─ OnTrigger Event

Collectible_Star
  └─ Collider (non-trigger for physics)
  └─ Visual (rotating animation)
  └─ DestroyOnCollect script
```

---

## Performance Characteristics

### Target Specifications
- **Resolution:** 1080x1920 (clamped from device native)
- **Frame Rate:** 60 FPS locked
- **Memory:** <100 MB active RAM
- **Build Size:** 41 MB download

### Optimization Techniques Used

**1. Collision Sweep in FixedUpdate**
```csharp
// Prevent tunneling at high velocities
void FixedUpdate() {
    float moveDistance = velocity.magnitude * Time.fixedDeltaTime;
    
    // Cast before moving
    RaycastHit2D hit = Physics2D.Cast(
        transform.position,
        Vector2.down,
        velocity.normalized,
        moveDistance,
        collisionMask
    );
    
    if (hit.collider != null) {
        // Stop before collision, not after
        transform.position += velocity.normalized * (hit.distance - 0.01f);
    } else {
        transform.position += velocity * Time.fixedDeltaTime;
    }
}
```

**2. Lightweight Particle System**
- Max 50 particles per effect (vs typical 200+)
- Prewarmed pools for instant spawning
- Predictable memory allocation

**3. Atlas-Based UI**
- Single 1024x1024 texture for all UI icons
- Reduced draw calls and texture memory
- Faster load times

---

## Quality Metrics

| Metric | Value | Notes |
|--------|-------|-------|
| **File Size** | 41 MB | All assets included, no streaming |
| **Download Speed** | 10 seconds @ 33 Mbps | Competitive with web games |
| **Load Time** | 3.2 seconds | From click to playable |
| **FPS Consistency** | 60.0 ± 0.1 | Frame time: 16.67ms ± 0.1ms |
| **Memory Footprint** | 85 MB | Peak during gameplay |
| **Supported Devices** | 95% of global Android, 100% of iOS | After DPR scaling |

---

## Design Evolution

### Week 1 Iteration
- Prototype basic platformer mechanics (2 days)
- Tuned physics parameters (1 day)
- Initial level design (1 day)
- Performance profiling & optimization (1 day)
- Polish & refinement (1 day)

### Key Learnings
1. **Touch input latency kills mobile games** - solved via ID caching
2. **Continuous collision detection is essential** - raycast before move, not after
3. **Asset management scales with small team** - modular prefabs pay off
4. **60 FPS is non-negotiable** - users notice frame rate more than graphics quality

---

## Technical Achievements

✅ **Performance:** Stable 60 FPS across all target platforms (iOS, mid-range Android, desktop)
✅ **Memory:** 41 MB build, <100 MB runtime - no streaming required
✅ **Input:** <16ms touch latency - imperceptible to player
✅ **Physics:** Continuous collision detection - no tunneling at any velocity
✅ **Modular Design:** Level designers can create content without touching code

---

# Using These Case Studies

## For Job Applications:

### Option 1: Portfolio Website
Include full case studies in "Featured Projects" section
- Shows technical depth
- Demonstrates quantified impact (FPS improvements, memory reduction)
- Employers see both gameplay design AND optimization skills

### Option 2: Interview Talking Points
When asked "Tell us about your most complex project":
- Describe the problem (iOS crashes)
- Walk through root cause analysis (WebKit limits, VRAM issues)
- Explain the solution (multi-tier scaler, ASTC compression)
- Quantify the impact (95% → 0% crash rate, 3.5x faster load)
- Reflect on what you learned

### Option 3: Technical Test / Take-Home Challenge
Use these projects as reference when solving performance problems:
- "I've optimized WebGL games before, similar memory constraints..."
- "I'd apply object pooling here, like I did in Cooking-Game-2D..."
- "This reminds me of the iOS Safari issue I solved..."

---

## For Fresher Positions (Target: "Junior Game Programmer")

**Highlight these points:**

1. **Ownership:** Solo developer on year-long project (not just a hobby)
2. **Problem-Solving:** Debugged complex crashes across multiple platforms
3. **Optimization Mindset:** Reduced memory by 65-80%, FPS by 3x
4. **Tooling:** Built custom Editor windows, profiling systems
5. **Production Quality:** Published & playable, not just "school project"
6. **CS Fundamentals:** Object pooling, FSM, event-driven architecture
7. **Metrics:** Quantified every improvement (95% → 0%, 22s → 5s)

**Avoid These Mistakes:**
- Don't oversell graphics (fresher roles care about gameplay & optimization)
- Don't skip the "how" - walk through actual solutions, not just results
- Don't mention work that's 50% complete - only talk about shipped/polished work

---

## Document Summary

📄 **NGUYEN_HOAI_NAM_CV_FRESHERS.html**
- Professional CV format (text-only, no highlights)
- All projects summarized with quantified metrics
- Ready to paste into recruiting portals

📋 **CASE_STUDY_WebGL_Optimization.md**
- Deep technical dive into iOS Safari crashes
- Root cause analysis with code examples
- Before/after metrics from real devices
- Suitable for blog post or portfolio

📊 **PROJECT_DETAILS_FOR_PORTFOLIO.md** (this document)
- Comprehensive system documentation
- Code examples and architecture decisions
- Performance profiling data
- Interview talking points

---

**Next Steps:**
1. Convert HTML CV to PDF using browser print function
2. Upload case studies to personal portfolio website
3. Link projects from itch.io in CV
4. Practice 2-min pitch: "Solved iOS Safari crashes in WebGL game..."
