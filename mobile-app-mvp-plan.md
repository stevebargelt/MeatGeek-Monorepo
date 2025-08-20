# MeatGeek Mobile App - 3-Day MVP Plan

## Executive Summary

This plan outlines a streamlined approach to building a functional MeatGeek mobile app MVP using Infinite Red's Ignite framework. The goal is to have a working iOS app that can interact with the production Sessions API within 3 days, leveraging Ignite's out-of-the-box tools and patterns for maximum development velocity.

## MVP Scope (3 Days)

### Day 1: Project Setup & Multi-Probe UI
- Initialize Ignite project with default settings
- Generate 3 core screens using Ignite CLI
- Set up basic navigation structure
- **Design multi-probe temperature display layout**
- Configure production API endpoint

### Day 2: API Integration & Auto-Refresh
- Implement MobX-State-Tree models with multi-probe support
- Configure Apisauce for Sessions API
- Wire up basic CRUD operations
- **Add auto-refresh timer (30-60 seconds)**
- **Implement pull-to-refresh on all screens**
- **Add session persistence with MMKV**

### Day 3: Reliability & Polish
- Test with production API and test device
- **Add connection status indicator**
- **Implement retry logic for failed API calls**
- **Add basic temperature threshold alerts**
- Polish UI with Ignite's theme system
- Prepare for initial deployment

## Core Features (MVP Only)

### ✅ Included in MVP
- View current cooking session status
- **Multiple temperature probe display** (grill + up to 4 meat probes)
- **Cook duration/elapsed time display** (critical for BBQ timing)
- **Probe naming/labeling** ("Brisket Point", "Ribs", etc.)
- **Cook name/description** for session identification
- **Temperature unit toggle** (Fahrenheit/Celsius)
- Start a new cooking session
- End an active cooking session
- View list of previous cooking sessions
- **Auto-refresh timer** (30-60 second intervals)
- **Pull-to-refresh gesture** on all screens
- **Session persistence** when app closes/reopens
- **Connection status indicator**
- **Error recovery with retry buttons**
- **Temperature presets** (225°F, 275°F, 350°F)
- **In-app alert sounds** when targets reached
- iOS-only (iPhone & iPad)

### ❌ Deferred for Later
- Real-time temperature updates
- Push notifications
- Complex temperature charts
- Advanced retry logic
- User authentication
- Device pairing/management
- Background monitoring
- TanStack Query integration

## Screen Design (3 Screens Only)

### 1. Active Cook Screen (Home)
The app's main screen shows the current cooking session status with large, readable temperature displays organized by probe type (grill chamber + up to 4 meat probes). Each temperature card displays the user-defined probe label ("Brisket Point", "Ribs") with current vs. target temperatures in the user's preferred units (F/C). A prominent elapsed time display shows how long the cook has been running. Color-coded indicators show at-target, close, or off-target temperatures with in-app sound alerts when targets are reached. Features auto-refresh every 30 seconds with pull-to-refresh gesture support and a connection status indicator in the header.

### 2. Start Cook Screen (Session Setup)
A streamlined form interface for creating new cooking sessions with essential BBQ workflow. Users enter a cook name ("Memorial Day Brisket"), select protein type, and configure probe settings including custom labels ("Point", "Flat", "Ribs"). Temperature inputs feature quick preset buttons (225°F, 275°F, 350°F) and unit toggle (F/C). Each probe can be individually configured with target temperatures and custom names. The screen validates inputs and includes a "Start Cook" button that creates the session via API and navigates to the active monitoring screen.

### 3. Cook History Screen (Past Sessions)
A chronological list view of completed cooking sessions with meaningful identification. Each row displays the cook name, date, protein type, total cook duration, and temperature summary with visual success indicators. Cook names like "Memorial Day Brisket" or "Competition Practice" make sessions easily identifiable. Users can tap any session to view detailed results including probe labels, temperature ranges, and timing data. The list supports pull-to-refresh and works offline with cached session data.

## Technology Stack (Ignite Defaults)

