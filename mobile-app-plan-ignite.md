# MeatGeek React Native Mobile App - Ignite Framework Implementation Plan

## Project Overview

This document outlines the comprehensive plan for developing a React Native mobile application for the MeatGeek BBQ/grilling IoT platform using **InfiniteRed's Ignite framework** as the foundation. The app will be integrated into the existing Nx monorepo and initially target iOS/iPad devices, with a focus on Sessions API interaction for cook management.

### MVP Scope vs Future Iterations

**MVP Focus (Weeks 1-7):**
- Core CRUD operations for cooking sessions
- Manual refresh for temperature updates
- View historical cook data
- Beautiful, responsive UI with Ignite's component system
- Offline viewing of cached data

**Post-MVP Enhancements:**
- Real-time temperature updates via WebSocket/polling
- Push notifications for temperature alerts
- Background monitoring and auto-refresh
- Device pairing and management
- Advanced analytics and recommendations

## Executive Summary

The MeatGeek mobile app MVP will serve as the primary interface for BBQ enthusiasts to manage their cooking sessions, monitor temperatures via manual refresh, and maintain a history of their grilling experiences. Built with **InfiniteRed's Ignite framework** for accelerated development and battle-tested architecture, the app emphasizes elegant design following Apple's Human Interface Guidelines, taking inspiration from award-winning applications like Robinhood and Overcast. The MVP prioritizes core functionality and user experience over real-time features, which will be added in subsequent iterations.

### Ignite Framework Philosophy

This plan leverages **Ignite's 9 years of continuous development** and battle-tested architecture to save 2-4 weeks of initial project setup time. Ignite provides an opinionated but flexible foundation with pre-configured libraries, generators for rapid development, and MobX-State-Tree for robust state management. This approach eliminates boilerplate overhead while ensuring every component serves a specific purpose in a proven architecture.

## Project Structure (Ignite-based)

```
mobile-app/
├── app/
│   ├── components/       # Reusable UI components (with generators)
│   ├── screens/          # Main application screens (with generators)
│   ├── models/           # MobX-State-Tree models (with generators)
│   ├── services/         # API services and external integrations
│   ├── i18n/             # Internationalization (built-in)
│   ├── theme/            # Design system and theming (Ignite system)
│   ├── navigators/       # React Navigation setup
│   └── utils/            # Helper functions and utilities
├── ios/                  # iOS native code
├── ignite/               # Ignite CLI templates and configurations
├── __tests__/            # Unit and integration tests (Jest + Maestro)
├── fastlane/             # iOS deployment automation
└── project.json          # Nx project configuration
```

## Essential Dependencies (Included with Ignite)

