# MeatGeek Mobile App - 3-Day MVP Plan

## Executive Summary

This plan outlines a streamlined approach to building a functional MeatGeek mobile app MVP using Infinite Red's Ignite framework. The goal is to have a working iOS app that can interact with the production Sessions API within 3 days, leveraging Ignite's out-of-the-box tools and patterns for maximum development velocity.

## MVP Scope (3 Days)

### Day 1: Project Setup & Core Screens
- Initialize Ignite project with default settings
- Generate 3 core screens using Ignite CLI
- Set up basic navigation structure
- Configure production API endpoint

### Day 2: API Integration & State Management
- Implement simple MobX-State-Tree models
- Configure Apisauce for Sessions API
- Wire up basic CRUD operations
- Add manual refresh functionality

### Day 3: Testing & Polish
- Test with production API and test device
- Add basic error handling
- Polish UI with Ignite's theme system
- Prepare for initial deployment

## Core Features (MVP Only)

### ✅ Included in MVP
- View current cooking session status
- Start a new cooking session
- End an active cooking session
- View list of previous cooking sessions
- Manual refresh to get latest data
- Basic error messages
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
The app's main screen shows the current cooking session status with large, readable temperature displays for the grill and probes. Features a prominent "Refresh" button to manually get latest data from the API. Quick action buttons allow users to adjust target temperatures or end the current session. If no active session exists, displays a "Start New Cook" button that navigates to the setup screen.

### 2. Start Cook Screen (Session Setup)
A simple form interface for creating new cooking sessions using Ignite's built-in form components. Users can set the protein type, target grill temperature, and probe temperatures through iOS-native pickers and input fields. The screen includes a "Start Cook" button that calls the Sessions API to create a new session and navigates back to the Active Cook screen.

### 3. Cook History Screen (Past Sessions)
A straightforward list view showing previous cooking sessions using Ignite's list components. Each row displays the session date, protein type, cook duration, and final temperatures. Users can tap on any session to view basic details in a simple modal or navigate to a detail view with essential session information.

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
const SessionModel = types.model("Session", {
  id: types.string,
  smokerId: types.string,
  targetTemp: types.number,
  currentTemp: types.maybeNull(types.number),
  startTime: types.Date,
  endTime: types.maybeNull(types.Date),
  status: types.enumeration(["active", "completed"]),
})