### Core Framework
- **React Native 0.79+** with **TypeScript 5**
- **Ignite CLI** for project generation and scaffolding
- **MobX-State-Tree** (keep Ignite's default state management)
- **Apisauce** (keep Ignite's default HTTP client)

### Pre-configured Libraries (From Ignite)
- **React Navigation 7** - Screen navigation
- **MMKV** - Local storage for offline data
- **React Native Reanimated 3** - Smooth animations
- **Reactotron** - Development debugging
- **Jest** - Unit testing framework
- **Maestro** - E2E testing (if time permits)

## MobX-State-Tree Implementation (Simplified)

### Root Store
```typescript
// app/models/RootStore.ts
export const RootStoreModel = types.model("RootStore", {
  sessionStore: types.optional(SessionStoreModel, {}),
})
```

### Session Store (Minimal)
```typescript
// app/models/SessionStore.ts
const ProbeReading = types.model("ProbeReading", {
  probeId: types.string,
  probeName: types.string, // "Grill", "Probe 1", "Probe 2", etc.
  probeLabel: types.maybeNull(types.string), // User-defined: "Brisket Point", "Ribs"
  currentTemp: types.maybeNull(types.number),
  targetTemp: types.maybeNull(types.number),
  isActive: types.boolean,
})

const SessionModel = types.model("Session", {
  id: types.string,
  smokerId: types.string,
  cookName: types.maybeNull(types.string), // "Memorial Day Brisket", "Test Cook #3"
  proteinType: types.maybeNull(types.string),
  probes: types.array(ProbeReading),
  startTime: types.Date,
  endTime: types.maybeNull(types.Date),
  status: types.enumeration(["active", "completed"]),
  lastUpdated: types.Date,
})
.views((self) => ({
  get duration() {
    const end = self.endTime || new Date()
    return Math.floor((end.getTime() - self.startTime.getTime()) / 1000) // seconds
  },
  get durationFormatted() {
    const hours = Math.floor(self.duration / 3600)
    const minutes = Math.floor((self.duration % 3600) / 60)
    return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`
  }
}))

export const SessionStoreModel = types
  .model("SessionStore", {
    sessions: types.array(SessionModel),
    isLoading: false,
    isConnected: true,
    lastRefresh: types.maybeNull(types.Date),
    error: types.maybeNull(types.string),
    autoRefreshEnabled: true,
    usesCelsius: false,
    temperaturePresets: types.array(types.model({
      name: types.string,
      temperature: types.number,
    })),
  })
  .actions((self) => ({
    afterCreate() {
      // Set up default temperature presets
      self.temperaturePresets.push(
        { name: "Low & Slow", temperature: 225 },
        { name: "Hot & Fast", temperature: 275 },
        { name: "Chicken", temperature: 350 },
      )
    },
  }))
  .actions((self) => ({
    loadSessions: flow(function* () {
      self.isLoading = true
      try {
        const sessions = yield api.getSessions(Config.SMOKER_ID)
        self.sessions.replace(sessions)
        self.lastRefresh = new Date()
        self.isConnected = true
        self.error = null
        // Persist to MMKV for offline access
        storage.set("sessions", JSON.stringify(getSnapshot(self.sessions)))
      } catch (error) {
        self.error = error.message
        self.isConnected = false
      } finally {
        self.isLoading = false
      }
    }),
    startSession: flow(function* (sessionData) {
      try {
        const newSession = yield api.createSession(sessionData)
        self.sessions.push(newSession)
        self.error = null
      } catch (error) {
        self.error = error.message
      }
    }),
    setAutoRefresh: (enabled: boolean) => {
      self.autoRefreshEnabled = enabled
    },
    hydrate: () => {
      // Load persisted sessions on app start
      const persistedSessions = storage.getString("sessions")
      if (persistedSessions) {
        try {
          const sessions = JSON.parse(persistedSessions)
          self.sessions.replace(sessions)
        } catch (error) {
          console.warn("Failed to hydrate sessions:", error)
        }
      }
      // Load temperature preference
      const tempUnit = storage.getString("temperatureUnit")
      if (tempUnit === "celsius") {
        self.usesCelsius = true
      }
    },
    toggleTemperatureUnit: () => {
      self.usesCelsius = !self.usesCelsius
      storage.set("temperatureUnit", self.usesCelsius ? "celsius" : "fahrenheit")
    },
    retryLastAction: () => {
      // Retry the last failed operation
      self.loadSessions()
    },
  }))
  .views((self) => ({
    get activeSession() {
      return self.sessions.find(session => session.status === "active")
    }
  }))