### Production Dependencies (Pre-configured)
- **react & react-native**: Core framework (0.79+)
- **mobx & mobx-state-tree**: State management (Ignite's preferred solution)
- **mobx-react-lite**: React bindings for MobX
- **@react-navigation/native + stack + bottom-tabs**: Navigation
- **apisauce**: HTTP client built on Axios (Ignite's preferred API client)
- **react-native-mmkv**: Fast storage solution (replaces AsyncStorage)
- **react-native-keychain**: Secure token storage
- **react-native-gesture-handler**: Touch interactions
- **react-native-safe-area-context**: iOS safe areas
- **react-native-screens**: Native screen optimization
- **react-native-reanimated**: Advanced animations
- **date-fns**: Date manipulation library

### Development Dependencies (Pre-configured)
- **TypeScript**: Type safety (full setup)
- **Reactotron**: Advanced debugging and state inspection
- **ESLint + Prettier**: Code quality (Ignite configuration)
- **Jest**: Unit testing framework
- **Maestro**: E2E testing (mobile-first)
- **Flipper**: Built-in debugging

### Ignite-Specific Tools
- **Ignite CLI**: Generators for screens, components, and models
- **Component Library**: Pre-built, customizable UI components
- **Theming System**: Built-in light/dark mode support

## MobX-State-Tree Implementation Strategy

### State Architecture
```typescript
// app/models/RootStore.ts
import { Instance, SnapshotOut, types } from "mobx-state-tree"
import { SessionStoreModel } from "./SessionStore"
import { AuthenticationStoreModel } from "./AuthenticationStore"
import { DeviceStoreModel } from "./DeviceStore"

export const RootStoreModel = types.model("RootStore", {
  sessionStore: types.optional(SessionStoreModel, {}),
  authenticationStore: types.optional(AuthenticationStoreModel, {}),
  deviceStore: types.optional(DeviceStoreModel, {}),
})

export interface RootStore extends Instance<typeof RootStoreModel> {}
export interface RootStoreSnapshot extends SnapshotOut<typeof RootStoreModel> {}
```

### Session Management Model
```typescript
// app/models/SessionStore.ts
import { Instance, SnapshotOut, types, flow } from "mobx-state-tree"
import { api } from "../services/api"

const SessionModel = types.model("Session", {
  id: types.string,
  smokerId: types.string,
  targetTemp: types.number,
  currentTemp: types.maybe(types.number),
  startTime: types.Date,
  endTime: types.maybe(types.Date),
  status: types.enumeration(["active", "completed", "paused"]),
})

export const SessionStoreModel = types
  .model("SessionStore", {
    sessions: types.array(SessionModel),
    activeSession: types.maybe(types.reference(SessionModel)),
  })
  .actions((self) => ({
    loadSessions: flow(function* () {
      try {
        const sessions = yield api.getSessions()
        self.sessions.replace(sessions)
      } catch (error) {
        console.error("Failed to load sessions:", error)
      }
    }),
    startSession: flow(function* (sessionData) {
      try {
        const newSession = yield api.createSession(sessionData)
        self.sessions.push(newSession)
        self.activeSession = newSession.id
      } catch (error) {
        console.error("Failed to start session:", error)
      }
    }),
  }))
  .views((self) => ({
    get activeSessions() {
      return self.sessions.filter(session => session.status === "active")
    },
    get completedSessions() {
      return self.sessions.filter(session => session.status === "completed")
    },
  }))
```

### Benefits for MeatGeek
- **Reactivity**: Automatic UI updates when data changes
- **Serialization**: Built-in persistence for offline capabilities
- **Time Travel**: Debug state changes with Reactotron
- **References**: Efficient linking between related data
- **Validation**: Runtime type checking and validation
- **Optimistic Updates**: Smooth UX with rollback capabilities

## UI Component System (Ignite Approach)

### Theme Configuration
```typescript
// app/theme/colors.ts
export const colors = {
  palette: {
    // BBQ-specific colors
    grillHot: '#E63946',
    grillWarm: '#F77F00',
    grillOptimal: '#06A77D',
    grillCool: '#457B9D',
    smokeGray: '#8D99AE',
    
    // Standard palette
    neutral100: '#FFFFFF',
    neutral200: '#F4F2F1',
    neutral300: '#D7CEC9',
    // ... rest of palette
  },
  transparent: 'rgba(0, 0, 0, 0)',
  
  // Semantic colors
  text: '#1D1D1D',
  textDim: '#686868',
  background: '#F4F2F1',
  error: '#C5292A',
  errorBackground: '#FCEAEA',
}
```

### Custom Components
```typescript
// app/components/TemperatureGauge.tsx
import { observer } from "mobx-react-lite"
import React from "react"
import { View, Text } from "react-native"
import { colors, spacing, typography } from "../theme"

interface TemperatureGaugeProps {
  temperature: number
  targetTemperature: number
  unit?: "F" | "C"
}

export const TemperatureGauge = observer(function TemperatureGauge(props: TemperatureGaugeProps) {
  const { temperature, targetTemperature, unit = "F" } = props
  
  const getTemperatureColor = () => {
    const diff = Math.abs(temperature - targetTemperature)
    if (diff <= 5) return colors.palette.grillOptimal
    if (diff <= 15) return colors.palette.grillWarm
    return colors.palette.grillHot
  }

  return (
    <View style={$container}>
      <Text style={[$temperatureText, { color: getTemperatureColor() }]}>
        {temperature}°{unit}
      </Text>
      <Text style={$targetText}>
        Target: {targetTemperature}°{unit}
      </Text>
    </View>
  )
})

const $container = {
  alignItems: 'center' as const,
  justifyContent: 'center' as const,
  padding: spacing.medium,
}

const $temperatureText = {
  ...typography.xxl,
  fontWeight: '800' as const,
}

const $targetText = {
  ...typography.sm,
  color: colors.textDim,
}
```

### Benefits of Ignite's Component System
- **Consistency**: Pre-built design system with consistent spacing, typography, and colors
- **Customization**: Easy to override and extend default components
- **Performance**: Optimized React Native components with proper memo usage
- **Accessibility**: Built-in accessibility support following platform guidelines
- **Dark Mode**: Automatic dark mode support with theme switching

## Screen Architecture & User Experience

### Dashboard Screen (Home)
Built using Ignite's screen generator: `npx ignite-cli generate screen Dashboard`

The app's main landing screen serves as the central hub for cooking session management. Features a clean, card-based layout showcasing the current cook status with large, readable temperature displays using Ignite's typography system. The screen includes quick action buttons for starting new cooks, accessing device controls, and viewing recent session summaries. Visual indicators provide at-a-glance status for connected devices, active sessions, and system health using Ignite's color palette.

### Active Cook Screen (Real-time Monitoring)
Generated with: `npx ignite-cli generate screen ActiveCook`

An immersive, full-screen interface designed for extended monitoring during active cooking sessions. The screen prominently displays live temperature readings for grill and up to 4 probes using large, color-coded gauges with Reanimated animations. Interactive charts show temperature trends over time with gesture handling. Session controls allow users to adjust target temperatures and set timers through MobX-State-Tree actions.

### Start Cook Screen (Session Creation)
Generated with: `npx ignite-cli generate screen StartCook`

A streamlined, wizard-style interface that guides users through creating new cooking sessions. The screen utilizes Ignite's form components and validation patterns. Device discovery uses MobX-State-Tree async actions, and the interface provides smart defaults and preset configurations stored in MST models.

### Cook History Screen (Past Sessions)
Generated with: `npx ignite-cli generate screen CookHistory`

A comprehensive archive using Ignite's list patterns and search components. MobX-State-Tree provides efficient filtering and sorting of session data with reactive updates. The interface supports iOS-standard gestures and lazy loading for performance.

### Session Details Screen (Deep Analysis)
Generated with: `npx ignite-cli generate screen SessionDetails`

In-depth analysis using Ignite's chart components and data visualization patterns. MobX-State-Tree manages complex session data relationships and provides computed views for analytics.

## Architecture & Technology Stack

### Core Framework
- **React Native 0.79+** with **TypeScript 5** for type-safe mobile development
- **Ignite CLI** for rapid project setup and code generation
- **Nx Monorepo Integration** using `@nx/react-native` plugin for seamless build orchestration

### State Management & Data Flow
- **MobX-State-Tree** for comprehensive state management with built-in serialization, validation, and async actions
- **Reactotron-MST** for advanced debugging and state inspection
- **MMKV** for high-performance persistent storage

### UI/UX Framework
- **Ignite Component Library** with customizable design system
- **React Navigation 7** for iOS-native navigation patterns
- **React Native Reanimated 3** for smooth animations
- **Custom Theme** extending Ignite's base with BBQ-specific design tokens

### API Integration
- **Apisauce** (enhanced Axios) for robust HTTP client with built-in error handling
- **MST Async Actions** for seamless API integration with automatic loading states

## Development Phases & Timeline (Ignite Accelerated)

### Phase 1: Ignite Setup & Configuration (Week 1)
- Initialize new Ignite project: `npx ignite-cli@latest new MeatGeekMobile`
- Integrate with Nx monorepo structure
- Configure custom theme with BBQ-specific colors and typography
- Set up CI/CD integration with existing GitHub Actions

### Phase 2: Models & API Integration (Week 2)
- Generate MST models: `npx ignite-cli generate model Session Device User`
- Implement Sessions API client using Apisauce
- Configure API integration with MST async actions
- Set up development mock data for testing

### Phase 3: Core Screens Generation (Week 3)
- Generate all main screens using Ignite CLI
- Implement navigation flow with React Navigation
- Create custom components for BBQ-specific needs (temperature gauges, timers)
- Configure responsive layouts for iPhone/iPad

### Phase 4: State Management & Data Flow (Week 4)
- Complete MST store implementation with all business logic
- Implement offline persistence with MMKV
- Add optimistic updates and error handling
- Configure Reactotron for debugging

### Phase 5: UI Polish & Animations (Week 5)
- Implement smooth animations with Reanimated
- Add pull-to-refresh and loading states
- Polish custom components and interactions
- Implement dark mode support

### Phase 6: Testing & Optimization (Week 6)
- Write unit tests for MST models and components
- Implement E2E tests with Maestro
- Performance optimization and memory management
- Accessibility improvements

### Phase 7: Deployment & Release (Week 7)
- FastLane configuration for iOS deployment
- TestFlight beta distribution
- Production build optimization
- App Store preparation

## API Integration Strategy

### Sessions API Architecture
The mobile app will interact with the production Sessions API using Apisauce, Ignite's preferred HTTP client. MST async actions will handle API calls with automatic retry logic and error handling.

```typescript
// app/services/api/api.ts
import { ApisauceInstance, create } from "apisauce"
import Config from "../config"

export class Api {
  apisauce: ApisauceInstance

  constructor() {
    this.apisauce = create({
      baseURL: Config.API_URL,
      timeout: 10000,
      headers: {
        "Accept": "application/json",
      },
    })
  }

  async getSessions(smokerId: string) {
    const response = await this.apisauce.get(`/api/sessions/${smokerId}`)
    if (!response.ok) {
      throw new Error(`API Error: ${response.problem}`)
    }
    return response.data
  }

  async createSession(sessionData: CreateSessionRequest) {
    const response = await this.apisauce.post("/api/sessions", sessionData)
    if (!response.ok) {
      throw new Error(`API Error: ${response.problem}`)
    }
    return response.data
  }
}

export const api = new Api()
```

### Real-time Data Strategy
MST async actions combined with background timers for polling. MobX reactions will automatically update UI components when data changes.

### Offline Capability
MST's built-in serialization with MMKV provides automatic offline storage. When network connectivity is restored, MST actions will sync data automatically.

## Nx Monorepo Integration

### Project Structure
The mobile app will be created using `npx ignite-cli@latest new MeatGeekMobile` and then integrated into the Nx workspace with shared libraries for TypeScript interfaces and API models.

### Shared Libraries Strategy
Extract common TypeScript interfaces into shared Nx libraries, enabling code reuse between the mobile app and existing services while maintaining Ignite's project structure.

### Build Optimization
Nx's intelligent caching will optimize builds while maintaining Ignite's development workflow and CLI generators.

## State Management Best Practices (MST)

### Clear Domain Separation
- **SessionStore**: All cooking session data and operations
- **AuthenticationStore**: User authentication and tokens
- **DeviceStore**: Connected device management
- **UIStore**: Application UI state and preferences

### MST Benefits Over React Query + Context
- **Automatic Serialization**: Built-in persistence without additional setup
- **Reactive Updates**: Automatic UI updates with observer pattern
- **Time Travel Debugging**: Full state history with Reactotron
- **Optimistic Updates**: Built-in rollback capabilities
- **Type Safety**: Runtime validation with TypeScript integration
- **References**: Efficient data relationships and computed values

### Implementation Guidelines
- Use MST actions for all state modifications
- Leverage computed views for derived data
- Implement async flows for API operations
- Use references for data relationships
- Configure snapshot serialization for persistence

## Testing Strategy (Ignite Enhanced)

### Unit Testing (Jest)
Pre-configured Jest setup with MST testing utilities. Test models, actions, and computed views in isolation.

```typescript
// app/models/SessionStore.test.ts
import { SessionStoreModel } from "./SessionStore"

describe("SessionStore", () => {
  it("should create sessions correctly", () => {
    const store = SessionStoreModel.create({})
    store.addSession({
      id: "test",
      smokerId: "smoker-1",
      targetTemp: 225,
      status: "active"
    })
    
    expect(store.sessions.length).toBe(1)
    expect(store.activeSessions.length).toBe(1)
  })
})
```

### E2E Testing (Maestro)
Mobile-first E2E testing with Maestro, pre-configured with Ignite. Test complete user flows on real devices.

### Component Testing
React Native Testing Library integration for component testing with MST mock stores.

## Security & Performance

### API Security
Apisauce configuration with certificate pinning and automatic token refresh using MST authentication store.

### Performance Optimization
- MST computed values for efficient derived data
- React Native Reanimated for 60fps animations
- MMKV for fast storage operations
- Optimized list rendering with MST observable arrays

## Success Metrics & KPIs

### Technical Performance
- App launch time under 2 seconds (improved with MST hydration)
- 60fps animation performance with Reanimated
- API response handling with automatic loading states
- Battery optimization for extended cooking sessions

### Development Efficiency
- **2-4 weeks saved** with Ignite foundation
- Rapid feature development with CLI generators
- Consistent code patterns and architecture
- Automated testing and deployment

### User Experience Goals
- Intuitive interface requiring minimal onboarding
- Reliable offline functionality with MST persistence
- Smooth, responsive UI with consistent design system
- Apple App Store rating target of 4.5+ stars

## Future Enhancement Roadmap

### Ignite Ecosystem Benefits
- **Component Library Updates**: Benefit from Ignite community components
- **Generator Enhancements**: Custom generators for MeatGeek patterns
- **Plugin Ecosystem**: Leverage Ignite plugins for additional functionality
- **Community Support**: Access to Infinite Red's expertise and community

### Advanced Features
- Real-time WebSocket integration with MST reactions
- Advanced analytics with MST computed views
- Social features with shared MST models
- Enterprise multi-user support

## Why Ignite Over Custom Setup

### Proven Architecture
- **9 years of continuous development** by React Native experts
- **Battle-tested** across hundreds of production apps
- **Active maintenance** with regular updates and improvements

### Development Velocity
- **Saves 2-4 weeks** of initial setup time
- **CLI generators** for rapid feature development
- **Pre-configured tooling** eliminates decision fatigue
- **Best practices** built-in from day one

### Long-term Maintainability
- **Consistent patterns** across the entire codebase
- **Community support** and extensive documentation
- **Professional backing** by Infinite Red consultancy
- **Upgrade path** with framework evolution

---

This comprehensive Ignite-based plan provides a faster, more maintainable foundation for building the MeatGeek mobile application while leveraging battle-tested patterns and tools from the React Native community.