export const SessionStoreModel = types
  .model("SessionStore", {
    sessions: types.array(SessionModel),
    isLoading: false,
    error: types.maybeNull(types.string),
  })
  .actions((self) => ({
    loadSessions: flow(function* () {
      self.isLoading = true
      try {
        const sessions = yield api.getSessions(Config.SMOKER_ID)
        self.sessions.replace(sessions)
        self.error = null
      } catch (error) {
        self.error = error.message
      } finally {
        self.isLoading = false
      }
    }),
    startSession: flow(function* (sessionData) {
      try {
        const newSession = yield api.createSession(sessionData)
        self.sessions.push(newSession)
      } catch (error) {
        self.error = error.message
      }
    }),
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

**Afternoon (3-4 hours):**
- Set up basic navigation in `app/navigators/AppNavigator.ts`
- Configure environment variables for API endpoint
- Create basic layouts for each screen using Ignite components
- Test navigation flow between screens

### Day 2: API & State (Integration)
**Morning (3-4 hours):**
```bash
npx ignite-cli generate model Session
```
- Implement SessionStore with basic CRUD operations
- Configure Apisauce for Sessions API
- Add API calls to MST actions

**Afternoon (3-4 hours):**
- Wire up screens to SessionStore
- Add loading states and error handling
- Implement manual refresh functionality
- Test with production API endpoints

### Day 3: Testing & Polish
**Morning (2-3 hours):**
- Test complete user flows with test device
- Add basic error messages and loading indicators
- Polish UI using Ignite's theme system

**Afternoon (2-3 hours):**
- Fix any bugs found during testing
- Add basic form validation
- Prepare for deployment (iOS build configuration)
- Document MVP features and known limitations

## Screen Implementation Details

### Active Cook Screen
```typescript
// app/screens/ActiveCookScreen.tsx
export const ActiveCookScreen: FC<ActiveCookScreenProps> = observer(function ActiveCookScreen() {
  const { sessionStore } = useStores()
  const activeSession = sessionStore.activeSession

  const refreshData = () => {
    sessionStore.loadSessions()
  }

  return (
    <Screen preset="scroll" safeAreaEdges={["top"]}>
      <Text preset="heading">Current Cook</Text>
      
      {activeSession ? (
        <View>
          <Text preset="subheading">Grill Temperature: {activeSession.currentTemp}°F</Text>
          <Text>Target: {activeSession.targetTemp}°F</Text>
          <Button text="Refresh" onPress={refreshData} />
          <Button text="End Cook" onPress={() => sessionStore.endSession()} />
        </View>
      ) : (
        <View>
          <Text>No active cook session</Text>
          <Button text="Start New Cook" onPress={() => navigation.navigate("StartCook")} />
        </View>
      )}
      
      <Button text="View History" onPress={() => navigation.navigate("CookHistory")} />
    </Screen>
  )
})
```

### Start Cook Screen
```typescript
// app/screens/StartCookScreen.tsx
export const StartCookScreen: FC<StartCookScreenProps> = observer(function StartCookScreen() {
  const { sessionStore } = useStores()
  const [targetTemp, setTargetTemp] = useState("225")
  const [proteinType, setProteinType] = useState("Brisket")

  const handleStartCook = async () => {
    await sessionStore.startSession({
      targetTemp: parseInt(targetTemp),
      proteinType,
      smokerId: Config.SMOKER_ID,
    })
    navigation.navigate("ActiveCook")
  }

  return (
    <Screen preset="scroll" safeAreaEdges={["top"]}>
      <Text preset="heading">Start New Cook</Text>
      
      <TextField
        label="Target Temperature (°F)"
        value={targetTemp}
        onChangeText={setTargetTemp}
        keyboardType="numeric"
      />
      
      <TextField
        label="Protein Type"
        value={proteinType}
        onChangeText={setProteinType}
      />
      
      <Button text="Start Cook" onPress={handleStartCook} />
      <Button text="Cancel" preset="reversed" onPress={() => navigation.goBack()} />
    </Screen>
  )
})
```

## Testing Strategy (Minimal for MVP)

### Manual Testing Checklist
- [ ] Start new cook session
- [ ] View active cook status
- [ ] Refresh temperature data
- [ ] End active cook session
- [ ] View cook history list
- [ ] Handle API errors gracefully

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
- ✅ Can create new cooking session via API
- ✅ Can view current session status
- ✅ Can manually refresh data from API
- ✅ Can end active cooking session
- ✅ Can view list of previous sessions
- ✅ Handles basic API errors gracefully

### Technical Requirements
- ✅ Uses Ignite's default stack (MST + Apisauce)
- ✅ iOS builds successfully
- ✅ Integrates with production Sessions API
- ✅ Works with test device from iot-edge
- ✅ Basic error handling and loading states

## Post-MVP Enhancement Path

### Week 2 Additions
- Pull-to-refresh for automatic data updates
- Better error handling with retry logic
- Enhanced UI polish and animations
- Settings screen for configuration

### Future Phases
- Replace MST with TanStack Query for better API state management
- Add real-time temperature updates
- Implement push notifications
- Add temperature charts and analytics
- Integrate with device management
- Add user authentication
- Nx monorepo integration

## Why This Approach Works

### Leverages Ignite Strengths
- **Battle-tested patterns** proven across hundreds of apps
- **CLI generators** for rapid screen/model creation
- **Pre-configured tooling** eliminates setup decisions
- **Consistent architecture** from day one

### Focuses on Core Value
- **Rapid MVP delivery** in 3 days instead of 3 weeks
- **Production API integration** validates core functionality
- **Real user testing** with actual IoT device
- **Iterative enhancement** path for future features

### Minimizes Risk
- **Proven technology stack** reduces unknowns
- **Simple feature set** limits scope creep
- **Manual refresh** eliminates real-time complexity
- **Single platform** (iOS) reduces testing surface

This streamlined approach prioritizes getting a working app in users' hands quickly while maintaining a solid foundation for future enhancements.