```

## API Integration (Simple Apisauce)

### Basic API Client
```typescript
// app/services/api/api.ts
import { ApisauceInstance, create } from "apisauce"
import Config from "../config"

export class Api {
  apisauce: ApisauceInstance

  constructor() {
    this.apisauce = create({
      baseURL: Config.API_URL, // https://meatgeeksessionsapi.azurewebsites.net
      timeout: 10000,
      headers: {
        "Accept": "application/json",
        "Content-Type": "application/json",
      },
    })
  }

  async getSessions(smokerId: string) {
    const response = await this.apisauce.get(`/api/sessions/${smokerId}`)
    if (!response.ok) throw new Error(`Failed to load sessions: ${response.problem}`)
    return response.data
  }

  async createSession(smokerId: string, sessionData: any) {
    const response = await this.apisauce.post(`/api/sessions/${smokerId}/start`, sessionData)
    if (!response.ok) throw new Error(`Failed to create session: ${response.problem}`)
    return response.data
  }

  async endSession(smokerId: string) {
    const response = await this.apisauce.delete(`/api/sessions/${smokerId}/end`)
    if (!response.ok) throw new Error(`Failed to end session: ${response.problem}`)
    return response.data
  }

  async getTemperatures(smokerId: string) {
    const response = await this.apisauce.get(`/api/sessions/${smokerId}/temperatures`)
    if (!response.ok) throw new Error(`Failed to get temperatures: ${response.problem}`)
    return response.data
  }
}
```

### Environment Configuration
```typescript
// app/config/config.base.ts
export interface ConfigBaseProps {
  API_URL: string
  SMOKER_ID: string
}

