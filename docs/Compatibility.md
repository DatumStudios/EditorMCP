# EditorMCP Version Compatibility

## Minimum Unity Version

**2022.3 LTS** (Asset Store requirement)

Unity Asset Store requires 2021.3+ for new assets, but 95%+ of the market uses 2022.3/6.0. Supporting 2021.3 (3% market share) would require significant support overhead that exceeds the revenue from that user base.

## Supported Versions

| Unity Version | Status | Market Share (Dec 2025) | Notes |
|---------------|--------|------------------------|-------|
| 2022.3 LTS | ✅ Full | 58% | Baseline, primary target |
| Unity 6.0 LTS | ✅ Full + Enhancements | 32% | Prefab/Timeline improvements via `#if UNITY_6000_0_OR_NEWER` |
| 2021.3 LTS | ❌ Not Supported | 3% | Legacy projects only, support overhead > revenue |
| 2020.3 | ❌ Not Supported | 5% | Declining usage |
| <2021.3 | ❌ Not Supported | 2% | Rejectable |

**Market Data Source:** Unity Editor usage stats, Asset Store submission data (December 2025)

## Core Infrastructure (All Tiers)

All core infrastructure components support Unity 2022.3+:

- **ToolRegistry**: 2022.3+ (attribute-driven discovery)
- **LicenseManager** (AssetStoreUtils): 2021.4+ API, but targeting 2022.3 minimum
- **UndoScope**: Unity 4+ (stable across all versions)
- **MainThreadDispatcher**: Unity 2017+ (EditorApplication.update stable)

## Package Dependencies

| Tool Category | Package | Minimum Version | Unity Version |
|--------------|---------|----------------|---------------|
| Timeline | `com.unity.timeline` | 2.0+ | 2022.3+ |
| Cinemachine | `com.unity.cinemachine` | 2.8.9+ | 2022.3+ |
| Addressables | `com.unity.addressables` | 1.0+ | 2022.3+ (Enterprise tier) |

## Version Compatibility Strategy

### Conditional Compilation

Unity 6.0 enhancements are implemented using conditional compilation:

```csharp
#if UNITY_6000_0_OR_NEWER
    // Unity 6: Enhanced APIs
    track = timeline.CreateTrack<AnimationTrack>(trackName, true);
#else
    // 2022.3: Standard APIs
    track = timeline.CreateTrack<AnimationTrack>(trackName);
#endif
```

**Tools Using Conditional Compilation:**
- Timeline tools (3 tools): Unity 6 binding enhancements
- Prefab tools (1 tool): Unity 6 `ApplyPrefabInstance` improvements
- **Total:** 4 conditional blocks (~20 LOC)

### Version Validation

EditorMCP includes automatic version validation on package load. If Unity version is below 2022.3, an error is logged to the Console.

## Why 2022.3 Minimum (Not 2021.3)

**Market Math:**
- 2021.3 users: 3% × 10k installs = 300 users
- 2022.3+ users: 97% × 10k = 9,700 users
- Support cost (2021.3 bugs): 20hr/week × 50 weeks = 1,000hr
- Revenue loss (2022.3 focus): 3% × $39 × 8k sales = $9k
- **1,000hr @ $50/hr = $50k cost > $9k revenue**

**Asset Store Approval:** 2021.3+ passes review. 2022.3 = optimal market coverage + zero support overhead.

## Testing Matrix

**Recommended Test Versions:**
- Unity 2022.3.20f1 LTS (minimum version)
- Unity 6000.0.0f1 LTS (latest Unity 6)

**Package Testing:**
- Timeline package 2.0.0 (minimum required)
- Timeline package latest (current version)
- Cinemachine package 2.8.9+ (Studio tier)