const BaseConfig: ConfigBaseProps = {
  API_URL: "https://meatgeeksessionsapi.azurewebsites.net",
  SMOKER_ID: process.env.EXPO_PUBLIC_SMOKER_ID || "test-smoker-1",
}
```

## 3-Day Development Timeline

### Day 1: Foundation (Setup & Screens)
**Morning (2-3 hours):**
```bash
npx ignite-cli@latest new MeatGeekMobile --yes
cd MeatGeekMobile
npx ignite-cli generate screen ActiveCook
npx ignite-cli generate screen StartCook
npx ignite-cli generate screen CookHistory
```

**Afternoon (4-5 hours):**
- Set up basic navigation in `app/navigators/AppNavigator.ts`
- Configure environment variables for API endpoint
- **Create multi-probe temperature display components**
  - Grill/chamber temperature card with large display
  - Up to 4 probe temperature cards with target indicators
  - Color-coded status (green=at target, yellow=close, red=off)
  - **Cook duration display component** (elapsed time)
  - **Probe naming/labeling UI**
  - **Temperature unit toggle**
- Test navigation flow between screens

### Day 2: API & State (Integration)
**Morning (3-4 hours):**
```bash
npx ignite-cli generate model Session
```
- Implement SessionStore with basic CRUD operations
- Configure Apisauce for Sessions API
- Add API calls to MST actions

**Afternoon (4-5 hours):**
- Wire up screens to SessionStore with multi-probe support
- **Implement cook naming and probe labeling in forms**
- **Add temperature presets (225°F, 275°F, 350°F) as quick buttons**
- **Implement auto-refresh timer:**
  ```typescript
  useEffect(() => {
    const timer = setInterval(() => {
      if (sessionStore.autoRefreshEnabled) {
        sessionStore.loadSessions()
      }
    }, 30000) // 30 seconds
    return () => clearInterval(timer)
  }, [])
  ```
- **Add pull-to-refresh to all scrollable screens**
- **Implement session persistence on app close/reopen**
- Add loading states and connection status indicators
- Test with production API endpoints

### Day 3: Testing & Polish
**Morning (3-4 hours):**
- Test complete user flows with test device
- **Add connection status indicator with retry buttons**
- **Implement in-app sound alerts for temperature targets**
- **Add error recovery with manual retry buttons:**
  ```typescript
  {error && (
    <View style={$errorContainer}>
      <Text style={$errorText}>{error}</Text>
      <Button text="Retry" onPress={() => sessionStore.loadSessions()} />
      <Button text="Work Offline" preset="reversed" />
    </View>
  )}
  ```
- Polish UI using Ignite's theme system

**Afternoon (3-4 hours):**
- Fix any bugs found during testing
- **Test all BBQ-critical features:**
  - Cook duration tracking accuracy
  - Probe naming and labeling
  - Temperature unit conversions (F/C)
  - Sound alerts when targets reached
  - Error recovery with offline mode
- **Test session persistence across app restarts**
- **Verify auto-refresh functionality with multiple probes**
- Add basic form validation for required fields
- Prepare for deployment (iOS build configuration)
- Document MVP features and known limitations

## Screen Implementation Details

### Active Cook Screen
```typescript
// app/screens/ActiveCookScreen.tsx
export const ActiveCookScreen: FC<ActiveCookScreenProps> = observer(function ActiveCookScreen() {
  const { sessionStore } = useStores()
  const activeSession = sessionStore.activeSession
  const [refreshing, setRefreshing] = useState(false)

  // Auto-refresh timer
  useEffect(() => {
    const timer = setInterval(() => {
      if (sessionStore.autoRefreshEnabled && !sessionStore.isLoading) {
        sessionStore.loadSessions()
      }
    }, 30000) // 30 seconds
    return () => clearInterval(timer)
  }, [])

  const onRefresh = async () => {
    setRefreshing(true)
    await sessionStore.loadSessions()
    setRefreshing(false)
  }

  return (
    <Screen preset="fixed" safeAreaEdges={["top"]}>
      {/* Connection Status Bar */}
      <View style={$statusBar}>
        <View style={[$statusDot, { backgroundColor: sessionStore.isConnected ? colors.success : colors.error }]} />
        <Text style={$statusText}>
          {sessionStore.isConnected ? 'Connected' : 'Offline'} • 
          Last update: {sessionStore.lastRefresh?.toLocaleTimeString() || 'Never'}
        </Text>
      </View>

      <ScrollView
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
        contentContainerStyle={$scrollContent}
      >
        <Text preset="heading">Current Cook</Text>
        
        {activeSession ? (
          <View>
            <Text preset="subheading">{activeSession.cookName}</Text>
            <CookDuration startTime={activeSession.startTime} />
            
            {/* Multi-probe temperature display */}
            {activeSession.probes.filter(p => p.isActive).map((probe) => (
              <TemperatureCard
                key={probe.probeId}
                probe={probe}
                usesCelsius={sessionStore.usesCelsius}
                onTargetReached={() => {
                  // Haptic feedback and sound alert
                  Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success)
                  // Play sound alert
                  // Sound.play('alert.mp3')
                }}
              />
            ))}
            
            <Button 
              text={`Toggle to ${sessionStore.usesCelsius ? 'Fahrenheit' : 'Celsius'}`} 
              preset="reversed" 
              onPress={() => sessionStore.toggleTemperatureUnit()} 
            />
            <Button text="End Cook" onPress={() => sessionStore.endSession()} />
          </View>
        ) : (
          <View>
            <Text>No active cook session</Text>
            <Button text="Start New Cook" onPress={() => navigation.navigate("StartCook")} />
          </View>
        )}
        
        <Button text="View History" onPress={() => navigation.navigate("CookHistory")} />
      </ScrollView>
    </Screen>
  )
})

const $statusBar = {
  flexDirection: 'row' as const,
  alignItems: 'center' as const,
  padding: spacing.small,
  backgroundColor: colors.background,
}

const $statusDot = {
  width: 8,
  height: 8,
  borderRadius: 4,
  marginRight: spacing.tiny,
}

const $statusText = {
  ...typography.caption,
  color: colors.textDim,
}

const $scrollContent = {
  padding: spacing.medium,
}
```

## Custom Components

### TemperatureCard Component
```typescript
// app/components/TemperatureCard.tsx
import { observer } from "mobx-react-lite"
import React from "react"
import { View, Text } from "react-native"
import { colors, spacing, typography } from "../theme"

interface TemperatureCardProps {
  probe: ProbeReading
  usesCelsius: boolean
  onTargetReached?: () => void
}

export const TemperatureCard = observer(function TemperatureCard(props: TemperatureCardProps) {
  const { probe, usesCelsius, onTargetReached } = props
  
  const convertTemp = (temp: number) => {
    return usesCelsius ? Math.round((temp - 32) * 5/9) : Math.round(temp)
  }
  
  const tempUnit = usesCelsius ? '°C' : '°F'
  
  const getTemperatureColor = () => {
    if (!probe.currentTemp || !probe.targetTemp) return colors.textDim
    const diff = Math.abs(probe.currentTemp - probe.targetTemp)
    if (diff <= 5) {
      onTargetReached?.()
      return colors.success
    }
    if (diff <= 15) return colors.warning
    return colors.error
  }

  const getStatusText = () => {
    if (!probe.currentTemp || !probe.targetTemp) return "--"
    const diff = convertTemp(probe.currentTemp) - convertTemp(probe.targetTemp)
    if (Math.abs(diff) <= 3) return "At Target"
    return diff > 0 ? `+${diff}${tempUnit}` : `${diff}${tempUnit}`
  }

  return (
    <View style={$card}>
      <Text style={$probeLabel}>
        {probe.probeLabel || probe.probeName}
      </Text>
      <View style={$temperatureRow}>
        <Text style={[$currentTemp, { color: getTemperatureColor() }]}>
          {probe.currentTemp ? convertTemp(probe.currentTemp) : "--"}{tempUnit}
        </Text>
        <View style={$targetInfo}>
          <Text style={$targetLabel}>
            Target: {probe.targetTemp ? convertTemp(probe.targetTemp) : "--"}{tempUnit}
          </Text>
          <Text style={[$statusText, { color: getTemperatureColor() }]}>
            {getStatusText()}
          </Text>
        </View>
      </View>
    </View>
  )
})

const $card = {
  backgroundColor: colors.palette.neutral100,
  borderRadius: 12,
  padding: spacing.medium,
  marginVertical: spacing.small,
  shadowColor: colors.palette.neutral800,
  shadowOffset: { width: 0, height: 2 },
  shadowOpacity: 0.1,
  shadowRadius: 4,
  elevation: 3,
}

const $probeLabel = {
  ...typography.body,
  fontWeight: '600',
  marginBottom: spacing.tiny,
}

const $temperatureRow = {
  flexDirection: 'row' as const,
  alignItems: 'center' as const,
  justifyContent: 'space-between' as const,
}

const $currentTemp = {
  ...typography.xxl,
  fontWeight: '800',
}

const $targetInfo = {
  alignItems: 'flex-end' as const,
}

const $targetLabel = {
  ...typography.caption,
  color: colors.textDim,
}

const $statusText = {
  ...typography.sm,
  fontWeight: '600',
}
```

### Cook Duration Component
```typescript
// app/components/CookDuration.tsx
import { observer } from "mobx-react-lite"
import React, { useEffect, useState } from "react"
import { Text } from "react-native"
import { typography, colors } from "../theme"

interface CookDurationProps {
  startTime: Date
  endTime?: Date
}

export const CookDuration = observer(function CookDuration(props: CookDurationProps) {
  const { startTime, endTime } = props
  const [duration, setDuration] = useState(0)
  
  useEffect(() => {
    const updateDuration = () => {
      const end = endTime || new Date()
      const seconds = Math.floor((end.getTime() - startTime.getTime()) / 1000)
      setDuration(seconds)
    }
    
    updateDuration()
    const timer = setInterval(updateDuration, 60000) // Update every minute
    return () => clearInterval(timer)
  }, [startTime, endTime])
  
  const formatDuration = (seconds: number) => {
    const hours = Math.floor(seconds / 3600)
    const minutes = Math.floor((seconds % 3600) / 60)
    return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`
  }
  
  return (
    <Text style={$durationText}>
      Cook Time: {formatDuration(duration)}
    </Text>
  )
})

const $durationText = {
  ...typography.title,
  color: colors.text,
  textAlign: 'center' as const,
  marginVertical: 16,
}
```

### Start Cook Screen
```typescript
// app/screens/StartCookScreen.tsx
export const StartCookScreen: FC<StartCookScreenProps> = observer(function StartCookScreen() {
  const { sessionStore } = useStores()
  const [cookName, setCookName] = useState("")
  const [proteinType, setProteinType] = useState("Brisket")
  const [grillTemp, setGrillTemp] = useState("225")
  const [probeLabels, setProbeLabels] = useState(["Point", "Flat", "", ""])
  const [probeTargets, setProbeTargets] = useState(["203", "203", "", ""])
  
  const tempUnit = sessionStore.usesCelsius ? '°C' : '°F'
  
  const setPresetTemp = (temp: number) => {
    setGrillTemp(temp.toString())
  }
  
  const handleStartCook = async () => {
    const probes = probeLabels.map((label, index) => ({
      probeId: `probe-${index + 1}`,
      probeName: `Probe ${index + 1}`,
      probeLabel: label || null,
      targetTemp: probeTargets[index] ? parseInt(probeTargets[index]) : null,
      currentTemp: null,
      isActive: !!probeTargets[index],
    }))
    
    await sessionStore.startSession({
      cookName: cookName || `${proteinType} Cook`,
      proteinType,
      smokerId: Config.SMOKER_ID,
      grillTarget: parseInt(grillTemp),
      probes,
    })
    navigation.navigate("ActiveCook")
  }

  return (
    <Screen preset="scroll" safeAreaEdges={["top"]}>
      <Text preset="heading">Start New Cook</Text>
      
      <TextField
        label="Cook Name"
        placeholder="Memorial Day Brisket"
        value={cookName}
        onChangeText={setCookName}
      />
      
      <TextField
        label="Protein Type"
        value={proteinType}
        onChangeText={setProteinType}
      />
      
      <Text preset="subheading">Grill Temperature</Text>
      <View style={$presetButtons}>
        {sessionStore.temperaturePresets.map((preset) => (
          <Button
            key={preset.name}
            text={`${preset.name} (${preset.temperature}${tempUnit})`}
            preset="reversed"
            onPress={() => setPresetTemp(preset.temperature)}
          />
        ))}
      </View>
      
      <TextField
        label={`Target Grill Temperature (${tempUnit})`}
        value={grillTemp}
        onChangeText={setGrillTemp}
        keyboardType="numeric"
      />
      
      <Text preset="subheading">Probe Setup</Text>
      {[0, 1, 2, 3].map((index) => (
        <View key={index} style={$probeRow}>
          <TextField
            label={`Probe ${index + 1} Label`}
            placeholder={`Brisket ${index < 2 ? (index === 0 ? 'Point' : 'Flat') : ''}`}
            value={probeLabels[index]}
            onChangeText={(text) => {
              const newLabels = [...probeLabels]
              newLabels[index] = text
              setProbeLabels(newLabels)
            }}
            style={$halfWidth}
          />
          <TextField
            label={`Target (${tempUnit})`}
            placeholder="203"
            value={probeTargets[index]}
            onChangeText={(text) => {
              const newTargets = [...probeTargets]
              newTargets[index] = text
              setProbeTargets(newTargets)
            }}
            keyboardType="numeric"
            style={$halfWidth}
          />
        </View>
      ))}
      
      <Button text="Start Cook" onPress={handleStartCook} />
      <Button text="Cancel" preset="reversed" onPress={() => navigation.goBack()} />
    </Screen>
  )
})

const $presetButtons = {
  flexDirection: 'row' as const,
  flexWrap: 'wrap' as const,
  gap: 8,
  marginVertical: 16,
}

const $probeRow = {
  flexDirection: 'row' as const,
  gap: 12,
  marginVertical: 8,
}

const $halfWidth = {
  flex: 1,
}
```

## Testing Strategy (Minimal for MVP)

### Manual Testing Checklist
- [ ] **Create cook with custom name** ("Test Brisket #1")
- [ ] **Label probes with custom names** ("Point", "Flat")
- [ ] **Toggle between Fahrenheit and Celsius**
- [ ] **Use temperature preset buttons** (225°F, 275°F, 350°F)
- [ ] Start new cook session with multiple probe targets
- [ ] **View elapsed cook time** (updates every minute)
- [ ] View active cook status with all probe temperatures
- [ ] **Sound alert plays when temperature target reached**
- [ ] **Auto-refresh works every 30 seconds**
- [ ] **Pull-to-refresh works on all screens**
- [ ] **Session persists when app is closed and reopened**
- [ ] **Connection status indicator updates correctly**
- [ ] **Error screens show retry buttons**
- [ ] **App works offline with cached data**
- [ ] End active cook session
- [ ] **View cook history with meaningful names**
- [ ] Handle API errors gracefully with retry options

### Basic Unit Tests (If Time Permits)
- Test SessionStore actions and views
- Test API client basic functionality
- Test screen navigation flows

## Environment Setup

### Required Environment Variables
```bash
# .env.local (not committed)
EXPO_PUBLIC_SMOKER_ID=test-smoker-1
EXPO_PUBLIC_API_URL=https://meatgeeksessionsapi.azurewebsites.net
```

### Test Device Integration
Use the existing `iot-edge/start-test-device.sh` to spin up a test IoT device for API interaction testing.

## Success Criteria for MVP

### Functional Requirements
- ✅ App launches without crashes
- ✅ **Cook duration displays and updates correctly**
- ✅ **Probe naming/labeling works in setup and display**
- ✅ **Cook names appear in session creation and history**
- ✅ **Temperature units toggle (F/C) converts all displays**
- ✅ **Temperature presets work as quick-select buttons**
- ✅ Can create new cooking session via API with multiple probe targets
- ✅ Can view current session status with all probe temperatures
- ✅ **Auto-refreshes data every 30 seconds**
- ✅ **Pull-to-refresh works on all screens**
- ✅ **Sessions persist across app restarts**
- ✅ **Connection status visible to user**
- ✅ **Sound alerts play when temperature targets reached**
- ✅ **Error recovery with retry buttons**
- ✅ Can end active cooking session
- ✅ Can view list of previous sessions with meaningful names
- ✅ Handles API errors gracefully with retry options

### Technical Requirements
- ✅ Uses Ignite's default stack (MST + Apisauce)
- ✅ iOS builds successfully
- ✅ Integrates with production Sessions API
- ✅ Works with test device from iot-edge
- ✅ Basic error handling and loading states

## Post-MVP Enhancement Path

### Week 2 Additions
- **Real-time WebSocket connections** (replace polling)
- Enhanced error handling with exponential backoff retry
- **Temperature trend charts** for historical analysis
- Settings screen for auto-refresh intervals and alert preferences
- **Session notes and photo attachments**
- **Multiple smoker device support**

### Future Phases
- Replace MST with TanStack Query for better API state management
- Add real-time temperature updates
- Implement push notifications
- Add temperature charts and analytics
- Integrate with device management
- Add user authentication
- Nx monorepo integration

## Final Timeline Summary

### Updated 4-Day Development Schedule
With the addition of critical BBQ functionality, the timeline has been extended by one day to ensure a truly usable MVP:

- **Day 1:** Project setup + multi-probe UI (5-6 hours)
- **Day 2:** API integration + BBQ features (6-7 hours)
- **Day 3:** Reliability + error handling (6-7 hours)
- **Day 4:** Testing + polish + deployment prep (4-5 hours)

**Total Development Time: ~24 hours over 4 days**

### Critical Features Added
These additions add only ~4 hours but prevent immediate user abandonment:
- Cook duration tracking (essential for BBQ timing)
- Probe naming/labeling (critical for multi-probe setups)
- Cook names (essential for session identification)
- Temperature units (F/C toggle for international users)
- Error recovery (retry buttons and offline mode)

## Why This Approach Works

### Leverages Ignite Strengths
- **Battle-tested patterns** proven across hundreds of apps
- **CLI generators** for rapid screen/model creation
- **Pre-configured tooling** eliminates setup decisions
- **Consistent architecture** from day one

### Focuses on Core BBQ Value
- **Complete BBQ workflow** (timing, multiple probes, temperature units)
- **Real-world usability** with probe labeling and cook names
- **Production API integration** validates core functionality
- **Genuine MVP** that BBQ enthusiasts would actually use
- **Iterative enhancement** path for future features

### Minimizes Risk While Maximizing Usability
- **Proven technology stack** reduces technical unknowns
- **Essential feature set** prevents scope creep while ensuring usability
- **4-day timeline** allows for proper BBQ-specific features
- **Single platform** (iOS) reduces testing surface
- **Offline capabilities** ensure reliability during long cooks

This approach delivers a genuinely useful BBQ monitoring app that users would keep and recommend, rather than a basic proof-of-concept they'd immediately delete.